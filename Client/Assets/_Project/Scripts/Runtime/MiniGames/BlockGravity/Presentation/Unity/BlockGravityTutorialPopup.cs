using System;
using System.Collections.Generic;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityTutorialPopup : UIPopup, ILocalizedDynamicText
    {
        public const string TutorialId="block_gravity.v1";
        [SerializeField] private TMP_Text title,body,progress,notice,nextLabel;
        [SerializeField] private Button previous,next,later;
        private int step;
        private bool saveFailed;
        public int Step=>step;
        public IEnumerable<TMP_Text> DynamicTextLabels=>new[]{title,body,progress,notice,nextLabel};
        protected override void OnOpen(object userData)
        {
            step=0;saveFailed=false;notice.text="";
            previous.onClick.AddListener(Previous);next.onClick.AddListener(Next);later.onClick.AddListener(Later);
            L10n.Changed+=Refresh;Refresh();
        }
        private void Previous(){if(step>0)step--;saveFailed=false;notice.text="";Refresh();}
        private void Next()
        {
            if(step<2){step++;Refresh();return;}
            var data=DataManager.Instance;
            if(data==null||!data.IsInitialized){notice.text=L10n.Text("bg.tutorial.data_missing");return;}
            try{data.CompleteTutorial(TutorialId);UIManager.Observe(Owner.Close(Key));}
            catch(Exception){saveFailed=true;notice.text=L10n.Text("bg.tutorial.save_error");Refresh();}
        }
        private void Later()=>UIManager.Observe(Owner.Close(Key));
        private void Refresh()
        {
            title.text=L10n.Text("bg.tutorial.title."+step);body.text=L10n.Text("bg.tutorial.body."+step);
            progress.text=L10n.Format("bg.tutorial.progress",step+1,3);previous.interactable=step>0;
            nextLabel.text=L10n.Text(saveFailed?"common.retry":step==2?"bg.tutorial.start":"bg.tutorial.next");
            if(saveFailed)notice.text=L10n.Text("bg.tutorial.save_error");
        }
        protected override bool OnBackRequested()=>true;
        protected override void OnClose()
        {previous.onClick.RemoveListener(Previous);next.onClick.RemoveListener(Next);later.onClick.RemoveListener(Later);L10n.Changed-=Refresh;}
    }
}
