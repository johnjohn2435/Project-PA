using System;
using System.IO;
using PocketArcade.BlockGravity.Unity;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.BlockGravity.Editor
{
    public static partial class BlockGravitySetup
    {
        private const string RetiredSettingsFile = ResourcesRoot + "UI/Overlays/BlockGravitySettings.prefab";
        private const string RetiredSettingsKey = "UI/Overlays/BlockGravitySettings";
        private const string IconFolder = ResourcesRoot + "UI/Icons/Audio/";

        private static Sprite AudioSprite(string name)
        {
            string path = IconFolder + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException(Text("bg.error.references"));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void UpgradeSoundIcons(BlockGravityPausePopup popup, RectTransform card)
        {
            var oldSettings = card.Find("Settings");
            if (oldSettings != null) Object.DestroyImmediate(oldSettings.gameObject);
            card.sizeDelta = new Vector2(760, 740);
            var title = (RectTransform)card.Find("Title");
            title.anchorMin = title.anchorMax = new Vector2(.5f, .88f);
            title.anchoredPosition = Vector2.zero;
            title.GetComponent<LocalizedText>().SetKey("bg.menu.title");
            var help=card.Find("Help");
            Set(popup,"help",help!=null?help.GetComponent<Button>():Button(card,"Help",new Vector2(.89f,.88f),"bg.tutorial.help",new Vector2(76,76)));
            var resume = card.Find("Resume").GetComponent<Button>();
            Place(resume.transform, new Vector2(.5f, .34f), new Vector2(560, 85));
            resume.GetComponentInChildren<LocalizedText>(true).SetKey("bg.menu.continue");
            if (card.Find("Exit") == null) Button(card, "Exit", new Vector2(.5f, .16f), "bg.exit", new Vector2(560, 85));
            Place(card.Find("Exit"), new Vector2(.5f, .16f), new Vector2(560, 85));
            var notice = card.Find("Notice") != null ? card.Find("Notice").GetComponent<TMP_Text>() : Label(card, "Notice", new Vector2(.5f, .44f), new Vector2(680, 45), 22);
            Place(notice.transform, new Vector2(.5f, .44f), new Vector2(680, 45));
            Set(popup, "resume", resume); Set(popup, "exit", card.Find("Exit").GetComponent<Button>()); Set(popup, "notice", notice);
            AudioButton(card, "Music", AudioSetting.Music, new Vector2(.35f, .64f), "settings.music", notice);
            AudioButton(card, "Effects", AudioSetting.Effects, new Vector2(.65f, .64f), "settings.sfx", notice);
        }

        private static void Place(Transform target, Vector2 center, Vector2 size)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = center;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void AudioButton(Transform parent, string name, AudioSetting setting, Vector2 center, string titleKey, TMP_Text notice)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var rect = Rect(parent, name, center, new Vector2(180, 180));
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(.87f, .91f, .84f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            Label(rect, "Title", new Vector2(.5f, .87f), new Vector2(176, 40), 28, titleKey);
            var picture = Rect(rect, "Icon", new Vector2(.5f, .53f), new Vector2(92, 92));
            var image = picture.gameObject.AddComponent<Image>(); image.sprite = AudioSprite(name); image.preserveAspect = true; image.raycastTarget = false;
            var slashRect = Rect(picture, "OffSlash", new Vector2(.5f, .5f), new Vector2(94, 8));
            slashRect.localRotation = Quaternion.Euler(0, 0, -45);
            var slash = slashRect.gameObject.AddComponent<Image>(); slash.color = new Color(.68f, .27f, .22f); slash.raycastTarget = false; slash.enabled = false;
            var state = Label(rect, "State", new Vector2(.5f, .14f), new Vector2(150, 36), 26);
            state.text = Text("bg.settings.on");
            var view = rect.gameObject.AddComponent<AudioSettingIcon>();
            Set(view, "button", button); Set(view, "icon", image); Set(view, "offSlash", slash); Set(view, "state", state); Set(view, "notice", notice);
            var data = new SerializedObject(view); data.FindProperty("setting").enumValueIndex = (int)setting; data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RetireSettingsPopup()
        {
            if (!File.Exists(RetiredSettingsFile)) return;
            const string retired = "Assets/_Project/Prefabs/Legacy/BlockGravitySettings.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(retired)); AssetDatabase.Refresh();
            string destination = File.Exists(retired) ? AssetDatabase.GenerateUniqueAssetPath(retired) : retired;
            string error = AssetDatabase.MoveAsset(RetiredSettingsFile, destination);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
    }
}
