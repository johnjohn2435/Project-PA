using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.BlockGravity.Unity;
using PocketArcade.Core;
using PocketArcade.Editor;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.BlockGravity.Editor
{
    public static partial class BlockGravitySetup
    {
        private const string ResourcesRoot = "Assets/_Project/Resources/";
        public const string WorldFile = ResourcesRoot + BlockGravityHud.WorldPath + ".prefab";
        public const string HudFile = ResourcesRoot + BlockGravityHud.ScreenKey + ".prefab";
        public const string PauseFile = ResourcesRoot + BlockGravityHud.PauseKey + ".prefab";
        public const string ResultFile = ResourcesRoot + BlockGravityHud.ResultKey + ".prefab";
        private const string SpriteFile = ResourcesRoot + "MiniGames/BlockGravity/Cell.png";
        private static readonly string GameSceneFile = SceneIds.PathFor(SceneIds.GameScene);
        private static string Text(string key) => StringTableSetup.Text(key);
        private static void Set(Object target, string field, Object value)
        { var data=new SerializedObject(target); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetArray(Object target,string field,SpriteRenderer[] values)
        {
            var data=new SerializedObject(target); var array=data.FindProperty(field); array.arraySize=values.Length;
            for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Save(GameObject root,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
            if(!success)throw new IOException(path);
        }
        private static Sprite EnsureSprite()
        {
            if(!File.Exists(SpriteFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SpriteFile));
                var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);
                try
                {
                    var pixels=new Color32[128*128]; for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,255);
                    texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(SpriteFile,texture.EncodeToPNG());
                }
                finally {Object.DestroyImmediate(texture);}
                AssetDatabase.ImportAsset(SpriteFile,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(SpriteFile);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=128;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFile);
            if(sprite==null)throw new InvalidOperationException(Text("bg.error.references"));
            return sprite;
        }
        private static SpriteRenderer SpriteNode(Transform parent,string name,Sprite sprite,Material material,int order)
        {
            var root=new GameObject(name,typeof(SpriteRenderer));root.transform.SetParent(parent,false);
            var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;return renderer;
        }
        private static SpriteRenderer[] Pool(Transform parent,string prefix,int count,Sprite sprite,Material material,int order)
        {
            var result=new SpriteRenderer[count];
            for(int i=0;i<count;i++){result[i]=SpriteNode(parent,prefix+i,sprite,material,order);result[i].enabled=false;}
            return result;
        }
        private static void CreateWorld()
        {
            if(File.Exists(WorldFile))return;
            var sprite=EnsureSprite();
            var material=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if(material==null)throw new InvalidOperationException(Text("bg.error.references"));
            var root=new GameObject("BlockGravityPlayfield");
            try
            {
                var cameraRoot=new GameObject("PlayCamera",typeof(Camera));cameraRoot.transform.SetParent(root.transform,false);cameraRoot.tag="MainCamera";
                var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,0,-10);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.95f,.95f,.91f);camera.nearClipPlane=.1f;camera.farClipPlane=50;
                var visual=new GameObject("Sprites");visual.transform.SetParent(root.transform,false);
                for(int y=0;y<8;y++)for(int x=0;x<8;x++)
                {
                    var tile=SpriteNode(visual.transform,"Cell_"+x+"_"+y,sprite,material,10);
                    tile.transform.localPosition=new Vector3(x-3.5f,y-3.5f,0);tile.transform.localScale=new Vector3(.97f,.97f,1);
                    tile.color=(x+y)%2==0?new Color(.86f,.87f,.81f):new Color(.89f,.90f,.84f);
                }
                var view=root.AddComponent<BlockGravityBoardView>();Set(view,"playCamera",camera);Set(view,"visualRoot",visual);
                SetArray(view,"blocks",Pool(visual.transform,"Block_",64,sprite,material,20));
                SetArray(view,"trayCells",Pool(visual.transform,"Tray_",3*ProgressivePieceSource.MaxPieceCells,sprite,material,40));
                SetArray(view,"ghostCells",Pool(visual.transform,"Preview_",ProgressivePieceSource.MaxPieceCells,sprite,material,30));
                SetArray(view,"draggedCells",Pool(visual.transform,"Drag_",ProgressivePieceSource.MaxPieceCells,sprite,material,100));
                var drag=root.AddComponent<PieceDragController>();Set(drag,"view",view);
                var presenter=root.AddComponent<BlockGravityPresenter>();Set(presenter,"view",view);Set(presenter,"input",drag);
                Save(root,WorldFile);
            }
            finally {Object.DestroyImmediate(root);}
        }

        [MenuItem("Pocket Arcade/Block Gravity/5. Apply Difficulty Pools")]
        public static void ApplyDifficultyPools()
        {
            string backup = "Logs/BlockGravityBackup/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy(WorldFile, Path.Combine(backup, "Playfield.prefab"));
            var root = PrefabUtility.LoadPrefabContents(WorldFile);
            try
            {
                var data = new SerializedObject(root.GetComponent<BlockGravityBoardView>());
                foreach (string field in new[] { "trayCells", "ghostCells", "draggedCells" })
                {
                    var array = data.FindProperty(field);
                    int count = ProgressivePieceSource.MaxPieceCells * (field == "trayCells" ? 3 : 1);
                    if (array.arraySize >= count) continue;
                    int original = array.arraySize;
                    var template = (SpriteRenderer)array.GetArrayElementAtIndex(0).objectReferenceValue;
                    array.arraySize = count;
                    for (int i = original; i < count; i++)
                    {
                        var clone = Object.Instantiate(template, template.transform.parent, false);
                        clone.name = field + "_" + i;
                        clone.enabled = false;
                        array.GetArrayElementAtIndex(i).objectReferenceValue = clone;
                    }
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                Save(root, WorldFile);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static RectTransform Rect(Transform parent,string name,Vector2 center,Vector2 size)
        {
            var result=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;result.SetParent(parent,false);
            result.anchorMin=result.anchorMax=result.pivot=new Vector2(.5f,.5f);result.sizeDelta=size;
            result.anchorMin=result.anchorMax=center;result.anchoredPosition=Vector2.zero;return result;
        }
        private static void Fill(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static GameObject Window<T>(string name,string key) where T:UIBase
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster));Fill((RectTransform)root.transform);
            var window=root.AddComponent<T>();var data=new SerializedObject(window);
            data.FindProperty("key").stringValue=key;data.FindProperty("placement").enumValueIndex=0;data.FindProperty("orderPolicy").enumValueIndex=0;
            data.ApplyModifiedPropertiesWithoutUndo();root.GetComponent<Canvas>().overrideSorting=true;root.GetComponent<Canvas>().sortingOrder=100;
            return root;
        }
        private static TMP_Text Label(Transform parent,string name,Vector2 center,Vector2 size,int fontSize,string key=null)
        {
            var rect=Rect(parent,name,center,size);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NanumGothic SDF.asset");
            if(label.font==null)throw new InvalidOperationException(Text("bg.error.font"));
            label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.17f,.25f,.20f);label.raycastTarget=false;label.text="";
            if(key!=null){label.text=Text(key);var localized=rect.gameObject.AddComponent<LocalizedText>();localized.SetKey(key);}
            return label;
        }
        private static Button Button(Transform parent,string name,Vector2 center,string key,Vector2 size)
        {
            var rect=Rect(parent,name,center,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.23f,.39f,.31f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var label=Label(rect,"Label",new Vector2(.5f,.5f),size,26,key);Fill((RectTransform)label.transform);label.color=Color.white;return button;
        }
        private static void CreateHud()
        {
            if(File.Exists(HudFile))return;
            var root=Window<BlockGravityHud>("BlockGravityHud",BlockGravityHud.ScreenKey);
            try
            {
                var hud=root.GetComponent<BlockGravityHud>();
                var region=Rect(root.transform,"HUD",new Vector2(.5f,.85f),new Vector2(960,260));
                var background=region.gameObject.AddComponent<Image>();background.color=new Color(.94f,.96f,.91f,.95f);background.raycastTarget=false;
                Set(hud,"hudRegion",region);
                Set(hud,"score",Label(region,"Score",new Vector2(.5f,.85f),new Vector2(440,52),36));
                Set(hud,"best",Label(region,"Best",new Vector2(.5f,.63f),new Vector2(800,42),28));
                Set(hud,"gravity",Label(region,"Gravity",new Vector2(.5f,.42f),new Vector2(920,54),28));
                Set(hud,"status",Label(region,"Status",new Vector2(.43f,.18f),new Vector2(560,44),26));
                Set(hud,"exitButton",Button(region,"Exit",new Vector2(.88f,.86f),"bg.exit",new Vector2(180,56)));
                root.SetActive(false);Save(root,HudFile);
            }
            finally {Object.DestroyImmediate(root);}
        }
        private static void CreatePause()
        {
            if(File.Exists(PauseFile))return;
            var root=Window<BlockGravityPausePopup>("BlockGravityPause",BlockGravityHud.PauseKey);
            try
            {
                var shade=Rect(root.transform,"Shade",new Vector2(.5f,.5f),Vector2.zero);Fill(shade);
                var image=shade.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,.55f);
                var card=Rect(root.transform,"Card",new Vector2(.5f,.5f),new Vector2(760,460));card.gameObject.AddComponent<Image>().color=new Color(.95f,.96f,.91f);
                Label(card,"Title",new Vector2(.5f,.75f),new Vector2(700,90),42,"bg.pause");
                Set(root.GetComponent<BlockGravityPausePopup>(),"resume",Button(card,"Resume",new Vector2(.5f,.34f),"bg.resume",new Vector2(400,100)));
                root.SetActive(false);Save(root,PauseFile);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void Modal(UIBase popup, int order)
        {
            var data = new SerializedObject(popup);
            data.FindProperty("placement").enumValueIndex = (int)Placement.Independent;
            data.FindProperty("orderPolicy").enumValueIndex = (int)OrderPolicy.Fixed;
            data.ApplyModifiedPropertiesWithoutUndo(); popup.Canvas.sortingOrder = order;
        }
        private static Button Dismiss(GameObject root)
        {
            var shade = root.transform.Find("Shade").GetComponent<Image>();
            var button = shade.GetComponent<Button>() ?? shade.gameObject.AddComponent<Button>();
            button.targetGraphic = shade; button.transition = Selectable.Transition.None; return button;
        }
        [MenuItem("Pocket Arcade/Block Gravity/3. Apply Exit Menu")]
        public static void ApplyExitMenu()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            string backup="Logs/BlockGravityBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(var path in new[]{HudFile,PauseFile,RetiredSettingsFile})if(File.Exists(path))File.Copy(path,backup+"/"+Path.GetFileName(path));
            UpgradeMenuPrefabs();RegisterResources();AssetDatabase.SaveAssets();Validate();
        }
        private static void UpgradeMenuPrefabs()
        {
            var root=PrefabUtility.LoadPrefabContents(HudFile);
            try
            {
                var hud=root.GetComponent<BlockGravityHud>();var region=root.transform.Find("HUD");
                foreach(var name in new[]{"Pause","Home","Restart"}) { var old=region.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject); }
                var exit=region.Find("Exit");
                Set(hud,"exitButton",exit!=null?exit.GetComponent<Button>():Button(region,"Exit",new Vector2(.88f,.86f),"bg.exit",new Vector2(180,56)));
                Save(root,HudFile);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            root=PrefabUtility.LoadPrefabContents(PauseFile);
            try
            {
                var popup=root.GetComponent<BlockGravityPausePopup>();Modal(popup,31000);Set(popup,"dismiss",Dismiss(root));
                var card=(RectTransform)root.transform.Find("Card");
                UpgradeSoundIcons(popup,card);Save(root,PauseFile);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            RetireSettingsPopup();
        }
        private static void CreateResult()
        {
            if(File.Exists(ResultFile))return;
            var root=Window<BlockGravityResultPopup>("BlockGravityResult",BlockGravityHud.ResultKey);
            try
            {
                var policy=new SerializedObject(root.GetComponent<BlockGravityResultPopup>());
                policy.FindProperty("closeOnBack").boolValue=false;policy.ApplyModifiedPropertiesWithoutUndo();
                var shade=Rect(root.transform,"Shade",new Vector2(.5f,.5f),Vector2.zero);Fill(shade);
                shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.55f);
                var card=Rect(root.transform,"Card",new Vector2(.5f,.5f),new Vector2(820,800));
                card.gameObject.AddComponent<Image>().color=new Color(.95f,.96f,.91f);
                var result=root.GetComponent<BlockGravityResultPopup>();
                Label(card,"Title",new Vector2(.5f,.91f),new Vector2(760,70),42,"bg.result.title");
                Set(result,"finalScore",Label(card,"FinalScore",new Vector2(.5f,.77f),new Vector2(760,75),40));
                Set(result,"best",Label(card,"Best",new Vector2(.5f,.65f),new Vector2(760,60),32));
                Set(result,"coins",Label(card,"Coins",new Vector2(.5f,.54f),new Vector2(760,60),32));
                Set(result,"saveStatus",Label(card,"SaveStatus",new Vector2(.5f,.40f),new Vector2(740,120),28));
                Set(result,"retrySave",Button(card,"RetrySave",new Vector2(.5f,.25f),"bg.result.retry",new Vector2(640,70)));
                Set(result,"restart",Button(card,"Restart",new Vector2(.27f,.10f),"bg.restart",new Vector2(340,80)));
                Set(result,"home",Button(card,"Home",new Vector2(.73f,.10f),"bg.home",new Vector2(340,80)));
                root.SetActive(false);Save(root,ResultFile);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Block Gravity/1. Create Playfield And Connect GameScene")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var original=EditorSceneManager.GetSceneManagerSetup();
            string backup="Logs/BlockGravityBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            File.Copy(GameSceneFile,backup+"/GameScene.unity");File.Copy(BootResourceSetup.ManifestFile,backup+"/BootResources.json");
            try
            {
                CreateWorld();CreateHud();CreatePause();CreateResult();UpgradeMenuPrefabs();
                var scene=EditorSceneManager.OpenScene(GameSceneFile,OpenSceneMode.Single);
                UISceneHost host=null;
                foreach(var root in scene.GetRootGameObjects())foreach(var found in root.GetComponentsInChildren<UISceneHost>(true))
                {if(host!=null)throw new InvalidOperationException(Text("bg.error.host"));host=found;}
                if(host==null)throw new InvalidOperationException(Text("bg.error.host"));
                var data=new SerializedObject(host);
                if(data.FindProperty("liveRoot").objectReferenceValue==null || data.FindProperty("inactiveRoot").objectReferenceValue==null)
                    throw new InvalidOperationException(Text("bg.error.frames"));
                data.FindProperty("screenPath").stringValue=BlockGravityHud.ScreenKey;data.FindProperty("rootKey").stringValue=BlockGravityHud.ScreenKey;data.ApplyModifiedPropertiesWithoutUndo();
                foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                    if(camera.CompareTag("MainCamera"))camera.enabled=false;
                if(!EditorSceneManager.SaveScene(scene))throw new IOException(GameSceneFile);
                RegisterResources();AssetDatabase.SaveAssets();Validate();
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(original);}
            Debug.Log(StringTableSetup.Format("bg.setup.done",backup));
        }
        private static void RegisterResources()
        {
            var data=JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(BootResourceSetup.ManifestFile));
            var slots=new List<BootResourceSlot>(data.resources);
            slots.RemoveAll(slot => slot.path == RetiredSettingsKey);
            foreach(var item in BootResourceSetup.DiscoverResources())
            {
                var slot=slots.Find(value=>value.path==item.Key);
                if(slot==null)slots.Add(new BootResourceSlot{path=item.Key,kind=item.Value,required=true});else slot.kind=item.Value;
            }
            slots.Sort((a,b)=>string.CompareOrdinal(a.path,b.path));data.resources=slots.ToArray();
            File.WriteAllText(BootResourceSetup.ManifestFile,JsonUtility.ToJson(data,true));AssetDatabase.ImportAsset(BootResourceSetup.ManifestFile);
        }
        [MenuItem("Pocket Arcade/Block Gravity/2. Validate Playfield")]
        public static void Validate()
        {
            foreach(var path in new[]{HudFile,PauseFile,ResultFile,TutorialFile})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(root==null)throw new InvalidOperationException(Text("bg.error.references"));
                root.GetComponent<UIBase>().ValidateDefinition();
                var fields=path==HudFile?new[]{"hudRegion","score","best","gravity","status","exitButton"}:
                    path==PauseFile?new[]{"resume","exit","dismiss","help","notice"}:
                    path==TutorialFile?new[]{"title","body","progress","notice","nextLabel","previous","next","later"}:new[]{"finalScore","best","coins","saveStatus","retrySave","restart","home"};
                var serialized=new SerializedObject(root.GetComponent<UIBase>());
                foreach(var field in fields)if(serialized.FindProperty(field).objectReferenceValue==null)
                    throw new InvalidOperationException(Text("bg.error.references"));
            }
            var pause=AssetDatabase.LoadAssetAtPath<GameObject>(PauseFile);
            var icons=pause.GetComponentsInChildren<AudioSettingIcon>(true);
            if(icons.Length!=2 || pause.transform.Find("Card/Settings")!=null)throw new InvalidOperationException(Text("bg.error.references"));
            foreach(var icon in icons)if(!icon.HasReferences)throw new InvalidOperationException(Text("bg.error.references"));
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(WorldFile);
            if(world==null || world.GetComponent<BlockGravityPresenter>()==null || world.GetComponent<BlockGravityBoardView>()==null || world.GetComponentInChildren<Camera>()==null)
                throw new InvalidOperationException(Text("bg.error.references"));
            var viewData=new SerializedObject(world.GetComponent<BlockGravityBoardView>());
            foreach(var field in new[]{"playCamera","visualRoot"})if(viewData.FindProperty(field).objectReferenceValue==null)
                throw new InvalidOperationException(Text("bg.error.references"));
            var counts=new[]{64,3*ProgressivePieceSource.MaxPieceCells,ProgressivePieceSource.MaxPieceCells,ProgressivePieceSource.MaxPieceCells};var arrays=new[]{"blocks","trayCells","ghostCells","draggedCells"};
            for(int index=0;index<arrays.Length;index++)
            {
                var array=viewData.FindProperty(arrays[index]);if(array.arraySize!=counts[index])throw new InvalidOperationException(Text("bg.error.references"));
                for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).objectReferenceValue==null)
                    throw new InvalidOperationException(Text("bg.error.references"));
            }
            var scene=SceneManager.GetSceneByPath(GameSceneFile);bool opened=!scene.IsValid();
            if(opened)scene=EditorSceneManager.OpenScene(GameSceneFile,OpenSceneMode.Additive);
            try
            {
                int hosts=0,events=0;
                foreach(var root in scene.GetRootGameObjects())
                {
                    events+=root.GetComponentsInChildren<EventSystem>(true).Length;
                    foreach(var host in root.GetComponentsInChildren<UISceneHost>(true))
                    {
                        hosts++;var data=new SerializedObject(host);
                        if(data.FindProperty("screenPath").stringValue!=BlockGravityHud.ScreenKey || data.FindProperty("rootKey").stringValue!=BlockGravityHud.ScreenKey ||
                            data.FindProperty("liveRoot").objectReferenceValue==null || data.FindProperty("inactiveRoot").objectReferenceValue==null)
                            throw new InvalidOperationException(Text("bg.error.host"));
                    }
                    foreach(var camera in root.GetComponentsInChildren<Camera>(true))if(camera.CompareTag("MainCamera")&&camera.enabled)
                        throw new InvalidOperationException(Text("bg.error.host"));
                }
                if(hosts!=1 || events!=1)throw new InvalidOperationException(Text("bg.error.host"));
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            BootResourceSetup.ValidateInventory();Debug.Log(Text("bg.setup.valid"));
        }
    }
}
