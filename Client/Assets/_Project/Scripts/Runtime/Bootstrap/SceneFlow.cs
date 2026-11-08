using System.Collections;
using PocketArcade.Core;
using PocketArcade.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketArcade
{
    public sealed class SceneFlow
    {
        private readonly MonoBehaviour runner;
        private readonly GameCatalog catalog;
        public bool IsLoading { get; private set; }
        public MiniGameDefinition SelectedGame { get; private set; }

        public SceneFlow(MonoBehaviour runner, GameCatalog catalog)
        {
            this.runner = runner;
            this.catalog = catalog;
        }

        public bool OpenHub(out string error) => Load(SceneIds.SelectScene, out error);

        public bool OpenGame(string id, out string error)
        {
            var game = catalog.MiniGame(id);
            if (game == null || !game.IsAvailable)
            {
                error = L10n.Text("game.coming");
                return false;
            }
            if (!Load(game.SceneName, out error)) return false;
            SelectedGame = game;
            return true;
        }

        private bool Load(string scene, out string error)
        {
            error = null;
            if (IsLoading) { error = L10n.Text("scene.wait"); return false; }
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                error = L10n.Text("scene.missing");
                return false;
            }
            IsLoading = true;
            runner.StartCoroutine(LoadRoutine(scene));
            return true;
        }

        private IEnumerator LoadRoutine(string scene)
        {
            try
            {
                var operation = SceneManager.LoadSceneAsync(scene);
                yield return operation;
            }
            finally { IsLoading = false; }
        }
    }
}
