using System;
using UnityEngine;
namespace PocketArcade.UI
{
    public enum Placement { Frame, Independent }
    public enum OrderPolicy { Managed, Fixed }
    [DisallowMultipleComponent]
    public abstract class UIBase : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Placement placement;
        [SerializeField] private OrderPolicy orderPolicy;
        [SerializeField] private bool closeOnBack = true;
        [SerializeField] private bool blocksInput = true;
        [SerializeField] private bool acceptsRaycasts = true;
        private int preservedFixedOrder;
        public string Key => key;
        public Placement Placement => placement;
        public OrderPolicy OrderPolicy => orderPolicy;
        public bool CanCloseOnBack => closeOnBack;
        public bool BlocksInput => blocksInput;
        public bool IsOpen { get; private set; }
        public bool IsVisible { get; private set; }
        public Canvas Canvas => GetComponent<Canvas>();
        protected UIManager Owner { get; private set; }
        protected virtual void OnOpen(object userData) { }
        protected virtual void OnHide() { }
        protected virtual void OnShow() { }
        protected virtual void OnClose() { }
        // true consumes Back without closing; false allows manager's default action.
        protected virtual bool OnBackRequested() => false;
        public void ValidateDefinition()
        {
            if (string.IsNullOrWhiteSpace(key) || GetComponents<UIBase>().Length != 1 ||
                Canvas == null || GetComponent<CanvasGroup>() == null || !(transform is RectTransform) ||
                !((placement == Placement.Frame && orderPolicy == OrderPolicy.Managed) ||
                  (placement == Placement.Independent && orderPolicy == OrderPolicy.Fixed)) ||
                (this is UIPanel && placement != Placement.Frame))
                throw new InvalidOperationException(L10n.Format("ui.error.definition_key", key));
        }
        internal void Bind(UIManager owner, int fixedOrder) { preservedFixedOrder = fixedOrder; Owner = owner; Canvas.overrideSorting = true; IsOpen = IsVisible = false; gameObject.SetActive(false); }
        internal void OpenInternal(object data)
        {
            if (IsOpen) throw new InvalidOperationException(L10n.Format("ui.error.open", key));
            IsOpen = true;
            try { OnOpen(data); ShowInternal(); }
            catch { CloseInternal(); throw; }
        }
        internal void HideInternal()
        {
            if (!IsVisible) return;
            IsVisible = false;
            try { OnHide(); } finally { gameObject.SetActive(false); }
        }
        internal void ShowInternal()
        {
            if (!IsOpen || IsVisible) return;
            IsVisible = true; gameObject.SetActive(true);
            if (orderPolicy == OrderPolicy.Fixed) OrderInternal(preservedFixedOrder);
            OnShow();
        }
        internal void CloseInternal()
        {
            if (!IsOpen) return;
            try { HideInternal(); }
            finally { IsOpen = false; try { OnClose(); } finally { gameObject.SetActive(false); } }
        }
        internal bool BackInternal() => OnBackRequested();
        internal void InputInternal(bool enabled)
        { var group = GetComponent<CanvasGroup>(); group.interactable = enabled; group.blocksRaycasts = enabled && acceptsRaycasts; }
        internal void OrderInternal(int value) { Canvas.overrideSorting = true; Canvas.sortingOrder = value; }
    }
}
