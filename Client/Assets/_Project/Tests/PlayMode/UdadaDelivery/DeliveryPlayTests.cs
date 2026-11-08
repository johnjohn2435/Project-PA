using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PocketArcade.Core.Save;
using PocketArcade.Meta;
using PocketArcade.UI;
using PocketArcade.UdadaDelivery;
using PocketArcade.UdadaDelivery.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace PocketArcade.Tests.UdadaDeliveryPlay
{
    public sealed class DeliveryPlayTests
    {
        private ResourceManager resources;private UIManager ui;private DataManager data;private GameObject canvas;
        private DeliveryHud hud;private GameCatalog catalog;private Store store;
        private sealed class Store:ISaveRepository
        {
            public SaveData Data=new SaveData();public int Writes;public bool Fail;
            public SaveData Load()=>Data.Copy();public void Save(SaveData value){if(Fail)throw new System.IO.IOException();Data=value.Copy();Writes++;}
        }
        private static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
        private static RectTransform Rect(string name,Transform parent)
        {var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        [UnitySetUp] public IEnumerator Setup()
        {
            L10n.Initialize(Resources.Load<TextAsset>("Data/StringTable").text);
            var character=ScriptableObject.CreateInstance<CharacterDefinition>();Field(character,"id","char_cat");
            var game=ScriptableObject.CreateInstance<MiniGameDefinition>();Field(game,"id",DeliverySession.GameId);Field(game,"scoreKey",DeliverySession.ScoreId);
            catalog=ScriptableObject.CreateInstance<GameCatalog>();Field(catalog,"characters",new[]{character});Field(catalog,"miniGames",new[]{game});
            store=new Store();data=new GameObject("TestData").AddComponent<DataManager>();data.Initialize(catalog,store);
            resources=new GameObject("TestResources").AddComponent<ResourceManager>();resources.Initialize();
            yield return resources.Preload(new[]{new BootResourceSlot{path=BootResourceManifest.CharacterPrefab("char_cat"),kind="Prefab",required=true},new BootResourceSlot{path=DeliveryHud.WorldPath,kind="Prefab",required=true},new BootResourceSlot{path=DeliveryHud.ScreenKey,kind="Prefab",required=true},new BootResourceSlot{path=DeliveryHud.DialogKey,kind="Prefab",required=true}});
            Assert.That(resources.PreloadError,Is.Null);
            ui=new GameObject("TestUI").AddComponent<UIManager>();ui.Initialize();
            canvas=new GameObject("TestCanvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=canvas.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1080,1920);
            var live=Rect("Live",canvas.transform);var inactive=Rect("Inactive",canvas.transform);inactive.gameObject.SetActive(false);
            var task=ui.Mount(live,inactive,DeliveryHud.ScreenKey,DeliveryHud.ScreenKey);while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();
            hud=(DeliveryHud)ui.Find(DeliveryHud.ScreenKey);hud.SendMessage("OnApplicationFocus",true);hud.SendMessage("OnApplicationPause",false);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ui!=null){ui.Shutdown();Object.Destroy(ui.gameObject);}if(resources!=null){resources.Shutdown();Object.Destroy(resources.gameObject);}if(data!=null){data.Shutdown();Object.Destroy(data.gameObject);}
            if(catalog!=null){foreach(var c in catalog.Characters)Object.Destroy(c);foreach(var g in catalog.MiniGames)Object.Destroy(g);Object.Destroy(catalog);}if(canvas!=null)Object.Destroy(canvas);
            yield return null;L10n.Reset();
        }
        [UnityTest] public IEnumerator ReadyPauseBackAndRestartUseCachedWindows()
        {
            Assert.That(hud.Session.State,Is.EqualTo(DeliveryState.Ready));Assert.That(Object.FindObjectsByType<DeliveryWorldView>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            hud.StartRun();yield return null;hud.Pause();yield return null;yield return null;
            var popup=ui.Find(DeliveryHud.DialogKey);Assert.That(popup.IsOpen,Is.True);double elapsed=hud.Session.Elapsed;
            yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(elapsed));
            var task=ui.Back();while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();Assert.That(popup.IsOpen,Is.False);
            hud.Pause();yield return null;yield return null;Assert.That(ui.Find(DeliveryHud.DialogKey),Is.SameAs(popup));
        }
        [UnityTest] public IEnumerator SelectedRigRunsInDeliveryWorld()
        {
            var world = Object.FindFirstObjectByType<DeliveryWorldView>();
            var rig = world.GetComponentInChildren<PocketArcade.Characters.CharacterRigController>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(3));
            var animator = rig.GetComponent<Animator>();
            Assert.That(animator.GetFloat("Speed"), Is.Zero);
            hud.StartRun();
            for (int frame = 0; frame < 60 && animator.GetFloat("Speed") == 0; frame++)
                yield return null;
            Assert.That(animator.GetFloat("Speed"), Is.GreaterThan(0));
            hud.Pause();
            Assert.That(animator.speed, Is.Zero);
        }
        private IEnumerator Finish()
        {
            hud.StartRun();
            for(int house=0;house<3;house++)
            {
                int guard=0;
                while(hud.Session.PredictedLanding<hud.Session.HouseAt(house)&&guard++<10000)hud.Session.Tick(1.0/120,false);
                hud.Session.Tick(1.0/120,true);
                while(hud.Session.ParcelInFlight&&guard++<12000)hud.Session.Tick(1.0/120,true);
            }
            Assert.That(hud.Session.Score,Is.EqualTo(1050));
            yield return null;yield return null;yield return null;
            Assert.That(ui.Find(DeliveryHud.DialogKey).IsOpen,Is.True);
        }
        [UnityTest] public IEnumerator ResultPaysOnceAndRestartCreatesFreshSession()
        {
            yield return Finish();Assert.That(hud.IsSaved,Is.True);int writes=store.Writes;int coins=data.Coins;Assert.That(coins,Is.EqualTo(10));
            hud.TrySave();hud.TrySave();Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(data.Coins,Is.EqualTo(coins));
            var previous=hud.Session;var popup=ui.Find(DeliveryHud.DialogKey);
            popup.transform.Find("Card/Primary").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.That(hud.Session,Is.Not.SameAs(previous));Assert.That(hud.Session.State,Is.EqualTo(DeliveryState.Running));Assert.That(popup.IsOpen,Is.False);
        }
        [UnityTest] public IEnumerator FailedSaveKeepsResultAndBackCannotDiscardIt()
        {
            store.Fail=true;yield return Finish();Assert.That(hud.IsSaved,Is.False);int coins=data.Coins;
            var task=ui.Back();while(!task.IsCompleted)yield return null;task.GetAwaiter().GetResult();Assert.That(ui.Find(DeliveryHud.DialogKey).IsOpen,Is.True);Assert.That(data.Coins,Is.EqualTo(coins));
            store.Fail=false;Assert.That(hud.TrySave(),Is.True);int writes=store.Writes;Assert.That(hud.TrySave(),Is.True);Assert.That(store.Writes,Is.EqualTo(writes));
        }
        [UnityTest] public IEnumerator FocusLossPausesUntilExplicitResume()
        {
            hud.StartRun();yield return null;hud.SendMessage("OnApplicationFocus",false);yield return null;double time=hud.Session.Elapsed;
            hud.SendMessage("OnApplicationFocus",true);yield return null;Assert.That(hud.Session.Elapsed,Is.EqualTo(time));Assert.That(ui.Find(DeliveryHud.DialogKey).IsOpen,Is.True);
        }
        [UnityTest] public IEnumerator BrakePointerKeepsOwnerAndCancelsOnPause()
        {
            var e=new GameObject("Events",typeof(UnityEngine.EventSystems.EventSystem));
            try
            {
                var brake=hud.GetComponentInChildren<DeliveryBrakeInput>(true);
                var first=new UnityEngine.EventSystems.PointerEventData(e.GetComponent<UnityEngine.EventSystems.EventSystem>()){pointerId=10};
                var second=new UnityEngine.EventSystems.PointerEventData(e.GetComponent<UnityEngine.EventSystems.EventSystem>()){pointerId=11};
                brake.OnPointerDown(first);brake.OnPointerDown(second);brake.OnPointerUp(second);Assert.That(brake.IsBraking,Is.True);
                hud.Pause();yield return null;Assert.That(brake.IsBraking,Is.False);
            }
            finally {Object.Destroy(e);}
        }
        [UnityTest] public IEnumerator AirborneParcelStopsTogetherWithPauseMenu()
        {
            hud.StartRun();hud.Session.Tick(1,false);hud.Session.Tick(1.0/120,true);hud.Pause();yield return null;yield return null;
            Assert.That(hud.Session.ParcelInFlight,Is.True);double parcelX=hud.Session.ParcelX;double elapsed=hud.Session.Elapsed;
            yield return null;yield return null;
            Assert.That(hud.Session.ParcelX,Is.EqualTo(parcelX));Assert.That(hud.Session.Elapsed,Is.EqualTo(elapsed));
        }
    }
}
