using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.EchoRoom.Unity
{
    public sealed class EchoMenu:UIPopup,ILocalizedDynamicText
    {
        [SerializeField] private Button resume,retry,home;
        [SerializeField] private TMP_Text notice;
        private EchoHud hud;
        public System.Collections.Generic.IEnumerable<TMP_Text> DynamicTextLabels=>new[]{notice};
        protected override void OnOpen(object data){hud=data as EchoHud;notice.text="";resume.onClick.AddListener(Resume);retry.onClick.AddListener(Retry);home.onClick.AddListener(Home);}
        private void Resume()=>UIManager.Observe(Owner.Close(Key));
        private void Retry(){if(hud!=null){hud.Reset();hud.Play();}Resume();}
        private void Home(){if(hud==null||!hud.Home())notice.text=L10n.Text("echo.error.home");}
        protected override void OnClose(){resume.onClick.RemoveListener(Resume);retry.onClick.RemoveListener(Retry);home.onClick.RemoveListener(Home);if(hud!=null)hud.MenuClosed();hud=null;}
    }
}
