using System;
using NUnit.Framework;
using PocketArcade.Meta;
using UnityEditor;
using UnityEngine;

namespace PocketArcade.Tests
{
    public sealed class CatalogAndSceneFlowTests
    {
        private GameCatalog catalog;
        private CharacterDefinition character;
        private MiniGameDefinition game;

        [SetUp]
        public void SetUp()
        {
            L10n.Initialize(System.IO.File.ReadAllText(PocketArcade.Editor.StringTableSetup.TableFile));
            character = ScriptableObject.CreateInstance<CharacterDefinition>();
            SetString(character, "id", "char_cat");
            game = ScriptableObject.CreateInstance<MiniGameDefinition>();
            SetString(game, "id", "coming_soon_1");
            catalog = ScriptableObject.CreateInstance<GameCatalog>();
            var data = new SerializedObject(catalog);
            var chars = data.FindProperty("characters");
            chars.arraySize = 1;
            chars.GetArrayElementAtIndex(0).objectReferenceValue = character;
            var games = data.FindProperty("miniGames");
            games.arraySize = 1;
            games.GetArrayElementAtIndex(0).objectReferenceValue = game;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            L10n.Reset();
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(character);
            UnityEngine.Object.DestroyImmediate(game);
        }

        [TestCase("coming_soon_1")]
        [TestCase("unknown")]
        public void LockedOrUnknownGameNeverStartsSceneLoad(string id)
        {
            var flow = new SceneFlow(null, catalog);
            Assert.That(flow.OpenGame(id, out string error), Is.False);
            Assert.That(error, Is.EqualTo(L10n.Text("game.coming")));
            Assert.That(flow.IsLoading, Is.False);
            Assert.That(flow.SelectedGame, Is.Null);
        }

        [Test]
        public void AvailableGameWithMissingBuildSceneFailsWithoutChangingSelection()
        {
            SetString(game, "sceneName", "MissingSceneForTest");
            var data = new SerializedObject(game);
            data.FindProperty("isAvailable").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            var flow = new SceneFlow(null, catalog);
            Assert.That(flow.OpenGame(game.Id, out string error), Is.False);
            Assert.That(error, Is.EqualTo(L10n.Text("scene.missing")));
            Assert.That(flow.SelectedGame, Is.Null);
        }

        [Test]
        public void DuplicateCharacterIdsAreRejected()
        {
            var data = new SerializedObject(catalog);
            var chars = data.FindProperty("characters");
            chars.arraySize = 2;
            chars.GetArrayElementAtIndex(1).objectReferenceValue = character;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => catalog.Validate());
        }

        [Test]
        public void AvailableGameRequiresSceneName()
        {
            var data = new SerializedObject(game);
            data.FindProperty("isAvailable").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => catalog.Validate());
        }

        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
