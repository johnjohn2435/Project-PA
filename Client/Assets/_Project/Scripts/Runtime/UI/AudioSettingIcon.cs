using System;
using System.Collections.Generic;
using PocketArcade.UI;
using PocketArcade.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    public enum AudioSetting { Music, Effects }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "PocketArcade.BlockGravity.Unity", "PocketArcade.BlockGravity.Unity", "AudioSettingIcon")]
    public sealed class AudioSettingIcon : MonoBehaviour, ILocalizedDynamicText
    {
        [SerializeField] private AudioSetting setting;
        [SerializeField] private Button button;
        [SerializeField] private Image icon, offSlash;
        [SerializeField] private TMP_Text state, notice;
        private DataManager data;
        public bool IsOn { get; private set; }
        public IEnumerable<TMP_Text> DynamicTextLabels => new[] { state, notice };
        public bool HasReferences => button != null && icon != null && icon.sprite != null && offSlash != null && state != null && notice != null;

        private void OnEnable()
        {
            button.onClick.AddListener(Change);
            data = DataManager.Instance;
            if (data != null) data.Changed += Refresh;
            L10n.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Change);
            if (data != null) data.Changed -= Refresh;
            L10n.Changed -= Refresh;
            data = null;
        }

        private void Refresh()
        {
            bool ready = data != null && data.IsInitialized;
            button.interactable = ready;
            var current = ready ? data.Settings : new PlayerSettingsData();
            IsOn = setting == AudioSetting.Music ? current.bgmEnabled : current.sfxEnabled;
            state.text = L10n.Text(IsOn ? "bg.settings.on" : "bg.settings.off");
            icon.color = IsOn ? new Color(.23f, .39f, .31f) : new Color(.56f, .59f, .56f);
            state.color = IsOn ? new Color(.23f, .39f, .31f) : new Color(.68f, .27f, .22f);
            offSlash.enabled = !IsOn;
            if (!ready) notice.text = L10n.Text("bg.settings.unavailable");
        }

        private void Change()
        {
            if (data == null || !data.IsInitialized) { Refresh(); return; }
            // Settings is a copy. Persist successfully before changing the displayed state.
            var next = data.Settings;
            if (setting == AudioSetting.Music) next.bgmEnabled = !next.bgmEnabled;
            else next.sfxEnabled = !next.sfxEnabled;
            try
            {
                data.UpdateSettings(next);
                notice.text = "";
            }
            catch (Exception) { notice.text = L10n.Text("bg.settings.save_error"); }
            Refresh();
        }
    }
}
