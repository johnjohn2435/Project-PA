using System;
using System.Collections.Generic;
using NUnit.Framework;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    public sealed class UIFoundationTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        private ResourceManager resources;
        private UIManager ui;
        private HubView hub;
        private int loads;
        private GameObject New(string name, bool active = true)
        {
            var go = new GameObject(name, typeof(RectTransform)); roots.Add(go); go.SetActive(active); return go;
        }
        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static TMP_Text Text(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); return go.GetComponent<TMP_Text>();
        }
        private static Button Button(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); return go.GetComponent<Button>();
        }
        [SetUp]
        public void Setup()
        {
            resources = New("Resources").AddComponent<ResourceManager>();
            resources.Initialize(path => { loads++; return templates.TryGetValue(path, out var prefab) ? prefab : null; });
            ui = New("UI").AddComponent<UIManager>(); ui.Initialize();
            hub = New("Hub").AddComponent<HubView>();
            foreach (string field in new[] { "homePanel", "characterPanel", "gamePanel", "settingsPanel" })
            { var go = new GameObject(field); go.transform.SetParent(hub.transform); Set(hub, field, go); }
            Set(hub, "notice", Text(hub.transform, "Notice")); ui.Register(hub);
            var message = New("Message template", false); var view = message.AddComponent<MessageBoxView>();
            Set(view, "title", Text(message.transform, "Title")); Set(view, "body", Text(message.transform, "Body"));
            Set(view, "confirm", Button(message.transform, "Confirm")); Set(view, "cancel", Button(message.transform, "Cancel"));
            templates["UI/Overlays/MessageBox"] = message;
            var popup = New("Popup template", false); var popupView = popup.AddComponent<PopupView>();
            Set(popupView, "close", Button(popup.transform, "Close")); templates["UI/Overlays/Popup"] = popup;
            foreach (string name in new[] { "Toast", "Tooltip" })
            {
                var go = New(name + " template", false); var textView = go.AddComponent<TransientTextView>();
                Set(textView, "label", Text(go.transform, "Text")); templates["UI/Overlays/" + name] = go;
            }
        }
        [TearDown]
        public void Teardown()
        {
            ui.Shutdown(); resources.Shutdown();
            foreach (var go in roots) if (go != null) Object.DestroyImmediate(go);
            roots.Clear(); templates.Clear(); loads = 0;
        }
        private MessageBoxView MessageInstance()
        {
            foreach (var view in Object.FindObjectsByType<MessageBoxView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (view.gameObject != templates["UI/Overlays/MessageBox"]) return view;
            throw new Exception("No message instance.");
        }
        [Test]
        public void PrefabCacheLoadsOnceAndRefusesEvictionUntilAllInstancesAreReleased()
        {
            var a = resources.Spawn("UI/Overlays/Toast", null); var b = resources.Spawn("UI/Overlays/Toast", null);
            Assert.That(loads, Is.EqualTo(1)); Assert.That(resources.LiveCount, Is.EqualTo(2));
            Assert.That(resources.Evict("UI/Overlays/Toast"), Is.False);
            resources.Release(a); resources.Release(a);
            Assert.That(resources.Evict("UI/Overlays/Toast"), Is.False);
            resources.Release(b); Assert.That(resources.Evict("UI/Overlays/Toast"), Is.True);
            Assert.That(resources.CachedCount, Is.Zero);
        }
        [Test]
        public void ExternallyDestroyedInstancesDoNotPinTheCache()
        {
            var instance = resources.Spawn("UI/Overlays/Toast", null); Object.DestroyImmediate(instance);
            Assert.That(resources.LiveCount, Is.Zero); Assert.That(resources.Evict("UI/Overlays/Toast"), Is.True);
        }
        [Test]
        public void MissingPrefabAndDuplicateResourceManagerFailWithoutReplacingSingleton()
        {
            Assert.Throws<InvalidOperationException>(() => resources.Spawn("missing", null));
            var duplicate = New("Duplicate").AddComponent<ResourceManager>();
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize());
            duplicate.Shutdown(); Assert.That(ResourceManager.Instance, Is.SameAs(resources)); Assert.That(resources.LiveCount, Is.Zero);
        }




        [Test]
        public void TogglePresentationDoesNotEmitInputAndRepeatedEnableDoesNotDuplicateListeners()
        {
            var go = New("Toggle"); var control = go.AddComponent<Toggle>(); var view = go.AddComponent<ToggleView>();
            int changes = 0; view.Changed += _ => changes++;
            view.Present("Music", false); Assert.That(changes, Is.Zero);
            go.SetActive(false); go.SetActive(true); go.SetActive(false); go.SetActive(true);
            control.isOn = true; Assert.That(changes, Is.EqualTo(1));
            view.ResetForReuse(); control.isOn = true; Assert.That(changes, Is.EqualTo(1));
        }

    }
}
