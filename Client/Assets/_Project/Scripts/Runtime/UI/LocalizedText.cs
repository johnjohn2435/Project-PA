using TMPro;
using UnityEngine;
namespace PocketArcade.UI
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        public string Key => key;
        public void SetKey(string value) { key = value; Refresh(); }
        private void OnEnable() { L10n.Changed += Refresh; Refresh(); }
        private void OnDisable() => L10n.Changed -= Refresh;
        private void Refresh()
        { if (L10n.IsReady && !string.IsNullOrEmpty(key)) GetComponent<TMP_Text>().text = L10n.Text(key); }
    }
}
