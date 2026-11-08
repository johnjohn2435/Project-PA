using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Characters;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PocketArcade.Editor
{
    public static class SharedCharacterSetup
    {
        [Serializable] private sealed class Spec { public float pixels_per_unit;public Bone[] bones;public string[] draw_order; }
        [Serializable] private sealed class Bone { public string name,parent;public float[] unity_local_position,sprite_pivot_normalized; }
        private const string Art="Assets/_Project/Art/Characters/SharedRig/";
        private const string Resources="Assets/_Project/Resources/";
        private static readonly string[] Ids={"char_cat","char_dog","char_hamster"};
        private static readonly string[] Old={"char_momo","char_bori","char_nabi"};
        private static readonly string[] Skins={"goyeongi","dog","hamster"};
        private static void Set(Object target,string field,Object value)
        {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
        private static void Move(string from,string to)
        {
            if(!File.Exists(from)&&!Directory.Exists(from))return;
            if(File.Exists(to)||Directory.Exists(to))throw new InvalidOperationException(StringTableSetup.Format("ui.error.key",to));
            string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new IOException(error);
        }
        private static void MigrateIds()
        {
            for(int i=0;i<Ids.Length;i++)
            {
                Move(Resources+"Characters/"+Old[i],Resources+"Characters/"+Ids[i]);
                Move("Assets/_Project/ScriptableObjects/Characters/"+Old[i]+".asset","Assets/_Project/ScriptableObjects/Characters/"+Ids[i]+".asset");
                Move("Assets/_Project/Art/Characters/Dummy/"+Old[i]+"_dummy.png","Assets/_Project/Art/Characters/Dummy/"+Ids[i]+"_dummy.png");
                var character=AssetDatabase.LoadAssetAtPath<PocketArcade.Meta.CharacterDefinition>("Assets/_Project/ScriptableObjects/Characters/"+Ids[i]+".asset");
                if(character==null)throw new InvalidOperationException(StringTableSetup.Text("bg.error.references"));
                character.name=Ids[i];
                var data=new SerializedObject(character);data.FindProperty("id").stringValue=Ids[i];data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Resources+"UI"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                try
                {
                    foreach(var label in root.GetComponentsInChildren<LocalizedText>(true))for(int i=0;i<Ids.Length;i++)
                    {
                        if(label.Key!="character."+Old[i]+".name")continue;
                        var data=new SerializedObject(label);data.FindProperty("key").stringValue="character."+Ids[i]+".name";data.ApplyModifiedPropertiesWithoutUndo();
                        label.GetComponent<TMP_Text>().text=StringTableSetup.Text("character."+Ids[i]+".name");
                        if(PrefabUtility.IsPartOfPrefabInstance(label))PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                        changed=true;
                    }
                    if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        private static int PreviewLayer()
        {
            var data=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=data.FindProperty("layers");
            for(int i=8;i<32;i++)if(layers.GetArrayElementAtIndex(i).stringValue=="CharacterPreview")return i;
            for(int i=31;i>=8;i--)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {layers.GetArrayElementAtIndex(i).stringValue="CharacterPreview";data.ApplyModifiedPropertiesWithoutUndo();return i;}
            throw new InvalidOperationException(StringTableSetup.Text("character.error.layer"));
        }
        private static AnimationClip Clip(string name,float duration,bool walking,float baseY,float ppu)
        {
            string path=Art+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing!=null)return existing;
            var clip=new AnimationClip{name=name,frameRate=30};var curve=new AnimationCurve();
            for(int i=0;i<=32;i++){float t=duration*i/32f,phase=2*Mathf.PI*i/32f;curve.AddKey(t,baseY+(walking?4*Mathf.Abs(Mathf.Sin(phase)):2*Mathf.Sin(phase))/ppu);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("body",typeof(Transform),"m_LocalPosition.y"),curve);
            foreach(var part in new[]{"arm_L","arm_R","leg_L","leg_R"})
            {
                float amplitude=walking?(part=="arm_L"?-18:part=="arm_R"?18:part=="leg_L"?22:-22):0;curve=new AnimationCurve();
                for(int i=0;i<=32;i++)curve.AddKey(duration*i/32f,amplitude*Mathf.Sin(2*Mathf.PI*i/32f));
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("body/"+part,typeof(Transform),"localEulerAnglesRaw.z"),curve);
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,path);return clip;
        }
        [MenuItem("Pocket Arcade/Characters/1. Import Shared Rigs")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(StringTableSetup.Text("bg.error.play"));
            string repo=Directory.GetParent(Application.dataPath).Parent.FullName;
            string source=Path.Combine(repo,"ArtPipeline/Rigging/Characters_20261105");
            var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(Path.Combine(source,"shared_rig_unity.json")));
            string backup="Logs/CharacterRigBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(var path in Directory.GetFiles("Assets/_Project/ScriptableObjects/Characters","*.asset"))File.Copy(path,backup+"/"+Path.GetFileName(path),true);
            File.Copy("Assets/_Project/Resources/UI/Screens/Hub.prefab",backup+"/Hub.prefab");
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();MigrateIds();int layer=PreviewLayer();
            for(int i=0;i<Ids.Length;i++)
            {
                string folder=Resources+"Characters/"+Ids[i]+"/";Directory.CreateDirectory(folder+"Parts");
                foreach(var bone in spec.bones)if(!File.Exists(folder+"Parts/"+bone.name+".png"))File.Copy(Path.Combine(source,Skins[i],bone.name+".png"),folder+"Parts/"+bone.name+".png");
                if(!File.Exists(folder+"Portrait.png"))File.Copy(Path.Combine(source,Skins[i],"assembled_right.png"),folder+"Portrait.png");
            }
            AssetDatabase.Refresh();
            foreach(var id in Ids)foreach(var bone in spec.bones)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Resources+"Characters/"+id+"/Parts/"+bone.name+".png");
                Import(importer,spec.pixels_per_unit,new Vector2(bone.sprite_pivot_normalized[0],bone.sprite_pivot_normalized[1]));
            }
            foreach(var id in Ids)Import((TextureImporter)AssetImporter.GetAtPath(Resources+"Characters/"+id+"/Portrait.png"),spec.pixels_per_unit,new Vector2(.5f,.5f));
            var body=Array.Find(spec.bones,b=>b.name=="body");var idle=Clip("Idle",1,false,body.unity_local_position[1],spec.pixels_per_unit);var walk=Clip("Walk",.64f,true,body.unity_local_position[1],spec.pixels_per_unit);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Art+"SharedCharacter.controller");
            if(controller==null)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(Art+"SharedCharacter.controller");controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
                var machine=controller.layers[0].stateMachine;var a=machine.AddState("Idle");a.motion=idle;var b=machine.AddState("Walk");b.motion=walk;machine.defaultState=a;
                var t=a.AddTransition(b);t.hasExitTime=false;t.duration=.1f;t.AddCondition(AnimatorConditionMode.Greater,.05f,"Speed");
                t=b.AddTransition(a);t.hasExitTime=false;t.duration=.1f;t.AddCondition(AnimatorConditionMode.Less,.05f,"Speed");
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            foreach(var id in Ids)
            {
                string path=Resources+"Characters/"+id+"/Character.prefab";if(File.Exists(path))continue;
                var root=new GameObject(id);root.layer=layer;
                try
                {
                    var transforms=new Dictionary<string,Transform>{{"root",root.transform}};
                    foreach(var bone in spec.bones)
                    {
                        var node=new GameObject(bone.name);node.layer=layer;node.transform.SetParent(transforms[bone.parent],false);var p=bone.unity_local_position;node.transform.localPosition=new Vector3(p[0],p[1],p[2]);transforms.Add(bone.name,node.transform);
                        var renderer=node.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Resources+"Characters/"+id+"/Parts/"+bone.name+".png");renderer.sortingOrder=Array.IndexOf(spec.draw_order,bone.name);renderer.sharedMaterial=material;
                    }
                    var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                    Set(root.AddComponent<CharacterRigController>(),"animator",animator);PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{Object.DestroyImmediate(root);}
            }
            AttachPreview(layer);AssetDatabase.SaveAssets();
        }
        private static void Import(TextureImporter importer,float ppu,Vector2 pivot)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        private static void AttachPreview(int layer)
        {
            string texturePath=Art+"Preview.renderTexture";var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(texturePath);
            if(texture==null){texture=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32){name="CharacterPreview"};AssetDatabase.CreateAsset(texture,texturePath);}
            string cameraPath=Resources+CharacterRigPreview.CameraPath+".prefab";Directory.CreateDirectory(Path.GetDirectoryName(cameraPath));
            if(!File.Exists(cameraPath))
            {
                var world=new GameObject("CharacterPreview");world.transform.position=new Vector3(1000,0,0);
                try
                {
                    var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.SetParent(world.transform,false);camera.transform.localPosition=new Vector3(0,1.4f,-10);camera.orthographic=true;camera.orthographicSize=1.55f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<layer;camera.targetTexture=texture;
                    var extra=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");if(extra!=null)camera.gameObject.AddComponent(extra);
                    new GameObject("Anchor").transform.SetParent(world.transform,false);PrefabUtility.SaveAsPrefabAsset(world,cameraPath);
                }
                finally{Object.DestroyImmediate(world);}
            }
            const string hubPath="Assets/_Project/Resources/UI/Screens/Hub.prefab";var hub=PrefabUtility.LoadPrefabContents(hubPath);
            try
            {
                var view=hub.GetComponent<HubView>();var data=new SerializedObject(view);var portrait=data.FindProperty("portrait").objectReferenceValue as Image;
                var preview=portrait.GetComponentInChildren<CharacterRigPreview>(true);
                if(preview==null)
                {
                    var node=new GameObject("RigPreview",typeof(RectTransform),typeof(RawImage),typeof(CharacterRigPreview));node.transform.SetParent(portrait.transform,false);var rect=(RectTransform)node.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;preview=node.GetComponent<CharacterRigPreview>();
                }
                var image=preview.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;Set(preview,"target",image);Set(view,"rigPreview",preview);PrefabUtility.SaveAsPrefabAsset(hub,hubPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(hub);}
        }
    }
}
