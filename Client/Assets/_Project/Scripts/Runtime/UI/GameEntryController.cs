using System;
namespace PocketArcade.UI
{
    public sealed class GameEntryController : IDisposable
    {
        private readonly GameEntryView view;
        private readonly DataManager data;
        private readonly SceneFlowManager scenes;
        private readonly UIManager ui;
        private readonly SoundManager sound;
        public GameEntryController(GameEntryView view, DataManager data, SceneFlowManager scenes, UIManager ui, SoundManager sound)
        { this.view = view; this.data = data; this.scenes = scenes; this.ui = ui; this.sound = sound; }
        public void Initialize()
        {
            view.BackRequested += RequestBack; ui.RootBackRequested += Back;
            if (scenes.SelectedGame == null) { Back(); return; }
            L10n.Changed += Refresh;
            Refresh();
        }
        private void Refresh() => view.Present(scenes.SelectedGame.DisplayName, L10n.Format("game.ready", data.Catalog.Character(data.SelectedCharacterId).DisplayName));
        private void RequestBack() => UIManager.Observe(ui.Back());
        private void Back()
        {
            sound.PlayUiClick();
            if (!scenes.OpenHub(out string error)) ui.ShowNotice(error);
        }
        public void Dispose() { view.BackRequested -= RequestBack; ui.RootBackRequested -= Back; L10n.Changed -= Refresh; }
    }
}
