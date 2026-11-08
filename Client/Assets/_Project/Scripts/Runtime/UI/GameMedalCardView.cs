using System;
using System.Collections.Generic;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    public sealed class GameMedalCardView : MonoBehaviour, ILocalizedDynamicText
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private ArcadePaperGraphic face;
        [SerializeField] private ArcadePaperGraphic check;
        [SerializeField] private Image thumbnail;
        private MiniGameDefinition definition;
        private UnityAction click;
        public string GameId => definition.Id;
        public IEnumerable<TMP_Text> DynamicTextLabels { get { yield return label; } }
        public void Bind(MiniGameDefinition game, Action<string> choose, ArcadePaperGraphic.Shape shape, Color color)
        {
            definition = game;
            gameObject.name = game.Id;
            face.Configure(shape, color);
            thumbnail.sprite = game.Thumbnail;
            thumbnail.enabled = game.Thumbnail != null;
            if (click != null) button.onClick.RemoveListener(click);
            click = () => choose(game.Id);
            button.onClick.AddListener(click);
        }
        public void Present(string selected)
        {
            label.text = definition.IsAvailable ? definition.DisplayName : L10n.Format("card.locked", definition.DisplayName);
            face.Select(definition.Id == selected);
            check.enabled = definition.Id == selected;
            button.interactable = definition.IsAvailable;
        }
        private void OnDestroy() { if (click != null) button.onClick.RemoveListener(click); }
    }
}
