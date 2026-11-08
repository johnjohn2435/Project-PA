using System;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    public sealed class PopupView : UIPopup
    {
        [SerializeField] private Button close;
        public event Action CloseRequested;
        private void OnEnable() { if(close!=null)close.onClick.AddListener(RequestClose); }
        private void OnDisable() { if(close!=null)close.onClick.RemoveListener(RequestClose); }
        private void RequestClose() { CloseRequested?.Invoke(); if(Owner!=null)UIManager.Observe(Owner.Close(Key)); }
        public void ResetForReuse()=>CloseRequested=null;
        protected override void OnClose()=>ResetForReuse();
    }
}
