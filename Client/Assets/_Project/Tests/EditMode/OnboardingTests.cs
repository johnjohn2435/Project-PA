using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PocketArcade.Core.Save;
using PocketArcade.UI;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace PocketArcade.Tests
{
    public sealed class OnboardingTests
    {
        private sealed class Store:ISaveRepository
        {
            public SaveData Data=new SaveData();public bool Fail;public int Writes;
            public SaveData Load()=>Data.Copy();
            public void Save(SaveData data){if(Fail)throw new IOException();Data=data.Copy();Writes++;}
        }
        [TestCase("char_momo","char_cat")]
        [TestCase("char_bori","char_dog")]
        [TestCase("char_nabi","char_hamster")]
        public void PreviousCharacterIdsMigrateWithoutLosingProgress(string old,string current)
        {
            var data=new SaveData{schemaVersion=2,selectedCharacterId=old,coins=123,unlockedCharacterIds=new List<string>{old}};
            data.highScores.Add(new HighScoreData{gameId="block_gravity",score=999});data.Normalize();
            Assert.That(data.selectedCharacterId,Is.EqualTo(current));Assert.That(data.unlockedCharacterIds,Does.Contain(current));Assert.That(data.unlockedCharacterIds,Does.Not.Contain(old));
            Assert.That(data.coins,Is.EqualTo(123));Assert.That(data.highScores[0].score,Is.EqualTo(999));Assert.That(data.completedTutorials,Is.Empty);
        }
        [Test]public void MixedAliasAndCanonicalIdsAreDeduplicated()
        {
            var data=new SaveData{unlockedCharacterIds=new List<string>{"char_momo","char_cat","char_bori","char_dog"}};data.Normalize();
            CollectionAssert.AreEquivalent(new[]{"char_cat","char_dog","char_hamster","char_poodle"},data.unlockedCharacterIds);
        }
        [Test]public void TutorialCompletionPersistsOnceAndSurvivesNewProfile()
        {
            var store=new Store();var profile=new PlayerProfile(store,new[]{"char_cat","char_dog","char_hamster","char_poodle"},"char_cat");
            Assert.That(profile.CompleteTutorial("block_gravity.v1"),Is.True);Assert.That(profile.CompleteTutorial("block_gravity.v1"),Is.False);Assert.That(store.Writes,Is.EqualTo(1));
            var reopened=new PlayerProfile(store,new[]{"char_cat","char_dog","char_hamster","char_poodle"},"char_cat");Assert.That(reopened.HasCompletedTutorial("block_gravity.v1"),Is.True);
            var copy=reopened.Snapshot;copy.completedTutorials.Clear();Assert.That(reopened.HasCompletedTutorial("block_gravity.v1"),Is.True);
        }
        [Test]public void FailedTutorialSaveDoesNotMarkCompletion()
        {
            var store=new Store{Fail=true};var profile=new PlayerProfile(store,new[]{"char_cat"},"char_cat");
            Assert.Throws<IOException>(()=>profile.CompleteTutorial("block_gravity.v1"));Assert.That(profile.HasCompletedTutorial("block_gravity.v1"),Is.False);
            store.Fail=false;profile.CompleteTutorial("block_gravity.v1");Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test]public void TutorialMigrationNormalizesAndDoesNotShareLists()
        {
            var data=new SaveData{completedTutorials=new List<string>{"",null,"block_gravity.v1","block_gravity.v1"}};data.Normalize();
            Assert.That(data.completedTutorials.Count,Is.EqualTo(1));data.Copy().completedTutorials.Clear();Assert.That(data.completedTutorials.Count,Is.EqualTo(1));
        }
        [Test]public void CharacterPrefabsShareOneAnimatorAndBoneLayout()
        {
            RuntimeAnimatorController shared=null;
            foreach(var id in new[]{"char_cat","char_dog","char_hamster","char_poodle"})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Characters/"+id+"/Character.prefab");Assert.That(root,Is.Not.Null,id);
                var tail = root.transform.Find("body/tail");
                if (id == "char_hamster")
                {
                    Assert.That(tail, Is.Not.Null);
                    var renderer = tail.GetComponent<SpriteRenderer>();
                    Assert.That(renderer.sprite, Is.Not.Null);
                    Assert.That(renderer.sortingOrder, Is.LessThan(root.transform.Find("TorsoSurface").GetComponent<Renderer>().sortingOrder));
                }
                else Assert.That(tail, Is.Null);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(tail == null ? 6 : 7));
                foreach(var bone in new[]{"body/arm_L","body/arm_R","body/leg_L","body/leg_R"})Assert.That(root.transform.Find(bone),Is.Not.Null,id+bone);
                var animator=root.GetComponent<Animator>();Assert.That(animator.runtimeAnimatorController,Is.Not.Null);
                if(shared==null)shared=animator.runtimeAnimatorController;else Assert.That(animator.runtimeAnimatorController,Is.SameAs(shared));
            }
        }
    }
}
