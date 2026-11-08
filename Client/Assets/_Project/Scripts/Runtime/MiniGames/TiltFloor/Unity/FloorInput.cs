using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace PocketArcade.TiltFloor.Unity
{
    public sealed class FloorInput:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler
    {
        [SerializeField] private RectTransform knob;
        private int? pointer;
        private Vector2 origin,drag;
        public Vector2 Move
        {
            get
            {
                var k=Keyboard.current;Vector2 keys=Vector2.zero;
                if(k!=null){if(k.aKey.isPressed||k.leftArrowKey.isPressed)keys.x--;if(k.dKey.isPressed||k.rightArrowKey.isPressed)keys.x++;if(k.wKey.isPressed||k.upArrowKey.isPressed)keys.y++;if(k.sKey.isPressed||k.downArrowKey.isPressed)keys.y--;}
                return Vector2.ClampMagnitude(keys+drag,1);
            }
        }
        public void OnPointerDown(PointerEventData e){if(pointer.HasValue)return;pointer=e.pointerId;origin=e.position;drag=Vector2.zero;}
        public void OnDrag(PointerEventData e){if(pointer==e.pointerId)drag=Vector2.ClampMagnitude((e.position-origin)/Mathf.Max(60,Screen.width*.14f),1);}
        public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)Cancel();}
        public void Cancel(){pointer=null;drag=Vector2.zero;if(knob!=null)knob.anchoredPosition=Vector2.zero;}
        private void Update(){if(knob!=null)knob.anchoredPosition=Move*80;}
        private void OnDisable()=>Cancel();
        private void OnApplicationFocus(bool value){if(!value)Cancel();}
    }
}
