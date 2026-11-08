using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PocketArcade.Core.Save;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.UdadaDelivery.Unity
{
    public sealed class DeliveryHud : UIPanel, ILocalizedDynamicText
    {
        public const string ScreenKey = "UI/Screens/UdadaDelivery", DialogKey = "UI/Overlays/UdadaDeliveryDialog";
        public const string WorldPath = "MiniGames/UdadaDelivery/Playfield";
        [SerializeField] private DeliveryTuning tuning;
        [SerializeField] private TMP_Text score, best, timer, hearts, status, startLabel;
        [SerializeField] private Slider progress;
        [SerializeField] private Button start, pause;
        [SerializeField] private DeliveryBrakeInput brake;
        private GameObject world;
        private DeliveryWorldView view;
        private ResourceManager resources;
        private bool suspended, dialogPending, applicationHidden, focused = true;
        private double feedbackUntil;
        private GameResultRequest result;
        private GameResultReceipt receipt;
        public DeliverySession Session { get; private set; }
        public bool IsSaved => receipt != null;
        public IEnumerable<TMP_Text> DynamicTextLabels => new[] { score, best, timer, hearts, status, startLabel };
        protected override void OnOpen(object userData)
        {
            resources = ResourceManager.Instance;
            if (resources == null || !resources.IsPreloaded || tuning == null) throw new InvalidOperationException(L10n.Text("ud.error.references"));
            world = resources.Spawn(WorldPath, null); view = world.GetComponent<DeliveryWorldView>();
            if (view == null) throw new InvalidOperationException(L10n.Text("ud.error.references"));
            view.AttachCharacter(resources, DataManager.Instance.SelectedCharacterId);
            start.onClick.AddListener(StartRun); pause.onClick.AddListener(Pause);
            L10n.Changed += Refresh;
            ResetRun(); SoundManager.Instance?.PlaySceneMusic(true);
        }
        protected override void OnHide() { brake.Cancel(); if (view != null) view.PlayCamera.enabled = false; }
        protected override void OnShow() { if (view != null) view.PlayCamera.enabled = true; }
        protected override void OnClose() => Cleanup();
        protected override bool OnBackRequested() { Pause(); return true; }
        private void OnDestroy() => Cleanup();
        private void Cleanup()
        {
            L10n.Changed -= Refresh;
            if (start != null) start.onClick.RemoveListener(StartRun);
            if (pause != null) pause.onClick.RemoveListener(Pause);
            if (view != null && resources != null) view.ReleaseCharacter(resources);
            if (world != null && resources != null) resources.Release(world);
            if (world != null) SoundManager.Instance?.PlaySceneMusic(false);
            world = null; view = null; resources = null; Session = null; result = null; receipt = null;
        }
        public void ResetRun()
        {
            Session = new DeliverySession(tuning.CreateRules());
            result = null; receipt = null; feedbackUntil = 0; suspended = false; dialogPending = false; brake.Cancel();
            view.SetCharacterPaused(false); view.Present(Session); Refresh();
        }
        public void StartRun()
        { if (Session?.State == DeliveryState.Ready) { Session.Start(); brake.Cancel(); SoundManager.Instance?.PlayCue(AudioCue.Confirm); Refresh(); } }
        private void Update()
        {
            if (!IsOpen || !IsVisible || Session == null || suspended || applicationHidden || !focused || !GetComponent<CanvasGroup>().interactable) return;
            int resolved = Session.ResolvedCount, deliveries = Session.Deliveries, shots = Session.Shots;
            // Unity limits giant foreground deltas; simulation has its own fixed substeps for collision safety.
            Session.Tick(Math.Min(Time.unscaledDeltaTime, .1f), brake.IsBraking);
            if (Session.Shots != shots) SoundManager.Instance?.PlayCue(AudioCue.Place);
            if (Session.ResolvedCount != resolved)
            {
                feedbackUntil = Session.Elapsed + 2;
                SoundManager.Instance?.PlayCue(Session.Deliveries != deliveries ? AudioCue.Confirm : AudioCue.Cancel);
            }
            view.Present(Session); Refresh();
            if (Session.State == DeliveryState.Finished && !dialogPending)
            {
                result = new GameResultRequest(Guid.NewGuid().ToString("N"), DeliverySession.ScoreId, Session.Score, Session.Coins);
                TrySave(); SoundManager.Instance?.PlayCue(AudioCue.GameOver); OpenDialog(true);
            }
        }
        private void OnApplicationPause(bool value) { applicationHidden = value; if (value) Pause(); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Pause(); }
        public void Pause() { if (IsOpen && Session != null) OpenDialog(Session.State == DeliveryState.Finished); }
        private void OpenDialog(bool finished)
        {
            if (dialogPending || Owner == null) return;
            dialogPending = true; suspended = true; view.SetCharacterPaused(true); brake.Cancel();
            UIManager.Observe(OpenDialogAsync(finished));
        }
        private async Task OpenDialogAsync(bool finished)
        {
            try { await Owner.Open(DialogKey, new DeliveryDialogContext(this, finished)); }
            catch { dialogPending = false; suspended = false; view.SetCharacterPaused(false); throw; }
        }
        public void DialogClosed() { dialogPending = false; suspended = false; view?.SetCharacterPaused(false); brake.Cancel(); }
        public bool TrySave()
        {
            if (IsSaved) return true;
            if (result == null || DataManager.Instance == null) return false;
            try { receipt = DataManager.Instance.RecordGameResult(result); return receipt != null; }
            catch { return false; }
        }
        public bool ReturnHome()
        {
            if (Session?.State == DeliveryState.Finished && !TrySave()) return false;
            return SceneFlowManager.Instance != null && SceneFlowManager.Instance.OpenHub(out _);
        }
        private static string OutcomeKey(DeliveryOutcome outcome)
        {
            switch(outcome)
            {
                case DeliveryOutcome.Perfect: return "ud.perfect";
                case DeliveryOutcome.Delivered: return "ud.delivered";
                case DeliveryOutcome.TooShort: return "ud.too_short";
                case DeliveryOutcome.TooFar: return "ud.too_far";
                default: return "ud.skipped";
            }
        }
        private void Refresh()
        {
            if (Session == null) return;
            score.text = L10n.Format("ud.score", Session.Score, Session.Deliveries);
            best.text = L10n.Format("bg.best", Math.Max(Session.Score, DataManager.Instance?.HighScore(DeliverySession.ScoreId) ?? 0));
            timer.text = L10n.Format("ud.timer", Mathf.CeilToInt((float)Session.Remaining));
            hearts.text = L10n.Format("ud.route", Session.ResolvedCount, Session.HouseCount, Session.Load);
            status.text = Session.State == DeliveryState.Ready ? L10n.Text("ud.instructions") :
                Session.Elapsed < feedbackUntil ? L10n.Text(OutcomeKey(Session.LastOutcome)) :
                Session.ParcelInFlight ? L10n.Text("ud.parcel_flying") :
                brake.IsBraking ? L10n.Text("ud.braking") : L10n.Format("ud.target", Session.ResolvedCount + 1, Math.Max(0, Session.TargetDistance), Session.Speed);
            startLabel.text = L10n.Text(Session.State == DeliveryState.Ready ? "ud.start" : "ud.in_progress");
            start.interactable = Session.State == DeliveryState.Ready;
            start.targetGraphic.raycastTarget = start.interactable;
            progress.value = (float)Session.SegmentProgress;
        }
    }
    public sealed class DeliveryDialogContext
    {
        public readonly DeliveryHud Hud;
        public readonly bool Finished;
        public DeliveryDialogContext(DeliveryHud hud, bool finished) { Hud = hud; Finished = finished; }
    }
}
