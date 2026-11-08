using System;
using System.Collections.Generic;

namespace PocketArcade.Core.Save
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;
        public int schemaVersion = CurrentVersion;
        public string selectedCharacterId = "char_cat";
        public List<string> unlockedCharacterIds = new List<string> { "char_cat", "char_dog", "char_hamster", "char_poodle" };
        public int coins;
        // JsonUtility에서 Dictionary 대신 직렬화 가능한 목록을 사용한다.
        public List<HighScoreData> highScores = new List<HighScoreData>();
        public List<GameResultReceipt> completedResults = new List<GameResultReceipt>();
        public List<string> completedTutorials = new List<string>();
        public PlayerSettingsData settings = new PlayerSettingsData();

        public void Normalize()
        {
            if (schemaVersion > CurrentVersion)
                throw new NotSupportedException("This save belongs to a newer version of the game.");
            if (schemaVersion < 0)
                throw new ArgumentException("Invalid save version.");
            schemaVersion = CurrentVersion; // 버전 없는 초기 데이터(v0)는 v1 기본값으로 보충.
            coins = Math.Max(0, coins);
            if (settings == null) settings = new PlayerSettingsData();
            if (unlockedCharacterIds == null) unlockedCharacterIds = new List<string>();
            var migratedIds = new List<string>();
            foreach(var old in unlockedCharacterIds)
            {
                var canonical=CharacterIds.FromLegacy(old);
                if(!string.IsNullOrWhiteSpace(canonical) && !migratedIds.Contains(canonical))migratedIds.Add(canonical);
            }
            unlockedCharacterIds=migratedIds;
            selectedCharacterId=CharacterIds.FromLegacy(selectedCharacterId);
            var tutorials=new HashSet<string>(System.StringComparer.Ordinal);
            if(completedTutorials!=null)foreach(var id in completedTutorials)if(!string.IsNullOrWhiteSpace(id))tutorials.Add(id);
            completedTutorials=new List<string>(tutorials);
            foreach (string id in new[] { "char_cat", "char_dog", "char_hamster", "char_poodle" })
                if (!unlockedCharacterIds.Contains(id)) unlockedCharacterIds.Add(id);
            if (string.IsNullOrEmpty(selectedCharacterId) || !unlockedCharacterIds.Contains(selectedCharacterId))
                selectedCharacterId = "char_cat";
            if (highScores == null) highScores = new List<HighScoreData>();
            highScores.RemoveAll(score => score == null || string.IsNullOrEmpty(score.gameId));
            var scores = new Dictionary<string, HighScoreData>(StringComparer.Ordinal);
            var normalizedScores = new List<HighScoreData>();
            foreach (var score in highScores)
            {
                score.score = Math.Max(0L, score.score);
                if (scores.TryGetValue(score.gameId, out var existing)) existing.score = Math.Max(existing.score, score.score);
                else { scores.Add(score.gameId, score); normalizedScores.Add(score); }
            }
            highScores = normalizedScores;
            if (completedResults == null) completedResults = new List<GameResultReceipt>();
            var resultIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var receipt in completedResults)
            {
                if (receipt == null || string.IsNullOrWhiteSpace(receipt.resultId) || string.IsNullOrWhiteSpace(receipt.gameId) ||
                    !resultIds.Add(receipt.resultId) || receipt.score < 0 || receipt.highScore < receipt.score ||
                    receipt.earnedCoins < 0 || receipt.earnedCoins > 100 || receipt.creditedCoins < 0 || receipt.creditedCoins > receipt.earnedCoins)
                    throw new ArgumentException(nameof(completedResults));
            }
        }

        public SaveData Copy()
        {
            var result = new SaveData
            {
                schemaVersion = schemaVersion,
                selectedCharacterId = selectedCharacterId,
                unlockedCharacterIds = new List<string>(unlockedCharacterIds),
                coins = coins,
                settings = settings.Copy(),
                highScores = new List<HighScoreData>(),
                completedResults = new List<GameResultReceipt>(),
                completedTutorials = completedTutorials == null ? new List<string>() : new List<string>(completedTutorials)
            };
            foreach (var score in highScores)
                result.highScores.Add(new HighScoreData { gameId = score.gameId, score = score.score });
            if (completedResults != null)
                foreach (var receipt in completedResults) result.completedResults.Add(receipt?.Copy());
            return result;
        }
    }

    [Serializable]
    public sealed class HighScoreData
    {
        public string gameId;
        public long score;
    }

    [Serializable]
    public sealed class PlayerSettingsData
    {
        public bool bgmEnabled = true;
        public bool sfxEnabled = true;
        public bool hapticsEnabled = true;

        public PlayerSettingsData Copy() => new PlayerSettingsData
        {
            bgmEnabled = bgmEnabled, sfxEnabled = sfxEnabled, hapticsEnabled = hapticsEnabled
        };
    }
}
