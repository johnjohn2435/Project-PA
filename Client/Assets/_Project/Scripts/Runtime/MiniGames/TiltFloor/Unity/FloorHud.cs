using System;
using System.Collections.Generic;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.TiltFloor.Unity
{
    public sealed class FloorHud:UIPanel,ILocalizedDynamicText
    {
        public const string ScreenKey="UI/Screens/TiltFloor",WorldPath="MiniGames/TiltFloor/Playfield",MenuKey="UI/Overlays/TiltFloorMenu";
        [SerializeField] private FloorTuning tuning;
        [SerializeField] private FloorInput input;
        [SerializeField] private TMP_Text progress,status,balance,primaryLabel;
        [SerializeField] private Slider danger;
        [SerializeField] private Button primary,pause;
        private GameObject world;
        private FloorWorldView view;
        private ResourceManager resources;
        private bool menuPending,applicationHidden,focused=true;
        public FloorSession Session{get;private set;}
        public IEnumerable<TMP_Text> DynamicTextLabels=>new[]{progress,status,balance,primaryLabel};
        protected override void OnOpen(object data)
        {
            resources=ResourceManager.Instance;
            if(resources==null||!resources.IsPreloaded||tuning==null)throw new InvalidOperationException(L10n.Text("floor.error.references"));
            world=resources.Spawn(WorldPath,null);view=world.GetComponent<FloorWorldView>();
            if(view==null)throw new InvalidOperationException(L10n.Text("floor.error.references"));
            view.AttachCharacter(resources, DataManager.Instance != null ? DataManager.Instance.SelectedCharacterId : "char_cat");
            primary.onClick.AddListener(Play);pause.onClick.AddListener(Pause);L10n.Changed+=Refresh;Reset();
        }
        public void Reset(){Session=new FloorSession(tuning.Rules(),UnityEngine.Random.Range(1,1000000));input.Cancel();view.SetCharacterPaused(false);view.Present(Session);Refresh();}
        public void Play(){if(Session==null)return;if(Session.State!=FloorState.Ready)Reset();Session.Start();input.Cancel();Refresh();}
        private void Update()
        {
            if(!IsOpen||!IsVisible||Session==null||menuPending||applicationHidden||!focused||!GetComponent<CanvasGroup>().interactable)return;
            Vector2 move=input.Move;Session.Tick(Math.Min(Time.unscaledDeltaTime,.1),new FloorVector(move.x,move.y));view.Present(Session, move);Refresh();
        }
        private void Refresh()
        {
            if(Session==null)return;
            progress.text=L10n.Format("floor.survival_score",Session.Score,Session.Elapsed);
            balance.text=L10n.Format("floor.survival_info",Session.Stage,Session.EnemyCount,Session.Defeated,Session.Collected,Math.Round(Session.Tilt.Length));danger.value=(float)Session.Danger;
            status.text=Session.State==FloorState.Ready?L10n.Text("floor.instructions"):
                Session.State==FloorState.Fell?L10n.Format("floor.survival_over",Session.Score,Session.Elapsed,Session.BonusScore):
                Session.State==FloorState.Hit?L10n.Format("floor.hit_over",Session.Score,Session.Elapsed,Session.BonusScore):
                Session.Elapsed-Session.LastCollectedTime<1.5?L10n.Format("floor.survival_bonus",Session.ItemPoints):
                L10n.Text(Session.Danger>.72?"floor.warning":"floor.playing");
            primary.interactable=Session.State!=FloorState.Playing;primary.targetGraphic.raycastTarget=primary.interactable;
            primaryLabel.text=L10n.Text(Session.State==FloorState.Ready?"floor.start":Session.State==FloorState.Playing?"floor.controls":"floor.retry");
        }
        public void Pause()=>UIManager.Observe(OpenMenu());
        private async System.Threading.Tasks.Task OpenMenu()
        {if(!IsOpen||menuPending)return;menuPending=true;view.SetCharacterPaused(true);input.Cancel();try{await Owner.Open(MenuKey,this);}catch{MenuClosed();throw;}}
        public void MenuClosed(){menuPending=false;if(view!=null)view.SetCharacterPaused(false);input.Cancel();}
        public bool Home()=>SceneFlowManager.Instance!=null&&SceneFlowManager.Instance.OpenHub(out _);
        private void OnApplicationFocus(bool value){focused=value;if(!value&&IsOpen)Pause();}
        private void OnApplicationPause(bool value){applicationHidden=value;if(value&&IsOpen)Pause();}
        protected override bool OnBackRequested(){Pause();return true;}
        protected override void OnHide(){input.Cancel();if(view!=null)view.PlayCamera.enabled=false;}
        protected override void OnShow(){if(view!=null)view.PlayCamera.enabled=true;}
        protected override void OnClose()=>Cleanup();
        private void OnDestroy()=>Cleanup();
        private void Cleanup()
        {L10n.Changed-=Refresh;if(primary!=null)primary.onClick.RemoveListener(Play);if(pause!=null)pause.onClick.RemoveListener(Pause);if(view!=null&&resources!=null)view.ReleaseCharacter(resources);if(world!=null&&resources!=null)resources.Release(world);world=null;view=null;Session=null;resources=null;}
    }
}
