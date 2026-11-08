using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core.Save;
using PocketArcade.Meta;
using PocketArcade.Save;
using UnityEngine;

namespace PocketArcade
{
    [DisallowMultipleComponent]
    public sealed class DataManager : PersistentManager
    {
        public static DataManager Instance { get; private set; }
        public bool IsInitialized => profile != null;
        public GameCatalog Catalog { get; private set; }
        public StringTable Strings => L10n.Table;
        public void SetLanguage(GameLanguage language) => L10n.SetLanguage(language);
        public string SelectedCharacterId => RequireProfile().SelectedCharacterId;
        public int Coins => RequireProfile().Coins;
        public PlayerSettingsData Settings => RequireProfile().Settings;
        public SaveData Snapshot => RequireProfile().Snapshot;
        public event Action Changed;
        private PlayerProfile profile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Initialize(GameCatalog catalog, ISaveRepository repository = null)
        {
            if (Instance != null && Instance != this)
                throw new InvalidOperationException("DataManager is already initialized by another object.");
            if (IsInitialized)
            {
                if (Catalog != catalog) throw new InvalidOperationException("Cannot replace an active data catalog.");
                Persist();
            Instance = this;
                return;
            }
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            catalog.Validate();
            var ids = new List<string>();
            foreach (var character in catalog.Characters) ids.Add(character.Id);
            var loadedProfile = new PlayerProfile(repository ?? new JsonSaveRepository(
                Path.Combine(Application.persistentDataPath, "player-save.json"), Debug.LogWarning), ids, ids[0]);
            Catalog = catalog;
            profile = loadedProfile;
            profile.Changed += NotifyChanged;
            Persist();
            Instance = this;
        }

        public bool IsUnlocked(string id) => RequireProfile().IsUnlocked(id);
        public bool SelectCharacter(string id) => RequireProfile().SelectCharacter(id);
        public void UpdateSettings(PlayerSettingsData settings) => RequireProfile().UpdateSettings(settings);
        public bool HasCompletedTutorial(string id) => RequireProfile().HasCompletedTutorial(id);
        public bool CompleteTutorial(string id) => RequireProfile().CompleteTutorial(id);
        public long HighScore(string gameId) => RequireProfile().HighScore(gameId);
        public GameResultReceipt RecordGameResult(GameResultRequest result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            bool known = false;
            if (Catalog != null)
                foreach (var game in Catalog.MiniGames) if (game.ScoreKey == result.GameId) { known = true; break; }
            if (!known) throw new ArgumentException(nameof(result));
            return RequireProfile().RecordGameResult(result);
        }

        private PlayerProfile RequireProfile() => profile ??
            throw new InvalidOperationException("DataManager must be initialized by GameBootstrap first.");

        private void NotifyChanged() => Changed?.Invoke();

        public void Shutdown()
        {
            if (profile != null) profile.Changed -= NotifyChanged;
            Changed = null;
            profile = null;
            Catalog = null;
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
