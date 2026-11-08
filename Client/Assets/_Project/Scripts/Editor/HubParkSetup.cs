using System;
using System.IO;
using System.Linq;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PocketArcade.Editor
{
    public static class HubParkSetup
    {
        const string Root="Assets/_Project/", Hub=Root+"Resources/UI/Screens/Hub.prefab";
        static readonly Color Cream=HubParkMeshBake.C(0xFFF5D7), Ink=HubParkMeshBake.C(0x45483B), Teal=HubParkMeshBake.C(0x168377), Gold=HubParkMeshBake.C(0xFFBF54);
        static TMP_FontAsset font;
        static GameObject Node(Transform parent,string name,params Type[] components)
        {var go=new GameObject(name,new[]{typeof(RectTransform)}.Concat(components).ToArray());go.layer=5;go.transform.SetParent(parent,false);return go;}
        static void Box(Component c,float x0,float y0,float x1,float y1)
        {var r=(RectTransform)c.transform;r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static T Ref<T>(Component c,string field) where T:Object => (T)new SerializedObject(c).FindProperty(field).objectReferenceValue;
        static void Set(Component c,string field,Object value)
        {var s=new SerializedObject(c);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static TMP_Text Text(Transform parent,string name,string key,float size,Color color,float x0,float y0,float x1,float y1)
        {
            var t=Node(parent,name,typeof(CanvasRenderer),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();Box(t,x0,y0,x1,y1);
            t.font=font;t.fontSize=t.fontSizeMax=size;t.fontSizeMin=size*.65f;t.enableAutoSizing=true;t.alignment=TextAlignmentOptions.Center;t.color=color;t.raycastTarget=false;
            if(key!=null){t.gameObject.AddComponent<LocalizedText>().SetKey(key);t.text=StringTableSetup.Text(key);}else t.text=string.Empty;
            return t;
        }
        static ArcadePaperGraphic Art(Transform parent,string name,ArcadePaperGraphic.Shape shape,Color fill,float x0,float y0,float x1,float y1)
        {var a=Node(parent,name,typeof(CanvasRenderer),typeof(ArcadePaperGraphic)).GetComponent<ArcadePaperGraphic>();a.Configure(shape,fill);a.raycastTarget=false;Box(a,x0,y0,x1,y1);return a;}
        static Image Image(Transform parent,string name,Color color,float x0,float y0,float x1,float y1)
        {var i=Node(parent,name,typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();i.color=color;i.raycastTarget=false;Box(i,x0,y0,x1,y1);return i;}
        static Button Button(Transform parent,string name,string key,Color fill,float x0,float y0,float x1,float y1)
        {
            var a=Art(parent,name,ArcadePaperGraphic.Shape.Capsule,fill,x0,y0,x1,y1);a.raycastTarget=true;
            var b=a.gameObject.AddComponent<Button>();b.targetGraphic=a;
            var colors=b.colors;colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.8f,.9f,.8f);b.colors=colors;
            Text(b.transform,"Label",key,40,Ink,.06f,.06f,.94f,.95f);return b;
        }
        static void Save(GameObject root,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            foreach(var c in root.GetComponentsInChildren<Component>(true))
            {EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            PrefabUtility.SaveAsPrefabAsset(root,path,out bool saved);if(!saved)throw new IOException(path);
        }
        [MenuItem("Pocket Arcade/Hub/Build Walking Park")]
        public static void Apply()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fonts/Jua SDF.asset");
            font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
            string glyphs=string.Concat(StringTableSetup.Read().Rows.Select(row=>row.ko+row.en));
            string pending=new string(glyphs.Where(c=>!char.IsControl(c)&&!font.HasCharacter(c)).Distinct().ToArray());
            if(pending.Length>0)font.TryAddCharacters(pending,out _);
            foreach(var atlas in font.atlasTextures)if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);
            font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);
            CreateWorld();CreateCard();BuildHub();
            typeof(BootResourceSetup).GetMethod("SynchronizeInventory",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
            typeof(StringTableSetup).GetMethod("EnsureFont",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{StringTableSetup.Read()});
            BootResourceSetup.ValidateInventory();StringTableSetup.Validate();AssetDatabase.SaveAssets();
        }
        static void CreateWorld()
        {
            Directory.CreateDirectory(Root+"Art/HubPark");Directory.CreateDirectory(Root+"Resources/Hub");AssetDatabase.Refresh();
            string meshPath=Root+"Art/HubPark/ParkMap.asset",materialPath=Root+"Art/HubPark/ParkMap.mat";
            var mesh=new HubParkMeshBake().Build();var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,materialPath);}
            var root=new GameObject("Pocket Park");
            try
            {
                var view=root.AddComponent<HubParkWorld>();
                var ground=new GameObject("Park map",typeof(MeshFilter),typeof(MeshRenderer));ground.transform.SetParent(root.transform,false);
                ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<MeshRenderer>().sharedMaterial=material;ground.GetComponent<MeshRenderer>().sortingOrder=-100;
                var camera=new GameObject("ParkCamera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform,false);
                camera.transform.localPosition=new Vector3(0,.6f,-10);camera.orthographic=true;camera.orthographicSize=6.9f;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=HubParkMeshBake.C(0xBFD5A1);camera.depth=5;camera.cullingMask=1;camera.rect=new Rect(0,.19f,1,.64f);camera.nearClipPlane=.1f;camera.farClipPlane=100;
                var anchor=new GameObject("Friend");anchor.transform.SetParent(root.transform,false);
                Set(view,"parkCamera",camera);Set(view,"actorAnchor",anchor.transform);
                var canvas=Node(root.transform,"Park signs",typeof(Canvas)).GetComponent<Canvas>();canvas.gameObject.layer=0;canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=10;
                canvas.transform.localScale=Vector3.one*.01f;((RectTransform)canvas.transform).sizeDelta=new Vector2(1000,1400);
                string[] ids={"block_gravity","tilt_floor","echo_room"};Vector2[] gates={new Vector2(-2.8f,.46f),new Vector2(2.8f,.46f),new Vector2(0,4.03f)};
                var data=new SerializedObject(view);var entries=data.FindProperty("entrances");entries.arraySize=3;
                for(int i=0;i<3;i++)
                {
                    var label=Text(canvas.transform,ids[i],null,29,Ink,0,0,0,0);label.gameObject.layer=0;
                    var rect=(RectTransform)label.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(278,44);rect.anchoredPosition=(gates[i]+new Vector2(0,.76f))*100;
                    var entry=entries.GetArrayElementAtIndex(i);entry.FindPropertyRelative("gameId").stringValue=ids[i];entry.FindPropertyRelative("position").vector2Value=gates[i];entry.FindPropertyRelative("radius").floatValue=.85f;entry.FindPropertyRelative("label").objectReferenceValue=label;
                }
                data.FindProperty("walkArea").rectValue=new Rect(-4.65f,-5.25f,9.3f,11.65f);
                data.FindProperty("spawnPoint").vector2Value=new Vector2(0,-4.28f);
                Rect[] obstacles={new Rect(-4.32f,1.13f,3.04f,2.3f),new Rect(1.28f,1.13f,3.04f,2.3f),new Rect(-1.52f,4.7f,3.04f,2.3f),new Rect(-.78f,-2.02f,1.56f,1.25f)};
                var blocked=data.FindProperty("obstacles");blocked.arraySize=obstacles.Length;for(int i=0;i<obstacles.Length;i++)blocked.GetArrayElementAtIndex(i).rectValue=obstacles[i];data.ApplyModifiedPropertiesWithoutUndo();
                Save(root,Root+"Resources/Hub/ParkWorld.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void CreateCard()
        {
            var root=Node(null,"ParkCharacterCard",typeof(CanvasRenderer),typeof(ArcadePaperGraphic),typeof(Button),typeof(CharacterCardView));
            try
            {
                var art=root.GetComponent<ArcadePaperGraphic>();art.Configure(ArcadePaperGraphic.Shape.Card,Cream);var button=root.GetComponent<Button>();button.targetGraphic=art;
                var portrait=Image(root.transform,"Portrait",Color.white,.1f,.26f,.9f,.92f);portrait.preserveAspect=true;
                var label=Text(root.transform,"Name",null,39,Ink,.04f,.055f,.96f,.235f);
                var selected=Image(root.transform,"Selected",Teal,.82f,.8f,.94f,.94f);selected.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/UI/CrispKit/check.png");selected.preserveAspect=true;selected.enabled=false;
                var locked=Image(root.transform,"Locked",Ink,.82f,.8f,.94f,.94f);locked.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/UI/CrispKit/lock.png");locked.preserveAspect=true;locked.enabled=false;
                var card=root.GetComponent<CharacterCardView>();Set(card,"portrait",portrait);Set(card,"label",label);Set(card,"button",button);Set(card,"selectedBadge",selected);Set(card,"lockedBadge",locked);
                Save(root,Root+"Resources/UI/Common/ParkCharacterCard.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void BuildHub()
        {
            var root=PrefabUtility.LoadPrefabContents(Hub);
            try
            {
                var hub=root.GetComponent<HubView>();var home=Ref<GameObject>(hub,"homePanel");var characters=Ref<GameObject>(hub,"characterPanel");
                // Preserve the panels and their UIManager keys; only replace their authored contents.
                foreach(Transform child in root.transform)if(child!=home.transform&&child!=characters.transform&&child!=Ref<GameObject>(hub,"gamePanel").transform&&child!=Ref<GameObject>(hub,"settingsPanel").transform)child.gameObject.SetActive(false);
                var rootImage=root.GetComponent<Image>();if(rootImage!=null)rootImage.color=Color.clear;
                foreach(var child in home.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                if(home.GetComponent<HubMedals>()!=null)Object.DestroyImmediate(home.GetComponent<HubMedals>());
                foreach(var graphic in home.GetComponents<Graphic>())graphic.color=Color.clear;
                Box(home.transform,0,0,1,1);
                var park=home.GetComponent<HubParkView>()??home.AddComponent<HubParkView>();
                Set(hub,"medals",null);Set(hub,"park",park);Set(hub,"rigPreview",null);Set(hub,"allGamesButton",null);
                Image(home.transform,"Header",Cream,0,.83f,1,1);
                Text(home.transform,"Park title","park.title",59,Teal,.04f,.937f,.69f,.995f).alignment=TextAlignmentOptions.Left;
                Text(home.transform,"Park subtitle","park.subtitle",30,Ink,.045f,.902f,.73f,.94f).alignment=TextAlignmentOptions.Left;
                var settings=Button(home.transform,"Settings","hub.settings",Cream,.79f,.932f,.965f,.987f);Set(hub,"settingsButton",settings);
                Art(home.transform,"Coin",ArcadePaperGraphic.Shape.PortraitMedal,Gold,.73f,.86f,.785f,.897f);
                var coins=Text(home.transform,"Coins",null,32,Ink,.79f,.858f,.96f,.902f);Set(hub,"coinsLabel",coins);
                var friend=Button(home.transform,"Choose friend",null,Cream,.04f,.84f,.64f,.9f);
                var name=friend.GetComponentInChildren<TMP_Text>();Box(name,.17f,.06f,.52f,.95f);name.fontSizeMax=36;Set(hub,"characterLabel",name);
                Text(friend.transform,"Choose","park.friends",30,Teal,.53f,.05f,.95f,.95f);
                var portrait=Image(friend.transform,"Portrait",Color.white,.02f,.08f,.17f,.92f);portrait.preserveAspect=true;
                Set(hub,"portrait",portrait);Set(hub,"charactersButton",friend);
                var portraitClick=portrait.gameObject.AddComponent<Button>();portrait.raycastTarget=true;portraitClick.targetGraphic=portrait;Set(hub,"portraitButton",portraitClick);
                Image(home.transform,"Controls",Cream,0,0,1,.195f);
                Art(home.transform,"Control rule",ArcadePaperGraphic.Shape.Rule,HubParkMeshBake.C(0xCDB780),.035f,.19f,.965f,.193f);
                var joystickArt=Art(home.transform,"Joystick",ArcadePaperGraphic.Shape.PortraitMedal,HubParkMeshBake.C(0xDDE8C8),.035f,.025f,.325f,.18f);joystickArt.raycastTarget=true;
                var joystick=joystickArt.gameObject.AddComponent<HubJoystick>();
                var knob=Art(joystick.transform,"Knob",ArcadePaperGraphic.Shape.PortraitMedal,Gold,.33f,.33f,.67f,.67f);Set(joystick,"knob",knob.rectTransform);
                var hint=Text(home.transform,"Attraction",null,37,Ink,.36f,.122f,.97f,.183f);
                var enter=Button(home.transform,"Enter attraction",null,Cream,.37f,.035f,.955f,.117f);enter.interactable=false;
                var enterColors=enter.colors;enterColors.disabledColor=Color.white;enter.colors=enterColors;
                Set(park,"joystick",joystick);Set(park,"enterButton",enter);Set(park,"attractionLabel",hint);Set(park,"actionLabel",enter.GetComponentInChildren<TMP_Text>());Set(park,"homeInput",home.GetComponent<CanvasGroup>());
                Set(hub,"playButton",enter);
                var account=Ref<Button>(hub,"accountButton");account.transform.SetParent(Ref<GameObject>(hub,"settingsPanel").transform,false);
                account.gameObject.SetActive(true);Box(account,.15f,.14f,.85f,.2f);
                BuildRoster(hub,characters);
                Save(root,Hub);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void BuildRoster(HubView hub,GameObject panel)
        {
            foreach(var child in panel.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            Box(panel.transform,0,0,1,1);panel.GetComponent<Image>().color=Cream;
            Art(panel.transform,"Paper",ArcadePaperGraphic.Shape.Paper,Cream,0,0,1,1);
            var back=Button(panel.transform,"Back","common.back",Cream,.045f,.925f,.25f,.978f);
            Text(panel.transform,"Title","park.friends",62,Teal,.29f,.918f,.93f,.987f);
            Text(panel.transform,"Hint","park.friend_hint",34,Ink,.05f,.86f,.95f,.912f);
            var count=Text(panel.transform,"Count",null,29,Ink,.05f,.805f,.95f,.85f);
            var viewport=Node(panel.transform,"Friends viewport",typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));Box(viewport.transform,.05f,.055f,.95f,.795f);
            viewport.GetComponent<Image>().color=new Color(1,1,1,0);viewport.GetComponent<Image>().raycastTarget=true;
            var content=(RectTransform)Node(viewport.transform,"Friends").transform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
            var roster=panel.GetComponent<CharacterRosterView>()??panel.AddComponent<CharacterRosterView>();Set(roster,"content",content);Set(roster,"scroll",scroll);Set(roster,"countLabel",count);Set(hub,"roster",roster);
            var data=new SerializedObject(hub);data.FindProperty("characterCards").arraySize=0;var backs=data.FindProperty("backButtons");backs.GetArrayElementAtIndex(0).objectReferenceValue=back;data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
