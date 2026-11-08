using System.Threading.Tasks;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityPausePopup : UIPopup
    {
        [SerializeField] private Button resume, exit, dismiss, help;
        [SerializeField] private TMP_Text notice;
        private BlockGravityHud hud;
        protected override void OnOpen(object data)
        {
            hud = data as BlockGravityHud; notice.text = "";
            resume.onClick.AddListener(Resume); dismiss.onClick.AddListener(Resume);
            exit.onClick.AddListener(Exit);help.onClick.AddListener(Help);
        }
        private void Help(){if(hud!=null)hud.ShowTutorial();}
        private void Resume() => UIManager.Observe(Owner.Close(Key));
        private void Exit() { if (hud == null || !hud.LeaveRun()) notice.text = L10n.Text("bg.error.scene"); }
        protected override void OnClose()
        {
            resume.onClick.RemoveListener(Resume); dismiss.onClick.RemoveListener(Resume);
            exit.onClick.RemoveListener(Exit);help.onClick.RemoveListener(Help);
            if (hud != null) hud.PauseClosed(); hud = null;
        }
    }
}
