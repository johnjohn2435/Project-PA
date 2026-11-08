using System;
using System.IO;
using PocketArcade.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace PocketArcade.Editor
{
    public static class IndependentManagerSetup
    {
        private const string Folder = "Assets/_Project/Prefabs/Managers/";
        private static readonly string[] Fields = { "resourcePrefab", "dataPrefab", "soundPrefab", "sceneFlowPrefab", "uiPrefab" };
        private static readonly Type[] Types = { typeof(ResourceManager), typeof(DataManager), typeof(SoundManager), typeof(SceneFlowManager), typeof(UIManager) };
        [MenuItem("Pocket Arcade/Boot/3. Separate Manager Prefabs")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string backup = "Logs/IndependentManagersBackup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup); File.Copy(SceneIds.PathFor(SceneIds.BootScene), backup + "/BootScene.unity");
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.BootScene), OpenSceneMode.Single);
                ConfigureScene(scene); ValidateScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save BootScene.");
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("Independent manager prefabs configured. BootScene creates five DDOL roots. Backup: " + backup);
        }
        private static GameBootstrap FindBootstrap(Scene scene)
        {
            GameBootstrap found = null;
            foreach (var root in scene.GetRootGameObjects()) foreach (var bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
            { if (found != null) throw new InvalidOperationException("Multiple bootstraps."); found = bootstrap; }
            if (found == null) throw new InvalidOperationException("BootScene needs GameBootstrap."); return found;
        }
        public static void ConfigureScene(Scene scene)
        {
            var old = FindBootstrap(scene);
            var oldData = new SerializedObject(old);
            var catalog = oldData.FindProperty("catalog").objectReferenceValue;
            if (catalog == null) throw new InvalidOperationException("Game catalog is missing.");
            if (old.transform.childCount != 0) throw new InvalidOperationException("Move unrelated children off the old manager root before migration.");
            foreach (var script in old.GetComponents<MonoBehaviour>())
                if (script != old && !(script is PersistentManager)) throw new InvalidOperationException("Unexpected script on old manager root; preserve it separately before migration.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var prefabs = new PersistentManager[Types.Length];
            for (int i = 0; i < Types.Length; i++) prefabs[i] = EnsurePrefab(Types[i], old);
            GameBootstrap target = old;
            if (old.GetComponents<PersistentManager>().Length > 0 || PrefabUtility.IsPartOfPrefabInstance(old))
            {
                target = new GameObject("Bootstrap", typeof(GameBootstrap)).GetComponent<GameBootstrap>();
                SceneManager.MoveGameObjectToScene(target.gameObject, scene);
            }
            target.gameObject.name = "Bootstrap";
            var data = new SerializedObject(target); data.FindProperty("catalog").objectReferenceValue = catalog;
            for (int i = 0; i < Fields.Length; i++) data.FindProperty(Fields[i]).objectReferenceValue = prefabs[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            if (target != old) Object.DestroyImmediate(old.gameObject);
            const string legacy = "Assets/_Project/Prefabs/Common/GameManagers.prefab";
            const string retired = "Assets/_Project/Prefabs/Legacy/GameManagers.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(legacy) != null && AssetDatabase.LoadAssetAtPath<GameObject>(retired) == null)
            {
                Directory.CreateDirectory("Assets/_Project/Prefabs/Legacy"); AssetDatabase.Refresh();
                string error = AssetDatabase.MoveAsset(legacy, retired);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            }
        }
        private static PersistentManager EnsurePrefab(Type type, GameBootstrap old)
        {
            string path = Folder + type.Name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var existing = prefab.GetComponent(type) as PersistentManager;
                GameBootstrap.ValidatePrefab(existing); return existing;
            }
            var go = new GameObject(type.Name); go.SetActive(false);
            try
            {
                var manager = (PersistentManager)go.AddComponent(type);
                // Preserve the only authored manager setting; never copy native ownership/hierarchy fields.
                if (type == typeof(SoundManager) && old.TryGetComponent<SoundManager>(out var sound))
                {
                    var source = new SerializedObject(sound); var target = new SerializedObject(manager);
                    target.FindProperty("uiClickClip").objectReferenceValue = source.FindProperty("uiClickClip").objectReferenceValue;
                    target.ApplyModifiedPropertiesWithoutUndo();
                }
                go.SetActive(true);
                var saved = PrefabUtility.SaveAsPrefabAsset(go,path,out bool success);
                if (!success) throw new IOException("Could not save " + path);
                return (PersistentManager)saved.GetComponent(type);
            }
            finally { Object.DestroyImmediate(go); }
        }
        public static void ValidateAssets()
        {
            foreach (var type in Types)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + type.Name + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Run Boot > 3. Separate Manager Prefabs.");
                GameBootstrap.ValidatePrefab(prefab.GetComponent(type) as PersistentManager);
            }
        }
        public static void ValidateScene(Scene scene)
        {
            var bootstrap = FindBootstrap(scene); var data = new SerializedObject(bootstrap);
            if (bootstrap.transform.parent != null || bootstrap.GetComponents<PersistentManager>().Length != 0)
                throw new InvalidOperationException("Bootstrap must be a scene-local root without managers.");
            if (data.FindProperty("catalog").objectReferenceValue == null) throw new InvalidOperationException("Missing GameCatalog.");
            foreach (string field in Fields)
            {
                var prefab = data.FindProperty(field).objectReferenceValue as PersistentManager;
                GameBootstrap.ValidatePrefab(prefab);
                if (!EditorUtility.IsPersistent(prefab)) throw new InvalidOperationException(field + " must reference a prefab asset, not a scene object.");
            }
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentsInChildren<PersistentManager>(true).Length != 0)
                    throw new InvalidOperationException("Managers are spawned at runtime; remove preplaced scene managers.");
        }
    }
}
