using System.Collections.Generic;
using System.Threading.Tasks;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UdadaDelivery.Unity
{
    public sealed class DeliveryDialog : UIPopup, ILocalizedDynamicText
    {
        [SerializeField] private TMP_Text title, summary, notice, primaryLabel;
        [SerializeField] private Button primary, home;
        private DeliveryDialogContext context;
        private bool busy;
        public IEnumerable<TMP_Text> DynamicTextLabels => new[] { title, summary, notice, primaryLabel };
        protected override void OnOpen(object userData)
        {
            context = userData as DeliveryDialogContext;
            primary.onClick.AddListener(Primary); home.onClick.AddListener(Home); L10n.Changed += Refresh;
            busy = false; Refresh();
        }
        protected override void OnClose()
        {
            primary.onClick.RemoveListener(Primary); home.onClick.RemoveListener(Home); L10n.Changed -= Refresh;
            context?.Hud.DialogClosed(); context = null;
        }
        protected override bool OnBackRequested() => context == null || context.Finished;
        private void Refresh()
        {
            if (context == null) return;
            title.text = L10n.Text(context.Finished ? "ud.result" : "bg.pause");
            var session = context.Hud.Session;
            summary.text = context.Finished ? L10n.Format("ud.summary", session.Score, session.Deliveries, session.PerfectDeliveries, session.Coins) : L10n.Text("ud.instructions");
            primaryLabel.text = L10n.Text(!context.Finished ? "bg.resume" : context.Hud.IsSaved ? "bg.restart" : "ud.retry_save");
            notice.text = context.Finished ? L10n.Text(context.Hud.IsSaved ? "ud.saved" : "ud.save_failed") : "";
        }
        private void Primary() => UIManager.Observe(HandlePrimary());
        private async Task HandlePrimary()
        {
            if (busy || context == null) return; busy = true;
            try
            {
                var hud = context.Hud;
                if (context.Finished)
                {
                    if (!hud.IsSaved) { hud.TrySave(); Refresh(); return; }
                    await Owner.Close(Key); hud.ResetRun(); hud.StartRun();
                }
                else await Owner.Close(Key);
            }
            finally { busy = false; }
        }
        private void Home()
        {
            if (context == null || busy) return;
            if (!context.Hud.ReturnHome()) { Refresh(); notice.text = L10n.Text("ud.home_failed"); }
        }
    }
}
