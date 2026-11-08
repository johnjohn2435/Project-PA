using System;
using System.IO;
using NUnit.Framework;
using PocketArcade.Core.Save;
using PocketArcade.Save;

namespace PocketArcade.Tests
{
    public sealed class GameResultTests
    {
        private sealed class Store : ISaveRepository
        {
            public SaveData Data = new SaveData();
            public bool Fail;
            public int Writes;
            public SaveData Load() => Data.Copy();
            public void Save(SaveData data) { if (Fail) throw new IOException(); Data = data.Copy(); Writes++; }
        }
        private static PlayerProfile Profile(ISaveRepository store) => new PlayerProfile(store,new[]{"char_cat"},"char_cat");
        private static GameResultRequest Result(string id = "run_1",long score = 1250,int coins = 5) => new GameResultRequest(id,"block_gravity",score,coins);
        private string directory;
        [TearDown] public void Cleanup()
        {
            if (directory == null || !Directory.Exists(directory)) return;
            string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(directory);
            if (!full.StartsWith(root,StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("PocketArcadeResultTests-",StringComparison.Ordinal))
                throw new ArgumentException(nameof(directory));
            Directory.Delete(full,true); directory = null;
        }
        private string FilePath()
        { directory = Path.Combine(Path.GetTempPath(),"PocketArcadeResultTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);return Path.Combine(directory,"save.json"); }

        [Test] public void ScoreBalanceAndReceiptCommitTogetherAndNotifyOnce()
        {
            var store = new Store(); var profile = Profile(store);int changes=0;profile.Changed+=()=>changes++;
            var receipt = profile.RecordGameResult(Result());
            Assert.That(profile.Coins,Is.EqualTo(5));Assert.That(profile.HighScore("block_gravity"),Is.EqualTo(1250));
            Assert.That(receipt.creditedCoins,Is.EqualTo(5));Assert.That(store.Data.completedResults.Count,Is.EqualTo(1));
            Assert.That(store.Writes,Is.EqualTo(1));Assert.That(changes,Is.EqualTo(1));
        }
        [Test] public void FailedSaveLeavesAllStateUntouchedAndRetryPaysOnce()
        {
            var store = new Store{Fail=true};var profile=Profile(store);int changes=0;profile.Changed+=()=>changes++;
            Assert.Throws<IOException>(()=>profile.RecordGameResult(Result()));
            Assert.That(profile.Coins,Is.Zero);Assert.That(profile.HighScore("block_gravity"),Is.Zero);
            Assert.That(profile.Snapshot.completedResults,Is.Empty);Assert.That(changes,Is.Zero);
            store.Fail=false;profile.RecordGameResult(Result());profile.RecordGameResult(Result());
            Assert.That(profile.Coins,Is.EqualTo(5));Assert.That(store.Writes,Is.EqualTo(1));Assert.That(changes,Is.EqualTo(1));
        }
        [Test] public void ReceiptPreventsReplayAfterReloadAndAfterOtherResults()
        {
            var store=new Store();var profile=Profile(store);profile.RecordGameResult(Result());
            profile.RecordGameResult(Result("run_2",2500,10));
            profile=Profile(store);profile.RecordGameResult(Result());
            Assert.That(profile.Coins,Is.EqualTo(15));Assert.That(profile.HighScore("block_gravity"),Is.EqualTo(2500));Assert.That(store.Writes,Is.EqualTo(2));
        }
        [Test] public void ReusedIdWithDifferentPayloadIsRejected()
        {
            var store=new Store();var profile=Profile(store);profile.RecordGameResult(Result());
            Assert.Throws<ArgumentException>(()=>profile.RecordGameResult(Result(score:1500)));
            Assert.Throws<ArgumentException>(()=>profile.RecordGameResult(Result(coins:6)));
            Assert.Throws<ArgumentException>(()=>profile.RecordGameResult(new GameResultRequest("run_1","other_game",1250,5)));
            Assert.That(profile.Coins,Is.EqualTo(5));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void LowerScoreDoesNotReplaceBestAndZeroRewardIsStillReceipted()
        {
            var profile=Profile(new Store());profile.RecordGameResult(Result());
            var receipt=profile.RecordGameResult(Result("run_2",10,0));
            Assert.That(receipt.highScore,Is.EqualTo(1250));Assert.That(profile.Coins,Is.EqualTo(5));Assert.That(profile.Snapshot.completedResults.Count,Is.EqualTo(2));
        }
        [Test] public void ReturnedReceiptsAndSnapshotsCannotMutateCommittedState()
        {
            var profile=Profile(new Store());var receipt=profile.RecordGameResult(Result());receipt.creditedCoins=99;receipt.resultId="tampered";
            var snapshot=profile.Snapshot;snapshot.completedResults[0].score=1;snapshot.highScores[0].score=1;
            var again=profile.RecordGameResult(Result());Assert.That(again.resultId,Is.EqualTo("run_1"));Assert.That(again.creditedCoins,Is.EqualTo(5));
            Assert.That(profile.HighScore("block_gravity"),Is.EqualTo(1250));
        }
        [Test] public void CoinBalanceSaturatesAndReportsActuallyCreditedAmount()
        {
            var store=new Store();store.Data.coins=int.MaxValue-2;var profile=Profile(store);
            var receipt=profile.RecordGameResult(Result());
            Assert.That(profile.Coins,Is.EqualTo(int.MaxValue));Assert.That(receipt.creditedCoins,Is.EqualTo(2));Assert.That(receipt.earnedCoins,Is.EqualTo(5));
            profile.RecordGameResult(Result());Assert.That(profile.Coins,Is.EqualTo(int.MaxValue));
        }
        [Test] public void ExistingSettingsAndCharacterSurviveResultSave()
        {
            var store=new Store();store.Data.settings.sfxEnabled=false;store.Data.coins=17;var profile=Profile(store);
            profile.RecordGameResult(Result());var reloaded=Profile(store);
            Assert.That(reloaded.Settings.sfxEnabled,Is.False);Assert.That(reloaded.SelectedCharacterId,Is.EqualTo("char_cat"));Assert.That(reloaded.Coins,Is.EqualTo(22));
        }
        [Test] public void LongScoreAndReceiptRoundTripThroughRealJsonRepository()
        {
            var path=FilePath();var profile=Profile(new JsonSaveRepository(path));long score=(long)int.MaxValue+12345;
            profile.RecordGameResult(Result(score:score,coins:100));
            var reloaded=Profile(new JsonSaveRepository(path));
            Assert.That(reloaded.HighScore("block_gravity"),Is.EqualTo(score));Assert.That(reloaded.Snapshot.completedResults[0].score,Is.EqualTo(score));
            reloaded.RecordGameResult(Result(score:score,coins:100));Assert.That(reloaded.Coins,Is.EqualTo(100));
            Assert.That(reloaded.Snapshot.schemaVersion,Is.EqualTo(SaveData.CurrentVersion));
        }
        [Test] public void VersionOneSaveMigratesWithoutLosingExistingData()
        {
            string path=FilePath();
            File.WriteAllText(path,"{\"schemaVersion\":1,\"coins\":42,\"selectedCharacterId\":\"char_cat\",\"highScores\":[{\"gameId\":\"block_gravity\",\"score\":987}],\"settings\":{\"bgmEnabled\":false}}");
            var repository=new JsonSaveRepository(path);var data=repository.Load();
            Assert.That(data.schemaVersion,Is.EqualTo(SaveData.CurrentVersion));Assert.That(data.coins,Is.EqualTo(42));Assert.That(data.highScores[0].score,Is.EqualTo(987));
            Assert.That(data.settings.bgmEnabled,Is.False);Assert.That(data.completedResults,Is.Empty);
            repository.Save(data);Assert.That(repository.Load().schemaVersion,Is.EqualTo(SaveData.CurrentVersion));
        }
        [Test] public void DuplicateHighScoresNormalizeToMaximumAndReceiptIdsMustBeUnique()
        {
            var data=new SaveData();data.highScores.Add(new HighScoreData{gameId="g",score=25});data.highScores.Add(new HighScoreData{gameId="g",score=12});data.Normalize();
            Assert.That(data.highScores.Count,Is.EqualTo(1));Assert.That(data.highScores[0].score,Is.EqualTo(25));
            var receipt=new GameResultReceipt{resultId="r",gameId="g",score=1,highScore=1};data.completedResults.Add(receipt);data.completedResults.Add(receipt.Copy());
            Assert.Throws<ArgumentException>(()=>data.Normalize());
        }
        [Test] public void CommitEventReentryCannotAwardTheSameResultAgain()
        {
            var store=new Store();var profile=Profile(store);profile.Changed+=()=>profile.RecordGameResult(Result());
            profile.RecordGameResult(Result());Assert.That(store.Writes,Is.EqualTo(1));Assert.That(profile.Coins,Is.EqualTo(5));
        }
        [Test] public void ReceiptSurvivesAnObserverExceptionAfterSuccessfulDiskWrite()
        {
            var store=new Store();var profile=Profile(store);profile.Changed+=()=>throw new InvalidOperationException();
            Assert.Throws<InvalidOperationException>(()=>profile.RecordGameResult(Result()));
            Assert.That(profile.RecordGameResult(Result()).creditedCoins,Is.EqualTo(5));Assert.That(store.Writes,Is.EqualTo(1));
        }
        [Test] public void RequestRejectsInvalidIdsScoresAndRewards()
        {
            Assert.Throws<ArgumentException>(()=>Result(" "));Assert.Throws<ArgumentOutOfRangeException>(()=>Result(score:-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Result(coins:-1));Assert.Throws<ArgumentOutOfRangeException>(()=>Result(coins:101));
        }
    }
}
