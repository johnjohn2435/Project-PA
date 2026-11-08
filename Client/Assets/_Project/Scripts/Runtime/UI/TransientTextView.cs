using TMPro;
using UnityEngine;
namespace PocketArcade.UI
{
    public sealed class TransientTextView : UIPopup
    {
        [SerializeField] private TMP_Text label;
        private TransientData request;
        private float until;
        private bool closing;
        public void Present(string text)=>label.text=text;
        public void ResetForReuse()=>label.text="";
        protected override void OnOpen(object data)
        {
            request=data as TransientData ?? throw new System.InvalidOperationException(L10n.Text("ui.error.data"));
            closing=false; Present(request.Text); until=Time.unscaledTime+request.Seconds;
        }
        protected override void OnClose()
        {var old=request;request=null;ResetForReuse();old?.Completion.TrySetResult(true);}
        private void Update()
        {
            if(!IsVisible || request==null || closing)return;
            bool expired=request.Seconds>0 ? Time.unscaledTime>=until : request.Target==null || !request.Target.gameObject.activeInHierarchy;
            if(expired){closing=true;UIManager.Observe(Owner.Close(Key));return;}
            if(request.Target==null)return;
            var rect=(RectTransform)transform; var parent=rect.parent as RectTransform;
            var canvas=GetComponentInParent<Canvas>();
            var camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var screen=RectTransformUtility.WorldToScreenPoint(camera,request.Target.TransformPoint(request.Target.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,camera,out var local);
            var half=rect.rect.size*.5f;
            rect.localPosition=new Vector3(Mathf.Clamp(local.x,parent.rect.xMin+half.x,parent.rect.xMax-half.x),
                Mathf.Clamp(local.y+half.y+30,parent.rect.yMin+half.y,parent.rect.yMax-half.y),0);
        }
    }
}
