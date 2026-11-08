using System;
using System.IO;
using PocketArcade.Core.Save;
using UnityEngine;
namespace PocketArcade.UI
{
    public sealed class HubController : IDisposable
    {
        private readonly HubView view;
        private readonly DataManager data;
        private readonly UIManager ui;
        private readonly SoundManager sound;
        private readonly SceneFlowManager scenes;
        public HubController(HubView view, DataManager data, UIManager ui, SoundManager sound, SceneFlowManager scenes)
        { this.view = view; this.data = data; this.ui = ui; this.sound = sound; this.scenes = scenes; }
        public void Initialize()
        {
            view.Bind(data.Catalog);
            view.NavigationRequested += Navigate; view.BackRequested += Back; view.AccountRequested += Account;
            view.CharacterSelected += Select; view.GameSelected += OpenGame; view.SettingsChanged += SaveSettings;
            data.Changed += Refresh; L10n.Changed += Refresh;
            Refresh();
        }
        private void Refresh() => view.Present(data.Catalog, data.SelectedCharacterId, data.Coins, data.Settings, data.IsUnlocked);
        private void Navigate(UIPage panel)
        {
            if (panel == UIPage.Home) UIManager.Observe(ui.ShowHome());
            else UIManager.Observe(ui.ShowPanel(panel));
            sound.PlayUiClick();
        }
        private void Back() { UIManager.Observe(ui.Back()); sound.PlayUiClick(); }
        private void Account() => ui.ShowMessageBox(L10n.Text("account.title"), L10n.Text("account.body"));
        private void Select(string id) => TrySave(() =>
        {
            if (data.SelectCharacter(id))
            {
                if (ui.CurrentPanel != UIPage.Home) UIManager.Observe(ui.ShowHome());
                sound.PlayUiClick();
            }
            else ui.ShowToast(L10n.Text("character.locked"));
        });
        private void SaveSettings(PlayerSettingsData settings) => TrySave(() => data.UpdateSettings(settings));
        private void TrySave(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is NotSupportedException)
            {
                Debug.LogWarning(L10n.Format("ui.error.save", error.Message));
                Refresh(); ui.ShowMessageBox(L10n.Text("save.title"), L10n.Text("save.body"));
            }
        }
        private void OpenGame(string id)
        {
            sound.PlayUiClick();
            if (!scenes.OpenGame(id, out string error)) ui.ShowToast(error);
        }
        public void Dispose()
        {
            data.Changed -= Refresh; L10n.Changed -= Refresh; view.NavigationRequested -= Navigate; view.BackRequested -= Back;
            view.AccountRequested -= Account; view.CharacterSelected -= Select; view.GameSelected -= OpenGame;
            view.SettingsChanged -= SaveSettings;
        }
    }
}
