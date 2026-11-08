using System;
using PocketArcade.Core.Save;
using PocketArcade.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace PocketArcade.UI
{
    public sealed class HubView : UISceneView
    {
        [SerializeField] private GameObject homePanel;
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text characterLabel;
        [SerializeField] private TMP_Text notice;
        [SerializeField] private Image portrait;
        [SerializeField, OptionalUIReference] private CharacterRigPreview rigPreview;
        [SerializeField] private Button portraitButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button accountButton;
        [SerializeField] private Button[] backButtons;
        [SerializeField] private CharacterCardView[] characterCards;
        [SerializeField] private MiniGameCardView[] gameCards;
        [SerializeField] private Toggle bgmToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle hapticsToggle;

        [SerializeField, OptionalUIReference] private HubMedals medals;
        [SerializeField, OptionalUIReference] private HubParkView park;
        [SerializeField, OptionalUIReference] private CharacterRosterView roster;
        [SerializeField, OptionalUIReference] private Button allGamesButton;
        private HubController controller;
        public event Action<UIPage> NavigationRequested;
        public event Action BackRequested, AccountRequested;
        public event Action<string> CharacterSelected, GameSelected;
        public event Action<PlayerSettingsData> SettingsChanged;
        private bool wired;
        private ToggleView bgmControl, sfxControl, hapticsControl;
        private void Start()
        {
            if (!GameBootstrap.RequireManagers()) return;
            controller = new HubController(this, DataManager.Instance, UIManager.Instance,
                SoundManager.Instance, SceneFlowManager.Instance);
            controller.Initialize();
            RegisterUI();
        }
        public void Bind(GameCatalog catalog)
        {
            if (wired) return;
            if (homePanel == null || characterPanel == null || gamePanel == null || settingsPanel == null ||
                coinsLabel == null || characterLabel == null || notice == null || portrait == null ||
                portraitButton == null || playButton == null || charactersButton == null || settingsButton == null ||
                accountButton == null || bgmToggle == null || sfxToggle == null || hapticsToggle == null ||
                backButtons == null || backButtons.Length != 3 || characterCards == null ||
                (roster == null && characterCards.Length != catalog.Characters.Count) || gameCards == null ||
                (medals == null && gameCards.Length != catalog.MiniGames.Count))
                throw new InvalidOperationException("Hub references do not match the catalog.");
            medals?.Bind(catalog);
            park?.Bind(catalog, id => GameSelected?.Invoke(id));
            roster?.Bind(catalog, id => CharacterSelected?.Invoke(id));
            playButton.onClick.AddListener(Play);
            if (allGamesButton != null) allGamesButton.onClick.AddListener(Games);
            charactersButton.onClick.AddListener(Characters);
            portraitButton.onClick.AddListener(Characters);
            settingsButton.onClick.AddListener(Settings);
            accountButton.onClick.AddListener(Account);
            foreach (var back in backButtons) back.onClick.AddListener(Back);
            for (int i = 0; i < characterCards.Length; i++) characterCards[i].Bind(catalog.Characters[i], id => CharacterSelected?.Invoke(id));
            if (medals == null)
                for (int i = 0; i < gameCards.Length; i++) gameCards[i].Bind(catalog.MiniGames[i], id => PickGame(catalog, id));
            bgmControl = Control(bgmToggle); bgmControl.Changed += SettingsInput;
            sfxControl = Control(sfxToggle); sfxControl.Changed += SettingsInput;
            hapticsControl = Control(hapticsToggle); hapticsControl.Changed += SettingsInput;
            wired = true;
        }
        private static ToggleView Control(Toggle toggle) => toggle.GetComponent<ToggleView>() ?? throw new InvalidOperationException(L10n.Text("ui.error.definition"));
        private void PickGame(GameCatalog catalog, string id)
        {
            if (medals == null || catalog.MiniGame(id)?.IsAvailable != true)
            {
                GameSelected?.Invoke(id);
                return;
            }
            medals.ChooseGame(id);
            NavigationRequested?.Invoke(UIPage.Home);
        }
        private void Play()
        {
            if (park != null) return;
            if (medals != null) medals.Launch(id => GameSelected?.Invoke(id));
            else Games();
        }
        private void Games() => NavigationRequested?.Invoke(UIPage.Games);
        private void Characters() => NavigationRequested?.Invoke(UIPage.Characters);
        private void Settings() => NavigationRequested?.Invoke(UIPage.Settings);
        private void Back() => BackRequested?.Invoke();
        private void Account() => AccountRequested?.Invoke();
        private void SettingsInput(bool _) => SettingsChanged?.Invoke(new PlayerSettingsData
        { bgmEnabled = bgmToggle.isOn, sfxEnabled = sfxToggle.isOn, hapticsEnabled = hapticsToggle.isOn });
        public void Present(GameCatalog catalog, string selectedId, int coins, PlayerSettingsData settings, Func<string, bool> unlocked)
        {
            var character = catalog.Character(selectedId);
            medals?.Present();
            park?.Present(selectedId);
            roster?.Present(selectedId, unlocked);
            coinsLabel.text = L10n.Format((medals != null || park != null) ? "hub.coin_amount" : "hub.coins", coins);
            characterLabel.text = character.DisplayName;
            portrait.enabled=rigPreview==null;
            if(rigPreview!=null && rigPreview.isActiveAndEnabled)portrait.enabled=!rigPreview.ShowCharacter(character.Id);
            portrait.sprite = character.Portrait;
            portrait.color = character.Portrait != null ? Color.white : character.ThemeColor;
            for (int i = 0; i < characterCards.Length; i++) characterCards[i].Refresh(selectedId, unlocked(catalog.Characters[i].Id));
            bgmControl.Present(L10n.Text("settings.music"), settings.bgmEnabled);
            sfxControl.Present(L10n.Text("settings.sfx"), settings.sfxEnabled);
            hapticsControl.Present(L10n.Text("settings.haptics"), settings.hapticsEnabled);
        }
        public override UIPage DefaultPanel => UIPage.Home;
        public override bool Supports(UIPage panel) => panel == UIPage.Home || panel == UIPage.Characters || panel == UIPage.Games || panel == UIPage.Settings;
        public override void DisplayPanel(UIPage panel)
        {
            if (!Supports(panel)) throw new ArgumentException("Unsupported hub panel.");
            UIManager.Observe(UIManager.Instance.ShowPanel(panel));
        }
        public override void DisplayNotice(string message) => notice.text = message;
        private void OnDestroy()
        {
            controller?.Dispose();
            if (!wired) return;
            if (allGamesButton != null) allGamesButton.onClick.RemoveListener(Games);
            playButton.onClick.RemoveListener(Play); charactersButton.onClick.RemoveListener(Characters);
            portraitButton.onClick.RemoveListener(Characters); settingsButton.onClick.RemoveListener(Settings);
            accountButton.onClick.RemoveListener(Account);
            foreach (var back in backButtons) back.onClick.RemoveListener(Back);
            bgmControl.Changed -= SettingsInput; sfxControl.Changed -= SettingsInput;
            hapticsControl.Changed -= SettingsInput;
        }
    }
}
