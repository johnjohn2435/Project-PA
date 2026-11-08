using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PocketArcade.Core.Save;
using PocketArcade.Save;

namespace PocketArcade.Tests
{
    public sealed class SaveRepositoryTests
    {
        private string directory;
        private string path;
        private JsonSaveRepository repository;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "PocketArcadeTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "save.json");
            repository = new JsonSaveRepository(path);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public void NewSaveHasAllFourCharactersAndEnabledSettings()
        {
            var data = repository.Load();
            Assert.That(data.schemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.selectedCharacterId, Is.EqualTo("char_cat"));
            Assert.That(data.unlockedCharacterIds, Is.EquivalentTo(new[] { "char_cat", "char_dog", "char_hamster", "char_poodle" }));
            Assert.That(data.coins, Is.Zero);
            Assert.That(data.settings.bgmEnabled && data.settings.sfxEnabled && data.settings.hapticsEnabled, Is.True);
        }

        [Test]
        public void RoundTripPreservesCharacterCoinsScoresAndSettings()
        {
            var data = new SaveData { selectedCharacterId = "char_hamster", coins = 135 };
            data.settings.sfxEnabled = false;
            data.highScores.Add(new HighScoreData { gameId = "block_gravity", score = 1250 });
            repository.Save(data);
            var loaded = new JsonSaveRepository(path).Load();
            Assert.That(loaded.selectedCharacterId, Is.EqualTo("char_hamster"));
            Assert.That(loaded.coins, Is.EqualTo(135));
            Assert.That(loaded.settings.sfxEnabled, Is.False);
            Assert.That(loaded.highScores[0].score, Is.EqualTo(1250));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [TestCase("")]
        [TestCase("broken json")]
        [TestCase("{broken}")]
        [TestCase("null")]
        [TestCase("[]")]
        public void CorruptSaveRecoversDefaults(string json)
        {
            File.WriteAllText(path, json);
            Assert.That(repository.Load().selectedCharacterId, Is.EqualTo("char_cat"));
        }

        [Test]
        public void MissingFieldsKeepDefaultsIncludingNestedBooleans()
        {
            File.WriteAllText(path, "{\"coins\":12,\"settings\":{\"sfxEnabled\":false}}");
            var loaded = repository.Load();
            Assert.That(loaded.coins, Is.EqualTo(12));
            Assert.That(loaded.selectedCharacterId, Is.EqualTo("char_cat"));
            Assert.That(loaded.settings.bgmEnabled, Is.True);
            Assert.That(loaded.settings.sfxEnabled, Is.False);
            Assert.That(loaded.settings.hapticsEnabled, Is.True);
        }

        [Test]
        public void NullCollectionsAndNegativeValuesAreRepaired()
        {
            File.WriteAllText(path, "{\"schemaVersion\":0,\"coins\":-20,\"selectedCharacterId\":null,\"settings\":null,\"unlockedCharacterIds\":null,\"highScores\":null}");
            var loaded = repository.Load();
            Assert.That(loaded.schemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.coins, Is.Zero);
            Assert.That(loaded.settings, Is.Not.Null);
            Assert.That(loaded.unlockedCharacterIds.Count, Is.EqualTo(4));
            Assert.That(loaded.highScores, Is.Empty);
        }

        [Test]
        public void ReplacingSaveKeepsPreviousVersionAsBackup()
        {
            repository.Save(new SaveData { coins = 10 });
            repository.Save(new SaveData { coins = 20 });
            repository.Save(new SaveData { coins = 30 });
            Assert.That(repository.Load().coins, Is.EqualTo(30));
            Assert.That(new JsonSaveRepository(path + ".bak").Load().coins, Is.EqualTo(20));
        }

        [Test]
        public void CorruptPrimaryRecoversBackup()
        {
            repository.Save(new SaveData { coins = 10 });
            repository.Save(new SaveData { coins = 20 });
            File.WriteAllText(path, "broken");
            Assert.That(repository.Load().coins, Is.EqualTo(10));
            repository.Save(new SaveData { coins = 15 });
            Assert.That(repository.Load().coins, Is.EqualTo(15));
        }

        [Test]
        public void FutureVersionIsNotSilentlyResetOrOverwritten()
        {
            const string json = "{\"schemaVersion\":99,\"coins\":77}";
            File.WriteAllText(path, json);
            Assert.Throws<NotSupportedException>(() => repository.Load());
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
        }

        [Test]
        public void InterruptedTemporaryWriteDoesNotReplaceGoodSave()
        {
            repository.Save(new SaveData { coins = 55 });
            File.WriteAllText(path + ".tmp", "{");
            Assert.That(repository.Load().coins, Is.EqualTo(55));
        }
    }
}
