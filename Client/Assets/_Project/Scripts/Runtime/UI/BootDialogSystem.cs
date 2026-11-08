using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    // Boot-only prefab. It never registers with UIManager or depends on its lifetime.
    public sealed class BootDialogSystem : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private TMP_Text title, body;
        [SerializeField] private Button confirm, cancel;
        private Action retry;
        private void Awake() { confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Hide); }
        private void OnEnable()=>GameBootstrap.Failed+=Failed;
        private void OnDisable()=>GameBootstrap.Failed-=Failed;
        private void Failed(string diagnostic)=>Show(L10n.Text("app.title"),L10n.Text("boot.failed"),()=>GameBootstrap.Retry());
        public void Show(string heading,string message,Action action)
        {title.text=heading;body.text=message;retry=action;content.SetActive(true);}
        private void Confirm(){var action=retry;Hide();action?.Invoke();}
        public void Hide(){retry=null;content.SetActive(false);}
        private void OnDestroy(){confirm.onClick.RemoveListener(Confirm);cancel.onClick.RemoveListener(Hide);}
    }
}
