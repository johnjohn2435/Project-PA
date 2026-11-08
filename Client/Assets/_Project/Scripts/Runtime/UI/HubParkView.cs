using System;
using System.Collections.Generic;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    public sealed class HubParkView : MonoBehaviour, ILocalizedDynamicText
    {
        [SerializeField] private HubJoystick joystick;
        [SerializeField] private Button enterButton;
        [SerializeField] private TMP_Text attractionLabel;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private CanvasGroup homeInput;
        private HubParkController controller;
        private GameCatalog catalog;
        private string shownGame;
        private bool focused = true, paused;
        public HubParkWorld World => controller?.World;
        public string NearbyGameId => controller?.NearbyGameId;
        public bool CanInteract => isActiveAndEnabled && focused && !paused && homeInput.interactable && homeInput.blocksRaycasts;
        public IEnumerable<TMP_Text> DynamicTextLabels { get { yield return attractionLabel; yield return actionLabel; } }
        public void Bind(GameCatalog source, Action<string> enter)
        {
            catalog = source;
            controller = new HubParkController(ResourceManager.Instance, source, enter);
            enterButton.onClick.AddListener(Enter);
            L10n.Changed += RefreshText;
            RefreshText();
        }
        public void Present(string characterId) => controller?.Refresh(characterId);
        private void Update()
        {
            if (controller == null) return;
            bool allowed = CanInteract;
            if (!allowed) joystick.Cancel();
            controller.Step(allowed ? joystick.Move : Vector2.zero, allowed, Time.unscaledDeltaTime);
            if (shownGame != NearbyGameId) RefreshText();
            enterButton.interactable = allowed && NearbyGameId != null && !SceneFlowManager.Instance.IsLoading;
            var k = Keyboard.current;
            if (allowed && k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) Enter();
        }
        private void Enter() { if (CanInteract) controller?.Enter(); }
        private void RefreshText()
        {
            shownGame = NearbyGameId;
            attractionLabel.text = shownGame == null ? L10n.Text("park.walk_hint") : catalog.MiniGame(shownGame).DisplayName;
            actionLabel.text = L10n.Text(shownGame == null ? "park.explore" : "park.enter");
            if (enterButton.targetGraphic is ArcadePaperGraphic paper)
                paper.Configure(ArcadePaperGraphic.Shape.Capsule, shownGame == null ? new Color32(255,245,215,255) : new Color32(255,191,84,255));
        }
        private void OnDisable()
        {
            joystick?.Cancel();
            controller?.Step(Vector2.zero, false, 0);
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) joystick.Cancel(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) joystick.Cancel(); }
        private void OnDestroy()
        {
            L10n.Changed -= RefreshText;
            enterButton.onClick.RemoveListener(Enter);
            controller?.Dispose();
        }
    }
}
