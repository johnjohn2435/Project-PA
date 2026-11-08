using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Editor
{
    public static class UIFoundationSetup
    {
        private const string AssetsRoot = "Assets/_Project/";
        private const string UI = AssetsRoot + "Resources/UI/";
        private static readonly Color Paper = new Color(1f, .96f, .89f);
        private static readonly Color Ink = new Color(.23f, .20f, .26f);
        private static readonly Color Mint = new Color(.66f, .86f, .75f);

        [MenuItem("Pocket Arcade/UI Foundation/1. Create Prefabs And Migrate Scenes")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import TMP Essential Resources first.");
            foreach (var name in new[] { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene })
                if (!File.Exists(SceneIds.PathFor(name))) throw new InvalidOperationException("Run Phase 0 and Phase 1 setup first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Back up exact scene files before migration. Existing prefab assets are never overwritten.
            string backup = "Logs/UIFoundationBackup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            Directory.CreateDirectory(backup);
            foreach (var name in new[] { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene })
                File.Copy(SceneIds.PathFor(name), Path.Combine(backup, name + ".unity"));
            foreach (var folder in new[] { "Common", "Panels", "Screens", "Overlays" }) Directory.CreateDirectory(UI + folder);
            Directory.CreateDirectory(AssetsRoot + "Prefabs/Common");
            AssetDatabase.Refresh();
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BuildOverlays(font);
                Migrate(SceneIds.SelectScene, scene => MigrateScreen<HubView>(scene, "Hub"));
                Migrate(SceneIds.GameScene, scene => MigrateScreen<GameEntryView>(scene, "GameEntry"));
                Migrate(SceneIds.BootScene, MigrateManagers);
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("UI Foundation saved. Existing prefab edits preserved. Scene backups: " + backup + ". Run Validate, then play BootScene.");
        }
        private static void Migrate(string name, Action<Scene> action)
        {
            var scene = EditorSceneManager.OpenScene(SceneIds.PathFor(name), OpenSceneMode.Single);
            action(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save " + name);
        }
        private static T Find<T>(Scene scene) where T : Component
        {
            T found = null;
            foreach (var root in scene.GetRootGameObjects()) foreach (var value in root.GetComponentsInChildren<T>(true))
            { if (found != null) throw new InvalidOperationException("Duplicate " + typeof(T).Name); found = value; }
            return found;
        }
        private static void MigrateScreen<T>(Scene scene, string name) where T : UISceneView
        {
            var existingHost = Find<UISceneHost>(scene);
            if (existingHost != null) return;
            var view = Find<T>(scene);
            if (view == null) throw new InvalidOperationException("Run Phase 1 setup before UI Foundation: " + scene.name);
            string path = UI + "Screens/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                ExtractControls(view.gameObject);
                if (view is HubView)
                {
                    var data = new SerializedObject(view);
                    foreach (string field in new[] { "homePanel", "characterPanel", "gamePanel", "settingsPanel" })
                    {
                        var panel = (GameObject)data.FindProperty(field).objectReferenceValue;
                        SaveConnected(panel, UI + "Panels/" + panel.name + ".prefab");
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(view.gameObject, path, out bool success);
                if (!success) throw new IOException("Could not save " + path);
            }
            // References remain internal to the saved screen; only the host stays in the scene.
            var host = new GameObject(name + " UI Host", typeof(RectTransform), typeof(UISceneHost));
            host.transform.SetParent(view.transform.parent, false);
            CopyRect((RectTransform)view.transform, (RectTransform)host.transform);
            var serialized = new SerializedObject(host.GetComponent<UISceneHost>());
            Set(serialized, "screenPath", "UI/Screens/" + name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(view.gameObject);
        }
        private static void MigrateManagers(Scene scene)
        {
            var bootstrap = Find<GameBootstrap>(scene);
            if (bootstrap == null) throw new InvalidOperationException("Run Phase 1 setup first.");

            if (Find<BootStatusView>(scene) == null)
            {
                var safe = Find<SafeAreaPanel>(scene);
                var label = safe.GetComponentInChildren<TMP_Text>(true);
                if (label == null) throw new InvalidOperationException("Missing Boot status label.");
                var status = label.gameObject.AddComponent<BootStatusView>();
                var data = new SerializedObject(status); Set(data, "label", label); data.ApplyModifiedPropertiesWithoutUndo();
            }
            IndependentManagerSetup.ConfigureScene(scene);
        }
        private static void SaveConnected(GameObject go, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction, out bool success);
            if (!success) throw new IOException("Could not save " + path);
        }
        private static void ExtractControls(GameObject screen)
        {
            foreach (var toggle in screen.GetComponentsInChildren<Toggle>(true))
            {
                var view = toggle.GetComponent<ToggleView>();
                if (view == null) view = toggle.gameObject.AddComponent<ToggleView>();
                var data = new SerializedObject(view); Set(data, "label", toggle.GetComponentInChildren<TMP_Text>(true)); data.ApplyModifiedPropertiesWithoutUndo();
                Common(screen, toggle.gameObject, UI + "Common/Toggle.prefab");
            }
            foreach (var button in screen.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<MiniGameCardView>() != null || button.GetComponentInChildren<TMP_Text>(true) == null) continue;
                Common(screen, button.gameObject, UI + "Common/Button.prefab");
            }
            foreach (var card in screen.GetComponentsInChildren<CharacterCardView>(true)) Common(screen, card.gameObject, UI + "Common/CharacterCard.prefab");
            foreach (var card in screen.GetComponentsInChildren<MiniGameCardView>(true)) Common(screen, card.gameObject, UI + "Common/MiniGameCard.prefab");
        }
        // Reuse the common prefab while retaining authored per-instance layout, text and color overrides.
        // Serialize references across the whole screen before destroying the old instance.
        private static void Common(GameObject screen, GameObject old, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { SaveConnected(old, path); return; }
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
            replacement.name = old.name; replacement.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            var map = new Dictionary<Object, Object>();
            foreach (var transform in old.GetComponentsInChildren<Transform>(true))
            {
                string relative = AnimationUtility.CalculateTransformPath(transform, old.transform);
                var target = relative.Length == 0 ? replacement.transform : replacement.transform.Find(relative);
                if (target == null)
                {
                    Object.DestroyImmediate(replacement);
                    throw new InvalidOperationException("Common prefab hierarchy differs: " + path + ". Restore scene backup or align its child names before retrying.");
                }
                map[transform.gameObject] = target.gameObject;
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component == null) continue;
                    var destination = target.GetComponent(component.GetType());
                    if (destination != null) map[component] = destination;
                }
                if (transform is RectTransform rect && target is RectTransform targetRect) CopyRect(rect, targetRect);
                if (transform.TryGetComponent<Image>(out var sourceImage) && target.TryGetComponent<Image>(out var image))
                { image.color = sourceImage.color; image.sprite = sourceImage.sprite; image.enabled = sourceImage.enabled; }
                if (transform.TryGetComponent<TMP_Text>(out var sourceText) && target.TryGetComponent<TMP_Text>(out var text))
                { text.text = sourceText.text; text.color = sourceText.color; text.fontSize = sourceText.fontSize; }
                if (transform.TryGetComponent<Toggle>(out var sourceToggle) && target.TryGetComponent<Toggle>(out var toggle)) toggle.SetIsOnWithoutNotify(sourceToggle.isOn);
                target.gameObject.SetActive(transform.gameObject.activeSelf);
            }
            UIReferenceRemapper.Remap(screen, old, map);
            foreach (var component in replacement.GetComponentsInChildren<Component>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Object.DestroyImmediate(old);
            UIReferenceRemapper.ValidateHierarchy(screen);
        }
        private static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta; to.anchoredPosition3D = from.anchoredPosition3D;
            to.localScale = from.localScale; to.localRotation = from.localRotation;
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (color.HasValue) go.AddComponent<Image>().color = color.Value;
            return rect;
        }
        private static TMP_Text Text(Transform parent, TMP_FontAsset font, string name, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, name, min, max); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.color = Ink; text.fontSize = 34; text.enableAutoSizing = true;
            text.fontSizeMin = 20; text.fontSizeMax = 34; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, TMP_FontAsset font, string label, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, label, min, max, Mint); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var text = Text(rect, font, "Label", Vector2.zero, Vector2.one); text.text = label; StringTableSetup.BindKnown(text); return button;
        }
        private static void BuildOverlays(TMP_FontAsset font)
        {
            foreach (string name in new[] { "MessageBox", "Popup", "Toast", "Tooltip" })
            {
                string path = UI + "Overlays/" + name + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                var root = Rect(null, name, Vector2.zero, Vector2.one);
                // Build inactive: serialized button references must exist before OnEnable runs.
                root.gameObject.SetActive(false);
                try
                {
                    if (name == "MessageBox" || name == "Popup")
                    {
                        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .45f);
                        root.gameObject.AddComponent<CanvasGroup>();
                        var panel = Rect(root, "Window", new Vector2(.08f, .30f), new Vector2(.92f, .70f), Paper);
                        var heading = Text(panel, font, "Title", new Vector2(.06f,.72f), new Vector2(.94f,.94f));
                        var body = Text(panel, font, "Body", new Vector2(.06f,.28f), new Vector2(.94f,.70f));
                        var confirm = Button(panel, font, StringTableSetup.Text("common.ok"), new Vector2(.53f,.06f), new Vector2(.94f,.23f));
                        if (name == "MessageBox")
                        {
                            var cancel = Button(panel, font, StringTableSetup.Text("common.cancel"), new Vector2(.06f,.06f), new Vector2(.47f,.23f));
                            var view = root.gameObject.AddComponent<MessageBoxView>(); var data = new SerializedObject(view);
                            Set(data, "title", heading); Set(data, "body", body); Set(data, "confirm", confirm); Set(data, "cancel", cancel); data.ApplyModifiedPropertiesWithoutUndo();
                        }
                        else
                        {
                            heading.text = StringTableSetup.Text("popup.title"); body.text = StringTableSetup.Text("popup.body");
                            StringTableSetup.BindKnown(heading); StringTableSetup.BindKnown(body);
                            var view = root.gameObject.AddComponent<PopupView>(); var data = new SerializedObject(view);
                            Set(data, "close", confirm); data.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    else
                    {
                        root.gameObject.AddComponent<Image>().color = Paper;
                        var group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
                        if (name == "Tooltip") { root.anchorMin = root.anchorMax = new Vector2(.5f,.5f); root.sizeDelta = new Vector2(460,180); }
                        else { root.anchorMin = new Vector2(.1f,.1f); root.anchorMax = new Vector2(.9f,.2f); }
                        var view = root.gameObject.AddComponent<TransientTextView>(); var data = new SerializedObject(view);
                        Set(data, "label", Text(root, font, "Text", new Vector2(.05f,.08f), new Vector2(.95f,.92f))); data.ApplyModifiedPropertiesWithoutUndo();
                    }
                    root.gameObject.SetActive(true);
                    PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool success);
                    if (!success) throw new IOException("Could not save " + path);
                }
                finally { Object.DestroyImmediate(root.gameObject); }
            }
        }
        private static void Set(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        private static void Set(SerializedObject data, string field, string value) => data.FindProperty(field).stringValue = value;

        [MenuItem("Pocket Arcade/UI Foundation/2. Validate Prefabs And Scenes")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            foreach (string relative in new[] { "Screens/Hub", "Screens/GameEntry", "Common/Button", "Common/Toggle", "Common/CharacterCard", "Common/MiniGameCard", "Panels/Home", "Panels/Characters", "Panels/Games", "Panels/Settings", "Overlays/MessageBox", "Overlays/Popup", "Overlays/Toast", "Overlays/Tooltip" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI + relative + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing prefab: " + relative);
                UIReferenceRemapper.ValidateHierarchy(prefab);
                foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) throw new InvalidOperationException("Missing script: " + relative);
                    if (component.GetType().Namespace != "PocketArcade.UI") continue;
                    var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                            throw new InvalidOperationException(relative + ": missing " + property.propertyPath);
                }
            }
            IndependentManagerSetup.ValidateAssets();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var boot = EditorSceneManager.OpenScene(SceneIds.PathFor(SceneIds.BootScene), OpenSceneMode.Single);
                IndependentManagerSetup.ValidateScene(boot);
                if (Find<BootStatusView>(boot) == null) throw new InvalidOperationException("BootScene needs BootStatusView.");
                foreach (string name in new[] { SceneIds.SelectScene, SceneIds.GameScene })
                {
                    var scene = EditorSceneManager.OpenScene(SceneIds.PathFor(name), OpenSceneMode.Single);
                    var host = Find<UISceneHost>(scene);
                    if (host == null || Find<UISceneView>(scene) != null) throw new InvalidOperationException("Scene must have one UI host and no embedded screen: " + name);
                    string key = new SerializedObject(host).FindProperty("screenPath").stringValue;
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot + "Resources/" + key + ".prefab") == null) throw new InvalidOperationException("Invalid host path: " + key);
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("UI prefab references and scene hosts validated. Play Mode interaction/device checks remain separate.");
        }
    }
}
