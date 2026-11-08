using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class PieceDragController : MonoBehaviour
    {
        [SerializeField] private BlockGravityBoardView view;
        private BlockGravityPresenter presenter;
        private readonly List<RaycastResult> raycasts = new List<RaycastResult>();
        private EventSystem events;
        private PointerEventData pointer;
        private PieceShape draggedShape;
        private BoardState dragBoard;
        private int slot = -1, touchId;
        private bool touch;
        public bool IsDragging => slot >= 0;
        public void Initialize(BlockGravityPresenter owner) { presenter = owner; Cancel(); }
        private bool OverUI(Vector2 point)
        {
            if (EventSystem.current == null) return false;
            if (events != EventSystem.current || pointer == null) { events = EventSystem.current; pointer = new PointerEventData(events); }
            raycasts.Clear();
            pointer.Reset(); pointer.position = point; events.RaycastAll(pointer, raycasts);
            foreach (var hit in raycasts) if (hit.gameObject.GetComponent<Graphic>() != null) return true;
            return false;
        }
        public bool Begin(Vector2 point, bool isTouch)
        {
            if (IsDragging || presenter == null || !presenter.CanInteract || OverUI(point)) return false;
            int hit = view.SlotAt(point);
            if (hit < 0 || presenter.Session.Tray[hit] == null) return false;
            slot = hit; touch = isTouch;
            draggedShape = presenter.Session.Tray[hit].Shape; dragBoard = presenter.Session.Board;
            view.ShowTray(presenter.Session.Tray, slot); Move(point); return true;
        }
        public void Move(Vector2 point)
        {
            if (!IsDragging) return;
            if (!presenter.CanInteract) { Cancel(); return; }
            var shape = draggedShape;
            Cell origin = view.DragOrigin(shape, point, touch);
            bool valid = !OverUI(point) && PlacementValidator.CanPlace(dragBoard, shape, origin);
            view.ShowDrag(shape, point, touch, origin, valid);
        }
        public bool End(Vector2 point)
        {
            if (!IsDragging) return false;
            int selected = slot;
            Cell origin = view.DragOrigin(draggedShape, point, touch);
            bool allowed = presenter.CanInteract && !OverUI(point);
            Cancel();
            return allowed && presenter.TryPlace(selected,origin);
        }
        public void Cancel()
        {
            slot = -1; draggedShape = null; dragBoard = null;
            if (view != null) { view.HideDrag(); if (presenter?.Session != null && view.Layout != null) view.ShowTray(presenter.Session.Tray); }
        }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
        private void Update()
        {
            if (presenter == null || !presenter.CanInteract) { if (IsDragging) Cancel(); return; }
            var screen = Touchscreen.current;
            if (IsDragging && touch)
            {
                if (screen == null) { Cancel(); return; }
                foreach (var finger in screen.touches)
                {
                    if (finger.touchId.ReadValue() != touchId) continue;
                    var phase = finger.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Canceled) Cancel();
                    else if (finger.press.wasReleasedThisFrame) End(finger.position.ReadValue());
                    else if (finger.press.isPressed) Move(finger.position.ReadValue());
                    else Cancel();
                    return;
                }
                Cancel(); return;
            }
            bool hasTouch = false;
            if (screen != null) foreach (var finger in screen.touches)
            {
                hasTouch |= finger.press.isPressed || finger.press.wasReleasedThisFrame;
            }
            if (!IsDragging && screen != null && screen.primaryTouch.press.wasPressedThisFrame && Begin(screen.primaryTouch.position.ReadValue(), true))
            { touchId = screen.primaryTouch.touchId.ReadValue(); return; }
            if (hasTouch && !IsDragging) return;
            var mouse = Mouse.current;
            if (mouse == null) { if (IsDragging) Cancel(); return; }
            if (IsDragging)
            { if (mouse.leftButton.wasReleasedThisFrame) End(mouse.position.ReadValue()); else if (mouse.leftButton.isPressed) Move(mouse.position.ReadValue()); else Cancel(); }
            else if (mouse.leftButton.wasPressedThisFrame) Begin(mouse.position.ReadValue(),false);
        }
    }
}
