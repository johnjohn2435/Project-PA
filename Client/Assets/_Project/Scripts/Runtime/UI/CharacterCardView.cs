using System;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PocketArcade.UI
{
    public sealed class CharacterCardView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        [SerializeField, OptionalUIReference] private Image selectedBadge;
        [SerializeField, OptionalUIReference] private Image lockedBadge;
        private UnityAction click;
        private CharacterDefinition definition;

        public void Bind(CharacterDefinition character, Action<string> select)
        {
            if (portrait == null || label == null || button == null)
                throw new InvalidOperationException("Character card references are missing.");
            definition = character;
            portrait.sprite = character.Portrait;
            portrait.color = character.Portrait != null ? Color.white : character.ThemeColor;
            if (click != null) button.onClick.RemoveListener(click);
            click = () => select(character.Id);
            button.onClick.AddListener(click);
        }

        public void Refresh(string selectedId, bool unlocked)
        {
            bool selected = definition.Id == selectedId;
            label.text = L10n.Format(selected ? "card.selected" : unlocked ? "card.select" : "card.locked", definition.DisplayName);
            if (selectedBadge != null) selectedBadge.enabled = selected;
            if (lockedBadge != null) lockedBadge.enabled = !unlocked && !selected;
            button.interactable = unlocked;
        }
    }
}
