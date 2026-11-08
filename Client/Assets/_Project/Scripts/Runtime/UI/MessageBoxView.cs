using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    public enum MessageBoxResult { Confirmed, Cancelled, SceneClosed }
    public sealed class MessageBoxView : UIPopup
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button confirm;
        [SerializeField] private Button cancel;
        public event Action<MessageBoxResult> Answered;
        private bool wired;
        private MessageData request;
        protected override void OnOpen(object userData)
        {
            request = userData as MessageData ?? throw new InvalidOperationException(L10n.Text("ui.error.data"));
            Present(request.Title, request.Body);
        }
        private void OnEnable() { if(request != null)Wire(); }
        protected override void OnClose()
        {
            var finished=request; request=null; ResetForReuse();
            finished?.Completion.TrySetResult(finished.Answer);
        }
        private void Answer(MessageBoxResult value)
        {
            Answered?.Invoke(value);
            if(request == null)return;
            request.Answer=value; UIManager.Observe(Owner.Close(Key));
        }
        private void Wire() { if (wired) return; confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Cancel); wired = true; }
        private void OnDisable() { if (!wired) return; confirm.onClick.RemoveListener(Confirm); cancel.onClick.RemoveListener(Cancel); wired = false; }
        private void Confirm() => Answer(MessageBoxResult.Confirmed);
        private void Cancel() => Answer(MessageBoxResult.Cancelled);
        public void Present(string heading, string message) { Wire(); title.text = heading; body.text = message; }
        public void ResetForReuse() { Answered = null; title.text = body.text = ""; }
    }
}
