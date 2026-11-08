using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    [RequireComponent(typeof(Toggle))]
    public sealed class ToggleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        private Toggle control;
        public event Action<bool> Changed;
        private Toggle Control => control != null ? control : control = GetComponent<Toggle>();
        private bool wired;
        private void Wire() { if (wired) return; Control.onValueChanged.AddListener(Notify); wired = true; }
        private void OnEnable() => Wire();
        private void OnDisable() { Control.onValueChanged.RemoveListener(Notify); wired = false; }
        private void Notify(bool value) => Changed?.Invoke(value);
        public void Present(string title, bool value)
        {
            Wire();
            if (label != null) label.text = title;
            Control.SetIsOnWithoutNotify(value);
        }
        public void ResetForReuse() { Changed = null; Control.SetIsOnWithoutNotify(false); }
    }
}
