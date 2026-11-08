using PocketArcade.UI;
using System.Collections;
using NUnit.Framework;
using PocketArcade.BlockGravity;
using PocketArcade.BlockGravity.Unity;
using PocketArcade.Core.Save;
using PocketArcade.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PocketArcade.Tests.BlockGravityPlayMode
{
    public sealed class GameplayTests
    {
        private ResourceManager resources;private UIManager ui;private GameObject canvasRoot,eventRoot;
        private Transform live;private BlockGravityHud hud;
        private DataManager resultData;
        private SoundManager resultSound;
        private GameCatalog resultCatalog;
        private CharacterDefinition resultCharacter;
        private MiniGameDefinition resultGame;
        private ResultStore resultStore;
        private sealed class ResultStore : ISaveRepository
        {
            public SaveData Data=new SaveData{coins=10};public int Writes;public bool Fail;
            public SaveData Load()=>Data.Copy();
            public void Save(SaveData data){if(Fail)throw new System.IO.IOException();Data=data.Copy();Writes++;}
        }
        private InputDevice simulatedDevice;
        private InputSettings.BackgroundBehavior oldBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        private BlockGravityPresenter Game=>hud.Presenter;
        [UnitySetUp] public IEnumerator Setup()
        {
            oldBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);
            resources=new GameObject("TestResources").AddComponent<ResourceManager>();resources.Initialize();
            yield return resources.Preload(new[]{
                new BootResourceSlot{path=BlockGravityHud.WorldPath,kind="Prefab",required=true},
                new BootResourceSlot{path="UI/Screens/Hub",kind="Prefab",required=true},
                new BootResourceSlot{path="Characters/Shared/PreviewCamera",kind="Prefab",required=true},
                new BootResourceSlot{path="Characters/char_cat/Character",kind="Prefab",required=true},
                new BootResourceSlot{path=BlockGravityHud.ScreenKey,kind="Prefab",required=true},
                new BootResourceSlot{path=BlockGravityHud.PauseKey,kind="Prefab",required=true},
                new BootResourceSlot{path=BlockGravityHud.TutorialKey,kind="Prefab",required=true},
                new BootResourceSlot{path=BlockGravityHud.ResultKey,kind="Prefab",required=true}});
            Assert.That(resources.PreloadError,Is.Null);
            ui=new GameObject("TestUI").AddComponent<UIManager>();ui.Initialize();
            canvasRoot=new GameObject("TestCanvas",typeof(RectTransform),typeof(Canvas));canvasRoot.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasRoot.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            live=Rect("Live",canvasRoot.transform);var staging=Rect("Inactive",canvasRoot.transform);staging.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();var task=ui.Mount(live,staging,BlockGravityHud.ScreenKey,BlockGravityHud.ScreenKey);
            while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();
            hud=(BlockGravityHud)ui.Find(BlockGravityHud.ScreenKey);
            Game.SendMessage("OnApplicationFocus",true);Game.SendMessage("OnApplicationPause",false);
            Canvas.ForceUpdateCanvases();yield return null;
            Assert.That(Game.CanInteract,Is.True);
        }
        private static RectTransform Rect(string name,Transform parent)
        {var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui!=null){ui.Shutdown();Object.Destroy(ui.gameObject);}
            if(resources!=null){resources.Shutdown();Object.Destroy(resources.gameObject);}
            if(resultSound!=null){resultSound.Shutdown();Object.Destroy(resultSound.gameObject);}
            if(resultData!=null){resultData.Shutdown();Object.Destroy(resultData.gameObject);}
            if(resultCatalog!=null)Object.Destroy(resultCatalog);if(resultCharacter!=null)Object.Destroy(resultCharacter);if(resultGame!=null)Object.Destroy(resultGame);
            if(canvasRoot!=null)Object.Destroy(canvasRoot);if(eventRoot!=null)Object.Destroy(eventRoot);
            if(simulatedDevice!=null && simulatedDevice.added)InputSystem.RemoveDevice(simulatedDevice);simulatedDevice=null;
            InputSystem.settings.backgroundBehavior=oldBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
#endif
            yield return null;L10n.Reset();
        }
        private Vector2 Slot(int slot)
        {var tray=Game.View.Layout.Tray;return new Vector2((float)(tray.X+tray.Width*(slot+.5)/3),(float)tray.CenterY);}
        private Vector2 Target(int slot,Cell origin)
        {
            var shape=Game.Session.Tray[slot].Shape;
            var center=Game.View.CellPosition(origin)+new Vector3((shape.Width-1)*.5f,(shape.Height-1)*.5f,0);
            return Game.View.PlayCamera.WorldToScreenPoint(center);
        }
        private IEnumerator Settle()
        {
            float until=Time.realtimeSinceStartup+5;
            while(Game.Session.State==SessionState.AwaitingPresentation && Time.realtimeSinceStartup<until)yield return null;
            Assert.That(Game.Session.State,Is.Not.EqualTo(SessionState.AwaitingPresentation));
        }
        [UnityTest] public IEnumerator DragPlacesPieceAnimatesAndRefreshesHud()
        {
            int size=Game.Session.Tray[0].Shape.Cells.Count;
            Assert.That(Game.Input.Begin(Slot(0),false),Is.True);
            var target=Target(0,new Cell(0,3));Game.Input.Move(target);
            Assert.That(Game.Input.End(target),Is.True);Assert.That(Game.Session.State,Is.EqualTo(SessionState.AwaitingPresentation));
            Assert.That(Game.Input.Begin(Slot(1),false),Is.False);
            yield return Settle();Assert.That(Game.Session.Score,Is.EqualTo(size*10));
            Assert.That(Game.View.VisibleBlockCount,Is.EqualTo(Game.Session.Board.OccupiedCount));Assert.That(Game.CanInteract,Is.True);
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var camera=Game.View.PlayCamera;var canvas=canvasRoot.GetComponent<Canvas>();
                var targetTexture=new RenderTexture(1080,1920,24,RenderTextureFormat.ARGB32);targetTexture.Create();
                camera.targetTexture=targetTexture;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();Game.View.Reflow(1080,1920,new Rect(0,0,1080,1920));hud.ApplyLayout(Game.View.Layout);Game.View.ShowTray(Game.Session.Tray);
                yield return new WaitForSecondsRealtime(.2f);
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=targetTexture});
                var previous=RenderTexture.active;RenderTexture.active=targetTexture;
                var image=new Texture2D(1080,1920,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();
                string file=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Logs/blockgravity-preview.png"));
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));System.IO.File.WriteAllBytes(file,image.EncodeToPNG());
                RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;targetTexture.Release();Object.Destroy(targetTexture);Object.Destroy(image);
                Game.View.Reflow(Screen.width,Screen.height,Screen.safeArea);hud.ApplyLayout(Game.View.Layout);Game.View.ShowTray(Game.Session.Tray);
                yield return null;
            }
        }

        private sealed class NineCellSource : IPieceSource
        {
            private readonly PieceDefinition piece;
            public NineCellSource()
            {
                var source=new ProgressivePieceSource(1);
                for(int i=0;i<200;i++){var next=source.Next();if(next.Shape.Cells.Count==9){piece=next;break;}}
                Assert.That(piece,Is.Not.Null);
            }
            public PieceDefinition Next()=>piece;
        }
        [UnityTest] public IEnumerator NineCellPieceCanBeDraggedSettledAndRestarted()
        {
            var session=new BlockGravitySession(new BlockGravityConfig(),new NineCellSource());
            typeof(BlockGravityPresenter).GetProperty("Session").SetValue(Game,session);
            Game.View.ShowBoard(session.Board);Game.View.ShowTray(session.Tray);
            Assert.That(Game.Input.Begin(Slot(0),false),Is.True);
            var target=Target(0,new Cell(2,4));Game.Input.Move(target);
            Assert.That(Game.Input.End(target),Is.True);yield return Settle();
            Assert.That(Game.Session.Score,Is.EqualTo(90));Assert.That(Game.View.VisibleBlockCount,Is.EqualTo(9));
            var texts=hud.GetComponentsInChildren<TMPro.TMP_Text>(true);
            string expected=L10n.Format("bg.difficulty_status",1,L10n.Text("bg.ready"));
            Assert.That(System.Array.Exists(texts,t=>t.text.Contains(expected)),Is.True);
            Game.Restart();Assert.That(Game.Session.Placements,Is.Zero);
            foreach(var p in Game.Session.Tray)Assert.That(p.Shape.Cells.Count,Is.LessThanOrEqualTo(4));
        }

        [UnityTest] public IEnumerator InvalidReleaseAndCancellationLeaveSessionUntouched()
        {
            Assert.That(Game.Input.Begin(Slot(0),false),Is.True);
            Assert.That(Game.Input.End(Target(0,new Cell(-5,-5))),Is.False);
            Assert.That(Game.Session.Score,Is.Zero);Assert.That(Game.Session.Placements,Is.Zero);
            Assert.That(Game.Input.Begin(Slot(0),true),Is.True);Game.Input.Cancel();
            Assert.That(Game.Input.IsDragging,Is.False);Assert.That(Game.Session.Board.OccupiedCount,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator PausePopupFreezesAnimationAndResumeCompletesIt()
        {
            Assert.That(Game.TryPlace(0,new Cell(0,3)),Is.True);hud.Pause();yield return null;
            Assert.That(Game.CanInteract,Is.False);Assert.That(ui.Find(BlockGravityHud.PauseKey).IsOpen,Is.True);
            yield return new WaitForSecondsRealtime(.3f);Assert.That(Game.Session.State,Is.EqualTo(SessionState.AwaitingPresentation));
            var close=ui.Close(BlockGravityHud.PauseKey);while(!close.IsCompleted)yield return null;close.GetAwaiter().GetResult();
            yield return Settle();Assert.That(Game.CanInteract,Is.True);
        }
        [UnityTest] public IEnumerator FocusLossCancelsDragAndRequiresExplicitResume()
        {
            Assert.That(Game.Input.Begin(Slot(0),false),Is.True);Game.SendMessage("OnApplicationFocus",false);yield return null;
            Assert.That(Game.Input.IsDragging,Is.False);Assert.That(Game.CanInteract,Is.False);
            Game.SendMessage("OnApplicationFocus",true);yield return null;Assert.That(Game.CanInteract,Is.False);
            var close=ui.Close(BlockGravityHud.PauseKey);while(!close.IsCompleted)yield return null;close.GetAwaiter().GetResult();
            Assert.That(Game.CanInteract,Is.True);Assert.That(Game.Session.Score,Is.Zero);
        }
        [UnityTest] public IEnumerator RaycastedUIAndHiddenHudBlockWorldInput()
        {
            eventRoot=new GameObject("Events",typeof(EventSystem));
            var cover=Rect("Cover",canvasRoot.transform);cover.gameObject.AddComponent<Canvas>().overrideSorting=true;cover.GetComponent<Canvas>().sortingOrder=5000;
            cover.gameObject.AddComponent<GraphicRaycaster>();cover.gameObject.AddComponent<Image>();Canvas.ForceUpdateCanvases();
            yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(Game.Input.Begin(Slot(0),false),Is.False);Object.Destroy(cover.gameObject);yield return null;
            Assert.That(Game.Input.Begin(Slot(0),false),Is.True);Game.SetVisible(false);
            Assert.That(Game.Input.IsDragging,Is.False);Assert.That(Game.CanInteract,Is.False);Game.SetVisible(true);
        }
        [UnityTest] public IEnumerator RestartAndUnmountClearWorldAndSubscriptions()
        {
            Game.TryPlace(0,new Cell(0,3));yield return Settle();Game.Restart();
            Assert.That(Game.Session.Score,Is.Zero);Assert.That(Game.Session.Placements,Is.Zero);Assert.That(Game.View.VisibleBlockCount,Is.Zero);
            var old=Game.gameObject;ui.Unmount(live);yield return null;Assert.That(old==null,Is.True);Assert.That(ui.CachedCount,Is.Zero);
        }
        [UnityTest] public IEnumerator FiveTurnsRotateGravityAndRefillTray()
        {
            for(int turn=0;turn<5;turn++)
            {
                bool placed=false;var board=Game.Session.Board;
                for(int slot=0;slot<3&&!placed;slot++)
                {
                    var piece=Game.Session.Tray[slot];if(piece==null)continue;
                    for(int y=0;y<8&&!placed;y++)for(int x=0;x<8&&!placed;x++)
                        if(PlacementValidator.CanPlace(board,piece.Shape,new Cell(x,y)))placed=Game.TryPlace(slot,new Cell(x,y));
                }
                Assert.That(placed,Is.True);yield return Settle();
            }
            Assert.That(Game.Session.Gravity,Is.EqualTo(GravityDirection.Left));Assert.That(Game.Session.RemainingUntilGravityChange,Is.EqualTo(5));
        }
        [UnityTest] public IEnumerator InputSystemMouseEventsDriveTheActualUpdateLoop()
        {
            var mouse=InputSystem.AddDevice<Mouse>();simulatedDevice=mouse;
            var target=Target(0,new Cell(0,3));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=Slot(0)}.WithButton(MouseButton.Left));yield return null;yield return null;
            Assert.That(mouse.leftButton.isPressed,Is.True);
            Assert.That(Game.Input.IsDragging,Is.True);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=target}.WithButton(MouseButton.Left));yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=target});yield return null;
            Assert.That(Game.Input.IsDragging,Is.False);Assert.That(Game.Session.Placements,Is.EqualTo(1));yield return Settle();
        }
        [UnityTest] public IEnumerator InputSystemTouchCancellationDoesNotPromoteSecondFinger()
        {
            var screen=InputSystem.AddDevice<Touchscreen>();simulatedDevice=screen;
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=71,phase=UnityEngine.InputSystem.TouchPhase.Began,position=Slot(0)});yield return null;yield return null;
            Assert.That(screen.primaryTouch.press.isPressed,Is.True);
            Assert.That(Game.Input.IsDragging,Is.True);
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=72,phase=UnityEngine.InputSystem.TouchPhase.Began,position=Slot(1)});yield return null;
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=71,phase=UnityEngine.InputSystem.TouchPhase.Canceled,position=Slot(0)});yield return null;
            Assert.That(Game.Input.IsDragging,Is.False);Assert.That(Game.Session.Placements,Is.Zero);
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=72,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=Target(1,new Cell(0,3))});yield return null;
            Assert.That(Game.Session.Placements,Is.Zero);
        }
        private static void Field(object target,string name,object value)
        {target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(target,value);}
        private void CreateResultData(bool fail=false)
        {
            resultCharacter=ScriptableObject.CreateInstance<CharacterDefinition>();Field(resultCharacter,"id","char_cat");
            resultGame=ScriptableObject.CreateInstance<MiniGameDefinition>();Field(resultGame,"id","block_gravity");
            resultCatalog=ScriptableObject.CreateInstance<GameCatalog>();Field(resultCatalog,"characters",new[]{resultCharacter});Field(resultCatalog,"miniGames",new[]{resultGame});
            resultStore=new ResultStore{Fail=fail};resultData=new GameObject("ResultData").AddComponent<DataManager>();resultData.Initialize(resultCatalog,resultStore);
        }
        private sealed class EndSource : IPieceSource
        {
            private int index;
            public PieceDefinition Next()=>index++%3==0?new PieceDefinition("single",1,new Cell(0,0)):
                new PieceDefinition("wide",1,new Cell(0,0),new Cell(1,0),new Cell(2,0));
        }
        private IEnumerator FinishRun()
        {
            // A small valid session completes deterministically with 1010 points and no remaining legal piece.
            var board=new BoardState(2,2);board[0,0]=1;board[1,0]=2;board[0,1]=3;
            var session=new BlockGravitySession(new BlockGravityConfig(2,2),new EndSource(),board);
            typeof(BlockGravityPresenter).GetProperty("Session").SetValue(Game,session);
            Game.View.ShowBoard(board);Assert.That(Game.TryPlace(0,new Cell(1,1)),Is.True);yield return Settle();
            float timeout=Time.realtimeSinceStartup+3;
            while((ui.Find(BlockGravityHud.ResultKey)==null || !ui.Find(BlockGravityHud.ResultKey).IsOpen) && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(ui.Find(BlockGravityHud.ResultKey).IsOpen,Is.True);Assert.That(Game.Session.Score,Is.EqualTo(1010));
        }
        private static Button NamedButton(Component root,string name)
        {foreach(var button in root.GetComponentsInChildren<Button>(true))if(button.name==name)return button;Assert.Fail(name);return null;}
        [UnityTest] public IEnumerator GameOverSavesOnceReopensSafelyAndRestartsWithNewResultId()
        {
            CreateResultData();yield return FinishRun();
            var popup=(BlockGravityResultPopup)ui.Find(BlockGravityHud.ResultKey);string id=Game.Result.Result.ResultId;
            Assert.That(popup.IsSaved,Is.True);Assert.That(resultData.Coins,Is.EqualTo(14));Assert.That(resultData.HighScore("block_gravity"),Is.EqualTo(1010));
            var back=ui.Back();while(!back.IsCompleted)yield return null;Assert.That(popup.IsOpen,Is.True);
            var close=ui.Close(BlockGravityHud.ResultKey);while(!close.IsCompleted)yield return null;
            Game.ShowResult();yield return null;Assert.That(popup.IsOpen,Is.True);Assert.That(resultStore.Writes,Is.EqualTo(1));
            NamedButton(popup,"Restart").onClick.Invoke();yield return null;
            Assert.That(Game.Result,Is.Null);Assert.That(Game.Session.Score,Is.Zero);Assert.That(Game.CanInteract,Is.True);
            yield return FinishRun();Assert.That(Game.Result.Result.ResultId,Is.Not.EqualTo(id));Assert.That(resultData.Coins,Is.EqualTo(18));Assert.That(resultStore.Writes,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator FailedResultSaveBlocksNavigationAndRetryDoesNotDuplicateCoins()
        {
            CreateResultData(true);yield return FinishRun();
            var popup=(BlockGravityResultPopup)ui.Find(BlockGravityHud.ResultKey);string id=Game.Result.Result.ResultId;
            Assert.That(popup.IsSaved,Is.False);Assert.That(NamedButton(popup,"Restart").interactable,Is.False);Assert.That(NamedButton(popup,"Home").interactable,Is.False);
            NamedButton(popup,"Restart").onClick.Invoke();NamedButton(popup,"Home").onClick.Invoke();Game.Restart();yield return null;
            Assert.That(Game.Result.Result.ResultId,Is.EqualTo(id));Assert.That(Game.Session.State,Is.EqualTo(SessionState.GameOver));Assert.That(resultData.Coins,Is.EqualTo(10));
            resultStore.Fail=false;popup.RetrySave();popup.RetrySave();
            Assert.That(popup.IsSaved,Is.True);Assert.That(resultData.Coins,Is.EqualTo(14));Assert.That(resultStore.Writes,Is.EqualTo(1));Assert.That(resultData.Snapshot.completedResults.Count,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator MissingDataManagerKeepsResultPendingUntilItBecomesAvailable()
        {
            yield return FinishRun();var popup=(BlockGravityResultPopup)ui.Find(BlockGravityHud.ResultKey);
            Assert.That(popup.IsSaved,Is.False);Assert.That(Game.Result.SaveError,Is.Not.Null);
            CreateResultData();popup.RetrySave();Assert.That(popup.IsSaved,Is.True);Assert.That(resultData.Coins,Is.EqualTo(14));
        }
        [UnityTest] public IEnumerator ExitMenuShadeClosesAndCardDoesNotDismiss()
        {
            Assert.That(hud.GetComponentsInChildren<Button>(true).Length,Is.EqualTo(1));
            NamedButton(hud,"Exit").onClick.Invoke();yield return null;
            var popup=ui.Find(BlockGravityHud.PauseKey);Assert.That(popup.IsOpen,Is.True);Assert.That(Game.CanInteract,Is.False);
            Assert.That(popup.Canvas.sortingOrder,Is.GreaterThan(30000));
            eventRoot=new GameObject("Events",typeof(EventSystem));Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(eventRoot.GetComponent<EventSystem>()){position=popup.transform.Find("Card").position};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.transform.IsChildOf(popup.transform.Find("Card")),Is.True);
            pointer.position=new Vector2(2,2);hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.name,Is.EqualTo("Shade"));
            var shade=popup.transform.Find("Shade").GetComponent<Button>();
            Assert.That(popup.transform.Find("Card").GetComponent<Image>().raycastTarget,Is.True);
            Assert.That(shade.transform,Is.Not.EqualTo(popup.transform.Find("Card").parent));
            shade.onClick.Invoke();yield return null;Assert.That(popup.IsOpen,Is.False);Assert.That(Game.CanInteract,Is.True);
        }
        [UnityTest] public IEnumerator AudioIconsToggleIndependentlySaveAndApplyToSoundManager()
        {
            CreateResultData();resultSound=new GameObject("TestSound").AddComponent<SoundManager>();resultSound.Initialize(resultData);
            bool haptics=resultData.Settings.hapticsEnabled;hud.Pause();yield return null;var menu=ui.Find(BlockGravityHud.PauseKey);
            Assert.That(menu.transform.Find("Card/Settings"),Is.Null);
            var music=NamedButton(menu,"Music");var effects=NamedButton(menu,"Effects");
            music.onClick.Invoke();Assert.That(resultData.Settings.bgmEnabled,Is.False);Assert.That(resultData.Settings.sfxEnabled,Is.True);
            Assert.That(resultSound.BgmEnabled,Is.False);Assert.That(resultSound.SfxEnabled,Is.True);
            Assert.That(music.GetComponent<AudioSettingIcon>().IsOn,Is.False);Assert.That(music.transform.Find("Icon/OffSlash").GetComponent<Image>().enabled,Is.True);
            effects.onClick.Invoke();Assert.That(resultData.Settings.sfxEnabled,Is.False);Assert.That(resultSound.SfxEnabled,Is.False);
            Assert.That(resultData.Settings.hapticsEnabled,Is.EqualTo(haptics));Assert.That(resultStore.Writes,Is.EqualTo(2));Assert.That(Game.CanInteract,Is.False);
            NamedButton(menu,"Resume").onClick.Invoke();yield return null;hud.Pause();yield return null;
            Assert.That(music.GetComponent<AudioSettingIcon>().IsOn,Is.False);Assert.That(effects.GetComponent<AudioSettingIcon>().IsOn,Is.False);
            Assert.That(music.transform.Find("State").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo(L10n.Text("bg.settings.off")));
            NamedButton(menu,"Shade").onClick.Invoke();yield return null;Assert.That(Game.CanInteract,Is.True);
        }
        [UnityTest] public IEnumerator AudioIconSaveFailureKeepsSavedStateAndCanRetry()
        {
            CreateResultData();hud.Pause();yield return null;var menu=ui.Find(BlockGravityHud.PauseKey);var music=NamedButton(menu,"Music");
            resultStore.Fail=true;music.onClick.Invoke();
            Assert.That(resultData.Settings.bgmEnabled,Is.True);Assert.That(music.GetComponent<AudioSettingIcon>().IsOn,Is.True);
            Assert.That(music.transform.Find("Icon/OffSlash").GetComponent<Image>().enabled,Is.False);Assert.That(resultStore.Writes,Is.Zero);
            Assert.That(menu.transform.Find("Card/Notice").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo(L10n.Text("bg.settings.save_error")));
            resultStore.Fail=false;music.onClick.Invoke();Assert.That(resultData.Settings.bgmEnabled,Is.False);Assert.That(resultStore.Writes,Is.EqualTo(1));
            Assert.That(menu.transform.Find("Card/Notice").GetComponent<TMPro.TMP_Text>().text,Is.Empty);
        }
        [UnityTest] public IEnumerator AudioIconsAreDisabledWithoutDataAndRefreshWhileOpen()
        {
            hud.Pause();yield return null;var menu=ui.Find(BlockGravityHud.PauseKey);
            Assert.That(NamedButton(menu,"Music").interactable,Is.False);Assert.That(NamedButton(menu,"Effects").interactable,Is.False);
            NamedButton(menu,"Resume").onClick.Invoke();yield return null;CreateResultData();hud.Pause();yield return null;
            var settings=resultData.Settings;settings.bgmEnabled=false;resultData.UpdateSettings(settings);
            Assert.That(NamedButton(menu,"Music").GetComponent<AudioSettingIcon>().IsOn,Is.False);
            Assert.That(NamedButton(menu,"Effects").GetComponent<AudioSettingIcon>().IsOn,Is.True);
        }
        private sealed class ComboSource : IPieceSource
        { public PieceDefinition Next()=>new PieceDefinition("domino",1,new Cell(0,0),new Cell(1,0)); }
        [UnityTest] public IEnumerator ContinuedComboPulsesOnceAndRefreshDoesNotReplayFeedback()
        {
            int pulses=0;Field(Game,"feedback",new ComboFeedback(()=>true,()=>pulses++));
            var session=new BlockGravitySession(new BlockGravityConfig(2,8,100),new ComboSource());
            typeof(BlockGravityPresenter).GetProperty("Session").SetValue(Game,session);
            Assert.That(Game.TryPlace(0,new Cell(0,7)),Is.True);yield return Settle();Assert.That(pulses,Is.Zero);
            Assert.That(Game.TryPlace(1,new Cell(0,7)),Is.True);yield return Settle();Assert.That(pulses,Is.EqualTo(1));
            Game.RefreshHud();Game.RefreshHud();Assert.That(pulses,Is.EqualTo(1));Assert.That(session.Combo,Is.EqualTo(2));
            Assert.That(session.Score,Is.EqualTo(340));
        }
        [UnityTest] public IEnumerator ComboShakeStopsAtPauseAndDoesNotMoveHud()
        {
            var session=new BlockGravitySession(new BlockGravityConfig(2,8,100),new ComboSource());
            typeof(BlockGravityPresenter).GetProperty("Session").SetValue(Game,session);
            Game.TryPlace(0,new Cell(0,7));yield return Settle();
            var rest=Game.View.PlayCamera.transform.position;var hudPosition=hud.transform.position;
            Game.TryPlace(1,new Cell(0,7));float timeout=Time.realtimeSinceStartup+3;
            while(Vector3.Distance(Game.View.PlayCamera.transform.position,rest)<.001f && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(Vector3.Distance(Game.View.PlayCamera.transform.position,rest),Is.GreaterThan(.001f));
            Assert.That(hud.transform.position,Is.EqualTo(hudPosition));hud.Pause();yield return null;
            Assert.That(Game.View.PlayCamera.transform.position,Is.EqualTo(rest));
            yield return new WaitForSecondsRealtime(.25f);Assert.That(Game.View.PlayCamera.transform.position,Is.EqualTo(rest));
            var close=ui.Close(BlockGravityHud.PauseKey);while(!close.IsCompleted)yield return null;yield return Settle();
            Assert.That(Game.View.PlayCamera.transform.position,Is.EqualTo(rest));
        }
        [UnityTest] public IEnumerator TutorialFirstVisitCompletesAndDoesNotAutoReopen()
        {
            CreateResultData();hud.OfferTutorial();yield return null;var tutorial=ui.Find(BlockGravityHud.TutorialKey);
            Assert.That(tutorial.IsOpen,Is.True);Assert.That(Game.CanInteract,Is.False);
            NamedButton(tutorial,"Next").onClick.Invoke();NamedButton(tutorial,"Next").onClick.Invoke();NamedButton(tutorial,"Next").onClick.Invoke();yield return null;
            Assert.That(tutorial.IsOpen,Is.False);Assert.That(resultData.HasCompletedTutorial(BlockGravityTutorialPopup.TutorialId),Is.True);Assert.That(resultStore.Writes,Is.EqualTo(1));
            hud.OfferTutorial();yield return null;Assert.That(tutorial.IsOpen,Is.False);
            hud.Pause();yield return null;NamedButton(ui.Find(BlockGravityHud.PauseKey),"Help").onClick.Invoke();yield return null;
            Assert.That(tutorial.IsOpen,Is.True);NamedButton(tutorial,"Later").onClick.Invoke();yield return null;
            Assert.That(ui.Find(BlockGravityHud.PauseKey).IsOpen,Is.True);Assert.That(Game.CanInteract,Is.False);
        }
        [UnityTest] public IEnumerator TutorialSaveFailureCanBeDeferredWithoutBlockingGame()
        {
            CreateResultData(true);hud.OfferTutorial();yield return null;var tutorial=ui.Find(BlockGravityHud.TutorialKey);
            NamedButton(tutorial,"Next").onClick.Invoke();NamedButton(tutorial,"Next").onClick.Invoke();NamedButton(tutorial,"Next").onClick.Invoke();
            Assert.That(tutorial.IsOpen,Is.True);Assert.That(resultData.HasCompletedTutorial(BlockGravityTutorialPopup.TutorialId),Is.False);
            Assert.That(tutorial.transform.Find("Card/Notice").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo(L10n.Text("bg.tutorial.save_error")));
            NamedButton(tutorial,"Later").onClick.Invoke();yield return null;Assert.That(tutorial.IsOpen,Is.False);Assert.That(Game.CanInteract,Is.True);
            resultStore.Fail=false;hud.OfferTutorial();yield return null;Assert.That(tutorial.IsOpen,Is.True);
        }
        [UnityTest] public IEnumerator GameplaySoundCuesAreRequestedAtPlacementBoundary()
        {
            CreateResultData();resultSound=new GameObject("CueSound").AddComponent<SoundManager>();resultSound.Initialize(resultData);resultSound.UseBootResources(resources);
            var cues=new System.Collections.Generic.List<AudioCue>();resultSound.CueRequested+=cues.Add;
            Assert.That(Game.TryPlace(0,new Cell(0,3)),Is.True);Assert.That(cues[0],Is.EqualTo(AudioCue.Place));yield return Settle();
            int places=cues.FindAll(x=>x==AudioCue.Place).Count;Game.RefreshHud();Assert.That(cues.FindAll(x=>x==AudioCue.Place).Count,Is.EqualTo(places));
        }
        [UnityTest] public IEnumerator SharedRigWalkAndFacingReuseAllSkins()
        {
            foreach(var id in new[]{"char_cat","char_dog","char_hamster","char_poodle"})
            {
                var rig=Object.Instantiate(Resources.Load<GameObject>("Characters/"+id+"/Character"));
                try
                {
                    yield return null;var control=rig.GetComponent<PocketArcade.Characters.CharacterRigController>();
                    control.SetMovement(Vector2.left);Assert.That(rig.transform.localScale.x,Is.LessThan(0));
                    control.SetMovement(Vector2.up);Assert.That(rig.transform.localScale.x,Is.LessThan(0));
                    control.SetMovement(Vector2.right);Assert.That(rig.transform.localScale.x,Is.GreaterThan(0));
                    var animator=rig.GetComponent<Animator>();animator.Play("Walk",0,.25f);animator.Update(0);
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,rig.transform.Find("body/arm_R").localEulerAngles.z)),Is.GreaterThan(1));
                }
                finally{Object.Destroy(rig);}
            }
        }
        [UnityTest] public IEnumerator HiddenHubDoesNotSpawnPreviewAndReleasesItOnHide()
        {
            CreateResultData();
            var template=Resources.Load<GameObject>("UI/Screens/Hub");
            var root=Object.Instantiate(template);root.SetActive(false);
            var view=root.GetComponent<PocketArcade.UI.HubView>();view.enabled=false;
            var catalog=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset"));
            var home=(GameObject)new UnityEditor.SerializedObject(view).FindProperty("homePanel").objectReferenceValue;
            try
            {
                root.SetActive(true);home.SetActive(false);view.Bind(catalog);
                int before=resources.LiveCount;
                view.Present(catalog,"char_cat",0,new PlayerSettingsData(),_=>true);
                Assert.That(resources.LiveCount,Is.EqualTo(before));
                home.SetActive(true);yield return null;
                Assert.That(resources.LiveCount,Is.EqualTo(before+2));
                home.SetActive(false);Assert.That(resources.LiveCount,Is.EqualTo(before));
            }
            finally{Object.Destroy(root);Object.Destroy(catalog);}
        }
    }
}

