using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PocketArcade.UI
{
    public sealed class HubJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform knob;
        private int? pointer;
        private Vector2 drag;
        public Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                Vector2 value = drag;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) value.x--;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) value.x++;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) value.y--;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) value.y++;
                }
                return Vector2.ClampMagnitude(value, 1);
            }
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer.HasValue) return;
            pointer = e.pointerId;
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var point))
            {
                float radius = Mathf.Min(rect.rect.width, rect.rect.height) * .31f;
                drag = Vector2.ClampMagnitude((point - rect.rect.center) / Mathf.Max(1, radius), 1);
                knob.anchoredPosition = drag * radius;
            }
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) Cancel(); }
        public void Cancel() { pointer = null; drag = Vector2.zero; if (knob != null) knob.anchoredPosition = Vector2.zero; }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
