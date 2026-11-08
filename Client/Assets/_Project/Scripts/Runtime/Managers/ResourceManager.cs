using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace PocketArcade
{
    // Resources backend for the small local MVP. Instances and loaded assets have separate lifetimes.
    [DisallowMultipleComponent]
    public sealed class ResourceManager : PersistentManager
    {
        public static ResourceManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        private readonly Dictionary<string, UnityEngine.Object> cache = new Dictionary<string, UnityEngine.Object>();
        private readonly Dictionary<GameObject, string> instances = new Dictionary<GameObject, string>();
        private Func<string, GameObject> loader;
        private readonly HashSet<string> pinned = new HashSet<string>();
        private readonly List<string> missingOptional = new List<string>();
        private int generation;
        private bool preloading;
        public bool IsPreloaded { get; private set; }
        public Exception PreloadError { get; private set; }
        public IReadOnlyList<string> MissingOptional => missingOptional;
        public T GetLoaded<T>(string path) where T : UnityEngine.Object => cache.TryGetValue(path, out var asset) ? asset as T : null;

        public IEnumerator Preload(IReadOnlyList<BootResourceSlot> slots, Action<float, string> progress = null,
            Func<string, Type, IResourceLoadOperation> load = null)
        {
            if (!IsInitialized) throw new InvalidOperationException("Initialize ResourceManager first.");
            if (preloading) throw new InvalidOperationException("Boot preload is already running.");
            if (IsPreloaded) throw new InvalidOperationException("Boot resources are already sealed for this session. Restart boot to change them.");
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            preloading = true; IsPreloaded = false; PreloadError = null; missingOptional.Clear();
            int version = generation;
            try
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!IsInitialized || generation != version) yield break;
                    var slot = slots[i];
                    IResourceLoadOperation operation = null;
                    UnityEngine.Object asset = null;
                    try
                    {
                        if (slot == null || string.IsNullOrWhiteSpace(slot.path)) throw new InvalidOperationException("Invalid boot resource slot.");
                        cache.TryGetValue(slot.path, out asset);
                        if (asset == null && slot.direct) asset = slot.fallback;
                        if (asset == null && !slot.direct) operation = load != null ? load(slot.path, slot.AssetType) : new ResourceLoadOperation(slot.path, slot.AssetType);
                    }
                    catch (Exception error) { PreloadError = error; }
                    if (PreloadError != null) yield break;
                    if (operation != null)
                    {
                        while (!operation.IsDone)
                        {
                            if (!IsInitialized || generation != version) yield break;
                            progress?.Invoke((i + Mathf.Clamp01(operation.Progress)) / slots.Count, slot.path);
                            yield return null;
                        }
                        if (!IsInitialized || generation != version) yield break;
                        asset = operation.Asset;
                    }
                    asset = asset != null ? asset : slot.fallback;
                    if (asset != null && !slot.AssetType.IsInstanceOfType(asset))
                    { PreloadError = new InvalidOperationException("Wrong resource type: " + slot.path); yield break; }
                    if (asset == null && slot.required)
                    { PreloadError = new InvalidOperationException("Missing required boot resource: " + slot.path); yield break; }
                    if (asset != null) { cache[slot.path] = asset; pinned.Add(slot.path); }
                    else missingOptional.Add(slot.path);
                    progress?.Invoke((i + 1f) / slots.Count, slot.path);
                    yield return null;
                }
                if (IsInitialized && generation == version) IsPreloaded = true;
            }
            finally { if (generation == version) preloading = false; }
        }
        public int CachedCount => cache.Count;
        public int LiveCount { get { Prune(); return instances.Count; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        public void Initialize(Func<string, GameObject> prefabLoader = null)
        {
            if (Instance != null && Instance != this) throw new InvalidOperationException("Duplicate ResourceManager.");
            if (IsInitialized) return;
            // An injected in-memory loader supports isolated tests. Production never loads from Spawn.
            loader = prefabLoader;
            Persist();
            Instance = this;
            IsInitialized = true;
        }
        public GameObject Spawn(string path, Transform parent)
        {
            if (!IsInitialized) throw new InvalidOperationException("Initialize ResourceManager first.");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A Resources prefab path is required.");
            if (loader == null && !IsPreloaded) throw new InvalidOperationException("Wait for all boot resources before spawning UI.");
            if (!cache.TryGetValue(path, out var prefab) || prefab == null)
            {
                if (loader == null || IsPreloaded) throw new InvalidOperationException("Resource was not loaded at boot: " + path + ". Update BootResources.json and restart from BootScene.");
                prefab = loader(path);
                if (prefab == null) throw new InvalidOperationException("Missing UI prefab: " + path + ". Run UI Foundation setup.");
                cache[path] = prefab;
            }
            if (!(prefab is GameObject gameObjectPrefab)) throw new InvalidOperationException("Not a prefab: " + path);
            var instance = Instantiate(gameObjectPrefab, parent, false);
            instances.Add(instance, path);
            return instance;
        }
        public void Release(GameObject instance)
        {
            if (ReferenceEquals(instance, null) || !instances.Remove(instance)) return;
            if (instance != null)
            {
                instance.SetActive(false);
                if (Application.isPlaying) Destroy(instance); else DestroyImmediate(instance);
            }
        }
        public bool Evict(string path)
        {
            Prune();
            if (pinned.Contains(path) || instances.ContainsValue(path)) return false;
            return cache.Remove(path);
        }
        public AsyncOperation TrimUnused()
        {
            if (!IsInitialized) throw new InvalidOperationException("Initialize ResourceManager first.");
            Prune();
            foreach (var key in new List<string>(cache.Keys))
                if (!pinned.Contains(key) && !instances.ContainsValue(key)) cache.Remove(key);
            // Do not call UnloadAsset on a prefab. Unity retains assets still referenced by live objects.
            return Resources.UnloadUnusedAssets();
        }
        private void Prune()
        {
            foreach (var item in new List<GameObject>(instances.Keys)) if (item == null) instances.Remove(item);
        }
        public void Shutdown()
        {
            foreach (var item in new List<GameObject>(instances.Keys)) Release(item);
            generation++; preloading = false; IsPreloaded = false; PreloadError = null;
            pinned.Clear(); missingOptional.Clear(); cache.Clear(); loader = null; IsInitialized = false;
            if (Instance == this) Instance = null;
        }
        private void OnDestroy() => Shutdown();
    }
}
