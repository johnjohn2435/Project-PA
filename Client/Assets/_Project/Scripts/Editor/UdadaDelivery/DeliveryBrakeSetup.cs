using System;
using System.IO;
using PocketArcade.Editor;
using PocketArcade.Meta;
using PocketArcade.UI;
using PocketArcade.UdadaDelivery.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PocketArcade.UdadaDelivery.Editor
{
    public static partial class DeliverySetup
    {
        private const string HouseFile=ResourcesRoot+"MiniGames/UdadaDelivery/House.prefab";
        private const string ParcelFile=ResourcesRoot+"MiniGames/UdadaDelivery/Parcel.prefab";
        private static GameObject HousePrefab()
        {
            if(File.Exists(HouseFile))return AssetDatabase.LoadAssetAtPath<GameObject>(HouseFile);
            var root=new GameObject("House");
            try
            {
                Sprite(root.transform,"Body",new Vector3(0,.8f,0),new Vector3(2.1f,2f,1),new Color(.86f,.74f,.56f),1);
                Sprite(root.transform,"Roof",new Vector3(0,1.85f,0),new Vector3(2.5f,.3f,1),new Color(.6f,.36f,.27f),2);
                var door=Sprite(root.transform,"Door",new Vector3(0,.27f,0),new Vector3(.64f,1f,1),new Color(.2f,.35f,.3f),3);
                var zone=Sprite(root.transform,"LandingZone",new Vector3(0,-.21f,0),new Vector3(2,.08f,1),new Color(.35f,.75f,.5f),5);
                var perfect=Sprite(root.transform,"PerfectZone",new Vector3(0,-.16f,0),new Vector3(.6f,.08f,1),new Color(.85f,.95f,.64f),6);
                var view=root.AddComponent<DeliveryHouseView>();Set(view,"door",door);Set(view,"landingZone",zone);Set(view,"perfectZone",perfect);Save(root,HouseFile);
            }
            finally{Object.DestroyImmediate(root);}
            return AssetDatabase.LoadAssetAtPath<GameObject>(HouseFile);
        }
        private static GameObject ParcelPrefab()
        {
            if(File.Exists(ParcelFile))return AssetDatabase.LoadAssetAtPath<GameObject>(ParcelFile);
            var root=new GameObject("ParcelVisual");
            try
            {
                Sprite(root.transform,"Box",Vector3.zero,new Vector3(.34f,.32f,1),new Color(.82f,.55f,.24f),30);
                Sprite(root.transform,"Tape",Vector3.zero,new Vector3(.07f,.32f,1),new Color(.98f,.87f,.61f),31);Save(root,ParcelFile);
            }
            finally{Object.DestroyImmediate(root);}
            return AssetDatabase.LoadAssetAtPath<GameObject>(ParcelFile);
        }
        private static void CreateBrakeWorld(bool replace)
        {
            string path=ResourcesRoot+DeliveryHud.WorldPath+".prefab";
            if(File.Exists(path)&&!replace)return;
            var tuning=Asset<DeliveryTuning>(TuningFile);var rules=tuning.CreateRules();rules.Validate();
            var house=HousePrefab();var parcel=ParcelPrefab();
            var root=new GameObject("BrakeDeliveryPlayfield");
            try
            {
                Sprite(root.transform,"Road",new Vector3(500,-.45f,0),new Vector3(1200,.4f,1),new Color(.68f,.73f,.66f),0);
                var cameraObject=new GameObject("PlayCamera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);cameraObject.tag="MainCamera";
                var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,0,-10);
                camera.backgroundColor=new Color(.96f,.95f,.91f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.1f;camera.farClipPlane=50;
                if(!File.Exists(PlayerFile))
                {
                    var p=new GameObject("PlayerVisual");
                    try{Sprite(p.transform,"Square",Vector3.zero,new Vector3(.48f,.48f,1),new Color(.2f,.55f,.75f),20);Save(p,PlayerFile);}finally{Object.DestroyImmediate(p);}
                }
                var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFile),root.transform);
                var parcelInstance=(GameObject)PrefabUtility.InstantiatePrefab(parcel,root.transform);
                var view=root.AddComponent<DeliveryWorldView>();Set(view,"playCamera",camera);Set(view,"playerVisual",player.transform);Set(view,"player",player.GetComponentInChildren<SpriteRenderer>());
                Set(view,"parcelVisual",parcelInstance.transform);Set(view,"parcel",parcelInstance.GetComponentInChildren<SpriteRenderer>());
                var preview=Sprite(root.transform,"LandingPreview",Vector3.zero,new Vector3(.12f,.34f,1),new Color(.9f,.58f,.22f),25);Set(view,"landingPreview",preview);
                var carried=new SpriteRenderer[3];for(int i=0;i<3;i++)carried[i]=Sprite(root.transform,"CarriedBox_"+i,new Vector3(0,.52f+i*.31f,0),new Vector3(.32f,.28f,1),new Color(.82f,.55f,.24f),21+i);
                SetArray(view,"carriedBoxes",carried);
                var marks=Pool(root.transform,"RoadMark_",30,new Color(.65f,.7f,.63f),2);foreach(var mark in marks)mark.transform.localScale=new Vector3(.5f,.05f,1);SetArray(view,"roadMarks",marks);
                var so=new SerializedObject(view);var houses=so.FindProperty("houses");houses.arraySize=rules.HouseCount;
                for(int i=0;i<rules.HouseCount;i++)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(house,root.transform);instance.name="House_"+i;
                    instance.transform.localPosition=new Vector3((float)(rules.FirstHouse+i*rules.HouseSpacing),0,0);
                    instance.transform.Find("LandingZone").localScale=new Vector3((float)rules.DoorTolerance*2,.08f,1);
                    instance.transform.Find("PerfectZone").localScale=new Vector3((float)rules.PerfectTolerance*2,.08f,1);
                    houses.GetArrayElementAtIndex(i).objectReferenceValue=instance.GetComponent<DeliveryHouseView>();
                }
                so.ApplyModifiedPropertiesWithoutUndo();Save(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/Udada Delivery/4. Apply Brake Delivery Experiment")]
        public static void ApplyBrakePrototype()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            string backup="Logs/UdadaDeliveryBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(string file in new[]{ResourcesRoot+DeliveryHud.WorldPath+".prefab",TuningFile,DefinitionFile})if(File.Exists(file))File.Copy(file,backup+"/"+Path.GetFileName(file));
            var tuning=Asset<DeliveryTuning>(TuningFile);var so=new SerializedObject(tuning);
            foreach(var field in new[]{"duration","runSpeed","acceleration","deceleration","parcelDrag","minimumLaunchSpeed","doorTolerance","perfectTolerance","firstHouse","houseSpacing"})
            {
                float value=field=="duration"?45:field=="runSpeed"?4:field=="acceleration"?12:field=="deceleration"?8:field=="parcelDrag"?2:field=="minimumLaunchSpeed"?2.5f:field=="doorTolerance"?1:field=="perfectTolerance"?.3f:field=="firstHouse"?9:14;
                so.FindProperty(field).floatValue=value;
            }
            so.FindProperty("houseCount").intValue=3;so.ApplyModifiedPropertiesWithoutUndo();CreateBrakeWorld(true);
            var definition=AssetDatabase.LoadAssetAtPath<MiniGameDefinition>(DefinitionFile);so=new SerializedObject(definition);so.FindProperty("scoreKey").stringValue=DeliverySession.ScoreId;so.ApplyModifiedPropertiesWithoutUndo();
            RegisterResources();AssetDatabase.SaveAssets();StringTableSetup.Apply();Validate();Debug.Log(StringTableSetup.Format("ud.brake.setup",backup));
        }
    }
}
