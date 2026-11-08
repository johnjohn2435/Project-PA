using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.Meta;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;
namespace PocketArcade.Editor
{
    public static class StringTableSetup
    {
        public const string TableFile = "Assets/_Project/Resources/Data/StringTable.json";
        private const string FontFile = "Assets/_Project/Fonts/NanumGothic-Regular.ttf";
        private const string FontAssetFile = "Assets/_Project/Fonts/NanumGothic SDF.asset";
        public static StringTable Read() => StringTable.Parse(File.ReadAllText(TableFile));
        public static string Text(string key) => Read().Get(key, GameLanguage.Korean);
        public static string Format(string key, params object[] args) => Read().Format(key, GameLanguage.Korean, args);
        public static void BindKnown(TMP_Text text)
        {
            if (string.IsNullOrWhiteSpace(text.text)) return;
            foreach (var row in Read().Rows)
            {
                if (row.en.Contains("{") || row.ko.Contains("{")) continue;
                if (text.text != row.en && text.text != row.ko) continue;
                var binding = text.GetComponent<LocalizedText>() ?? text.gameObject.AddComponent<LocalizedText>();
                binding.enabled = true; binding.SetKey(row.key); Record(binding); text.text = row.ko; return;
            }
        }
        private static void Record(LocalizedText binding)
        {
            EditorUtility.SetDirty(binding);
            if (PrefabUtility.IsPartOfPrefabInstance(binding)) PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
        }
        private static HashSet<TMP_Text> DynamicLabels(GameObject root)
        {
            var labels = new HashSet<TMP_Text>();
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILocalizedDynamicText dynamicText)
                    foreach (var label in dynamicText.DynamicTextLabels) if (label != null) labels.Add(label);
                if (!(component is HubView || component is CharacterCardView || component is MiniGameCardView ||
                    component is GameEntryView || component is BootStatusView || component is MessageBoxView ||
                    component is TransientTextView || component is BootDialogSystem || component is ToggleView)) continue;
                var data = new SerializedObject(component); var property = data.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is TMP_Text text)
                        labels.Add(text);
            }
            return labels;
        }
        private static TMP_FontAsset EnsureFont(StringTable table)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetFile);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(FontFile);
                if (source == null) throw new InvalidOperationException("Missing Korean font: " + FontFile);
                font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                font.name = "NanumGothic SDF";
                AssetDatabase.CreateAsset(font, FontAssetFile);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            }
            string characters = "0123456789[]";
            foreach (var row in table.Rows) characters += row.ko + row.en;
            characters = characters.Replace("\n", "").Replace("\r", "");
            font.ReadFontAssetDefinition();
            // TMP returns false when every requested glyph is already present.
            string pending = new string(characters.Distinct().Where(c => !font.HasCharacter(c)).ToArray());
            if (pending.Length > 0 && !font.TryAddCharacters(pending, out string missing))
                throw new InvalidOperationException("Font is missing table characters: " + missing);
            foreach (var atlas in font.atlasTextures)
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssets(); return font;
        }
        private static void Configure(GameObject root, TMP_FontAsset font)
        {
            var dynamicLabels = DynamicLabels(root);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                if (dynamicLabels.Contains(label))
                {
                    var old = label.GetComponent<LocalizedText>();
                    if (old != null) { old.enabled = false; Record(old); }
                    // Dynamic views fill these at runtime; never serialize obsolete display phrases.
                    label.text = "";
                }
                else
                {
                    BindKnown(label);
                    var binding = label.GetComponent<LocalizedText>();
                    if (binding != null)
                    {
                        if (!Read().Contains(binding.Key)) throw new InvalidOperationException("Missing string key: " + binding.Key);
                        label.text = Text(binding.Key);
                    }
                    else BindKnown(label);
                    if (!string.IsNullOrWhiteSpace(label.text) && label.GetComponent<LocalizedText>() == null)
                        throw new InvalidOperationException("Unmapped UI text in " + root.name + ": " + label.text);
                }
                EditorUtility.SetDirty(label);
                if (PrefabUtility.IsPartOfPrefabInstance(label)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
        }
        private static IEnumerable<string> PrefabPaths()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Resources/UI", "Assets/_Project/Prefabs/UI" }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }
        private static void Backup(string path, string folder)
        { string target = folder + "/" + path; Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(path, target, true); }
        [MenuItem("Pocket Arcade/Localization/1. Apply String Table And Korean Font")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var table = Read();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string backup = "Logs/StringTableBackup/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            var paths = new List<string>(PrefabPaths());
            foreach (var path in paths) Backup(path, backup);
            foreach (var name in new[] { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene }) Backup(SceneIds.PathFor(name), backup);
            if (File.Exists(FontAssetFile)) Backup(FontAssetFile, backup);
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var font = EnsureFont(table);
                // Common prefabs first, then panels, then screens, preserving nested references.
                paths.Sort((a,b) => Rank(a).CompareTo(Rank(b)));
                foreach (var path in paths)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Configure(root, font);
                        PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                        if (!success) throw new IOException("Could not save " + path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                foreach (var name in new[] { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene })
                {
                    var scene = EditorSceneManager.OpenScene(SceneIds.PathFor(name), OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects()) Configure(root, font);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + name);
                }
                AssetDatabase.SaveAssets(); Validate();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("String table and Korean font applied. Check both languages in Play Mode. Backup: " + backup);
        }
        private static int Rank(string path) => path.Contains("/Common/") ? 0 : path.Contains("/Panels/") ? 1 : 2;
        [MenuItem("Pocket Arcade/Localization/2. Validate String Table")]
        public static void Validate()
        {
            var table = Read(); BootResourceSetup.ValidateInventory();
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            if (catalog != null)
            {
                foreach (var item in catalog.Characters) RequireKey(table,"character." + item.Id + ".name");
                foreach (var item in catalog.MiniGames) RequireKey(table,"game." + item.Id + ".name");
            }
            foreach (var path in PrefabPaths())
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var dynamicLabels = DynamicLabels(root);
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    var binding = label.GetComponent<LocalizedText>();
                    if (dynamicLabels.Contains(label))
                    { if (binding != null && binding.enabled) throw new InvalidOperationException("Dynamic label must not have an enabled LocalizedText: " + path); }
                    else if (binding != null) RequireKey(table, binding.Key);
                    else if (!string.IsNullOrWhiteSpace(label.text)) throw new InvalidOperationException("Apply localization to " + path);
                    if (label.font == null || !label.font.HasCharacter('한')) throw new InvalidOperationException("Apply Korean font to " + path);
                }
            }
            Debug.Log("String keys, Korean/English rows, UI bindings and font validated.");
        }
        private static void RequireKey(StringTable table, string key)
        { if (!table.Contains(key)) throw new InvalidOperationException("Missing string key: " + key); }
        [MenuItem("Pocket Arcade/Localization/Preview Korean (Play Mode)")]
        public static void Korean() { if (Application.isPlaying) L10n.SetLanguage(GameLanguage.Korean); }
        [MenuItem("Pocket Arcade/Localization/Preview English (Play Mode)")]
        public static void English() { if (Application.isPlaying) L10n.SetLanguage(GameLanguage.English); }
    }
}
