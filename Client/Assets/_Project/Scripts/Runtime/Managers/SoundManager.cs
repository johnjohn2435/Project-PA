using System;
using UnityEngine;

namespace PocketArcade
{
    [DisallowMultipleComponent]
    public sealed class SoundManager : PersistentManager
    {
        [SerializeField] private AudioClip uiClickClip;
        public static SoundManager Instance { get; private set; }
        public bool IsInitialized => data != null && bgmSource != null && sfxSource != null;
        public bool BgmEnabled { get; private set; }
        public bool SfxEnabled { get; private set; }
        private DataManager data;
        private AudioSource bgmSource;
        private AudioSource sfxSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Initialize(DataManager dataManager)
        {
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("SoundManager is already initialized by another object.");
            if (dataManager == null || !dataManager.IsInitialized)
                throw new InvalidOperationException("Initialize DataManager before SoundManager.");
            if (IsInitialized)
            {
                if (data != dataManager) throw new InvalidOperationException("Cannot replace active sound settings.");
                Persist();
            Instance = this;
                return;
            }
            data = dataManager;
            if (bgmSource == null) bgmSource = CreateSource(true);
            if (sfxSource == null) sfxSource = CreateSource(false);
            data.Changed += ApplySettings;
            ApplySettings();
            Persist();
            Instance = this;
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.loop = loop;
            return source;
        }

        private void ApplySettings()
        {
            var settings = data.Settings;
            BgmEnabled = settings.bgmEnabled;
            SfxEnabled = settings.sfxEnabled;
            // BGM 시간은 유지하고 음소거만 전환하므로 다시 켜도 이어서 재생된다.
            bgmSource.mute = !BgmEnabled;
            sfxSource.mute = !SfxEnabled;
            if (!SfxEnabled) sfxSource.Stop();
        }

        public bool PlayBgm(AudioClip clip)
        {
            RequireInitialized();
            if (clip == null) return false;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return true;
            bgmSource.clip = clip;
            bgmSource.Play();
            return true;
        }

        public void StopBgm()
        {
            RequireInitialized();
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        public bool PlaySfx(AudioClip clip)
        {
            RequireInitialized();
            if (clip == null || !SfxEnabled) return false;
            sfxSource.PlayOneShot(clip);
            return true;
        }

        public event Action<AudioCue> CueRequested;
        private ResourceManager bootResources;
        public bool PlayCue(AudioCue cue)
        {
            RequireInitialized(); CueRequested?.Invoke(cue);
            return PlaySfx(bootResources!=null?bootResources.GetLoaded<AudioClip>(AudioPaths.Cue(cue)):null);
        }
        public bool PlaySceneMusic(bool game)
        {
            RequireInitialized();
            var clip=bootResources!=null?bootResources.GetLoaded<AudioClip>(game?AudioPaths.GameBgm:BootResourceManifest.HubBgm):null;
            if(clip!=null)return PlayBgm(clip);
            StopBgm();return false;
        }
        private AudioClip bootUiClick;
        public void UseBootResources(ResourceManager resources)
        {
            RequireInitialized();
            bootResources = resources;
            bootUiClick = resources.GetLoaded<AudioClip>(BootResourceManifest.UiClick);
            var hubBgm = resources.GetLoaded<AudioClip>(BootResourceManifest.HubBgm);
            if (hubBgm != null) PlayBgm(hubBgm);
        }
        public bool PlayUiClick() => PlaySfx(bootUiClick != null ? bootUiClick : uiClickClip);

        private void RequireInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("SoundManager must be initialized by GameBootstrap first.");
        }

        public void Shutdown()
        {
            if (data != null) data.Changed -= ApplySettings;
            if (bgmSource != null) { bgmSource.Stop(); bgmSource.clip = null; }
            if (sfxSource != null) sfxSource.Stop();
            data = null; bootUiClick = null; bootResources = null; CueRequested = null;
            BgmEnabled = SfxEnabled = false;
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
