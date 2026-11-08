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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Editor
{
    public static class Phase1Setup
    {
        private const string Root = "Assets/_Project/";
        private static readonly Color Ink = new Color(0.23f, 0.20f, 0.26f);
        private static readonly Color Paper = new Color(1f, 0.96f, 0.89f);
        private static readonly Color Coral = new Color(1f, 0.67f, 0.62f);
        private static readonly Color Mint = new Color(0.66f, 0.86f, 0.75f);
        private static readonly Color Violet = new Color(0.79f, 0.72f, 0.92f);

        [MenuItem("Pocket Arcade/Phase 1/1. Create Hub And Data")]
        public static void CreateHubAndData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before running setup.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null)
                throw new InvalidOperationException("First run Window > TextMeshPro > Import TMP Essential Resources.");
            foreach (string name in new[] { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene })
                if (!File.Exists(SceneIds.PathFor(name)))
                    throw new InvalidOperationException("First run Pocket Arcade > Phase 0 > 1. Apply Foundation.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var catalog = CreateCatalog();
                ConfigureScene(SceneIds.BootScene, scene => BuildBoot(scene, catalog, font));
                ConfigureScene(SceneIds.SelectScene, scene => BuildHub(scene, catalog, font));
                ConfigureScene(SceneIds.GameScene, scene => BuildGame(scene, font));
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("Phase 1 setup saved. Existing definitions and configured screens were preserved. Run validation, then play BootScene.");
        }

        private static GameCatalog CreateCatalog()
        {
            var characters = new CharacterDefinition[3];
            string[] ids = { "char_cat", "char_dog", "char_hamster" };
            
            Color[] colors = { Coral, Mint, Violet };
            for (int i = 0; i < ids.Length; i++)
            {
                int index = i;
                characters[i] = CreateIfMissing<CharacterDefinition>("ScriptableObjects/Characters/" + ids[i] + ".asset", serialized =>
                {
                    serialized.FindProperty("id").stringValue = ids[index];
                    serialized.FindProperty("themeColor").colorValue = colors[index];
                    serialized.FindProperty("defaultUnlocked").boolValue = true;
                    serialized.FindProperty("portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(
                        Root + "Art/Characters/Dummy/" + ids[index] + "_dummy.png");
                });
            }
            var games = new MiniGameDefinition[3];
            string[] gameIds = { "block_gravity", "coming_soon_1", "coming_soon_2" };
            for (int i = 0; i < games.Length; i++)
            {
                int index = i;
                games[i] = CreateIfMissing<MiniGameDefinition>("ScriptableObjects/MiniGames/" + gameIds[i] + ".asset", serialized =>
                {
                    serialized.FindProperty("id").stringValue = gameIds[index];
                    serialized.FindProperty("sceneName").stringValue = index == 0 ? SceneIds.GameScene : "";
                    serialized.FindProperty("isAvailable").boolValue = index == 0;
                });
            }
            return CreateIfMissing<GameCatalog>("ScriptableObjects/GameCatalog.asset", serialized =>
            {
                SetArray(serialized, "characters", characters);
                SetArray(serialized, "miniGames", games);
            });
        }

        private static T CreateIfMissing<T>(string relativePath, Action<SerializedObject> configure) where T : ScriptableObject
        {
            string path = Root + relativePath;
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Unexpected asset type at " + path);
            var asset = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(asset);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ConfigureScene(string name, Action<Scene> build)
        {
            var scene = SceneManager.GetSceneByPath(SceneIds.PathFor(name));
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(SceneIds.PathFor(name), OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                build(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + name);
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }

        private static T InScene<T>(Scene scene) where T : Component
        {
            T found = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (found != null) throw new InvalidOperationException("Multiple " + typeof(T).Name + " components in " + scene.name);
                    found = component;
                }
            return found;
        }

        private static RectTransform ContentRoot(Scene scene, string name)
        {
            var safeArea = InScene<SafeAreaPanel>(scene);
            if (safeArea == null) throw new InvalidOperationException("Missing SafeAreaPanel in " + scene.name);
            return Panel(safeArea.transform, name, Vector2.zero, Vector2.one, Paper);
        }

        private static void BuildBoot(Scene scene, GameCatalog catalog, TMP_FontAsset font)
        {
            var existing = InScene<GameBootstrap>(scene);
            if (existing != null)
            {
                IndependentManagerSetup.ConfigureScene(scene);
                return;
            }
            var root = ContentRoot(scene, "Boot Content");
            var label = Text(root, font, StringTableSetup.Text("boot.initial"), new Vector2(.1f, .35f), new Vector2(.9f, .65f), 64);
            var bootstrap = new GameObject("Bootstrap", typeof(GameBootstrap)).GetComponent<GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            Set(serialized, "catalog", catalog);
            var status = label.gameObject.AddComponent<BootStatusView>();
            var statusData = new SerializedObject(status);
            Set(statusData, "label", label);
            statusData.ApplyModifiedPropertiesWithoutUndo();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            IndependentManagerSetup.ConfigureScene(scene);
        }

        private static void BuildHub(Scene scene, GameCatalog catalog, TMP_FontAsset font)
        {
            if (InScene<HubView>(scene) != null || InScene<UISceneHost>(scene) != null) return;
            catalog.Validate();
            var root = ContentRoot(scene, "Pocket Arcade Hub");
            var view = root.gameObject.AddComponent<HubView>();
            Text(root, font, StringTableSetup.Text("app.title"), new Vector2(.06f, .91f), new Vector2(.94f, .98f), 55);
            var coins = Text(root, font, StringTableSetup.Format("hub.coins", 0), new Vector2(.06f, .84f), new Vector2(.46f, .90f), 36);
            Text(root, font, StringTableSetup.Text("hub.guest"), new Vector2(.52f, .84f), new Vector2(.94f, .90f), 36);
            var account = Button(root, font, StringTableSetup.Text("hub.link"), new Vector2(.15f, .02f), new Vector2(.85f, .085f), Mint);
            var notice = Text(root, font, "", new Vector2(.06f, .09f), new Vector2(.94f, .17f), 30);
            var home = Panel(root, "Home", new Vector2(.04f, .18f), new Vector2(.96f, .83f), Paper);
            var characters = Panel(root, "Characters", new Vector2(.04f, .18f), new Vector2(.96f, .83f), Paper);
            var games = Panel(root, "Games", new Vector2(.04f, .18f), new Vector2(.96f, .83f), Paper);
            var settings = Panel(root, "Settings", new Vector2(.04f, .18f), new Vector2(.96f, .83f), Paper);

            var portrait = Picture(home, "Portrait", new Vector2(.24f, .48f), new Vector2(.76f, .93f), Coral);
            portrait.raycastTarget = true;
            var portraitButton = portrait.gameObject.AddComponent<Button>();
            portraitButton.targetGraphic = portrait;
            var characterLabel = Text(home, font, StringTableSetup.Text("character.char_cat.name"), new Vector2(.1f, .39f), new Vector2(.9f, .48f), 48);
            var play = Button(home, font, StringTableSetup.Text("hub.choose_game"), new Vector2(.1f, .25f), new Vector2(.9f, .36f), Coral);
            var choose = Button(home, font, StringTableSetup.Text("hub.characters"), new Vector2(.1f, .12f), new Vector2(.9f, .23f), Mint);
            var settingsButton = Button(home, font, StringTableSetup.Text("hub.settings"), new Vector2(.1f, .0f), new Vector2(.9f, .10f), Violet);

            Text(characters, font, StringTableSetup.Text("hub.choose_character"), new Vector2(.03f, .85f), new Vector2(.97f, .98f), 46);
            var cards = new CharacterCardView[catalog.Characters.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                float width = .94f / cards.Length;
                float left = .03f + width * i;
                var card = Panel(characters, "Character Card " + i, new Vector2(left, .25f),
                    new Vector2(left + width - .02f, .78f), catalog.Characters[i].ThemeColor);
                var image = Picture(card, "Portrait", new Vector2(.06f, .35f), new Vector2(.94f, .95f), Color.white);
                var select = Button(card, font, StringTableSetup.Text("common.select"), new Vector2(.04f, .04f), new Vector2(.96f, .30f), Paper);
                cards[i] = card.gameObject.AddComponent<CharacterCardView>();
                var cardData = new SerializedObject(cards[i]);
                Set(cardData, "portrait", image);
                Set(cardData, "label", select.GetComponentInChildren<TMP_Text>());
                Set(cardData, "button", select);
                cardData.ApplyModifiedPropertiesWithoutUndo();
            }
            var charBack = Button(characters, font, StringTableSetup.Text("common.back"), new Vector2(.1f, .02f), new Vector2(.9f, .14f), Mint);
            Text(games, font, StringTableSetup.Text("hub.pick_game"), new Vector2(.03f, .85f), new Vector2(.97f, .98f), 46);
            var gameCards = new MiniGameCardView[catalog.MiniGames.Count];
            float gameHeight = .65f / gameCards.Length;
            for (int i = 0; i < gameCards.Length; i++)
            {
                float top = .82f - i * gameHeight;
                var button = Button(games, font, StringTableSetup.Text("common.game"), new Vector2(.08f, top - gameHeight + .025f),
                    new Vector2(.92f, top), i == 0 ? Coral : new Color(.85f, .84f, .84f));
                gameCards[i] = button.gameObject.AddComponent<MiniGameCardView>();
                var thumbnail = Picture(button.transform, "Thumbnail", new Vector2(.03f, .1f), new Vector2(.2f, .9f), Color.white);
                var cardData = new SerializedObject(gameCards[i]);
                Set(cardData, "label", button.GetComponentInChildren<TMP_Text>());
                Set(cardData, "button", button);
                Set(cardData, "thumbnail", thumbnail);
                cardData.ApplyModifiedPropertiesWithoutUndo();
            }
            var gameBack = Button(games, font, StringTableSetup.Text("common.back"), new Vector2(.1f, .02f), new Vector2(.9f, .14f), Mint);
            Text(settings, font, StringTableSetup.Text("hub.settings"), new Vector2(.03f, .85f), new Vector2(.97f, .98f), 46);
            var bgm = Toggle(settings, font, StringTableSetup.Text("settings.music"), .67f);
            var sfx = Toggle(settings, font, StringTableSetup.Text("settings.sfx"), .49f);
            var haptics = Toggle(settings, font, StringTableSetup.Text("settings.haptics"), .31f);
            var settingsBack = Button(settings, font, StringTableSetup.Text("common.back"), new Vector2(.1f, .02f), new Vector2(.9f, .14f), Mint);

            var serialized = new SerializedObject(view);
            Set(serialized, "homePanel", home.gameObject);
            Set(serialized, "characterPanel", characters.gameObject);
            Set(serialized, "gamePanel", games.gameObject);
            Set(serialized, "settingsPanel", settings.gameObject);
            Set(serialized, "coinsLabel", coins);
            Set(serialized, "characterLabel", characterLabel);
            Set(serialized, "notice", notice);
            Set(serialized, "portrait", portrait);
            Set(serialized, "portraitButton", portraitButton);
            Set(serialized, "playButton", play);
            Set(serialized, "charactersButton", choose);
            Set(serialized, "settingsButton", settingsButton);
            Set(serialized, "accountButton", account);
            Set(serialized, "bgmToggle", bgm);
            Set(serialized, "sfxToggle", sfx);
            Set(serialized, "hapticsToggle", haptics);
            SetArray(serialized, "backButtons", new[] { charBack, gameBack, settingsBack });
            SetArray(serialized, "characterCards", cards);
            SetArray(serialized, "gameCards", gameCards);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            characters.gameObject.SetActive(false);
            games.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
        }

        private static void BuildGame(Scene scene, TMP_FontAsset font)
        {
            if (InScene<GameEntryView>(scene) != null || InScene<UISceneHost>(scene) != null) return;
            var root = ContentRoot(scene, "Game Entry");
            var view = root.gameObject.AddComponent<GameEntryView>();
            var title = Text(root, font, StringTableSetup.Text("game.block_gravity.name"), new Vector2(.08f, .75f), new Vector2(.92f, .90f), 60);
            var status = Text(root, font, "", new Vector2(.08f, .35f), new Vector2(.92f, .70f), 40);
            var back = Button(root, font, StringTableSetup.Text("game.back"), new Vector2(.12f, .12f), new Vector2(.88f, .22f), Mint);
            var serialized = new SerializedObject(view);
            Set(serialized, "title", title);
            Set(serialized, "status", status);
            Set(serialized, "backButton", back);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text Text(Transform parent, TMP_FontAsset font, string content, Vector2 min, Vector2 max, float size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content; StringTableSetup.BindKnown(text);
            text.color = Ink;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Image Picture(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rect = Panel(parent, name, min, max, color);
            var image = rect.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Button Button(Transform parent, TMP_FontAsset font, string label, Vector2 min, Vector2 max, Color color)
        {
            var rect = Panel(parent, label, min, max, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Text(rect, font, label, new Vector2(.04f, .05f), new Vector2(.96f, .95f), 38);
            return button;
        }

        private static Toggle Toggle(Transform parent, TMP_FontAsset font, string label, float bottom)
        {
            var root = Panel(parent, label, new Vector2(.08f, bottom), new Vector2(.92f, bottom + .13f), Mint);
            var box = Picture(root, "Check Background", new Vector2(.04f, .18f), new Vector2(.19f, .82f), Color.white);
            var check = Picture(box.transform, "Check", new Vector2(.18f, .18f), new Vector2(.82f, .82f), Ink);
            Text(root, font, label, new Vector2(.25f, .05f), new Vector2(.95f, .95f), 38);
            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.graphic = check;
            return toggle;
        }

        private static void Set(SerializedObject target, string name, Object value) =>
            target.FindProperty(name).objectReferenceValue = value;

        private static void SetArray<T>(SerializedObject target, string name, T[] values) where T : Object
        {
            var array = target.FindProperty(name);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        [MenuItem("Pocket Arcade/Phase 1/2. Validate Hub")]
        public static void ValidateHub()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before validation.");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "ScriptableObjects/GameCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("Run Phase 1 setup first.");
            catalog.Validate();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                string[] names = { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene };
                for (int i = 0; i < names.Length; i++)
                {
                    string path = SceneIds.PathFor(names[i]);
                    var buildScenes = EditorBuildSettings.scenes;
                    if (i >= buildScenes.Length || !buildScenes[i].enabled || buildScenes[i].path != path)
                        throw new InvalidOperationException("Run Phase 0 setup to register the scene order.");
                    var scene = SceneManager.GetSceneByPath(path);
                    bool loaded = scene.IsValid() && scene.isLoaded;
                    if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try
                    {
                        Component entry = i == 0 ? (Component)InScene<GameBootstrap>(scene) :
                            i == 1 ? InScene<HubView>(scene) : InScene<GameEntryView>(scene);
                        if (entry == null && i > 0) entry = InScene<UISceneHost>(scene);
                        if (entry == null) throw new InvalidOperationException("Missing entry component in " + names[i]);
                        CheckReferences(entry);
                        if (i == 0) IndependentManagerSetup.ValidateScene(scene);
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            foreach (var card in root.GetComponentsInChildren<CharacterCardView>(true)) CheckReferences(card);
                            foreach (var card in root.GetComponentsInChildren<MiniGameCardView>(true)) CheckReferences(card);
                        }
                    }
                    finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(original); }
            Debug.Log("Phase 1 scene references validated. Next: play BootScene and run the manual smoke checklist.");
        }

        private static void CheckReferences(Component component)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    throw new InvalidOperationException(component.GetType().Name + ": missing " + property.propertyPath);
        }
    }
}
