using System;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PocketArcade.UI
{
    public sealed class MiniGameCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        private UnityAction click;
        private MiniGameDefinition definition;
        private void OnEnable() { L10n.Changed += Refresh; Refresh(); }
        private void OnDisable() => L10n.Changed -= Refresh;
        private void Refresh() { if (definition != null && label != null) label.text = L10n.Format(definition.IsAvailable ? "card.play" : "card.locked", definition.DisplayName); }
        [SerializeField] private Image thumbnail;
        [SerializeField, OptionalUIReference] private Image playIcon;
        [SerializeField, OptionalUIReference] private Image lockIcon;
        public void Bind(MiniGameDefinition game, Action<string> open)
        {
            if (label == null || button == null || thumbnail == null)
                throw new InvalidOperationException("Mini game card references are missing.");
            definition = game; Refresh();
            thumbnail.sprite = game.Thumbnail;
            thumbnail.enabled = game.Thumbnail != null;
            if (playIcon != null) playIcon.enabled = game.IsAvailable;
            if (lockIcon != null) lockIcon.enabled = !game.IsAvailable;
            // 잠긴 카드도 안내는 표시하되 SceneFlow에서 진입을 차단한다.
            if (click != null) button.onClick.RemoveListener(click);
            click = () => open(game.Id);
            button.onClick.AddListener(click);
        }
    }
}
