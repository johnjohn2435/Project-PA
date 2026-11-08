using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace PocketArcade.Editor
{
    public static class BootResourceSetup
    {
        public const string ResourceRoot = "Assets/_Project/Resources/";
        public const string ManifestFile = ResourceRoot + "Boot/BootResources.json";
        private const string PrefabFile = "Assets/_Project/Prefabs/UI/BootLoadingUI.prefab";

        public static Dictionary<string, string> DiscoverResources()
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("", new[] { ResourceRoot.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == ManifestFile || AssetDatabase.IsValidFolder(path)) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null || asset is MonoScript) continue;
                string key = path.Substring(ResourceRoot.Length);
                key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
                if (found.ContainsKey(key)) throw new InvalidOperationException("Resources path collision: " + key);
                string kind = asset is GameObject ? "Prefab" : asset is AudioClip ? "AudioClip" : "Asset";
                if (AssetImporter.GetAtPath(path) is TextureImporter texture && texture.textureType == TextureImporterType.Sprite)
                {
                    if (texture.spriteImportMode != SpriteImportMode.Single) throw new InvalidOperationException("Use Sprite Mode Single for a boot resource: " + path);
                    kind = "Sprite";
                }
                found.Add(key, kind);
            }
            return found;
        }
        public static void ValidateInventory()
        {
            if (!File.Exists(ManifestFile)) throw new InvalidOperationException("Run Pocket Arcade > Boot > Configure Loading Screen And Resources.");
            var data = JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(ManifestFile));
            if (data == null || data.version != 1 || data.resources == null) throw new InvalidOperationException("Invalid boot resource manifest.");
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in data.resources)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.path)) throw new InvalidOperationException("Invalid boot resource path.");
                var type = slot.AssetType;
                if (entries.ContainsKey(slot.path)) throw new InvalidOperationException("Duplicate boot resource path: " + slot.path);
                entries.Add(slot.path, slot.kind);
            }
            foreach (var item in DiscoverResources())
                if (!entries.TryGetValue(item.Key, out var kind) || kind != item.Value)
                    throw new InvalidOperationException("Resource is not registered correctly for boot: " + item.Key + ". Run Pocket Arcade > Boot > Configure Loading Screen And Resources.");
        }
        private static void SynchronizeInventory()
        {
            var data = JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(ManifestFile));
            if (data == null || data.resources == null || data.version != 1) throw new InvalidOperationException("Invalid boot resource manifest.");
            var slots = new List<BootResourceSlot>(data.resources);
            var existing = new Dictionary<string, BootResourceSlot>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.path) || existing.ContainsKey(slot.path)) throw new InvalidOperationException("Invalid or duplicate boot resource slot.");
                existing.Add(slot.path, slot);
            }
            foreach (var item in DiscoverResources())
            {
                if (existing.TryGetValue(item.Key, out var slot)) slot.kind = item.Value;
                else slots.Add(new BootResourceSlot { path = item.Key, kind = item.Value, required = true });
            }
            slots.Sort((a, b) => string.CompareOrdinal(a.path, b.path)); data.resources = slots.ToArray();
            File.WriteAllText(ManifestFile, JsonUtility.ToJson(data, true)); AssetDatabase.ImportAsset(ManifestFile);
        }
        [MenuItem("Pocket Arcade/Boot/1. Configure Loading Screen And Resources")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string backup = "Logs/BootResourceBackup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            File.Copy(SceneIds.PathFor(SceneIds.BootScene), backup + "/BootScene.unity");
            File.Copy(ManifestFile, backup + "/BootResources.json");
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                SynchronizeInventory();
                var scene = EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.BootScene), OpenSceneMode.Single);
                BootStatusView current = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var view in root.GetComponentsInChildren<BootStatusView>(true))
                    { if (current != null) throw new InvalidOperationException("Multiple BootStatusViews."); current = view; }
                if (current == null) throw new InvalidOperationException("Run Phase 1 setup first.");
                if (!current.HasProgressBar)
                {
                    var prefab = GetOrCreatePrefab();
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, current.transform.parent);
                    instance.name = "Boot Loading UI";
                    Object.DestroyImmediate(current.gameObject);
                }
                IndependentManagerSetup.ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save BootScene.");
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("Boot loading screen and local resource inventory configured. Backup: " + backup);
        }
        [MenuItem("Pocket Arcade/Boot/2. Validate Local Resource Inventory")]
        public static void Validate()
        { ValidateInventory(); Debug.Log("All project Resources assets are registered for boot. Check loading bar and retry in Play Mode."); }
        private static GameObject GetOrCreatePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFile);
            if (existing != null)
            {
                if (existing.GetComponent<BootStatusView>() == null || !existing.GetComponent<BootStatusView>().HasProgressBar)
                    throw new InvalidOperationException("BootLoadingUI prefab needs a wired progress bar.");
                return existing;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import TMP Essential Resources first.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabFile)); AssetDatabase.Refresh();
            var root = Rect(null, "Boot Loading UI", Vector2.zero, Vector2.one);
            root.gameObject.SetActive(false);
            try
            {
                Text(root, font, "Title", StringTableSetup.Text("app.title"), new Vector2(.08f,.64f), new Vector2(.92f,.76f), 54);
                var status = Text(root, font, "Status", StringTableSetup.Text("boot.prepare"), new Vector2(.1f,.48f), new Vector2(.9f,.59f), 32);
                var track = Rect(root, "Progress", new Vector2(.12f,.41f), new Vector2(.88f,.44f));
                track.gameObject.AddComponent<Image>().color = new Color(.84f,.87f,.83f);
                var fillArea = Rect(track, "Fill Area", Vector2.zero, Vector2.one);
                var fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.one);
                fill.gameObject.AddComponent<Image>().color = new Color(.35f,.66f,.49f);
                var slider = track.gameObject.AddComponent<Slider>(); slider.fillRect = fill;
                slider.minValue = 0; slider.maxValue = 1; slider.interactable = false;
                slider.navigation = new Navigation { mode = Navigation.Mode.None }; slider.SetValueWithoutNotify(0);
                var percent = Text(root, font, "Percentage", StringTableSetup.Format("boot.percent", 0), new Vector2(.25f,.34f), new Vector2(.75f,.40f), 30);
                var retry = Rect(root, "Retry", new Vector2(.27f,.23f), new Vector2(.73f,.31f));
                retry.gameObject.AddComponent<Image>().color = new Color(.66f,.86f,.75f);
                var button = retry.gameObject.AddComponent<Button>(); button.targetGraphic = retry.GetComponent<Image>();
                Text(retry,font,"Label",StringTableSetup.Text("common.retry"),Vector2.zero,Vector2.one,30);
                var view = root.gameObject.AddComponent<BootStatusView>(); var data = new SerializedObject(view);
                data.FindProperty("label").objectReferenceValue = status;
                data.FindProperty("progressBar").objectReferenceValue = slider;
                data.FindProperty("percentage").objectReferenceValue = percent;
                data.FindProperty("retryButton").objectReferenceValue = button;
                data.ApplyModifiedPropertiesWithoutUndo(); retry.gameObject.SetActive(false);
                root.gameObject.SetActive(true);
                var result = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFile, out bool success);
                if (!success) throw new IOException("Cannot save boot UI prefab."); return result;
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect = (RectTransform)go.transform; rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static TMP_Text Text(Transform parent,TMP_FontAsset font,string name,string text,Vector2 min,Vector2 max,float size)
        {
            var rect=Rect(parent,name,min,max);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;StringTableSetup.BindKnown(label);
            label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=size;label.color=new Color(.23f,.20f,.26f);
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
    }
    [InitializeOnLoad]
    public static class BootResourcePlayGuard
    {
        static BootResourcePlayGuard() => EditorApplication.playModeStateChanged += Check;
        private static void Check(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || Application.isBatchMode) return;
            try { BootResourceSetup.ValidateInventory(); }
            catch (Exception error) { EditorApplication.isPlaying = false; Debug.LogError(error.Message); }
        }
    }
    public sealed class BootResourceBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        { try { BootResourceSetup.ValidateInventory(); } catch (Exception error) { throw new BuildFailedException(error.Message); } }
    }
}
