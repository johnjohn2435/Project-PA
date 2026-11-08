using System;
using System.Collections;
using System.Collections.Generic;
using PocketArcade.Core;
using PocketArcade.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketArcade
{
    public enum BootState { Idle, Loading, Failed, Ready }
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private ResourceManager resourcePrefab;
        [SerializeField] private DataManager dataPrefab;
        [SerializeField] private SoundManager soundPrefab;
        [SerializeField] private SceneFlowManager sceneFlowPrefab;
        [SerializeField] private UIManager uiPrefab;
        private ResourceManager resources;
        private DataManager data;
        private SoundManager sound;
        private SceneFlowManager scenes;
        private UIManager ui;
        public static event Action<string> Failed;
        public static event Action<float, string> ProgressChanged;
        public static BootState State { get; private set; }
        public static float Progress { get; private set; }
        public static string Status { get; private set; } = "";
        private static GameBootstrap activeBootstrap;
        private bool duplicate;
        public static bool ManagersReady => ResourceManager.Instance != null && ResourceManager.Instance.IsInitialized &&
            DataManager.Instance != null && DataManager.Instance.IsInitialized &&
            SoundManager.Instance != null && SoundManager.Instance.IsInitialized &&
            SceneFlowManager.Instance != null && SceneFlowManager.Instance.IsInitialized &&
            UIManager.Instance != null && UIManager.Instance.IsInitialized;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        { activeBootstrap = null; Failed = null; ProgressChanged = null; State = BootState.Idle; Progress = 0; Status = ""; }
        private void Awake()
        {
            duplicate = activeBootstrap != null && activeBootstrap != this;
            if (duplicate) return;
            activeBootstrap = this;
        }
        private void Start()
        {
            if (duplicate)
            {
                if (State == BootState.Ready && ManagersReady && !SceneFlowManager.Instance.OpenHub(out string error)) Debug.LogWarning(error, this);
                Destroy(gameObject); return;
            }
            // Returning to BootScene reuses the five independent live managers.
            if (ManagersReady && ResourceManager.Instance.IsPreloaded)
            {
                State = BootState.Ready; Report(1, L10n.Text("boot.ready"));
                if (!SceneFlowManager.Instance.OpenHub(out string error)) Fail(new InvalidOperationException(error));
                return;
            }
            ShutdownManagers(); State = BootState.Idle;
            Retry();
        }
        public static bool Retry()
        {
            if (activeBootstrap == null || State == BootState.Loading || State == BootState.Ready) return false;
            State = BootState.Loading;
            activeBootstrap.StartCoroutine(activeBootstrap.Load());
            return true;
        }
        private void Report(float value, string message)
        { Progress = value; Status = message; ProgressChanged?.Invoke(value, message); }
        private IEnumerator Load()
        {
            var stringsRequest = Resources.LoadAsync<TextAsset>(StringTable.ResourcePath);
            yield return stringsRequest;
            Exception stringError = null;
            try
            {
                var asset = stringsRequest.asset as TextAsset;
                if (asset == null) throw new InvalidOperationException("Missing Resources/Data/StringTable.json");
                L10n.Initialize(asset.text);
            }
            catch (Exception error) { stringError = error; }
            if (stringError != null) { Fail(stringError); yield break; }
            Report(0, L10n.Text("boot.list"));
            Exception creationError = null;
            try
            {
                if (GetComponents<PersistentManager>().Length != 0)
                    throw new InvalidOperationException("Apply Pocket Arcade > Boot > 3. Separate Manager Prefabs first.");
                ValidatePrefab(resourcePrefab); ValidatePrefab(dataPrefab); ValidatePrefab(soundPrefab);
                ValidatePrefab(sceneFlowPrefab); ValidatePrefab(uiPrefab);
                resources = Create(resourcePrefab); resources.Initialize();
            }
            catch (Exception error) { creationError = error; }
            if (creationError != null) { Fail(creationError); yield break; }
            var request = Resources.LoadAsync<TextAsset>(BootResourceManifest.ManifestPath);
            yield return request;
            List<BootResourceSlot> slots = null;
            Exception failure = null;
            try
            {
                if (catalog == null) throw new InvalidOperationException("GameBootstrap needs a GameCatalog.");
                var manifest = request.asset as TextAsset;
                if (manifest == null) throw new InvalidOperationException("Missing Resources/Boot/BootResources.json");
                slots = BootResourceManifest.Parse(manifest.text, catalog);
                bool hasStrings = false;
                foreach (var slot in slots)
                    if (slot.path == StringTable.ResourcePath)
                    { slot.direct = true; slot.required = true; slot.fallback = stringsRequest.asset; hasStrings = true; }
                if (!hasStrings) throw new InvalidOperationException("Register Data/StringTable in BootResources.json.");
                foreach (var character in catalog.Characters)
                    if (!L10n.Table.Contains("character." + character.Id + ".name")) throw new InvalidOperationException("Missing character name: " + character.Id);
                foreach (var game in catalog.MiniGames)
                    if (!L10n.Table.Contains("game." + game.Id + ".name")) throw new InvalidOperationException("Missing game name: " + game.Id);
            }
            catch (Exception error) { failure = error; }
            if (failure != null) { Fail(failure); yield break; }
            yield return resources.Preload(slots, (value, path) => Report(.05f + value * .85f, path.StartsWith("UI/") ? L10n.Text("boot.ui") : path.StartsWith("Audio/") ? L10n.Text("boot.audio") : L10n.Text("boot.assets")));
            if (!resources.IsPreloaded) { Fail(resources.PreloadError ?? new InvalidOperationException("Resource preload interrupted.")); yield break; }
            try
            {
                Report(.95f, L10n.Text("boot.services"));
                data = Create(dataPrefab); data.Initialize(catalog);
                sound = Create(soundPrefab); sound.Initialize(data); sound.UseBootResources(resources);
                scenes = Create(sceneFlowPrefab); scenes.Initialize(data.Catalog);
                ui = Create(uiPrefab); ui.Initialize();
                if (resources.MissingOptional.Count > 0) Debug.Log("Optional boot resources not supplied (fallback/disabled): " + string.Join(", ", resources.MissingOptional), this);
                State = BootState.Ready; Report(1, L10n.Text("boot.ready"));
                if (!scenes.OpenHub(out string error)) throw new InvalidOperationException(error);
            }
            catch (Exception error) { failure = error; }
            if (failure != null) Fail(failure);
        }
        private void Fail(Exception error)
        {
            ShutdownManagers(); State = BootState.Failed;
            Report(Progress, error.Message);
            Failed?.Invoke(error.Message);
            Debug.LogWarning("Boot failed: " + error.Message, this);
        }
        public static void ValidatePrefab(PersistentManager prefab)
        {
            if (prefab == null) throw new InvalidOperationException("Missing independent manager prefab. Run Boot > 3. Separate Manager Prefabs.");
            prefab.ValidateIndependentRoot();
        }
        private static T Create<T>(T prefab) where T : PersistentManager
        {
            ValidatePrefab(prefab);
            var instance = Instantiate(prefab);
            instance.name = typeof(T).Name;
            return instance;
        }
        private static void Remove(PersistentManager manager, Action shutdown)
        {
            if (manager == null) return;
            shutdown();
            manager.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(manager.gameObject); else DestroyImmediate(manager.gameObject);
        }
        private void ShutdownManagers()
        {
            var oldUI = ui != null ? ui : UIManager.Instance;
            var oldSound = sound != null ? sound : SoundManager.Instance;
            var oldScenes = scenes != null ? scenes : SceneFlowManager.Instance;
            var oldData = data != null ? data : DataManager.Instance;
            var oldResources = resources != null ? resources : ResourceManager.Instance;
            if (oldUI != null) Remove(oldUI, oldUI.Shutdown);
            if (oldSound != null) Remove(oldSound, oldSound.Shutdown);
            if (oldScenes != null) Remove(oldScenes, oldScenes.Shutdown);
            if (oldData != null) Remove(oldData, oldData.Shutdown);
            if (oldResources != null) Remove(oldResources, oldResources.Shutdown);
            ui = null; sound = null; scenes = null; data = null; resources = null;
        }
        private void OnDestroy()
        {
            if (activeBootstrap != this) return;
            // Normal BootScene unload must not destroy the independent DDOL managers.
            if (State != BootState.Ready) { ShutdownManagers(); State = BootState.Idle; }
            activeBootstrap = null;
        }
        public static bool RequireManagers()
        {
            if (State == BootState.Ready && ManagersReady) return true;
            if (SceneManager.GetActiveScene().name != SceneIds.BootScene && Application.CanStreamedLevelBeLoaded(SceneIds.BootScene)) SceneManager.LoadScene(SceneIds.BootScene);
            return false;
        }
    }
}
