using System;
using System.Threading.Tasks;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityResultContext
    {
        public BlockGravityHud Hud { get; }
        public BlockGravityResultController Controller { get; }
        public long PreviousBest { get; }
        public BlockGravityResultContext(BlockGravityHud hud, BlockGravityResultController controller, long previousBest)
        { Hud = hud; Controller = controller; PreviousBest = previousBest; }
    }

    public sealed class BlockGravityResultPopup : UIPopup
    {
        [SerializeField] private TMP_Text finalScore, best, coins, saveStatus;
        [SerializeField] private Button retrySave, restart, home;
        private BlockGravityResultContext context;
        private bool navigating, navigationFailed;
        public bool IsSaved => context != null && context.Controller.IsSaved;
        protected override void OnOpen(object data)
        {
            context = data as BlockGravityResultContext ?? throw new ArgumentException(nameof(data));
            navigating = navigationFailed = false;
            retrySave.onClick.AddListener(RetrySave); restart.onClick.AddListener(Restart); home.onClick.AddListener(Home);
            L10n.Changed += Refresh;
            RetrySave();
        }
        protected override bool OnBackRequested() => true;
        protected override void OnClose()
        {
            L10n.Changed -= Refresh;
            retrySave.onClick.RemoveListener(RetrySave); restart.onClick.RemoveListener(Restart); home.onClick.RemoveListener(Home);
            if (context?.Hud != null) context.Hud.ResultClosed(context.Controller);
            context = null;
        }
        public void RetrySave()
        {
            if (context == null || navigating) return;
            if (context.Controller.TrySave()) context.Hud?.AcceptSavedResult(context.Controller);
            Refresh();
        }
        private void Refresh()
        {
            if (context == null) return;
            var controller = context.Controller; var result = controller.Result; var receipt = controller.Receipt;
            finalScore.text = L10n.Format("bg.result.score", result.Score);
            best.text = L10n.Format("bg.best", receipt?.highScore ?? Math.Max(context.PreviousBest,result.Score));
            coins.text = L10n.Format(receipt == null ? "bg.result.coins_pending" : "bg.result.coins",receipt?.creditedCoins ?? result.EarnedCoins);
            saveStatus.text = L10n.Text(navigationFailed ? "bg.error.scene" : controller.IsSaved ? "bg.result.saved" : "bg.result.save_failed");
            retrySave.interactable = !navigating && !controller.IsSaved && !controller.IsSaving;
            restart.interactable = home.interactable = !navigating && controller.IsSaved;
            var label = retrySave.GetComponentInChildren<LocalizedText>(true);
            if (label != null) label.SetKey(controller.IsSaved ? "bg.result.saved" : "bg.result.retry");
        }
        private void Restart() => UIManager.Observe(RestartAsync());
        private async Task RestartAsync()
        {
            if (context == null || !context.Controller.IsSaved || navigating) return;
            var target = context; navigating = true; Refresh();
            try
            {
                await Owner.Close(Key);
                if (target.Hud != null) target.Hud.RestartCompletedResult(target.Controller);
            }
            finally { navigating = false; Refresh(); }
        }
        private void Home()
        {
            if (context == null || !context.Controller.IsSaved || navigating) return;
            navigating = true; Refresh();
            if (context.Hud == null || !context.Hud.ReturnToHub(context.Controller))
            { navigating = false; navigationFailed = true; Refresh(); }
        }
    }
}
