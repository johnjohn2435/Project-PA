using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace PocketArcade.UdadaDelivery.Unity
{
    public sealed class DeliveryBrakeInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private int? pointer;
        public bool IsBraking => pointer.HasValue || (Keyboard.current?.spaceKey.isPressed ?? false);
        public void OnPointerDown(PointerEventData data) { if (!pointer.HasValue) pointer = data.pointerId; }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) pointer = null; }
        public void Cancel() => pointer = null;
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
    }
}
