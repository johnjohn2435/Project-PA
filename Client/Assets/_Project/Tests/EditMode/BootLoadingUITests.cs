using System;
using System.Collections;
using NUnit.Framework;
using PocketArcade.Editor;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace PocketArcade.Tests
{
    public sealed class BootLoadingUITests
    {
        private GameObject root;
        private BootStatusView view;
        private Slider bar;
        private TMP_Text percent;
        private Button retry;
        [SetUp]
        public void Setup()
        {
            L10n.Initialize(System.IO.File.ReadAllText(StringTableSetup.TableFile));
            root = new GameObject("Boot test", typeof(RectTransform)); root.SetActive(false);
            view = root.AddComponent<BootStatusView>();
            var label = Child("Status").AddComponent<TextMeshProUGUI>();
            percent = Child("Percent").AddComponent<TextMeshProUGUI>();
            bar = Child("Progress").AddComponent<Slider>(); bar.minValue = 0; bar.maxValue = 1;
            retry = Child("Retry").AddComponent<Button>();
            var data = new SerializedObject(view);
            data.FindProperty("label").objectReferenceValue = label;
            data.FindProperty("percentage").objectReferenceValue = percent;
            data.FindProperty("progressBar").objectReferenceValue = bar;
            data.FindProperty("retryButton").objectReferenceValue = retry;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private GameObject Child(string name)
        { var child = new GameObject(name, typeof(RectTransform)); child.transform.SetParent(root.transform); return child; }
        [TearDown] public void Teardown() { Object.DestroyImmediate(root); L10n.Reset(); }
        [TestCase(.25f, .25f, "25%")]
        [TestCase(-2f, 0f, "0%")]
        [TestCase(2f, 1f, "100%")]
        public void ProgressUpdatesBarAndPercentageWithoutInputEvents(float input, float expected, string text)
        {
            int events = 0; bar.onValueChanged.AddListener(_ => events++);
            view.Present(input, "Loading", BootState.Loading);
            Assert.That(bar.value, Is.EqualTo(expected)); Assert.That(percent.text, Is.EqualTo(text));
            Assert.That(events, Is.Zero); Assert.That(retry.gameObject.activeSelf, Is.False);
        }
        [Test]
        public void RetryIsVisibleOnlyAfterFailureAndResetsForTheNextAttempt()
        {
            view.Present(.6f, "Internal path", BootState.Failed);
            Assert.That(retry.gameObject.activeSelf, Is.True);
            view.Present(0f, "Loading", BootState.Loading);
            Assert.That(retry.gameObject.activeSelf, Is.False); Assert.That(bar.value, Is.Zero);
            view.Present(1, "Ready", BootState.Ready); Assert.That(percent.text, Is.EqualTo("100%"));
        }
        [Test]
        public void CurrentLocalResourceInventoryIsFullyRegistered()
        {
            BootResourceSetup.ValidateInventory();
            Assert.That(BootResourceSetup.DiscoverResources().ContainsKey("UI/Screens/Hub"), Is.True);
        }
        [Test]
        public void ProductionSpawnNeverLoadsOnDemandAndCompletedPreloadIsSealed()
        {
            var resources = root.AddComponent<ResourceManager>(); resources.Initialize();
            try
            {
                Assert.Throws<InvalidOperationException>(() => resources.Spawn("UI/Screens/Hub", null));
                var template = Child("Template");
                IEnumerator routine = resources.Preload(new[] { new BootResourceSlot { path = "Loaded", kind = "Prefab", required = true, direct = true, fallback = template } });
                while (routine.MoveNext()) { }
                Assert.That(resources.IsPreloaded, Is.True);
                var spawned = resources.Spawn("Loaded", root.transform); Assert.That(spawned, Is.Not.Null);
                Assert.Throws<InvalidOperationException>(() => resources.Spawn("UI/Screens/Hub", null));
                var second = resources.Preload(new[] { new BootResourceSlot { path = "Other", kind = "Prefab" } });
                Assert.Throws<InvalidOperationException>(() => second.MoveNext());
            }
            finally { resources.Shutdown(); }
        }
        [Test]
        public void GenericLocalAssetCanBePreloadedAndRetained()
        {
            var resources = root.AddComponent<ResourceManager>(); resources.Initialize();
            var text = new TextAsset("Local data");
            try
            {
                var routine = resources.Preload(new[] { new BootResourceSlot { path="Data/Local",kind="Asset",required=true,direct=true,fallback=text } });
                while (routine.MoveNext()) { }
                Assert.That(resources.GetLoaded<TextAsset>("Data/Local"), Is.SameAs(text));
                Assert.That(resources.Evict("Data/Local"), Is.False);
            }
            finally { resources.Shutdown(); Object.DestroyImmediate(text); }
        }
    }
}
