using System;
using System.Collections.Generic;

namespace PocketArcade.Core.Save
{
    public sealed class PlayerProfile
    {
        private readonly ISaveRepository repository;
        private readonly HashSet<string> characterIds;
        private SaveData data;

        public event Action Changed;
        public string SelectedCharacterId => data.selectedCharacterId;
        public int Coins => data.coins;
        public PlayerSettingsData Settings => data.settings.Copy();
        public SaveData Snapshot => data.Copy();

        public PlayerProfile(ISaveRepository repository, IEnumerable<string> characterIds, string defaultCharacterId)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.characterIds = new HashSet<string>(characterIds);
            if (!this.characterIds.Contains(defaultCharacterId))
                throw new ArgumentException("Default character must exist in the catalog.");
            data = repository.Load();
            data.Normalize();
            if (!this.characterIds.Contains(data.selectedCharacterId)) data.selectedCharacterId = defaultCharacterId;
        }

        public bool IsUnlocked(string id) => characterIds.Contains(id) && data.unlockedCharacterIds.Contains(id);

        public bool SelectCharacter(string id)
        {
            if (!IsUnlocked(id)) return false;
            var next = data.Copy();
            next.selectedCharacterId = id;
            Commit(next);
            return true;
        }

        public void UpdateSettings(PlayerSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var next = data.Copy();
            next.settings = settings.Copy();
            Commit(next);
        }

        public bool HasCompletedTutorial(string id) => data.completedTutorials.Contains(id);
        public bool CompleteTutorial(string id)
        {
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException(nameof(id));
            if(HasCompletedTutorial(id))return false;
            var next=data.Copy();next.completedTutorials.Add(id);Commit(next);return true;
        }

        public long HighScore(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException(nameof(gameId));
            long best = 0;
            foreach (var entry in data.highScores) if (entry.gameId == gameId) best = Math.Max(best, entry.score);
            return best;
        }

        public GameResultReceipt RecordGameResult(GameResultRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            foreach (var receipt in data.completedResults)
                if (receipt.resultId == request.ResultId)
                {
                    if (receipt.gameId != request.GameId || receipt.score != request.Score || receipt.earnedCoins != request.EarnedCoins)
                        throw new ArgumentException(nameof(request));
                    return receipt.Copy();
                }
            var next = data.Copy();
            var best = next.highScores.Find(entry => entry.gameId == request.GameId);
            if (best == null) { best = new HighScoreData { gameId = request.GameId }; next.highScores.Add(best); }
            best.score = Math.Max(best.score, request.Score);
            int credited = Math.Min(request.EarnedCoins, int.MaxValue - next.coins);
            next.coins += credited;
            var result = new GameResultReceipt
            {
                resultId = request.ResultId, gameId = request.GameId, score = request.Score,
                earnedCoins = request.EarnedCoins, creditedCoins = credited, highScore = best.score
            };
            next.completedResults.Add(result);
            // Score, balance and receipt are committed by one atomic repository write.
            Commit(next);
            return result.Copy();
        }

        private void Commit(SaveData next)
        {
            // 디스크 저장 실패 시 현재 선택/UI 상태도 유지한다.
            repository.Save(next);
            data = next;
            Changed?.Invoke();
        }
    }
}
