using System;
using System.Collections.Generic;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    public sealed class HubMedals : MonoBehaviour, ILocalizedDynamicText
    {
        public const string CardPath = "UI/Common/GameMedalCard";
        [Serializable] private class Artwork
        {
            public MiniGameDefinition game;
            public ArcadePaperGraphic.Shape shape;
            public Color color;
        }
        [SerializeField] private Artwork[] artwork;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private Button start;
        private readonly List<GameMedalCardView> cards = new List<GameMedalCardView>();
        private GameCatalog catalog;
        private ResourceManager resources;
        private string chosenGame;
        private float stride;
        private float browsePosition;
        private bool layingOut;
        public int Count => cards.Count;
        public IEnumerable<TMP_Text> DynamicTextLabels { get { yield return startLabel; } }

        public void Bind(GameCatalog source)
        {
            if (catalog != null) return;
            catalog = source;
            resources = ResourceManager.Instance;
            foreach (var game in catalog.MiniGames)
            {
                var card = resources.Spawn(CardPath, scroll.content).GetComponent<GameMedalCardView>();
                var style = Array.Find(artwork, item => item.game == game);
                card.Bind(game, ChooseGame, style != null ? style.shape : ArcadePaperGraphic.Shape.PortraitMedal,
                    style != null ? style.color : new Color32(198, 228, 216, 255));
                cards.Add(card);
                if (chosenGame == null && game.IsAvailable) chosenGame = game.Id;
            }
            previous.onClick.AddListener(Previous);
            next.onClick.AddListener(Next);
            scroll.onValueChanged.AddListener(Scrolled);
            Layout();
            RefreshGames();
        }

        private void Layout()
        {
            if (scroll == null || catalog == null) return;
            float width = scroll.viewport.rect.width;
            if (width <= 0) return;
            float position = browsePosition;
            layingOut = true;
            float cardWidth = width * .4f;
            float gap = width * .025f;
            stride = cardWidth + gap;
            for (int i = 0; i < cards.Count; i++)
            {
                var rect = (RectTransform)cards[i].transform;
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, .5f);
                rect.sizeDelta = new Vector2(cardWidth, 0);
                rect.anchoredPosition = new Vector2(i * stride, 0);
            }
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(width, cards.Count * stride - gap));
            scroll.StopMovement();
            scroll.horizontalNormalizedPosition = position;
            layingOut = false;
            RefreshArrows();
        }

        public void ChooseGame(string id)
        {
            if (catalog.MiniGame(id)?.IsAvailable != true) return;
            chosenGame = id;
            RefreshGames();
            // Reveal a partly visible card while keeping selection separate from starting a game.
            int index = cards.FindIndex(card => card.GameId == id);
            float left = -scroll.content.anchoredPosition.x;
            float right = index * stride + scroll.viewport.rect.width * .4f;
            if (index * stride < left) MoveTo(index * stride);
            else if (right > left + scroll.viewport.rect.width) MoveTo(right - scroll.viewport.rect.width);
        }

        public void Launch(Action<string> open)
        {
            if (chosenGame != null && catalog.MiniGame(chosenGame)?.IsAvailable == true) open(chosenGame);
        }
        public void Present() => RefreshGames();
        private void RefreshGames()
        {
            if (catalog == null) return;
            foreach (var card in cards) card.Present(chosenGame);
            start.interactable = chosenGame != null;
            startLabel.text = chosenGame == null ? L10n.Text("hub.choose_game") :
                L10n.Format("hub.start_game", catalog.MiniGame(chosenGame).DisplayName);
        }
        private void Previous() => MoveTo(-scroll.content.anchoredPosition.x - stride);
        private void Next() => MoveTo(-scroll.content.anchoredPosition.x + stride);
        private void MoveTo(float distance)
        {
            float overflow = scroll.content.rect.width - scroll.viewport.rect.width;
            scroll.StopMovement();
            browsePosition = overflow > .01f ? Mathf.Clamp01(distance / overflow) : 0;
            scroll.horizontalNormalizedPosition = browsePosition;
            RefreshArrows();
        }
        private void Scrolled(Vector2 value)
        {
            if (!layingOut) browsePosition = Mathf.Clamp01(value.x);
            RefreshArrows();
        }
        private void RefreshArrows()
        {
            bool overflow = scroll.content.rect.width > scroll.viewport.rect.width + .5f;
            previous.interactable = overflow && scroll.horizontalNormalizedPosition > .001f;
            next.interactable = overflow && scroll.horizontalNormalizedPosition < .999f;
        }
        private void OnRectTransformDimensionsChange() => Layout();
        private void OnEnable() => Layout();
        private void OnDisable() { if (scroll != null) scroll.StopMovement(); }
        private void OnDestroy()
        {
            if (catalog == null) return;
            previous.onClick.RemoveListener(Previous);
            next.onClick.RemoveListener(Next);
            scroll.onValueChanged.RemoveListener(Scrolled);
            if (resources != null)
                foreach (var card in cards) if (card != null) resources.Release(card.gameObject);
        }
    }
}
