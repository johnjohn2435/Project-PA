using System;
using System.IO;
using PocketArcade.Core;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace PocketArcade.Editor
{
    public static class UIFrameSetup
    {
        private const string UI = "Assets/_Project/Resources/UI/";
        private const string BootPopup = "Assets/_Project/Prefabs/UI/BootDialog.prefab";
        private static string Text(string key) => StringTableSetup.Text("ui.error."+key);
        private static void Set(Object target,string field,Object value)
        {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
        private static void Key(UIBase window,string key,bool independent=false,int order=100)
        {
            var data=new SerializedObject(window);
            data.FindProperty("key").stringValue=key;
            data.FindProperty("placement").enumValueIndex=(int)(independent?Placement.Independent:Placement.Frame);
            data.FindProperty("orderPolicy").enumValueIndex=(int)(independent?OrderPolicy.Fixed:OrderPolicy.Managed);
            data.FindProperty("blocksInput").boolValue=!independent;
            data.FindProperty("acceptsRaycasts").boolValue=!independent;
            data.ApplyModifiedPropertiesWithoutUndo();
            var canvas=window.GetComponent<Canvas>(); if(canvas==null)canvas=window.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting=true;canvas.sortingOrder=order;
            if(window.GetComponent<CanvasGroup>()==null)window.gameObject.AddComponent<CanvasGroup>();
            if(window.GetComponent<GraphicRaycaster>()==null)window.gameObject.AddComponent<GraphicRaycaster>();
            window.gameObject.SetActive(false);
            if(PrefabUtility.IsPartOfPrefabInstance(window))PrefabUtility.RecordPrefabInstancePropertyModifications(window);
        }
        private static void Change(string path,Action<GameObject> change,string backup)
        {
            Backup(path,backup);
            var root=PrefabUtility.LoadPrefabContents(path);
            try {change(root);PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);if(!success)throw new IOException(path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Backup(string path,string folder)
        {var target=folder+"/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target,true);}
        [MenuItem("Pocket Arcade/UI Frames/1. Migrate Prefabs And Hosts")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("play"));
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string backup="Logs/UIFramesBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            var original=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var page in new[]{UIPage.Home,UIPage.Characters,UIPage.Games,UIPage.Settings})
                    Change(UI+"Panels/"+page+".prefab",root=>Key(root.GetComponent<UIPanel>()??root.AddComponent<UIPanel>(),UIManager.Key(page)),backup);
                Change(UI+"Screens/Hub.prefab",root=>
                {
                    if(root.GetComponent<CanvasGroup>()==null)root.AddComponent<CanvasGroup>();
                    foreach(var toggle in root.GetComponentsInChildren<Toggle>(true))
                    {
                        var view=toggle.GetComponent<ToggleView>()??toggle.gameObject.AddComponent<ToggleView>();
                        Set(view,"label",toggle.GetComponentInChildren<TMP_Text>(true));
                    }
                    // Existing scene overrides can otherwise reactivate a nested panel.
                    foreach(var panel in root.GetComponentsInChildren<UIPanel>(true))Key(panel,panel.Key);
                    root.SetActive(true);
                },backup);
                Change(UI+"Screens/GameEntry.prefab",root=>Key(root.GetComponent<UIPanel>()??root.AddComponent<UIPanel>(),UIManager.Key(UIPage.GameEntry)),backup);
                foreach(var name in new[]{"MessageBox","Popup","Toast","Tooltip"})
                    Change(UI+"Overlays/"+name+".prefab",root=>
                    {var view=root.GetComponent<UIBase>();if(view==null)throw new InvalidOperationException(Text("definition"));Key(view,"UI/Overlays/"+name,name=="Toast"||name=="Tooltip",name=="Tooltip"?10010:10000);},backup);
                CreateBootDialog(backup);
                foreach(var sceneName in new[]{SceneIds.BootScene,SceneIds.SelectScene,SceneIds.GameScene})
                {
                    var path=SceneIds.PathFor(sceneName);Backup(path,backup);
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        foreach(var host in root.GetComponentsInChildren<UISceneHost>(true))
                        {
                            var live=Root(host.transform,"Live",true);var staging=Root(host.transform,"Inactive",false);
                            Set(host,"liveRoot",live);Set(host,"inactiveRoot",staging);
                            var data=new SerializedObject(host);data.FindProperty("rootKey").stringValue=UIManager.Key(sceneName==SceneIds.SelectScene?UIPage.Home:UIPage.GameEntry);data.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    if(sceneName==SceneIds.BootScene)
                    {
                        Canvas canvas=null;BootDialogSystem existing=null;
                        foreach(var root in scene.GetRootGameObjects())
                        {if(canvas==null)canvas=root.GetComponentInChildren<Canvas>(true);if(existing==null)existing=root.GetComponentInChildren<BootDialogSystem>(true);}
                        if(canvas==null)throw new InvalidOperationException(Text("host"));
                        if(existing==null)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BootPopup),canvas.transform);
                    }
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException(path);
                }
                AssetDatabase.SaveAssets();Validate();
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(original);}
            Debug.Log(StringTableSetup.Format("ui.error.setup",backup));
        }
        private static RectTransform Root(Transform parent,string name,bool active)
        {
            var existing=parent.Find(name);
            var rect=existing!=null?(RectTransform)existing:(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.gameObject.SetActive(active);return rect;
        }
        private static void CreateBootDialog(string backup)
        {
            if(File.Exists(BootPopup)){Backup(BootPopup,backup);return;}
            var root=new GameObject("BootDialog",typeof(RectTransform));
            try
            {
                var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var content=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UI+"Overlays/MessageBox.prefab"),root.transform,false);
                var message=content.GetComponent<MessageBoxView>();var data=new SerializedObject(message);
                var title=data.FindProperty("title").objectReferenceValue;var body=data.FindProperty("body").objectReferenceValue;
                var confirm=data.FindProperty("confirm").objectReferenceValue;var cancel=data.FindProperty("cancel").objectReferenceValue;
                Object.DestroyImmediate(message);
                content.GetComponent<Canvas>().sortingOrder=30000;content.SetActive(false);
                var system=root.AddComponent<BootDialogSystem>();Set(system,"content",content);Set(system,"title",title);Set(system,"body",body);Set(system,"confirm",confirm);Set(system,"cancel",cancel);
                var label=((Button)confirm).GetComponentInChildren<TMP_Text>(true);var binding=label.GetComponent<LocalizedText>();
                if(binding!=null)binding.SetKey("common.retry");label.text=StringTableSetup.Text("common.retry");
                Directory.CreateDirectory(Path.GetDirectoryName(BootPopup));PrefabUtility.SaveAsPrefabAsset(root,BootPopup,out bool success);if(!success)throw new IOException(BootPopup);
            }
            finally {Object.DestroyImmediate(root);}
        }
        [MenuItem("Pocket Arcade/UI Frames/2. Validate Frames")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("play"));
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var bootPopup=AssetDatabase.LoadAssetAtPath<GameObject>(BootPopup);
            if(bootPopup==null || bootPopup.GetComponent<BootDialogSystem>()==null || bootPopup.GetComponentInChildren<UIBase>(true)!=null)
                throw new InvalidOperationException(Text("definition"));
            foreach(var path in new[]{"Screens/Hub","Screens/GameEntry","Overlays/MessageBox","Overlays/Popup","Overlays/Toast","Overlays/Tooltip"})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(UI+path+".prefab");
                var keys=new System.Collections.Generic.HashSet<string>();
                if(root==null||root.GetComponentsInChildren<UIBase>(true).Length==0)throw new InvalidOperationException(Text("definition"));
                foreach(var window in root.GetComponentsInChildren<UIBase>(true))
                {window.ValidateDefinition();if(!keys.Add(window.Key))throw new InvalidOperationException(Text("key"));}
            }
            var original=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var name in new[]{SceneIds.SelectScene,SceneIds.GameScene})
                {
                    var scene=EditorSceneManager.OpenScene(SceneIds.PathFor(name),OpenSceneMode.Single);int count=0;
                    foreach(var root in scene.GetRootGameObjects())foreach(var host in root.GetComponentsInChildren<UISceneHost>(true))
                    {
                        count++;var data=new SerializedObject(host);
                        var live=data.FindProperty("liveRoot").objectReferenceValue as Transform;var inactive=data.FindProperty("inactiveRoot").objectReferenceValue as Transform;
                        if(live==null||inactive==null||inactive.gameObject.activeInHierarchy||string.IsNullOrEmpty(data.FindProperty("rootKey").stringValue))throw new InvalidOperationException(Text("host"));
                    }
                    if(count!=1)throw new InvalidOperationException(Text("host"));
                }
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(original);}
            Debug.Log(Text("validate"));
        }
    }
}
