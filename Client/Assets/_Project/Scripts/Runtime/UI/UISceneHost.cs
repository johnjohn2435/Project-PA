using UnityEngine;
namespace PocketArcade.UI
{
    [System.Serializable]
    public sealed class MiniGameScreenRoute
    {
        public string gameId;
        public string screenPath;
    }
    public sealed class UISceneHost : MonoBehaviour
    {
        [SerializeField] private string screenPath;
        [SerializeField] private string rootKey;
        [SerializeField] private Transform liveRoot;
        [SerializeField] private Transform inactiveRoot;
        [SerializeField] private MiniGameScreenRoute[] gameScreens = System.Array.Empty<MiniGameScreenRoute>();
        private UIManager manager;
        public string ScreenFor(string gameId)
        {
            if (gameScreens != null)
                foreach (var route in gameScreens)
                    if (route != null && route.gameId == gameId && !string.IsNullOrWhiteSpace(route.screenPath)) return route.screenPath;
            return screenPath;
        }
        private void Start()
        {
            if (!GameBootstrap.RequireManagers()) return;
            manager = UIManager.Instance;
            string selected = ScreenFor(SceneFlowManager.Instance.SelectedGame?.Id);
            UIManager.Observe(manager.Mount(liveRoot, inactiveRoot, selected, selected == screenPath ? rootKey : selected));
        }
        private void OnDestroy() { if (manager != null) manager.Unmount(liveRoot); }
    }
}
