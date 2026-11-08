using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    public sealed class GameEntryView : UISceneView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button backButton;
        private GameEntryController controller;
        public event Action BackRequested;
        public override UIPage DefaultPanel => UIPage.GameEntry;
        public override bool Supports(UIPage panel) => panel == UIPage.GameEntry;
        public override void DisplayPanel(UIPage panel)
        { if (!Supports(panel)) throw new ArgumentException("Unsupported game entry panel."); }
        public override void DisplayNotice(string message) => status.text = message;
        public void Present(string heading, string message) { title.text = heading; status.text = message; }
        private void Start()
        {
            if (!GameBootstrap.RequireManagers()) return;
            if (title == null || status == null || backButton == null) throw new InvalidOperationException("Game entry references are missing.");
            backButton.onClick.AddListener(Back);
            RegisterUI();
            controller = new GameEntryController(this, DataManager.Instance, SceneFlowManager.Instance, UIManager.Instance, SoundManager.Instance);
            controller.Initialize();
        }
        private void Back() => BackRequested?.Invoke();
        private void OnDestroy() { controller?.Dispose(); if (backButton != null) backButton.onClick.RemoveListener(Back); }
    }
}
