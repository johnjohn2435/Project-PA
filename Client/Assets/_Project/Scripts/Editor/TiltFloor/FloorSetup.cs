using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.Editor;
using PocketArcade.Meta;
using PocketArcade.TiltFloor.Unity;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PocketArcade.TiltFloor.Editor
{
    public static partial class FloorSetup
    {
        private const string R="Assets/_Project/Resources/",A="Assets/_Project/Art/TiltFloor/";
        private const string TuningFile="Assets/_Project/ScriptableObjects/TiltFloor/FloorTuning.asset",Definition="Assets/_Project/ScriptableObjects/MiniGames/tilt_floor.asset";
        private static string Text(string key)=>StringTableSetup.Text(key);
        private static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Array(Object target,string field,SpriteRenderer[] values){var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Save(GameObject root,string path){Directory.CreateDirectory(Path.GetDirectoryName(path));PrefabUtility.SaveAsPrefabAsset(root,path,out bool ok);if(!ok)throw new IOException(path);}
        private static T Asset<T>(string path)where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a!=null)return a;Directory.CreateDirectory(Path.GetDirectoryName(path));a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);return a;}
        private static SpriteRenderer Sprite(Transform parent,string name,Color color,int order,Vector2 size)
        {
            var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.localScale=new Vector3(size.x,size.y,1);
            var r=go.GetComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(R+"MiniGames/BlockGravity/Cell.png");r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");r.color=color;r.sortingOrder=order;return r;
        }
        private static SpriteRenderer[] Pool(Transform parent,string name,int count,Color color,int order,Vector2 size)
        {var a=new SpriteRenderer[count];for(int i=0;i<count;i++)a[i]=Sprite(parent,name+i,color,order,size);return a;}
        private static void World(FloorTuning tuning)=>CircleWorld(tuning,false);
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 size){var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;return r;}
        private static void Fill(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static GameObject Window<T>(string name,string key,bool modal)where T:UIBase
        {var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster));Fill((RectTransform)root.transform);var window=root.AddComponent<T>();var so=new SerializedObject(window);so.FindProperty("key").stringValue=key;so.FindProperty("placement").enumValueIndex=modal?1:0;so.FindProperty("orderPolicy").enumValueIndex=modal?1:0;so.ApplyModifiedPropertiesWithoutUndo();window.Canvas.overrideSorting=true;window.Canvas.sortingOrder=modal?31000:100;return root;}
        private static Image Plate(Transform p,string n,Vector2 a,Vector2 size,Color color){var image=Rect(p,n,a,size).gameObject.AddComponent<Image>();image.color=color;return image;}
        private static TMP_Text Label(Transform p,string n,Vector2 a,Vector2 size,int font,string key=null)
        {var r=Rect(p,n,a,size);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NanumGothic SDF.asset");label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.17f,.27f,.21f);label.raycastTarget=false;label.text="";if(key!=null){label.text=Text(key);r.gameObject.AddComponent<LocalizedText>().SetKey(key);}return label;}
        private static Button Button(Transform p,string n,Vector2 a,Vector2 size,string key)
        {var r=Rect(p,n,a,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.22f,.4f,.33f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;var text=Label(r,"Label",new Vector2(.5f,.5f),size,30,key);Fill((RectTransform)text.transform);text.color=Color.white;return button;}
        private static void Screen(FloorTuning tuning)
        {
            string path=R+FloorHud.ScreenKey+".prefab";if(File.Exists(path))return;var root=Window<FloorHud>("TiltFloorHud",FloorHud.ScreenKey,false);
            try
            {
                var hud=root.GetComponent<FloorHud>();Set(hud,"tuning",tuning);
                var inputArea=Plate(root.transform,"MoveInput",Vector2.one*.5f,Vector2.zero,Color.clear);Fill((RectTransform)inputArea.transform);var input=inputArea.gameObject.AddComponent<FloorInput>();Set(hud,"input",input);
                var head=Plate(root.transform,"HUD",new Vector2(.5f,.87f),new Vector2(1000,340),new Color(.96f,.96f,.91f,.97f));head.raycastTarget=false;
                Label(head.transform,"Title",new Vector2(.4f,.83f),new Vector2(720,70),42,"game.tilt_floor.name");Set(hud,"pause",Button(head.transform,"Pause",new Vector2(.87f,.83f),new Vector2(180,80),"bg.pause"));
                Set(hud,"progress",Label(head.transform,"Progress",new Vector2(.5f,.58f),new Vector2(920,60),36));Set(hud,"balance",Label(head.transform,"Balance",new Vector2(.5f,.37f),new Vector2(920,90),24));
                var track=Plate(head.transform,"Danger",new Vector2(.5f,.14f),new Vector2(900,24),new Color(.8f,.84f,.77f));track.raycastTarget=false;var fill=Plate(track.transform,"Fill",Vector2.one*.5f,Vector2.zero,new Color(.82f,.43f,.29f));Fill((RectTransform)fill.transform);fill.raycastTarget=false;var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=(RectTransform)fill.transform;slider.interactable=false;slider.transition=Selectable.Transition.None;Set(hud,"danger",slider);
                Set(hud,"status",Label(root.transform,"Status",new Vector2(.5f,.28f),new Vector2(960,130),28));
                var pad=Plate(root.transform,"Stick",new Vector2(.5f,.155f),new Vector2(220,220),new Color(.75f,.81f,.72f,.6f));pad.raycastTarget=false;var knob=Plate(pad.transform,"Knob",Vector2.one*.5f,new Vector2(60,60),new Color(.24f,.48f,.4f));knob.raycastTarget=false;Set(input,"knob",knob.rectTransform);
                var primary=Button(root.transform,"Primary",new Vector2(.5f,.045f),new Vector2(960,90),null);Set(hud,"primary",primary);Set(hud,"primaryLabel",primary.GetComponentInChildren<TMP_Text>());root.SetActive(false);Save(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void Menu()
        {
            string path=R+FloorHud.MenuKey+".prefab";if(File.Exists(path))return;var root=Window<FloorMenu>("TiltFloorMenu",FloorHud.MenuKey,true);
            try
            {var shade=Plate(root.transform,"Shade",Vector2.one*.5f,Vector2.zero,new Color(0,0,0,.5f));Fill(shade.rectTransform);var card=Plate(root.transform,"Card",Vector2.one*.5f,new Vector2(850,680),new Color(.96f,.96f,.91f));Label(card.transform,"Title",new Vector2(.5f,.88f),new Vector2(760,70),42,"bg.pause");var menu=root.GetComponent<FloorMenu>();Set(menu,"resume",Button(card.transform,"Resume",new Vector2(.5f,.65f),new Vector2(700,100),"bg.resume"));Set(menu,"retry",Button(card.transform,"Retry",new Vector2(.5f,.43f),new Vector2(700,100),"floor.retry"));Set(menu,"home",Button(card.transform,"Home",new Vector2(.5f,.21f),new Vector2(700,100),"bg.home"));Set(menu,"notice",Label(card.transform,"Notice",new Vector2(.5f,.07f),new Vector2(760,70),24));root.SetActive(false);Save(root,path);}
            finally{Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Tilt Floor/1. Configure Experiment")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string backup="Logs/TiltFloorBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(string p in new[]{"Assets/_Project/ScriptableObjects/GameCatalog.asset",SceneIds.PathFor(SceneIds.GameScene),BootResourceSetup.ManifestFile})File.Copy(p,backup+"/"+Path.GetFileName(p));
            var tuning=Asset<FloorTuning>(TuningFile);World(tuning);Screen(tuning);Menu();var game=Asset<MiniGameDefinition>(Definition);var so=new SerializedObject(game);so.FindProperty("id").stringValue=FloorSession.GameId;so.FindProperty("sceneName").stringValue=SceneIds.GameScene;so.FindProperty("isAvailable").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");so=new SerializedObject(catalog);so.FindProperty("miniGames").GetArrayElementAtIndex(1).objectReferenceValue=game;so.ApplyModifiedPropertiesWithoutUndo();
            var original=EditorSceneManager.GetSceneManagerSetup();try{var scene=EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.GameScene),OpenSceneMode.Single);foreach(var root in scene.GetRootGameObjects())foreach(var host in root.GetComponentsInChildren<UISceneHost>(true)){so=new SerializedObject(host);var routes=so.FindProperty("gameScreens");int index=-1;for(int i=0;i<routes.arraySize;i++)if(routes.GetArrayElementAtIndex(i).FindPropertyRelative("gameId").stringValue==FloorSession.GameId)index=i;if(index<0){index=routes.arraySize;routes.arraySize++;}var entry=routes.GetArrayElementAtIndex(index);entry.FindPropertyRelative("gameId").stringValue=FloorSession.GameId;entry.FindPropertyRelative("screenPath").stringValue=FloorHud.ScreenKey;so.ApplyModifiedPropertiesWithoutUndo();}EditorSceneManager.SaveScene(scene);}finally{EditorSceneManager.RestoreSceneManagerSetup(original);}
            var manifest=JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(BootResourceSetup.ManifestFile));var slots=new List<BootResourceSlot>(manifest.resources);foreach(var found in BootResourceSetup.DiscoverResources()){var slot=slots.Find(s=>s.path==found.Key);if(slot==null)slots.Add(new BootResourceSlot{path=found.Key,kind=found.Value,required=true});else slot.kind=found.Value;}slots.Sort((a,b)=>string.CompareOrdinal(a.path,b.path));manifest.resources=slots.ToArray();File.WriteAllText(BootResourceSetup.ManifestFile,JsonUtility.ToJson(manifest,true));AssetDatabase.ImportAsset(BootResourceSetup.ManifestFile);AssetDatabase.SaveAssets();StringTableSetup.Apply();Validate();Debug.Log(StringTableSetup.Format("floor.setup.done",backup));
        }
        [MenuItem("Pocket Arcade/Tilt Floor/2. Validate Experiment")]
        public static void Validate()
        {
            foreach(string p in new[]{FloorHud.ScreenKey,FloorHud.MenuKey}){var root=AssetDatabase.LoadAssetAtPath<GameObject>(R+p+".prefab");if(root==null)throw new InvalidOperationException(Text("floor.error.references"));root.GetComponent<UIBase>().ValidateDefinition();}
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(R+FloorHud.WorldPath+".prefab");var view=world?.GetComponent<FloorWorldView>();if(view==null)throw new InvalidOperationException(Text("floor.error.references"));
            var rules=AssetDatabase.LoadAssetAtPath<FloorTuning>(TuningFile).Rules();rules.Validate();
            var data=new SerializedObject(view);if(data.FindProperty("bodies").arraySize!=rules.Capacity)throw new InvalidOperationException(Text("floor.error.references"));
            if(data.FindProperty("warnings").arraySize!=rules.MaxEnemies||data.FindProperty("borders").arraySize!=64||world.transform.Find("Surface").GetComponent<MeshFilter>().sharedMesh.vertexCount!=65)throw new InvalidOperationException(Text("floor.error.references"));
            BootResourceSetup.ValidateInventory();Debug.Log(Text("floor.setup.valid"));
        }
    }
}
