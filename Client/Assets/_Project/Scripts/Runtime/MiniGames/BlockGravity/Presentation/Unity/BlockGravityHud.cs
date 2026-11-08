using System;
using System.Threading.Tasks;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityHud : UIPanel
    {
        public const string ScreenKey = "UI/Screens/BlockGravity";
        public const string WorldPath = "MiniGames/BlockGravity/Playfield";
        public const string PauseKey = "UI/Overlays/BlockGravityPause";
        public const string TutorialKey = "UI/Overlays/BlockGravityTutorial";
        public const string ResultKey = "UI/Overlays/BlockGravityResult";
        [SerializeField] private RectTransform hudRegion;
        [SerializeField] private TMP_Text score, best, gravity, status;
        [SerializeField] private Button exitButton;
        private GameObject world;
        private ResourceManager resources;
        private BlockGravityPresenter presenter;
        private bool pauseRequested;
        private bool resultRequested;
        public BlockGravityPresenter Presenter => presenter;
        public bool AllowsGameplay => IsOpen && IsVisible && GetComponent<CanvasGroup>().interactable;
        protected override void OnOpen(object userData)
        {
            resources = ResourceManager.Instance;
            if (resources == null || !resources.IsPreloaded) throw new InvalidOperationException(L10n.Text("bg.error.boot"));
            world = resources.Spawn(WorldPath,null);
            presenter = world.GetComponent<BlockGravityPresenter>();
            if (presenter == null) throw new InvalidOperationException(L10n.Text("bg.error.references"));
            exitButton.onClick.AddListener(Pause);
            L10n.Changed += Refresh;
            presenter.Initialize(this);
            if(SoundManager.Instance!=null&&SoundManager.Instance.IsInitialized)SoundManager.Instance.PlaySceneMusic(true);
            OfferTutorial();
        }
        protected override void OnShow() { if (presenter != null) presenter.SetVisible(true); }
        protected override void OnHide() { if (presenter != null) presenter.SetVisible(false); }
        protected override void OnClose() => Cleanup();
        protected override bool OnBackRequested() { Pause(); return true; }
        private void OnDestroy() => Cleanup();
        private void Cleanup()
        {
            L10n.Changed -= Refresh;
            if (exitButton != null) exitButton.onClick.RemoveListener(Pause);
            if (presenter != null) presenter.Shutdown(); presenter = null;
            if(world!=null&&SoundManager.Instance!=null&&SoundManager.Instance.IsInitialized)SoundManager.Instance.PlaySceneMusic(false);
            if (world != null && resources != null) resources.Release(world);
            world = null; resources = null; pauseRequested = resultRequested = false;
        }
        private void Refresh() { if (presenter != null) presenter.RefreshHud(); }
        public void Present(long value, long highest, GravityDirection direction, int remaining, string stateKey, int chain = 0, int combo = 0, int difficultyLevel = 1)
        {
            score.text = L10n.Format("bg.score",value); best.text = L10n.Format("bg.best",Math.Max(value,highest));
            gravity.text = L10n.Format("bg.gravity",L10n.Text(DirectionKey(direction)),L10n.Text(DirectionKey(GravityResolver.Next(direction))),remaining);
            status.text = stateKey == "bg.over" ? L10n.Text(stateKey) : combo > 0 ? L10n.Format("bg.combo",combo,combo) : chain > 1 ? L10n.Format("bg.chain",chain) : L10n.Text(stateKey);
            if (stateKey != "bg.over") status.text = L10n.Format("bg.difficulty_status",difficultyLevel,status.text);
            exitButton.interactable = presenter != null && presenter.Session != null;
        }
        private static string DirectionKey(GravityDirection value)
        {
            switch (value) { case GravityDirection.Down:return "bg.down"; case GravityDirection.Left:return "bg.left";
                case GravityDirection.Up:return "bg.up"; default:return "bg.right"; }
        }
        public void ApplyLayout(PlayfieldLayout layout)
        {
            var parent = (RectTransform)hudRegion.parent;
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2((float)layout.Hud.X,(float)layout.Hud.Y),camera,out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2((float)(layout.Hud.X+layout.Hud.Width),(float)(layout.Hud.Y+layout.Hud.Height)),camera,out var max);
            hudRegion.anchorMin = hudRegion.anchorMax = hudRegion.pivot = new Vector2(.5f,.5f);
            hudRegion.sizeDelta = max-min; hudRegion.localPosition = (min+max)*.5f;
        }
        public void OfferTutorial()
        {
            var data=DataManager.Instance;
            if(data!=null&&data.IsInitialized&&!data.HasCompletedTutorial(BlockGravityTutorialPopup.TutorialId))ShowTutorial();
        }
        public void ShowTutorial()
        {if(Owner.Find(TutorialKey)?.IsOpen!=true)UIManager.Observe(Owner.Open(TutorialKey));}
        public void Pause() => UIManager.Observe(OpenPause());
        private async Task OpenPause()
        {
            if(Owner.Find(TutorialKey)?.IsOpen==true)return;
            if (presenter?.Session.State == SessionState.GameOver) { presenter.ShowResult(); return; }
            if (pauseRequested || presenter == null || !IsOpen) return;
            pauseRequested = true; presenter.SetPaused(true);
            try { await Owner.Open(PauseKey,this); }
            catch { PauseClosed(); throw; }
        }
        public void PauseClosed() { pauseRequested = false; if (presenter != null) presenter.SetPaused(false); }
        public void ShowResult(BlockGravityResultController result,long previousBest) => UIManager.Observe(OpenResult(result,previousBest));
        private async Task OpenResult(BlockGravityResultController result,long previousBest)
        {
            if (resultRequested || !IsOpen) return;
            resultRequested = true;
            try
            {
                await Owner.Close(PauseKey);
                if (!IsOpen || presenter == null || presenter.Result != result) { resultRequested = false; return; }
                await Owner.Open(ResultKey,new BlockGravityResultContext(this,result,previousBest));
            }
            catch { resultRequested = false; throw; }
        }
        public void ResultClosed(BlockGravityResultController result)
        { if (presenter != null && presenter.Result == result) resultRequested = false; }
        public void AcceptSavedResult(BlockGravityResultController result) { if (presenter != null) presenter.AcceptSavedResult(result); }
        public void RestartCompletedResult(BlockGravityResultController result) { if (presenter != null) presenter.RestartCompletedResult(result); }
        public bool ReturnToHub(BlockGravityResultController result)
        {
            if (presenter == null || presenter.Result != result || !result.IsSaved) return false;
            return SceneFlowManager.Instance != null && SceneFlowManager.Instance.OpenHub(out _);
        }
        public bool LeaveRun()
        {
            return SceneFlowManager.Instance != null && SceneFlowManager.Instance.OpenHub(out _);
        }
    }
}
