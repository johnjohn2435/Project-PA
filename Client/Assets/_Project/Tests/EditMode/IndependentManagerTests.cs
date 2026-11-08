using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
namespace PocketArcade.Tests
{
    public sealed class IndependentManagerTests
    {
        private GameObject root;
        [SetUp] public void Setup() => root = new GameObject("Manager structure test");
        [TearDown] public void Teardown() { if (root != null) Object.DestroyImmediate(root); }
        [Test]
        public void BootstrapDoesNotAutoAttachAnyManagers()
        {
            root.AddComponent<GameBootstrap>();
            Assert.That(root.GetComponents<PersistentManager>(), Is.Empty);
        }
        [TestCase(typeof(ResourceManager))]
        [TestCase(typeof(DataManager))]
        [TestCase(typeof(SoundManager))]
        [TestCase(typeof(SceneFlowManager))]
        [TestCase(typeof(UIManager))]
        public void EachManagerIsValidAsItsOwnRoot(Type type)
        {
            var manager = root.AddComponent(type) as PersistentManager;
            Assert.That(manager, Is.Not.Null);
            Assert.DoesNotThrow(() => GameBootstrap.ValidatePrefab(manager));
        }
        [Test]
        public void CombinedManagerRootIsRejected()
        {
            var manager = root.AddComponent<ResourceManager>(); root.AddComponent<UIManager>();
            Assert.Throws<InvalidOperationException>(() => GameBootstrap.ValidatePrefab(manager));
        }
        [Test]
        public void NestedManagerAndBootstrapContainerAreRejected()
        {
            var manager = root.AddComponent<ResourceManager>();
            var child = new GameObject("Nested"); child.transform.SetParent(root.transform); child.AddComponent<UIManager>();
            Assert.Throws<InvalidOperationException>(() => manager.ValidateIndependentRoot());
            Object.DestroyImmediate(child);
            root.AddComponent<GameBootstrap>();
            Assert.Throws<InvalidOperationException>(() => manager.ValidateIndependentRoot());
        }
        [Test]
        public void ParentedOrDisabledManagerCannotBecomeAPersistentRoot()
        {
            var child = new GameObject("Child"); child.transform.SetParent(root.transform); var manager = child.AddComponent<ResourceManager>();
            Assert.Throws<InvalidOperationException>(() => manager.ValidateIndependentRoot());
            child.transform.SetParent(null);
            try
            {
                manager.enabled = false; Assert.Throws<InvalidOperationException>(() => manager.ValidateIndependentRoot());
                manager.enabled = true; child.SetActive(false); Assert.Throws<InvalidOperationException>(() => manager.ValidateIndependentRoot());
            }
            finally { Object.DestroyImmediate(child); }
        }
        [Test]
        public void MissingManagerPrefabIsRejectedBeforeInstantiation()
        { Assert.Throws<InvalidOperationException>(() => GameBootstrap.ValidatePrefab(null)); }
    }
}
