using System;
using System.IO;
using PocketArcade.Editor;
using PocketArcade.TiltFloor.Unity;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace PocketArcade.TiltFloor.Editor
{
    public static partial class FloorSetup
    {
        private const int Segments=64;
        private static Mesh DiscMesh(string file,Color color)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(file);if(mesh!=null)return mesh;
            mesh=new Mesh{name=Path.GetFileNameWithoutExtension(file)};var v=new Vector3[Segments+1];var uv=new Vector2[v.Length];var colors=new Color[v.Length];var triangles=new int[Segments*3];colors[0]=color;uv[0]=Vector2.one*.5f;
            for(int i=0;i<Segments;i++){float a=i*Mathf.PI*2/Segments;v[i+1]=new Vector3(Mathf.Cos(a)*3.2f,Mathf.Sin(a)*3.2f*.66f,.1f);uv[i+1]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.5f+Vector2.one*.5f;colors[i+1]=color;triangles[i*3]=0;triangles[i*3+1]=(i+1)%Segments+1;triangles[i*3+2]=i+1;}
            mesh.vertices=v;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,file);return mesh;
        }
        private static void CircleWorld(FloorTuning tuning,bool replace)
        {
            string file=R+FloorHud.WorldPath+".prefab";if(File.Exists(file)&&!replace)return;
            Directory.CreateDirectory(A);AssetDatabase.Refresh();var material=AssetDatabase.LoadAssetAtPath<Material>(A+"Board.mat");
            if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Sprite>(R+"MiniGames/BlockGravity/Cell.png").texture;AssetDatabase.CreateAsset(material,A+"Board.mat");}
            var disc=DiscMesh(A+"Disc.asset",new Color(.88f,.91f,.8f));var shadow=DiscMesh(A+"DiscShadow.asset",new Color(.2f,.28f,.22f,.15f));var rules=tuning.Rules();
            var root=new GameObject("DiscSurvivalPlayfield");
            try
            {
                var cameraObject=new GameObject("PlayCamera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,.2f,-10);camera.backgroundColor=new Color(.96f,.95f,.91f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.1f;camera.farClipPlane=50;
                var view=root.AddComponent<FloorWorldView>();Set(view,"playCamera",camera);
                foreach(bool isShadow in new[]{true,false}){var board=new GameObject(isShadow?"Shadow":"Surface",typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(root.transform,false);board.GetComponent<MeshFilter>().sharedMesh=isShadow?shadow:disc;board.GetComponent<MeshRenderer>().sharedMaterial=material;board.GetComponent<MeshRenderer>().sortingOrder=isShadow?0:1;if(isShadow){board.transform.localPosition=new Vector3(0,-.35f,.1f);board.transform.localScale=new Vector3((float)rules.HalfSize/3.2f,(float)rules.HalfSize/3.2f,1);}else Set(view,"surface",board.GetComponent<MeshFilter>());}
                Set(view,"player",Sprite(root.transform,"PlayerAnchor",new Color(.2f,.57f,.8f),30,new Vector2(.43f,.43f)));Set(view,"pulse",Sprite(root.transform,"BonusPulse",new Color(.4f,.85f,.55f,.5f),25,Vector2.one));
                Array(view,"borders",Pool(root.transform,"Rim_",Segments,Color.gray,10,Vector2.one));Array(view,"legs",Pool(root.transform,"Support_",4,new Color(.4f,.43f,.38f),3,Vector2.one));Array(view,"grid",Pool(root.transform,"Grid_",10,new Color(.64f,.73f,.61f,.4f),2,Vector2.one));Array(view,"arrow",Pool(root.transform,"SlopeArrow_",3,Color.gray,12,Vector2.one));
                Array(view,"bodies",Pool(root.transform,"EnemyOrBonus_",rules.Capacity,Color.red,20,Vector2.one));Array(view,"warnings",Pool(root.transform,"DropWarning_",rules.MaxEnemies,new Color(.85f,.22f,.18f,.35f),12,Vector2.one));Save(root,file);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Tilt Floor/3. Apply Disc Survival")]
        public static void ApplySurvival()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));string backup="Logs/TiltFloorBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(string p in new[]{R+FloorHud.WorldPath+".prefab",TuningFile})if(File.Exists(p))File.Copy(p,backup+"/"+Path.GetFileName(p));
            CircleWorld(Asset<FloorTuning>(TuningFile),true);
            var hud=PrefabUtility.LoadPrefabContents(R+FloorHud.ScreenKey+".prefab");
            try
            {
                var label=hud.transform.Find("HUD/Balance").GetComponent<TMPro.TMP_Text>();label.fontSize=24;((RectTransform)label.transform).sizeDelta=new Vector2(920,90);
                hud.transform.Find("HUD/Progress").GetComponent<TMPro.TMP_Text>().fontSize=32;
                Save(hud,R+FloorHud.ScreenKey+".prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            AssetDatabase.SaveAssets();StringTableSetup.Apply();Validate();Debug.Log(StringTableSetup.Format("floor.survival.setup",backup));
        }
    }
}
