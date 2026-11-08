using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.Editor;
using PocketArcade.Meta;
using PocketArcade.UI;
using PocketArcade.UdadaDelivery.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.UdadaDelivery.Editor
{
    public static partial class DeliverySetup
    {
        private const string ResourcesRoot="Assets/_Project/Resources/";
        private const string TuningFile="Assets/_Project/ScriptableObjects/UdadaDelivery/DeliveryTuning.asset";
        private const string DefinitionFile="Assets/_Project/ScriptableObjects/MiniGames/udada_delivery.asset";
        private const string PlayerFile=ResourcesRoot+"MiniGames/UdadaDelivery/Player.prefab";
        private static string Text(string key)=>StringTableSetup.Text(key);
        private static void Set(Object target,string field,Object value)
        {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetArray(Object target,string field,SpriteRenderer[] values)
        {var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Save(GameObject root,string path)
        {Directory.CreateDirectory(Path.GetDirectoryName(path));PrefabUtility.SaveAsPrefabAsset(root,path,out bool ok);if(!ok)throw new IOException(path);}
        private static T Asset<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        private static SpriteRenderer Sprite(Transform parent,string name,Vector3 position,Vector3 scale,Color color,int order)
        {
            var root=new GameObject(name,typeof(SpriteRenderer));root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localScale=scale;
            var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesRoot+"MiniGames/BlockGravity/Cell.png");
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if(renderer.sprite==null||renderer.sharedMaterial==null)throw new InvalidOperationException(Text("ud.error.references"));
            renderer.color=color;renderer.sortingOrder=order;return renderer;
        }
        private static SpriteRenderer[] Pool(Transform parent,string prefix,int count,Color color,int order)
        {var result=new SpriteRenderer[count];for(int i=0;i<count;i++)result[i]=Sprite(parent,prefix+i,Vector3.zero,Vector3.one,color,order);return result;}
        private static void CreateWorld() => CreateBrakeWorld(false);
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 size)
        {var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;return r;}
        private static void Fill(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static GameObject Window<T>(string name,string key,bool modal) where T:UIBase
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster));Fill((RectTransform)root.transform);
            var window=root.AddComponent<T>();var so=new SerializedObject(window);so.FindProperty("key").stringValue=key;
            so.FindProperty("placement").enumValueIndex=modal?1:0;so.FindProperty("orderPolicy").enumValueIndex=modal?1:0;so.ApplyModifiedPropertiesWithoutUndo();
            window.Canvas.overrideSorting=true;window.Canvas.sortingOrder=modal?31000:100;return root;
        }
        private static TMP_Text Label(Transform parent,string name,Vector2 anchor,Vector2 size,int fontSize,string key=null)
        {
            var r=Rect(parent,name,anchor,size);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Fonts/NanumGothic SDF.asset");
            label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.16f,.24f,.22f);label.raycastTarget=false;label.text="";
            if(key!=null){label.text=Text(key);r.gameObject.AddComponent<LocalizedText>().SetKey(key);}return label;
        }
        private static Button Button(Transform parent,string name,Vector2 anchor,Vector2 size,string key)
        {
            var r=Rect(parent,name,anchor,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.22f,.4f,.35f);
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var label=Label(r,"Label",new Vector2(.5f,.5f),size,32,key);Fill((RectTransform)label.transform);label.color=Color.white;return button;
        }
        private static Image Plate(Transform parent,string name,Vector2 anchor,Vector2 size,Color color)
        {var image=Rect(parent,name,anchor,size).gameObject.AddComponent<Image>();image.color=color;return image;}
        private static void CreateHud(DeliveryTuning tuning)
        {
            string path=ResourcesRoot+DeliveryHud.ScreenKey+".prefab";if(File.Exists(path))return;
            var root=Window<DeliveryHud>("UdadaDeliveryHud",DeliveryHud.ScreenKey,false);
            try
            {
                // Authored full-screen brake target is behind all controls.
                var input=Plate(root.transform,"BrakeArea",new Vector2(.5f,.5f),Vector2.zero,new Color(0,0,0,0));Fill((RectTransform)input.transform);
                var brake=input.gameObject.AddComponent<DeliveryBrakeInput>();var hud=root.GetComponent<DeliveryHud>();Set(hud,"brake",brake);Set(hud,"tuning",tuning);
                var plate=Plate(root.transform,"HUD",new Vector2(.5f,.86f),new Vector2(1000,440),new Color(.96f,.96f,.91f,.97f));plate.raycastTarget=false;
                Label(plate.transform,"Title",new Vector2(.4f,.85f),new Vector2(700,70),42,"game.udada_delivery.name");
                Set(hud,"pause",Button(plate.transform,"Pause",new Vector2(.87f,.85f),new Vector2(180,80),"bg.pause"));
                Set(hud,"score",Label(plate.transform,"Score",new Vector2(.5f,.64f),new Vector2(960,68),38));
                Set(hud,"best",Label(plate.transform,"Best",new Vector2(.5f,.48f),new Vector2(960,52),28));
                Set(hud,"timer",Label(plate.transform,"Timer",new Vector2(.3f,.31f),new Vector2(460,60),34));
                Set(hud,"hearts",Label(plate.transform,"Hearts",new Vector2(.75f,.31f),new Vector2(400,60),34));
                var track=Plate(plate.transform,"Progress",new Vector2(.5f,.12f),new Vector2(900,22),new Color(.8f,.84f,.8f));track.raycastTarget=false;
                var fill=Plate(track.transform,"Fill",new Vector2(.5f,.5f),Vector2.zero,new Color(.24f,.65f,.46f));Fill((RectTransform)fill.transform);fill.raycastTarget=false;
                var slider=track.gameObject.AddComponent<Slider>();slider.fillRect=(RectTransform)fill.transform;slider.interactable=false;slider.transition=Selectable.Transition.None;Set(hud,"progress",slider);
                var bottom=Plate(root.transform,"ControlGuide",new Vector2(.5f,.13f),new Vector2(1000,320),new Color(.96f,.96f,.91f,.98f));bottom.raycastTarget=false;
                Set(hud,"status",Label(bottom.transform,"Status",new Vector2(.5f,.73f),new Vector2(940,120),30));
                var start=Button(bottom.transform,"Start",new Vector2(.5f,.27f),new Vector2(850,94),null);Set(hud,"start",start);Set(hud,"startLabel",start.GetComponentInChildren<TMP_Text>());
                root.SetActive(false);Save(root,path);
            }
            finally {Object.DestroyImmediate(root);}
        }
        private static void CreateDialog()
        {
            string path=ResourcesRoot+DeliveryHud.DialogKey+".prefab";if(File.Exists(path))return;
            var root=Window<DeliveryDialog>("UdadaDeliveryDialog",DeliveryHud.DialogKey,true);
            try
            {
                var shade=Plate(root.transform,"Shade",new Vector2(.5f,.5f),Vector2.zero,new Color(0,0,0,.55f));Fill((RectTransform)shade.transform);
                var card=Plate(root.transform,"Card",new Vector2(.5f,.5f),new Vector2(880,1040),new Color(.96f,.96f,.91f));
                var popup=root.GetComponent<DeliveryDialog>();Set(popup,"title",Label(card.transform,"Title",new Vector2(.5f,.92f),new Vector2(810,80),44));
                Set(popup,"summary",Label(card.transform,"Summary",new Vector2(.5f,.72f),new Vector2(800,270),32));
                var notice=Label(card.transform,"Notice",new Vector2(.5f,.28f),new Vector2(820,100),24);Set(popup,"notice",notice);
                var primary=Button(card.transform,"Primary",new Vector2(.5f,.17f),new Vector2(700,100),null);Set(popup,"primary",primary);Set(popup,"primaryLabel",primary.GetComponentInChildren<TMP_Text>());
                Set(popup,"home",Button(card.transform,"Home",new Vector2(.5f,.06f),new Vector2(700,90),"bg.home"));
                // Reuse the project's already-authored sound controls, remapping only their notice field.
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+"UI/Overlays/BlockGravityPause.prefab");
                foreach(string name in new[]{"Music","Effects"})
                {
                    var clone=Object.Instantiate(source.transform.Find("Card/"+name).gameObject,card.transform,false);
                    clone.name=name;var r=(RectTransform)clone.transform;r.anchorMin=r.anchorMax=new Vector2(name=="Music"?.35f:.65f,.43f);r.anchoredPosition=Vector2.zero;
                    Set(clone.GetComponent<AudioSettingIcon>(),"notice",notice);
                }
                root.SetActive(false);Save(root,path);
            }
            finally {Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Udada Delivery/1. Configure Prototype")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string backup="Logs/UdadaDeliveryBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(string file in new[]{SceneIds.PathFor(SceneIds.GameScene),BootResourceSetup.ManifestFile,"Assets/_Project/ScriptableObjects/GameCatalog.asset"})File.Copy(file,backup+"/"+Path.GetFileName(file));
            var tuning=Asset<DeliveryTuning>(TuningFile);CreateWorld();ConfigureHorizontalWorld();CreateHud(tuning);CreateDialog();
            var definition=Asset<MiniGameDefinition>(DefinitionFile);var so=new SerializedObject(definition);
            so.FindProperty("id").stringValue=DeliverySession.GameId;so.FindProperty("scoreKey").stringValue=DeliverySession.ScoreId;so.FindProperty("sceneName").stringValue=SceneIds.GameScene;so.FindProperty("isAvailable").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");so=new SerializedObject(catalog);var games=so.FindProperty("miniGames");
            int index=-1;for(int i=0;i<games.arraySize;i++){var game=(MiniGameDefinition)games.GetArrayElementAtIndex(i).objectReferenceValue;if(game.Id==DeliverySession.GameId||game.Id=="coming_soon_1"){index=i;break;}}
            if(index<0)throw new InvalidOperationException(Text("ud.error.references"));games.GetArrayElementAtIndex(index).objectReferenceValue=definition;so.ApplyModifiedPropertiesWithoutUndo();
            var original=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.GameScene),OpenSceneMode.Single);
                foreach(var root in scene.GetRootGameObjects())foreach(var host in root.GetComponentsInChildren<UISceneHost>(true))
                {
                    so=new SerializedObject(host);var routes=so.FindProperty("gameScreens");int route=-1;
                    for(int i=0;i<routes.arraySize;i++)if(routes.GetArrayElementAtIndex(i).FindPropertyRelative("gameId").stringValue==DeliverySession.GameId)route=i;
                    if(route<0){route=routes.arraySize;routes.arraySize++;}var entry=routes.GetArrayElementAtIndex(route);
                    entry.FindPropertyRelative("gameId").stringValue=DeliverySession.GameId;entry.FindPropertyRelative("screenPath").stringValue=DeliveryHud.ScreenKey;so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(original);}
            RegisterResources();AssetDatabase.SaveAssets();StringTableSetup.Apply();Validate();Debug.Log(StringTableSetup.Format("ud.setup.done",backup));
        }
        [MenuItem("Pocket Arcade/Udada Delivery/3. Apply Horizontal Course")]
        public static void ApplyHorizontalCourse()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            string backup="Logs/UdadaDeliveryBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            File.Copy(ResourcesRoot+DeliveryHud.WorldPath+".prefab",backup+"/Playfield.prefab");
            ConfigureHorizontalWorld();AssetDatabase.SaveAssets();Validate();
        }
        private static void ConfigureHorizontalWorld()
        {
            string path=ResourcesRoot+DeliveryHud.WorldPath+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var road=root.transform.Find("Road");
                if(road==null)throw new InvalidOperationException(Text("ud.error.references"));
                road.localPosition=new Vector3(500,-.45f,0);road.localScale=new Vector3(1200,.4f,1);
                Save(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void RegisterResources()
        {
            var manifest=JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(BootResourceSetup.ManifestFile));var slots=new List<BootResourceSlot>(manifest.resources);
            foreach(var item in BootResourceSetup.DiscoverResources())
            {var slot=slots.Find(s=>s.path==item.Key);if(slot==null)slots.Add(new BootResourceSlot{path=item.Key,kind=item.Value,required=true});else slot.kind=item.Value;}
            slots.Sort((a,b)=>string.CompareOrdinal(a.path,b.path));manifest.resources=slots.ToArray();File.WriteAllText(BootResourceSetup.ManifestFile,JsonUtility.ToJson(manifest,true));AssetDatabase.ImportAsset(BootResourceSetup.ManifestFile);
        }
        [MenuItem("Pocket Arcade/Udada Delivery/2. Validate Prototype")]
        public static void Validate()
        {
            foreach(string key in new[]{DeliveryHud.ScreenKey,DeliveryHud.DialogKey})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+key+".prefab");if(root==null)throw new InvalidOperationException(Text("ud.error.references"));root.GetComponent<UIBase>().ValidateDefinition();
                foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(!(component is DeliveryHud||component is DeliveryDialog||component is AudioSettingIcon))continue;
                    var so=new SerializedObject(component);var p=so.GetIterator();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==null)throw new InvalidOperationException(Text("ud.error.references"));
                }
            }
            var world=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+DeliveryHud.WorldPath+".prefab");
            if(world==null||world.GetComponent<DeliveryWorldView>()==null)throw new InvalidOperationException(Text("ud.error.references"));
            var rules=AssetDatabase.LoadAssetAtPath<DeliveryTuning>(TuningFile).CreateRules();rules.Validate();
            if(world.GetComponentsInChildren<DeliveryHouseView>(true).Length!=rules.HouseCount)throw new InvalidOperationException(Text("ud.error.references"));
            var worldData=new SerializedObject(world.GetComponent<DeliveryWorldView>());var property=worldData.GetIterator();
            while(property.NextVisible(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue==null)throw new InvalidOperationException(Text("ud.error.references"));
            if(AssetDatabase.LoadAssetAtPath<MiniGameDefinition>(DefinitionFile).ScoreKey!=DeliverySession.ScoreId)throw new InvalidOperationException(Text("ud.error.references"));
            BootResourceSetup.ValidateInventory();Debug.Log(Text("ud.setup.valid"));
        }
    }
}
