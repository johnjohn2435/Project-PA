using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PocketArcade.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    public sealed class BootResourceTests
    {
        private GameObject root;
        private ResourceManager resources;
        private readonly List<Object> objects = new List<Object>();
        private sealed class Operation : IResourceLoadOperation
        {
            public bool IsDone { get; set; }
            public float Progress { get; set; }
            public Object Asset { get; set; }
        }
        [SetUp]
        public void Setup() { root = new GameObject("Boot resource tests"); resources = root.AddComponent<ResourceManager>(); resources.Initialize(); }
        [TearDown]
        public void Teardown()
        {
            resources.Shutdown(); Object.DestroyImmediate(root);
            foreach (var value in objects) if (value != null) Object.DestroyImmediate(value);
            objects.Clear();
        }
        private static BootResourceSlot Slot(string path, string kind = "Prefab", bool required = true) => new BootResourceSlot { path = path, kind = kind, required = required };
        private static void Drain(IEnumerator routine)
        {
            int count = 0;
            while (routine.MoveNext()) if (++count > 100) Assert.Fail("Preload must finish without an infinite wait.");
        }
        [Test]
        public void PreloadWaitsForCompletionAndSpawnUsesPinnedCache()
        {
            var prefab = new GameObject("Template"); objects.Add(prefab);
            var operation = new Operation { Progress = .5f };
            float progress = -1;
            var routine = resources.Preload(new[] { Slot("UI/Test") }, (value, _) => progress = value, (_, __) => operation);
            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(resources.IsPreloaded, Is.False); Assert.That(resources.GetLoaded<GameObject>("UI/Test"), Is.Null);
            Assert.That(progress, Is.EqualTo(.5f));
            operation.Asset = prefab; operation.IsDone = true; Drain(routine);
            Assert.That(resources.IsPreloaded, Is.True); Assert.That(progress, Is.EqualTo(1f));
            Assert.That(resources.Evict("UI/Test"), Is.False);
            var instance = resources.Spawn("UI/Test", root.transform); Assert.That(instance, Is.Not.Null);
            resources.Release(instance); Assert.That(resources.Evict("UI/Test"), Is.False);
        }
        [Test]
        public void RequiredMissingAssetFailsAndCanRetrySuccessfully()
        {
            Drain(resources.Preload(new[] { Slot("UI/Required") }, null, (_, __) => new Operation { IsDone = true }));
            Assert.That(resources.IsPreloaded, Is.False); Assert.That(resources.PreloadError.Message, Does.Contain("UI/Required"));
            resources.Shutdown(); resources.Initialize();
            var prefab = new GameObject("Supplied"); objects.Add(prefab);
            Drain(resources.Preload(new[] { Slot("UI/Required") }, null, (_, __) => new Operation { IsDone = true, Asset = prefab }));
            Assert.That(resources.IsPreloaded, Is.True); Assert.That(resources.PreloadError, Is.Null);
        }
        [Test]
        public void MissingAudioIsOptionalAndLaterProvidedClipCanBeLoaded()
        {
            var slot = Slot(BootResourceManifest.UiClick, "AudioClip", false);
            Drain(resources.Preload(new[] { slot }, null, (_, __) => new Operation { IsDone = true }));
            Assert.That(resources.IsPreloaded, Is.True); Assert.That(resources.MissingOptional, Does.Contain(slot.path));
            Assert.That(resources.GetLoaded<AudioClip>(slot.path), Is.Null);
            resources.Shutdown(); resources.Initialize();
            var clip = AudioClip.Create("Click", 64, 1, 44100, false); objects.Add(clip);
            Drain(resources.Preload(new[] { slot }, null, (_, __) => new Operation { IsDone = true, Asset = clip }));
            Assert.That(resources.GetLoaded<AudioClip>(slot.path), Is.SameAs(clip)); Assert.That(resources.MissingOptional, Is.Empty);
        }
        [Test]
        public void WrongAssetTypeIsAnErrorEvenForAnOptionalSlot()
        {
            var wrong = new GameObject("NotAudio"); objects.Add(wrong);
            Drain(resources.Preload(new[] { Slot("Audio/Click", "AudioClip", false) }, null, (_, __) => new Operation { IsDone = true, Asset = wrong }));
            Assert.That(resources.IsPreloaded, Is.False); Assert.That(resources.PreloadError.Message, Does.Contain("Wrong resource type"));
        }
        [Test]
        public void ShutdownPreventsLateAsyncResultFromRepopulatingCache()
        {
            var operation = new Operation();
            var routine = resources.Preload(new[] { Slot("UI/Late") }, null, (_, __) => operation);
            Assert.That(routine.MoveNext(), Is.True);
            resources.Shutdown(); resources.Initialize();
            operation.Asset = new GameObject("Late"); objects.Add(operation.Asset); operation.IsDone = true;
            Assert.That(routine.MoveNext(), Is.False);
            Assert.That(resources.CachedCount, Is.Zero); Assert.That(resources.IsPreloaded, Is.False);
        }
        [Test]
        public void ConcurrentPreloadIsRejected()
        {
            var operation = new Operation();
            var first = resources.Preload(new[] { Slot("UI/One") }, null, (_, __) => operation);
            Assert.That(first.MoveNext(), Is.True);
            var second = resources.Preload(new[] { Slot("UI/Two") });
            Assert.Throws<InvalidOperationException>(() => second.MoveNext());
            resources.Shutdown(); Assert.That(first.MoveNext(), Is.False);
        }
        [Test]
        public void FallbackPortraitRemainsAvailableWhenOptionalPathIsMissing()
        {
            var texture = new Texture2D(2,2); objects.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0,0,2,2), Vector2.zero); objects.Add(sprite);
            var definition = ScriptableObject.CreateInstance<CharacterDefinition>(); objects.Add(definition);
            var data = new SerializedObject(definition); data.FindProperty("id").stringValue = "test";
            data.FindProperty("portrait").objectReferenceValue = sprite; data.ApplyModifiedPropertiesWithoutUndo();
            var slot = Slot(BootResourceManifest.Portrait("test"), "Sprite", false); slot.fallback = definition.SourcePortrait;
            Drain(resources.Preload(new[] { slot }, null, (_, __) => new Operation { IsDone = true }));
            Assert.That(definition.Portrait, Is.SameAs(sprite)); Assert.That(resources.MissingOptional, Is.Empty);
        }
        [Test]
        public void ManifestRejectsDuplicatePathsAndUnknownTypes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            Assert.Throws<InvalidOperationException>(() => BootResourceManifest.Parse("{\"version\":1,\"resources\":[{\"path\":\"UI/X\",\"kind\":\"Prefab\"},{\"path\":\"UI/X\",\"kind\":\"Prefab\"}]}", catalog));
            Assert.Throws<InvalidOperationException>(() => BootResourceManifest.Parse("{\"version\":1,\"resources\":[{\"path\":\"UI/X\",\"kind\":\"Unknown\"}]}", catalog));
        }
        [UnityTest]
        public IEnumerator ActualManifestLoadsRequiredUIAndCatalogThroughAsyncRequests()
        {
            var request = Resources.LoadAsync<TextAsset>(BootResourceManifest.ManifestPath);
            while (!request.isDone) yield return null;
            Assert.That(request.asset, Is.Not.Null);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/_Project/ScriptableObjects/GameCatalog.asset");
            var slots = BootResourceManifest.Parse(((TextAsset)request.asset).text, catalog);
            var routine = resources.Preload(slots);
            while (routine.MoveNext()) yield return null;
            Assert.That(resources.PreloadError, Is.Null); Assert.That(resources.IsPreloaded, Is.True);
            Assert.That(resources.GetLoaded<GameObject>("UI/Screens/Hub"), Is.Not.Null);
            Assert.That(resources.GetLoaded<GameObject>("UI/Overlays/MessageBox"), Is.Not.Null);
            foreach (var character in catalog.Characters)
                Assert.That(resources.GetLoaded<CharacterDefinition>("Characters/"+character.Id+"/Definition"), Is.SameAs(character));
            Assert.That(resources.LiveCount, Is.Zero, "Preloading must not instantiate UI or characters.");
        }
    }
}
