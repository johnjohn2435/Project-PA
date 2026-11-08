using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PocketArcade.Editor
{
    public static class UIReferenceRemapper
    {
        public static void Remap(GameObject screen, GameObject replaced, IDictionary<Object, Object> map)
        {
            // Never serialize Transform ownership (m_Father/m_Children), GameObject ownership,
            // or prefab metadata. Only fields declared by managed UI behaviours are references to remap.
            foreach (var component in screen.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.transform.IsChildOf(replaced.transform)) continue;
                var fields = SerializedFieldNames(component.GetType());
                var data = new SerializedObject(component);
                var property = data.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    string path = property.propertyPath;
                    int separator = path.IndexOf('.');
                    string field = separator < 0 ? path : path.Substring(0, separator);
                    if (!fields.Contains(field) || property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var original = property.objectReferenceValue;
                    if (original == null || !map.TryGetValue(original, out var replacement)) continue;
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
                if (changed)
                {
                    data.ApplyModifiedPropertiesWithoutUndo();
                    if (PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
        }

        private static HashSet<string> SerializedFieldNames(Type type)
        {
            var names = new HashSet<string>();
            for (; type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic && !field.IsInitOnly && !field.IsDefined(typeof(NonSerializedAttribute), false) &&
                        (field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false)))
                        names.Add(field.Name);
            return names;
        }

        public static void ValidateHierarchy(GameObject root)
        {
            var visited = new HashSet<Transform>();
            var pending = new Stack<Transform>();
            pending.Push(root.transform);
            while (pending.Count > 0)
            {
                var parent = pending.Pop();
                if (!visited.Add(parent)) throw new InvalidOperationException("Duplicate Transform link in " + root.name + ": " + parent.name);
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child == null || child.parent != parent) throw new InvalidOperationException("Invalid Transform parent in " + root.name);
                    pending.Push(child);
                }
            }
        }
    }
}
