using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PocketArcade.Core.Save;
using PocketArcade.Meta;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PocketArcade.Tests
{
    public sealed class ManagerTests
    {
        private readonly List<Object> objects = new List<Object>();
        private GameObject root;
        private GameCatalog catalog;
        private MemoryRepository repository;
        private DataManager data;

        private sealed class MemoryRepository : ISaveRepository
        {
            public SaveData Saved = new SaveData();
            public bool FailWrites;
            public int Loads;
            public SaveData Load() { Loads++; return Saved.Copy(); }
            public void Save(SaveData next)
            {
                if (FailWrites) throw new IOException("Disk full");
                Saved = next.Copy();
            }
        }

        private T Track<T>(T item) where T : Object { objects.Add(item); return item; }

        [SetUp]
        public void SetUp()
        {
            root = Track(new GameObject("Manager test root"));
            var momo = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            var bori = Track(ScriptableObject.CreateInstance<CharacterDefinition>());
            SetString(momo, "id", "char_cat");
            SetString(bori, "id", "char_dog");
            var game = Track(ScriptableObject.CreateInstance<MiniGameDefinition>());
            SetString(game, "id", "coming_soon_1");
            catalog = Track(ScriptableObject.CreateInstance<GameCatalog>());
            var serialized = new SerializedObject(catalog);
            var characters = serialized.FindProperty("characters");
            characters.arraySize = 2;
            characters.GetArrayElementAtIndex(0).objectReferenceValue = momo;
            characters.GetArrayElementAtIndex(1).objectReferenceValue = bori;
            var games = serialized.FindProperty("miniGames");
            games.arraySize = 1;
            games.GetArrayElementAtIndex(0).objectReferenceValue = game;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            repository = new MemoryRepository();
            data = root.AddComponent<DataManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // 매니저를 먼저 정리하여 이벤트와 static 참조가 다음 테스트에 남지 않게 한다.
            foreach (var item in objects)
                if (item is GameObject go && go != null)
                {
                    go.GetComponent<UIManager>()?.Shutdown();
                    go.GetComponent<SoundManager>()?.Shutdown();
                    go.GetComponent<SceneFlowManager>()?.Shutdown();
                    go.GetComponent<DataManager>()?.Shutdown();
                    Object.DestroyImmediate(go);
                }
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
            objects.Clear();
        }

        [Test]
        public void DataManagerPublishesOnlyAfterSuccessfulLoad()
        {
            Assert.That(DataManager.Instance, Is.Null);
            Assert.That(data.IsInitialized, Is.False);
            Assert.Throws<ArgumentNullException>(() => data.Initialize(null, repository));
            Assert.That(DataManager.Instance, Is.Null);
            data.Initialize(catalog, repository);
            Assert.That(DataManager.Instance, Is.SameAs(data));
            Assert.That(data.Catalog, Is.SameAs(catalog));
            Assert.That(data.SelectedCharacterId, Is.EqualTo("char_cat"));
        }

        [Test]
        public void ReinitializingDataDoesNotReloadOrLoseSelection()
        {
            data.Initialize(catalog, repository);
            data.SelectCharacter("char_dog");
            data.Initialize(catalog, repository);
            Assert.That(repository.Loads, Is.EqualTo(1));
            Assert.That(data.SelectedCharacterId, Is.EqualTo("char_dog"));
            Assert.That(repository.Saved.selectedCharacterId, Is.EqualTo("char_dog"));
        }

        [Test]
        public void DuplicateDataManagerCannotReplaceSingleton()
        {
            data.Initialize(catalog, repository);
            var duplicate = Track(new GameObject("Duplicate data")).AddComponent<DataManager>();
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(catalog, repository));
            Object.DestroyImmediate(duplicate.gameObject);
            Assert.That(DataManager.Instance, Is.SameAs(data));
        }

        [Test]
        public void SoundRequiresLoadedData()
        {
            var sound = root.AddComponent<SoundManager>();
            Assert.Throws<InvalidOperationException>(() => sound.Initialize(data));
            Assert.That(SoundManager.Instance, Is.Null);
            Assert.That(root.GetComponents<AudioSource>(), Is.Empty);
        }

        [Test]
        public void SoundLoadsSavedSettingsAndTracksSuccessfulChanges()
        {
            repository.Saved.settings.bgmEnabled = false;
            data.Initialize(catalog, repository);
            var sound = root.AddComponent<SoundManager>();
            sound.Initialize(data);
            var sources = root.GetComponents<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(2));
            Assert.That(sources[0].loop, Is.True);
            Assert.That(sources[0].mute, Is.True);
            Assert.That(sources[1].mute, Is.False);
            data.UpdateSettings(new PlayerSettingsData { bgmEnabled = true, sfxEnabled = false });
            Assert.That(sound.BgmEnabled, Is.True);
            Assert.That(sound.SfxEnabled, Is.False);
            Assert.That(sources[0].mute, Is.False);
            Assert.That(sources[1].mute, Is.True);
        }

        [Test]
        public void FailedSettingsSaveDoesNotChangeAudioOrStoredSettings()
        {
            data.Initialize(catalog, repository);
            var sound = root.AddComponent<SoundManager>();
            sound.Initialize(data);
            repository.FailWrites = true;
            Assert.Throws<IOException>(() => data.UpdateSettings(
                new PlayerSettingsData { bgmEnabled = false, sfxEnabled = false }));
            Assert.That(sound.BgmEnabled && sound.SfxEnabled, Is.True);
            Assert.That(repository.Saved.settings.bgmEnabled, Is.True);
            Assert.That(root.GetComponents<AudioSource>()[0].mute, Is.False);
        }

        [Test]
        public void SoundReinitializeDoesNotDuplicateSourcesAndMissingClipsAreNoOp()
        {
            data.Initialize(catalog, repository);
            var sound = root.AddComponent<SoundManager>();
            sound.Initialize(data);
            sound.Initialize(data);
            Assert.That(root.GetComponents<AudioSource>().Length, Is.EqualTo(2));
            Assert.That(sound.PlayBgm(null), Is.False);
            Assert.That(sound.PlaySfx(null), Is.False);
            Assert.That(sound.PlayUiClick(), Is.False);
        }

        [Test]
        public void DuplicateSoundManagerDoesNotCreateAudioSources()
        {
            data.Initialize(catalog, repository);
            var sound = root.AddComponent<SoundManager>();
            sound.Initialize(data);
            var duplicate = Track(new GameObject("Duplicate sound")).AddComponent<SoundManager>();
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(data));
            Assert.That(duplicate.GetComponents<AudioSource>(), Is.Empty);
            Object.DestroyImmediate(duplicate.gameObject);
            Assert.That(SoundManager.Instance, Is.SameAs(sound));
        }

        [Test]
        public void SceneManagerRejectsRequestsBeforeInitializationAndLockedGamesAfterIt()
        {
            var scenes = root.AddComponent<SceneFlowManager>();
            Assert.That(scenes.OpenHub(out string error), Is.False);
            Assert.That(error, Does.Contain("initialized"));
            scenes.Initialize(catalog);
            Assert.That(scenes.OpenGame("coming_soon_1", out error), Is.False);
            Assert.That(scenes.IsLoading, Is.False);
            Assert.That(scenes.SelectedGame, Is.Null);
        }

        [Test]
        public void DuplicateSceneManagerCannotReplaceSingleton()
        {
            var scenes = root.AddComponent<SceneFlowManager>();
            scenes.Initialize(catalog);
            var duplicate = Track(new GameObject("Duplicate scenes")).AddComponent<SceneFlowManager>();
            Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(catalog));
            Object.DestroyImmediate(duplicate.gameObject);
            Assert.That(SceneFlowManager.Instance, Is.SameAs(scenes));
        }

        [Test]
        public void ShuttingDownManagersClearsAllSingletonsAndReadiness()
        {
            data.Initialize(catalog, repository);
            root.AddComponent<SoundManager>().Initialize(data);
            root.AddComponent<SceneFlowManager>().Initialize(catalog);
            root.AddComponent<UIManager>().Initialize();
            root.AddComponent<ResourceManager>().Initialize();
            Assert.That(GameBootstrap.ManagersReady, Is.True);
            root.GetComponent<ResourceManager>().Shutdown();
            root.GetComponent<UIManager>().Shutdown();
            root.GetComponent<SoundManager>().Shutdown();
            root.GetComponent<SceneFlowManager>().Shutdown();
            data.Shutdown();
            Object.DestroyImmediate(root);
            Assert.That(UIManager.Instance, Is.Null);
            Assert.That(DataManager.Instance, Is.Null);
            Assert.That(SoundManager.Instance, Is.Null);
            Assert.That(SceneFlowManager.Instance, Is.Null);
            Assert.That(GameBootstrap.ManagersReady, Is.False);
        }

        [Test]
        public void SoundShutdownUnsubscribesAndCanReinitializeWithoutExtraSources()
        {
            data.Initialize(catalog, repository);
            var sound = root.AddComponent<SoundManager>();
            sound.Initialize(data);
            sound.Shutdown();
            data.UpdateSettings(new PlayerSettingsData { bgmEnabled = false });
            Assert.That(sound.IsInitialized, Is.False);
            Assert.That(SoundManager.Instance, Is.Null);
            sound.Initialize(data);
            Assert.That(root.GetComponents<AudioSource>().Length, Is.EqualTo(2));
            Assert.That(sound.BgmEnabled, Is.False);
        }

        private static void SetString(Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [Test]
        public void VersionedScoreKeyPreservesPriorRulesRecordsAndRejectsUnknownKeys()
        {
            SetString(catalog.MiniGames[0], "scoreKey", "brake_test.v1");
            repository.Saved.highScores.Add(new HighScoreData { gameId = "coming_soon_1", score = 9999 });
            data.Initialize(catalog, repository);
            var request = new GameResultRequest("brake_result", "brake_test.v1", 300, 3);
            data.RecordGameResult(request); data.RecordGameResult(request);
            Assert.That(data.HighScore("coming_soon_1"), Is.EqualTo(9999));
            Assert.That(data.HighScore("brake_test.v1"), Is.EqualTo(300));
            Assert.That(data.Coins, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => data.RecordGameResult(new GameResultRequest("bad_result", "unregistered_rules", 100, 1)));
        }
    }
}
