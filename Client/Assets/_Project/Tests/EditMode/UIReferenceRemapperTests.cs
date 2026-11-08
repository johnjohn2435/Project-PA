using System.Collections.Generic;
using NUnit.Framework;
using PocketArcade.Editor;
using PocketArcade.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    public sealed class UIReferenceRemapperTests
    {
        [Test]
        public void RemappingViewReferenceNeverRewritesTransformChildrenOrSiblingOrder()
        {
            var screen = new GameObject("Screen", typeof(RectTransform));
            try
            {
                var view = screen.AddComponent<HubView>();
                var old = new GameObject("Old", typeof(RectTransform)); old.transform.SetParent(screen.transform);
                var replacement = new GameObject("New", typeof(RectTransform)); replacement.transform.SetParent(screen.transform);
                var data = new SerializedObject(view); data.FindProperty("homePanel").objectReferenceValue = old; data.ApplyModifiedPropertiesWithoutUndo();
                UIReferenceRemapper.Remap(screen, old, new Dictionary<Object, Object>
                { [old] = replacement, [old.transform] = replacement.transform });
                data.Update();
                Assert.That(data.FindProperty("homePanel").objectReferenceValue, Is.SameAs(replacement));
                Assert.That(screen.transform.childCount, Is.EqualTo(2));
                Assert.That(screen.transform.GetChild(0), Is.SameAs(old.transform));
                Assert.That(screen.transform.GetChild(1), Is.SameAs(replacement.transform));
                Assert.That(old.transform.parent, Is.SameAs(screen.transform));
                Assert.That(replacement.transform.parent, Is.SameAs(screen.transform));
                Object.DestroyImmediate(old);
                Assert.That(screen.transform.childCount, Is.EqualTo(1));
                Assert.That(screen.transform.GetChild(0), Is.SameAs(replacement.transform));
                UIReferenceRemapper.ValidateHierarchy(screen);
            }
            finally { Object.DestroyImmediate(screen); }
        }

        [Test]
        public void ArraysAndInheritedNestedUIReferencesAreRemappedButOriginalSubtreeIsUntouched()
        {
            var screen = new GameObject("Screen", typeof(RectTransform));
            try
            {
                var view = screen.AddComponent<HubView>();
                var old = new GameObject("Old", typeof(RectTransform), typeof(Button)); old.transform.SetParent(screen.transform);
                var replacement = new GameObject("New", typeof(RectTransform), typeof(Button)); replacement.transform.SetParent(screen.transform);
                var oldButton = old.GetComponent<Button>(); var newButton = replacement.GetComponent<Button>();
                var outside = screen.AddComponent<Button>();
                var nav = outside.navigation; nav.selectOnRight = oldButton; outside.navigation = nav; oldButton.navigation = nav;
                var data = new SerializedObject(view); var array = data.FindProperty("backButtons");
                array.arraySize = 2; array.GetArrayElementAtIndex(0).objectReferenceValue = oldButton; array.GetArrayElementAtIndex(1).objectReferenceValue = outside;
                data.ApplyModifiedPropertiesWithoutUndo();
                UIReferenceRemapper.Remap(screen, old, new Dictionary<Object, Object>
                { [old] = replacement, [old.transform] = replacement.transform, [oldButton] = newButton });
                data.Update();
                Assert.That(data.FindProperty("backButtons").GetArrayElementAtIndex(0).objectReferenceValue, Is.SameAs(newButton));
                Assert.That(data.FindProperty("backButtons").GetArrayElementAtIndex(1).objectReferenceValue, Is.SameAs(outside));
                Assert.That(outside.navigation.selectOnRight, Is.SameAs(newButton));
                Assert.That(oldButton.navigation.selectOnRight, Is.SameAs(oldButton));
                UIReferenceRemapper.ValidateHierarchy(screen);
            }
            finally { Object.DestroyImmediate(screen); }
        }

        [TestCase("Common/Button")]
        [TestCase("Common/Toggle")]
        [TestCase("Common/CharacterCard")]
        [TestCase("Common/MiniGameCard")]
        [TestCase("Common/GameMedalCard")]
        [TestCase("Panels/Home")]
        [TestCase("Panels/Characters")]
        [TestCase("Panels/Games")]
        [TestCase("Panels/Settings")]
        [TestCase("Screens/Hub")]
        [TestCase("Screens/GameEntry")]
        [TestCase("Overlays/MessageBox")]
        [TestCase("Overlays/Popup")]
        [TestCase("Overlays/Toast")]
        [TestCase("Overlays/Tooltip")]
        public void GeneratedPrefabHierarchyAndUIReferencesAreValid(string name)
        {
            // Read-only asset inspection. Does not invoke setup or generate/migrate any scene/prefab.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/UI/" + name + ".prefab");
            if (prefab == null) Assert.Ignore("Run UI Foundation setup in the editor before this asset inspection.");
            UIReferenceRemapper.ValidateHierarchy(prefab);
            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Assert.That(component, Is.Not.Null, name + " has a missing script");
                if (component.GetType().Namespace != "PocketArcade.UI") continue;
                var property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // uGUI uses its built-in default when a Graphic has no explicit material.
                        if (component is UnityEngine.UI.Graphic && property.propertyPath == "m_Material") continue;
                        var field = component.GetType().GetField(property.name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (field != null && System.Attribute.IsDefined(field, typeof(OptionalUIReferenceAttribute))) continue;
                        Assert.That(property.objectReferenceValue, Is.Not.Null, name + ": " + property.propertyPath);
                    }
            }
            if (name == "Screens/Hub")
            {
                Assert.That(prefab.GetComponentsInChildren<CharacterCardView>(true).Length, Is.Zero);
                Assert.That(prefab.GetComponentInChildren<CharacterRosterView>(true), Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<MiniGameCardView>(true).Length, Is.EqualTo(3));
                Assert.That(prefab.GetComponentsInChildren<Toggle>(true).Length, Is.EqualTo(3));
            }
        }
    }
}
