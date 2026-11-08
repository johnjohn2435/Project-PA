using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace PocketArcade.UI
{
    public sealed class BootStatusView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text percentage;
        [SerializeField] private Button retryButton;
        public bool HasProgressBar => progressBar != null;
        private void OnEnable()
        {
            GameBootstrap.ProgressChanged += Show; L10n.Changed += Refresh;
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            Show(GameBootstrap.Progress, GameBootstrap.Status);
        }
        private void OnDisable()
        {
            GameBootstrap.ProgressChanged -= Show; L10n.Changed -= Refresh;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        }
        private void Refresh() => Show(GameBootstrap.Progress, GameBootstrap.Status);
        private void Show(float progress, string message) => Present(progress, message, GameBootstrap.State);
        public void Present(float progress, string message, BootState state)
        {
            progress = float.IsNaN(progress) ? 0 : Mathf.Clamp01(progress);
            bool failed = state == BootState.Failed;
            if (progressBar != null) progressBar.SetValueWithoutNotify(progress);
            if (percentage != null) percentage.text = L10n.Format("boot.percent", Mathf.RoundToInt(progress * 100));
            if (retryButton != null) retryButton.gameObject.SetActive(failed);
            if (label == null || (!L10n.IsReady && !failed)) return;
            label.raycastTarget = failed && retryButton == null;
            label.text = failed ? L10n.Text("boot.failed") :
                progressBar != null ? message : L10n.Format("boot.detail", Mathf.RoundToInt(progress * 100), message);
        }
        private void Retry() => GameBootstrap.Retry();
        public void OnPointerClick(PointerEventData eventData)
        { if (GameBootstrap.State == BootState.Failed) Retry(); }
        private void Update()
        { if (GameBootstrap.State == BootState.Failed && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) Retry(); }
    }
}
