using System;
using PocketArcade.Core.Save;
using PocketArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravitySettingsPopup : UIPopup
    {
        [SerializeField] private Button music, sound, haptics, back, dismiss;
        [SerializeField] private TMP_Text notice;
        private PlayerSettingsData current;
        protected override void OnOpen(object data)
        {
            music.onClick.AddListener(Music); sound.onClick.AddListener(Sound); haptics.onClick.AddListener(Haptics);
            back.onClick.AddListener(Back); dismiss.onClick.AddListener(Back); L10n.Changed += Refresh;
            Read();
        }
        private void Read()
        {
            var data = DataManager.Instance;
            bool ready = data != null && data.IsInitialized;
            current = ready ? data.Settings : new PlayerSettingsData();
            music.interactable = sound.interactable = haptics.interactable = ready;
            notice.text = ready ? "" : L10n.Text("bg.settings.unavailable"); Refresh();
        }
        private void Refresh()
        {
            Label(music,"settings.music",current.bgmEnabled); Label(sound,"settings.sfx",current.sfxEnabled);
            Label(haptics,"settings.haptics",current.hapticsEnabled);
        }
        private static void Label(Button button,string key,bool enabled)
        { button.GetComponentInChildren<TMP_Text>(true).text = L10n.Format("bg.settings.value",L10n.Text(key),L10n.Text(enabled ? "bg.settings.on" : "bg.settings.off")); }
        private void Music() => Change(0);
        private void Sound() => Change(1);
        private void Haptics() => Change(2);
        private void Change(int setting)
        {
            var next = new PlayerSettingsData { bgmEnabled = current.bgmEnabled, sfxEnabled = current.sfxEnabled, hapticsEnabled = current.hapticsEnabled };
            if (setting == 0) next.bgmEnabled = !next.bgmEnabled;
            if (setting == 1) next.sfxEnabled = !next.sfxEnabled;
            if (setting == 2) next.hapticsEnabled = !next.hapticsEnabled;
            try { DataManager.Instance.UpdateSettings(next); current = next; notice.text = ""; Refresh(); }
            catch (Exception) { notice.text = L10n.Text("bg.settings.save_error"); Refresh(); }
        }
        private void Back() => UIManager.Observe(Owner.Close(Key));
        protected override void OnClose()
        {
            music.onClick.RemoveListener(Music); sound.onClick.RemoveListener(Sound); haptics.onClick.RemoveListener(Haptics);
            back.onClick.RemoveListener(Back); dismiss.onClick.RemoveListener(Back); L10n.Changed -= Refresh;
        }
    }
}
