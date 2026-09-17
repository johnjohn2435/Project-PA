using UnityEngine;

namespace PocketArcade.UI
{
    public static class SafeAreaLayout
    {
        // Screen.safeArea와 전체 화면 Canvas의 좌표를 정규화한다.
        public static bool TryGetAnchors(Rect safeArea, Vector2 screenSize,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
            if (!IsFinite(screenSize.x) || !IsFinite(screenSize.y) ||
                screenSize.x <= 0f || screenSize.y <= 0f ||
                !IsFinite(safeArea.x) || !IsFinite(safeArea.y) ||
                !IsFinite(safeArea.width) || !IsFinite(safeArea.height) ||
                safeArea.width <= 0f || safeArea.height <= 0f)
                return false;

            float left = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            float bottom = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            float right = Mathf.Clamp(safeArea.xMax, 0f, screenSize.x);
            float top = Mathf.Clamp(safeArea.yMax, 0f, screenSize.y);
            if (right <= left || top <= bottom)
                return false;

            anchorMin = new Vector2(left / screenSize.x, bottom / screenSize.y);
            anchorMax = new Vector2(right / screenSize.x, top / screenSize.y);
            return true;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
