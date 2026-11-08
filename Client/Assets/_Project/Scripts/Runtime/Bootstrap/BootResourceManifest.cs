using System;
using System.Collections.Generic;
using PocketArcade.Meta;
using UnityEngine;
namespace PocketArcade
{
    [Serializable]
    public sealed class BootResourceSlot
    {
        public string path;
        public string kind;
        public bool required;
        [NonSerialized] public UnityEngine.Object fallback;
        [NonSerialized] public bool direct;
        public Type AssetType => kind == "Prefab" ? typeof(GameObject) : kind == "Sprite" ? typeof(Sprite) :
            kind == "Asset" ? typeof(UnityEngine.Object) : kind == "AudioClip" ? typeof(AudioClip) : kind == "Character" ? typeof(CharacterDefinition) :
            throw new InvalidOperationException("Unknown resource kind: " + kind);
    }
    [Serializable]
    public sealed class BootResourceManifest
    {
        public int version;
        public BootResourceSlot[] resources;
        public const string ManifestPath = "Boot/BootResources";
        public const string UiClick = "Audio/UI/Click";
        public const string HubBgm = "Audio/BGM/Hub";
        public static string Portrait(string id) => "Characters/" + id + "/Portrait";
        public static string CharacterPrefab(string id) => "Characters/" + id + "/Character";
        public static List<BootResourceSlot> Parse(string json, GameCatalog catalog)
        {
            var manifest = JsonUtility.FromJson<BootResourceManifest>(json);
            if (manifest == null || manifest.version != 1 || manifest.resources == null || manifest.resources.Length == 0)
                throw new InvalidOperationException("BootResources.json needs version 1 and a non-empty resources list.");
            catalog.Validate();
            var result = new List<BootResourceSlot>(manifest.resources);
            var entries = new Dictionary<string, BootResourceSlot>();
            foreach (var slot in result)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.path) || slot.path.StartsWith("/") || slot.path.Contains("..") || slot.path.Contains("\\") || slot.path.Contains("."))
                    throw new InvalidOperationException("Use a Resources path without extension: " + slot?.path);
                var type = slot.AssetType;
                if (entries.ContainsKey(slot.path)) throw new InvalidOperationException("Duplicate boot resource: " + slot.path);
                entries.Add(slot.path, slot);
            }
            foreach (var character in catalog.Characters)
            {
                var definitionPath = "Characters/" + character.Id + "/Definition";
                if (entries.ContainsKey(definitionPath)) throw new InvalidOperationException("Character Definition keys are supplied by GameCatalog.");
                result.Add(new BootResourceSlot { path = definitionPath, kind = "Character", required = true, fallback = character, direct = true });
                AddCharacterSlot(result, entries, Portrait(character.Id), "Sprite", character.SourcePortrait);
                AddCharacterSlot(result, entries, CharacterPrefab(character.Id), "Prefab", null);
            }
            return result;
        }
        private static void AddCharacterSlot(List<BootResourceSlot> result, Dictionary<string, BootResourceSlot> entries, string path, string kind, UnityEngine.Object fallback)
        {
            if (!entries.TryGetValue(path, out var slot))
            { slot = new BootResourceSlot { path = path, kind = kind }; result.Add(slot); entries.Add(path, slot); }
            if (slot.kind != kind) throw new InvalidOperationException("Wrong character resource kind: " + path);
            slot.fallback = fallback;
        }
    }
}
