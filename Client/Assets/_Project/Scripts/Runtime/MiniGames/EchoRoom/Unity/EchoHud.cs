using System;
using System.Collections.Generic;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.EchoRoom.Unity
{
    public sealed class EchoHud:UIPanel,ILocalizedDynamicText
    {
        public const string ScreenKey="UI/Screens/EchoRoom",MenuKey="UI/Overlays/EchoRoomMenu",WorldPath="MiniGames/EchoRoom/Playfield";
        [SerializeField] private EchoTuning tuning;
        [SerializeField] private EchoInput input;
        [SerializeField] private TMP_Text progress,timer,status,primaryLabel;
        [SerializeField] private Slider timeline;
        [SerializeField] private Button primary,pause;
        private ResourceManager resources;
        private GameObject world;
        private EchoWorldView view;
        private bool menuPending,focused=true,hidden;
        private int lastTenth=-1,lastDelivered=-1,lastState=-1;
        public EchoSession Session{get;private set;}
        public IEnumerable<TMP_Text> DynamicTextLabels=>new[]{progress,timer,status,primaryLabel};
        protected override void OnOpen(object data)
        {
            resources=ResourceManager.Instance;if(resources==null||!resources.IsPreloaded||tuning==null)throw new InvalidOperationException(L10n.Text("echo.error.references"));
            world=resources.Spawn(WorldPath,null);view=world.GetComponent<EchoWorldView>();if(view==null)throw new InvalidOperationException(L10n.Text("echo.error.references"));
            view.Attach(resources,DataManager.Instance!=null?DataManager.Instance.SelectedCharacterId:"char_cat");primary.onClick.AddListener(Play);pause.onClick.AddListener(Pause);L10n.Changed+=Refresh;Reset();
        }
        public void Reset(){Session=new EchoSession(tuning.Rules());input.Cancel();view.SetPaused(false);view.Present(Session);timeline.value=0;Refresh();}
        public void Play(){if(Session==null)return;if(Session.State!=EchoState.Ready)Reset();Session.Start();input.Cancel();Refresh();}
        private void Update()
        {
            if(!IsOpen||!IsVisible||Session==null||menuPending||!focused||hidden||!GetComponent<CanvasGroup>().interactable)return;
            var move=input.Move;Session.Tick(Math.Min(Time.unscaledDeltaTime,.1),new EchoPoint(move.x,move.y));view.Present(Session,move);timeline.value=(float)Session.LoopProgress;DrawHud();
        }
        private void Refresh(){lastTenth=-1;DrawHud();}
        private void DrawHud()
        {
            if(Session==null)return;int tenth=(int)Math.Ceiling(Session.Remaining*10),state=(int)Session.State;
            if(lastTenth==tenth&&lastDelivered==Session.Delivered&&lastState==state)return;lastTenth=tenth;lastDelivered=Session.Delivered;lastState=state;
            progress.text=L10n.Format("echo.progress",Session.Delivered,Session.Target,Session.GhostCount,Session.GhostDeliveries);
            timer.text=L10n.Format("echo.timer",Session.Loop,Session.Rounds,Session.LoopRemaining,Session.Remaining);
            status.text=Session.State==EchoState.Ready?L10n.Text("echo.instructions"):Session.State==EchoState.Complete?L10n.Format("echo.complete",Session.Delivered,Session.GhostDeliveries):Session.State==EchoState.TimeUp?L10n.Format("echo.time_up",Session.Delivered,Session.Target,Session.GhostDeliveries):Session.Loop>1&&Session.LoopElapsed<1.6?L10n.Format("echo.new_ghost",Session.GhostCount):Session.LoopRemaining<2?L10n.Text("echo.reset_warning"):L10n.Text("echo.playing");
            primary.interactable=Session.State!=EchoState.Playing;primary.targetGraphic.raycastTarget=primary.interactable;
            primaryLabel.text=L10n.Text(Session.State==EchoState.Ready?"echo.start":Session.State==EchoState.Playing?"echo.controls":"floor.retry");
        }
        public void Pause()=>UIManager.Observe(OpenMenu());
        private async System.Threading.Tasks.Task OpenMenu(){if(!IsOpen||menuPending)return;menuPending=true;input.Cancel();view.SetPaused(true);try{await Owner.Open(MenuKey,this);}catch{MenuClosed();throw;}}
        public void MenuClosed(){menuPending=false;input.Cancel();if(view!=null)view.SetPaused(false);}
        public bool Home()=>SceneFlowManager.Instance!=null&&SceneFlowManager.Instance.OpenHub(out _);
        private void OnApplicationFocus(bool value){focused=value;if(!value&&IsOpen)Pause();}
        private void OnApplicationPause(bool value){hidden=value;if(value&&IsOpen)Pause();}
        protected override bool OnBackRequested(){Pause();return true;}
        protected override void OnHide(){input.Cancel();if(view!=null){view.PlayCamera.enabled=false;view.SetPaused(true);}}
        protected override void OnShow(){if(view!=null){view.PlayCamera.enabled=true;view.SetPaused(menuPending);}}
        protected override void OnClose()=>Cleanup();
        private void OnDestroy()=>Cleanup();
        private void Cleanup(){L10n.Changed-=Refresh;if(primary!=null)primary.onClick.RemoveListener(Play);if(pause!=null)pause.onClick.RemoveListener(Pause);if(view!=null&&resources!=null)view.Release(resources);if(world!=null&&resources!=null)resources.Release(world);resources=null;world=null;view=null;Session=null;}
    }
}
