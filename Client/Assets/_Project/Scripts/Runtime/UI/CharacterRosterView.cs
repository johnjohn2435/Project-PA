using System;
using System.Collections.Generic;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    public sealed class CharacterRosterView : MonoBehaviour, ILocalizedDynamicText
    {
        public const string CardPath = "UI/Common/ParkCharacterCard";
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text countLabel;
        private readonly List<CharacterCardView> cards = new List<CharacterCardView>();
        private GameCatalog catalog;
        private ResourceManager resources;
        public int Count => cards.Count;
        public IEnumerable<TMP_Text> DynamicTextLabels { get { yield return countLabel; } }
        public void Bind(GameCatalog source, Action<string> select)
        {
            catalog = source; resources = ResourceManager.Instance;
            foreach (var character in catalog.Characters)
            {
                var card = resources.Spawn(CardPath, content).GetComponent<CharacterCardView>();
                card.Bind(character, select); cards.Add(card);
            }
            Layout();
        }
        private void Layout()
        {
            const float gap = 24, height = 350;
            float width = (content.rect.width - gap) * .5f;
            for (int i = 0; i < cards.Count; i++)
            {
                var rect = (RectTransform)cards[i].transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2((i % 2) * (width + gap), -(i / 2) * (height + gap));
            }
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.CeilToInt(cards.Count / 2f) * (height + gap));
        }
        private void OnRectTransformDimensionsChange() { if (content != null && cards.Count > 0) Layout(); }
        public void Present(string selected, Func<string, bool> unlocked)
        {
            for (int i = 0; i < cards.Count; i++) cards[i].Refresh(selected, unlocked(catalog.Characters[i].Id));
            countLabel.text = L10n.Format("park.friend_count", cards.Count);
        }
        private void OnEnable() { if (scroll != null) scroll.StopMovement(); if (cards.Count > 0) Layout(); }
        private void OnDestroy()
        { if (resources != null) foreach (var card in cards) if (card != null) resources.Release(card.gameObject); }
    }
}
