using System.Collections;
using PocketArcade.Core.Save;
using UnityEngine;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityPresenter : MonoBehaviour
    {
        [SerializeField] private BlockGravityBoardView view;
        [SerializeField] private PieceDragController input;
        [SerializeField, Min(.01f)] private float moveDuration = .18f, clearDuration = .15f;
        private BlockGravityHud hud;
        private bool visible, paused, lostFocus, applicationPaused;
        private int width, height;
        private Rect safe;
        private long savedBest, displayedScore;
        private GravityDirection displayedGravity;
        private string stateKey = "bg.ready";
        private int chain, displayedRemaining, displayedCombo;
        private ComboFeedback feedback;
        private PieceDefinition[] displayTray;
        private string resultId;
        public BlockGravityResultController Result { get; private set; }
        public BlockGravitySession Session { get; private set; }
        public BlockGravityBoardView View => view;
        public PieceDragController Input => input;
        public bool CanInteract => Session != null && Session.State == SessionState.WaitingForInput && CanAnimate;
        private bool CanAnimate => visible && !paused && !lostFocus && !applicationPaused && hud != null && hud.AllowsGameplay &&
            (SceneFlowManager.Instance == null || !SceneFlowManager.Instance.IsLoading);
        public void Initialize(BlockGravityHud owner, ComboFeedback comboFeedback = null)
        {
            feedback = comboFeedback ?? new ComboFeedback(() => DataManager.Instance != null && DataManager.Instance.IsInitialized && DataManager.Instance.Settings.hapticsEnabled, ComboFeedback.PulseDevice);
            hud = owner; view.Initialize(); input.Initialize(this);
            if (DataManager.Instance != null && DataManager.Instance.IsInitialized)
                foreach (var score in DataManager.Instance.Snapshot.highScores) if (score.gameId == "block_gravity") savedBest = score.score;
            Restart();
        }
        public void Restart()
        {
            if (Result != null) return;
            if (Session != null && Session.State == SessionState.AwaitingPresentation) return;
            if (Session != null) savedBest = System.Math.Max(savedBest,Session.Score);
            input.Cancel(); view.ResetShake(); StopAllCoroutines();
            Session = new BlockGravitySession(new BlockGravityConfig(),new ProgressivePieceSource(System.Environment.TickCount));
            resultId = System.Guid.NewGuid().ToString("N");
            CopyTray(); Reflow(); view.ShowBoard(Session.Board); view.ShowTray(displayTray);
            displayedCombo = 0; displayedScore = 0; displayedGravity = Session.Gravity; displayedRemaining = Session.RemainingUntilGravityChange; chain = 0; stateKey = "bg.ready"; RefreshHud();
        }
        public bool TryPlace(int slot, Cell origin)
        {
            if (!CanInteract || Session.TryPlace(slot,origin,out var result) != PlacementFailure.None) return false;
            Cue(AudioCue.Place);
            input.Cancel(); displayTray[slot] = null; view.ShowTray(displayTray);
            StartCoroutine(Play(result)); return true;
        }
        private IEnumerator Play(TurnResult result)
        {
            BoardState previous = null;
            bool feedbackPlayed = false;
            foreach (var frame in result.Frames)
            {
                while (!CanAnimate) yield return null;
                BoardState board = frame.Board;
                if (frame.Stage == TurnStage.RefillTray) { CopyTray(); view.ShowTray(displayTray); }
                if(frame.Stage==TurnStage.ChangingGravity)Cue(AudioCue.Gravity);
                displayedScore = frame.Score; displayedGravity = frame.Gravity; chain = frame.Chain; displayedCombo = frame.Combo;
                if (frame.Stage == TurnStage.ClearingLines && !feedbackPlayed)
                { feedbackPlayed = true; Cue(frame.Combo>=2?AudioCue.Combo:AudioCue.Clear); feedback.OnClear(frame.Combo); view.StartComboShake(frame.Combo); }
                displayedRemaining = frame.Gravity == Session.Gravity ? Session.RemainingUntilGravityChange : 0;
                stateKey = "bg.resolving"; RefreshHud();
                float duration = frame.Stage == TurnStage.ResolvingGravity ? moveDuration : frame.Stage == TurnStage.ClearingLines ? clearDuration : frame.Stage == TurnStage.ChangingGravity ? moveDuration : 0;
                if (frame.Stage == TurnStage.ResolvingGravity) view.BeginMovement(board);
                float elapsed = 0;
                while (elapsed < duration)
                {
                    if (!CanAnimate) { yield return null; continue; }
                    elapsed = Mathf.Min(duration,elapsed+Time.unscaledDeltaTime);
                    if (frame.Stage == TurnStage.ResolvingGravity) view.AnimateMovement(elapsed/duration);
                    if (frame.Stage == TurnStage.ClearingLines) view.AnimateClear(frame.Lines,previous,elapsed/duration);
                    yield return null;
                }
                view.ShowBoard(board); previous = board;
            }
            Session.CompletePresentation(); view.ResetShake(); CopyTray(); view.ShowTray(displayTray);
            displayedCombo = Session.Combo; displayedScore = Session.Score; displayedGravity = Session.Gravity; chain = 0;
            stateKey = result.IsGameOver ? "bg.over" : "bg.ready"; RefreshHud();
            if (result.IsGameOver) { Cue(AudioCue.GameOver); ShowResult(); }
        }
        private static void Cue(AudioCue cue)
        {var sound=SoundManager.Instance;if(sound!=null&&sound.IsInitialized)sound.PlayCue(cue);}
        public void ShowResult()
        {
            if (Session == null || Session.State != SessionState.GameOver || hud == null) return;
            if (Result == null)
                Result = new BlockGravityResultController(new GameResultRequest(resultId,"block_gravity",Session.Score,ScoreCalculator.Coins(Session.Score)),
                    request =>
                    {
                        var data = DataManager.Instance;
                        if (data == null || !data.IsInitialized) throw new System.InvalidOperationException(L10n.Text("bg.result.data_missing"));
                        return data.RecordGameResult(request);
                    });
            hud.ShowResult(Result,savedBest);
        }
        public void AcceptSavedResult(BlockGravityResultController result)
        {
            if (Result != result || !result.IsSaved) return;
            savedBest = System.Math.Max(savedBest,result.Receipt.highScore); RefreshHud();
        }
        public bool RestartCompletedResult(BlockGravityResultController result)
        {
            if (Result != result || !result.IsSaved) return false;
            AcceptSavedResult(result); Result = null; Restart(); return true;
        }
        private void CopyTray()
        { var tray = Session.Tray; displayTray = new PieceDefinition[tray.Count]; for (int i=0;i<tray.Count;i++) displayTray[i]=tray[i]; }
        public void RefreshHud()
        { if (hud != null && Session != null) hud.Present(displayedScore,savedBest,displayedGravity,displayedRemaining,stateKey,chain,displayedCombo,ProgressivePieceSource.LevelForPlacements(Session.Placements)); }
        public void SetVisible(bool value) { visible = value; view.SetVisible(value); if (!value) { input.Cancel(); view.ResetShake(); } else if (displayTray != null) view.ShowTray(displayTray); }
        public void SetPaused(bool value) { paused = value; if (value) { input.Cancel(); view.ResetShake(); } }
        private void Reflow()
        {
            input.Cancel(); width = Screen.width; height = Screen.height; safe = Screen.safeArea;
            if (width <= 0 || height <= 0 || safe.width <= 0 || safe.height <= 0) return;
            view.Reflow(width,height,safe); hud.ApplyLayout(view.Layout); if (displayTray != null) view.ShowTray(displayTray);
        }
        private void Update()
        { if (hud != null && (width != Screen.width || height != Screen.height || safe != Screen.safeArea)) Reflow(); }
        private void LateUpdate()
        { if (CanAnimate) view.TickShake(Time.unscaledDeltaTime); else view.ResetShake(); }
        private void OnApplicationFocus(bool focus)
        { lostFocus = !focus; if (!focus) { input.Cancel(); if (hud != null && hud.IsOpen && Session?.State != SessionState.GameOver) hud.Pause(); } }
        private void OnApplicationPause(bool value)
        { applicationPaused = value; if (value) { input.Cancel(); if (hud != null && hud.IsOpen && Session?.State != SessionState.GameOver) hud.Pause(); } }
        public void Shutdown()
        { input.Cancel(); view.ResetShake(); StopAllCoroutines(); hud = null; Session = null; }
        private void OnDestroy() { if (input != null) Shutdown(); }
    }
}
