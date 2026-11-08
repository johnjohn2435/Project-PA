using UnityEngine;

namespace PocketArcade.UI
{
    public enum UIPage { Home, Characters, Games, Settings, GameEntry }

    // 씬/프리팹 내부 참조는 View가 소유하고 매니저에는 활성 View만 등록한다.
    public abstract class UISceneView : MonoBehaviour
    {
        private bool ready;
        public abstract UIPage DefaultPanel { get; }
        public abstract bool Supports(UIPage panel);
        public abstract void DisplayPanel(UIPage panel);
        public abstract void DisplayNotice(string message);

        protected void RegisterUI()
        {
            UIManager.Instance.Register(this);
            ready = true;
        }

        protected virtual void OnEnable()
        {
            if (ready && UIManager.Instance != null && UIManager.Instance.IsInitialized)
                UIManager.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (UIManager.Instance != null) UIManager.Instance.Unregister(this);
        }
    }
}
