using System;
using PocketArcade.Meta;
using UnityEngine;

namespace PocketArcade
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowManager : PersistentManager
    {
        public static SceneFlowManager Instance { get; private set; }
        public bool IsInitialized => flow != null;
        public bool IsLoading => flow != null && flow.IsLoading;
        public MiniGameDefinition SelectedGame => flow?.SelectedGame;
        private SceneFlow flow;
        private GameCatalog catalog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Initialize(GameCatalog gameCatalog)
        {
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("SceneFlowManager is already initialized by another object.");
            if (gameCatalog == null) throw new ArgumentNullException(nameof(gameCatalog));
            if (IsInitialized)
            {
                if (catalog != gameCatalog) throw new InvalidOperationException("Cannot replace an active scene catalog.");
                Persist();
            Instance = this;
                return;
            }
            gameCatalog.Validate();
            catalog = gameCatalog;
            flow = new SceneFlow(this, gameCatalog);
            Persist();
            Instance = this;
        }

        public bool OpenHub(out string error)
        {
            if (!IsInitialized) { error = "SceneFlowManager has not been initialized."; return false; }
            return flow.OpenHub(out error);
        }

        public bool OpenGame(string id, out string error)
        {
            if (!IsInitialized) { error = "SceneFlowManager has not been initialized."; return false; }
            return flow.OpenGame(id, out error);
        }

        public void Shutdown()
        {
            StopAllCoroutines();
            flow = null;
            catalog = null;
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
