using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.Editor;
using PocketArcade.EchoRoom.Unity;
using PocketArcade.Meta;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PocketArcade.EchoRoom.Editor
{
    public static class EchoSetup
    {
        private const string R="Assets/_Project/Resources/",TuningFile="Assets/_Project/ScriptableObjects/EchoRoom/EchoTuning.asset",Definition="Assets/_Project/ScriptableObjects/MiniGames/echo_room.asset";
        private static string Text(string key)=>StringTableSetup.Text(key);
        private static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetArray<T>(Object target,string field,T[] values)where T:Object{var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Save(GameObject root,string path){Directory.CreateDirectory(Path.GetDirectoryName(path));PrefabUtility.SaveAsPrefabAsset(root,path,out bool ok);if(!ok)throw new IOException(path);}
        private static T Asset<T>(string path)where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a!=null)return a;Directory.CreateDirectory(Path.GetDirectoryName(path));a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);return a;}
        private static SpriteRenderer Sprite(Transform parent,string name,Color color,int order,Vector2 size,Vector2 position)
        {var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.localScale=new Vector3(size.x,size.y*.8f,1);go.transform.localPosition=new Vector3(position.x,position.y*.8f,0);var r=go.GetComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(R+"MiniGames/BlockGravity/Cell.png");r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");r.color=color;r.sortingOrder=order;return r;}
        private static void World(EchoTuning tuning)
        {
            if(File.Exists(R+EchoHud.WorldPath+".prefab"))return;var rules=tuning.Rules();var root=new GameObject("EchoRoomPlayfield");
            try
            {
                var c=new GameObject("PlayCamera",typeof(Camera));c.transform.SetParent(root.transform,false);c.tag="MainCamera";var camera=c.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,-.6f,-10);camera.backgroundColor=new Color(.96f,.95f,.91f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.1f;camera.farClipPlane=50;
                var view=root.AddComponent<EchoWorldView>();Set(view,"playCamera",camera);Sprite(root.transform,"Floor",new Color(.9f,.92f,.85f),0,new Vector2(6.8f,6.2f),Vector2.zero);
                Sprite(root.transform,"DeliveryBay",new Color(.56f,.81f,.67f),3,new Vector2(1.35f,1.35f),Vector2.zero);
                for(int i=0;i<4;i++)Sprite(root.transform,"Rim_"+i,new Color(.34f,.49f,.41f),8,i<2?new Vector2(6.9f,.08f):new Vector2(.08f,6.3f),i==0?new Vector2(0,3.12f):i==1?new Vector2(0,-3.12f):i==2?new Vector2(-3.45f,0):new Vector2(3.45f,0));
                var gates=new SpriteRenderer[2];var plates=new SpriteRenderer[2];
                for(int i=0;i<2;i++)
                {
                    float x=i==0?-1.25f:1.25f;Sprite(root.transform,"DividerTop_"+i,new Color(.57f,.63f,.55f),8,new Vector2(.16f,2.6f),new Vector2(x,1.9f));Sprite(root.transform,"DividerBottom_"+i,new Color(.57f,.63f,.55f),8,new Vector2(.16f,2.6f),new Vector2(x,-1.9f));
                    gates[i]=Sprite(root.transform,"Gate_"+i,Color.blue,9,new Vector2(.16f,1.2f),new Vector2(x,0));var p=i==0?new Vector2(.65f,-1.1f):new Vector2(-.65f,1.1f);plates[i]=Sprite(root.transform,"Plate_"+i,Color.blue,4,new Vector2(.55f,.55f),p);
                }
                SetArray(view,"gates",gates);SetArray(view,"plates",plates);
                var parcels=new SpriteRenderer[rules.Parcels.Length];for(int i=0;i<parcels.Length;i++)parcels[i]=Sprite(root.transform,"Parcel_"+i,new Color(.93f,.66f,.2f),12,new Vector2(.3f,.3f),new Vector2((float)rules.Parcels[i].X,(float)rules.Parcels[i].Y));SetArray(view,"parcels",parcels);
                var actors=new SpriteRenderer[rules.Rounds];var cargo=new SpriteRenderer[rules.Rounds];var trails=new LineRenderer[rules.Rounds-1];
                for(int i=0;i<actors.Length;i++){actors[i]=Sprite(root.transform,"Actor_"+i,new Color(.2f,.57f,.8f),i==0?30:20,new Vector2(.35f,.35f),Vector2.zero);cargo[i]=Sprite(root.transform,"Cargo_"+i,new Color(.93f,.66f,.2f,i==0?1:.5f),40,new Vector2(.22f,.22f),Vector2.zero);}
                for(int i=0;i<trails.Length;i++){var go=new GameObject("RecordedPath_"+i,typeof(LineRenderer));go.transform.SetParent(root.transform,false);var line=go.GetComponent<LineRenderer>();line.sharedMaterial=actors[0].sharedMaterial;line.useWorldSpace=true;line.startWidth=line.endWidth=.025f;line.sortingOrder=2;line.positionCount=0;trails[i]=line;}
                SetArray(view,"actors",actors);SetArray(view,"cargo",cargo);SetArray(view,"trails",trails);Save(root,R+EchoHud.WorldPath+".prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 size){var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;return r;}
        private static void Fill(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static Image Plate(Transform p,string n,Vector2 a,Vector2 size,Color color){var image=Rect(p,n,a,size).gameObject.AddComponent<Image>();image.color=color;return image;}
        private static TMP_Text Label(Transform p,string n,Vector2 a,Vector2 size,int font,string key=null)
        {var r=Rect(p,n,a,size);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NanumGothic SDF.asset");label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.17f,.27f,.21f);label.raycastTarget=false;label.text="";if(key!=null){label.text=Text(key);r.gameObject.AddComponent<LocalizedText>().SetKey(key);}return label;}
        private static Button Button(Transform p,string n,Vector2 a,Vector2 size,string key)
        {var r=Rect(p,n,a,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.22f,.4f,.33f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;var text=Label(r,"Label",Vector2.one*.5f,size,30,key);Fill((RectTransform)text.transform);text.color=Color.white;return button;}
        private static GameObject Window<T>(string name,string key,bool modal)where T:UIBase
        {var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster));Fill((RectTransform)root.transform);var window=root.AddComponent<T>();var so=new SerializedObject(window);so.FindProperty("key").stringValue=key;so.FindProperty("placement").enumValueIndex=modal?1:0;so.FindProperty("orderPolicy").enumValueIndex=modal?1:0;so.ApplyModifiedPropertiesWithoutUndo();window.Canvas.overrideSorting=true;window.Canvas.sortingOrder=modal?31000:100;return root;}
        private static void Screen(EchoTuning tuning)
        {
            string path=R+EchoHud.ScreenKey+".prefab";if(File.Exists(path))return;var root=Window<EchoHud>("EchoRoomHud",EchoHud.ScreenKey,false);
            try
            {
                var hud=root.GetComponent<EchoHud>();Set(hud,"tuning",tuning);var inputArea=Plate(root.transform,"MoveInput",Vector2.one*.5f,Vector2.zero,Color.clear);Fill(inputArea.rectTransform);var input=inputArea.gameObject.AddComponent<EchoInput>();Set(hud,"input",input);
                var head=Plate(root.transform,"HUD",new Vector2(.5f,.87f),new Vector2(1000,340),new Color(.96f,.96f,.91f,.97f));head.raycastTarget=false;
                Label(head.transform,"Title",new Vector2(.4f,.83f),new Vector2(720,70),42,"game.echo_room.name");Set(hud,"pause",Button(head.transform,"Pause",new Vector2(.87f,.83f),new Vector2(180,80),"bg.pause"));Set(hud,"progress",Label(head.transform,"Progress",new Vector2(.5f,.57f),new Vector2(950,90),28));Set(hud,"timer",Label(head.transform,"Timer",new Vector2(.5f,.32f),new Vector2(950,80),26));
                var track=Plate(head.transform,"Timeline",new Vector2(.5f,.1f),new Vector2(900,22),new Color(.8f,.84f,.77f));track.raycastTarget=false;var fill=Plate(track.transform,"Fill",Vector2.one*.5f,Vector2.zero,new Color(.45f,.55f,.85f));Fill(fill.rectTransform);fill.raycastTarget=false;var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.interactable=false;slider.transition=Selectable.Transition.None;Set(hud,"timeline",slider);
                Set(hud,"status",Label(root.transform,"Status",new Vector2(.5f,.28f),new Vector2(960,130),28));var pad=Plate(root.transform,"Stick",new Vector2(.5f,.155f),new Vector2(220,220),new Color(.75f,.81f,.72f,.6f));pad.raycastTarget=false;var knob=Plate(pad.transform,"Knob",Vector2.one*.5f,new Vector2(60,60),new Color(.24f,.48f,.4f));knob.raycastTarget=false;Set(input,"knob",knob.rectTransform);
                var primary=Button(root.transform,"Primary",new Vector2(.5f,.045f),new Vector2(960,90),null);Set(hud,"primary",primary);Set(hud,"primaryLabel",primary.GetComponentInChildren<TMP_Text>());root.SetActive(false);Save(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void Menu()
        {
            string path=R+EchoHud.MenuKey+".prefab";if(File.Exists(path))return;var root=Window<EchoMenu>("EchoRoomMenu",EchoHud.MenuKey,true);
            try{var shade=Plate(root.transform,"Shade",Vector2.one*.5f,Vector2.zero,new Color(0,0,0,.5f));Fill(shade.rectTransform);var card=Plate(root.transform,"Card",Vector2.one*.5f,new Vector2(850,680),new Color(.96f,.96f,.91f));Label(card.transform,"Title",new Vector2(.5f,.88f),new Vector2(760,70),42,"bg.pause");var menu=root.GetComponent<EchoMenu>();Set(menu,"resume",Button(card.transform,"Resume",new Vector2(.5f,.65f),new Vector2(700,100),"bg.resume"));Set(menu,"retry",Button(card.transform,"Retry",new Vector2(.5f,.43f),new Vector2(700,100),"floor.retry"));Set(menu,"home",Button(card.transform,"Home",new Vector2(.5f,.21f),new Vector2(700,100),"bg.home"));Set(menu,"notice",Label(card.transform,"Notice",new Vector2(.5f,.07f),new Vector2(760,70),24));root.SetActive(false);Save(root,path);}finally{Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Echo Room/1. Configure Prototype")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string backup="Logs/EchoRoomBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);foreach(string p in new[]{"Assets/_Project/ScriptableObjects/GameCatalog.asset",SceneIds.PathFor(SceneIds.GameScene),BootResourceSetup.ManifestFile})File.Copy(p,backup+"/"+Path.GetFileName(p));
            var tuning=Asset<EchoTuning>(TuningFile);World(tuning);Screen(tuning);Menu();var game=Asset<MiniGameDefinition>(Definition);var so=new SerializedObject(game);so.FindProperty("id").stringValue=EchoSession.GameId;so.FindProperty("sceneName").stringValue=SceneIds.GameScene;so.FindProperty("isAvailable").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");so=new SerializedObject(catalog);so.FindProperty("miniGames").GetArrayElementAtIndex(2).objectReferenceValue=game;so.ApplyModifiedPropertiesWithoutUndo();
            var original=EditorSceneManager.GetSceneManagerSetup();try{var scene=EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.GameScene),OpenSceneMode.Single);foreach(var root in scene.GetRootGameObjects())foreach(var host in root.GetComponentsInChildren<UISceneHost>(true)){so=new SerializedObject(host);var routes=so.FindProperty("gameScreens");int index=-1;for(int i=0;i<routes.arraySize;i++)if(routes.GetArrayElementAtIndex(i).FindPropertyRelative("gameId").stringValue==EchoSession.GameId)index=i;if(index<0){index=routes.arraySize;routes.arraySize++;}var entry=routes.GetArrayElementAtIndex(index);entry.FindPropertyRelative("gameId").stringValue=EchoSession.GameId;entry.FindPropertyRelative("screenPath").stringValue=EchoHud.ScreenKey;so.ApplyModifiedPropertiesWithoutUndo();}EditorSceneManager.SaveScene(scene);}finally{EditorSceneManager.RestoreSceneManagerSetup(original);}
            var manifest=JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(BootResourceSetup.ManifestFile));var slots=new List<BootResourceSlot>(manifest.resources);foreach(var found in BootResourceSetup.DiscoverResources()){var slot=slots.Find(s=>s.path==found.Key);if(slot==null)slots.Add(new BootResourceSlot{path=found.Key,kind=found.Value,required=true});else slot.kind=found.Value;}slots.Sort((a,b)=>string.CompareOrdinal(a.path,b.path));manifest.resources=slots.ToArray();File.WriteAllText(BootResourceSetup.ManifestFile,JsonUtility.ToJson(manifest,true));AssetDatabase.ImportAsset(BootResourceSetup.ManifestFile);AssetDatabase.SaveAssets();StringTableSetup.Apply();Validate();Debug.Log(StringTableSetup.Format("echo.setup.done",backup));
        }
        [MenuItem("Pocket Arcade/Echo Room/2. Validate Prototype")]
        public static void Validate()
        {
            foreach(string key in new[]{EchoHud.ScreenKey,EchoHud.MenuKey}){var root=AssetDatabase.LoadAssetAtPath<GameObject>(R+key+".prefab");if(root==null)throw new InvalidOperationException(Text("echo.error.references"));root.GetComponent<UIBase>().ValidateDefinition();}
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(R+EchoHud.WorldPath+".prefab");if(world?.GetComponent<EchoWorldView>()==null)throw new InvalidOperationException(Text("echo.error.references"));var rules=AssetDatabase.LoadAssetAtPath<EchoTuning>(TuningFile).Rules();rules.Validate();var data=new SerializedObject(world.GetComponent<EchoWorldView>());if(data.FindProperty("actors").arraySize<rules.Rounds||data.FindProperty("parcels").arraySize!=rules.Parcels.Length)throw new InvalidOperationException(Text("echo.error.references"));BootResourceSetup.ValidateInventory();Debug.Log(Text("echo.setup.valid"));
        }
    }
}
