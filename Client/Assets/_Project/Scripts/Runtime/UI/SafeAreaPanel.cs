using UnityEngine;

namespace PocketArcade.UI
{
    // 전체 화면 Screen Space Overlay Canvas의 직계 자식에 부착한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect previousSafeArea;
        private Vector2 previousScreenSize;
        private bool hasApplied;

        private void Awake() => panel = GetComponent<RectTransform>();

        private void OnEnable()
        {
            hasApplied = false;
            ApplyIfChanged();
        }

        private void Update() => ApplyIfChanged();

        private void ApplyIfChanged()
        {
            if (panel == null)
                panel = GetComponent<RectTransform>();

            var screenSize = new Vector2(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (hasApplied && safeArea == previousSafeArea && screenSize == previousScreenSize)
                return;

            if (!SafeAreaLayout.TryGetAnchors(safeArea, screenSize, out var min, out var max))
                return;

            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            previousSafeArea = safeArea;
            previousScreenSize = screenSize;
            hasApplied = true;
        }
    }
}
