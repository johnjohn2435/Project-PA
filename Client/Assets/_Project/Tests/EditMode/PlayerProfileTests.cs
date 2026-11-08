using System;
using System.IO;
using NUnit.Framework;
using PocketArcade.Core.Save;

namespace PocketArcade.Tests
{
    public sealed class PlayerProfileTests
    {
        private static readonly string[] Ids = { "char_cat", "char_dog", "char_hamster" };

        private sealed class MemoryRepository : ISaveRepository
        {
            public SaveData Data = new SaveData();
            public bool Fail;
            public int Writes;
            public SaveData Load() => Data.Copy();
            public void Save(SaveData data)
            {
                if (Fail) throw new IOException("Disk full");
                Data = data.Copy();
                Writes++;
            }
        }

        [Test]
        public void CharacterSelectionIsImmediatelySavedAndSurvivesNewProfile()
        {
            var repository = new MemoryRepository();
            var profile = new PlayerProfile(repository, Ids, Ids[0]);
            int changes = 0;
            profile.Changed += () => changes++;
            Assert.That(profile.SelectCharacter("char_dog"), Is.True);
            Assert.That(profile.SelectedCharacterId, Is.EqualTo("char_dog"));
            Assert.That(new PlayerProfile(repository, Ids, Ids[0]).SelectedCharacterId, Is.EqualTo("char_dog"));
            Assert.That(repository.Writes, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void UnknownCharacterCannotBeSelectedOrSaved()
        {
            var repository = new MemoryRepository();
            var profile = new PlayerProfile(repository, Ids, Ids[0]);
            Assert.That(profile.SelectCharacter("unknown"), Is.False);
            Assert.That(repository.Writes, Is.Zero);
            Assert.That(profile.SelectedCharacterId, Is.EqualTo(Ids[0]));
        }

        [Test]
        public void DiskFailureDoesNotChangeSelectionOrNotify()
        {
            var repository = new MemoryRepository { Fail = true };
            var profile = new PlayerProfile(repository, Ids, Ids[0]);
            int changes = 0;
            profile.Changed += () => changes++;
            Assert.Throws<IOException>(() => profile.SelectCharacter("char_hamster"));
            Assert.That(profile.SelectedCharacterId, Is.EqualTo(Ids[0]));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void SettingsAndCoinsSurviveCharacterChanges()
        {
            var repository = new MemoryRepository { Data = new SaveData { coins = 123 } };
            var profile = new PlayerProfile(repository, Ids, Ids[0]);
            profile.UpdateSettings(new PlayerSettingsData { bgmEnabled = false, hapticsEnabled = false });
            profile.SelectCharacter("char_hamster");
            var reloaded = new PlayerProfile(repository, Ids, Ids[0]);
            Assert.That(reloaded.Coins, Is.EqualTo(123));
            Assert.That(reloaded.Settings.bgmEnabled, Is.False);
            Assert.That(reloaded.Settings.hapticsEnabled, Is.False);
            Assert.That(reloaded.Settings.sfxEnabled, Is.True);
        }

        [Test]
        public void SnapshotAndSettingsCannotMutateProfile()
        {
            var profile = new PlayerProfile(new MemoryRepository(), Ids, Ids[0]);
            profile.Snapshot.coins = 999;
            profile.Settings.bgmEnabled = false;
            profile.Snapshot.unlockedCharacterIds.Clear();
            Assert.That(profile.Coins, Is.Zero);
            Assert.That(profile.Settings.bgmEnabled, Is.True);
            Assert.That(profile.IsUnlocked("char_dog"), Is.True);
        }

        [Test]
        public void RemovedCatalogCharacterFallsBackToDefault()
        {
            var data = new SaveData { selectedCharacterId = "removed" };
            data.unlockedCharacterIds.Add("removed");
            var profile = new PlayerProfile(new MemoryRepository { Data = data }, Ids, Ids[0]);
            Assert.That(profile.SelectedCharacterId, Is.EqualTo(Ids[0]));
        }
    }
}
