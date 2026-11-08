using System;

namespace PocketArcade.Core.Save
{
    public sealed class GameResultRequest
    {
        public string ResultId { get; }
        public string GameId { get; }
        public long Score { get; }
        public int EarnedCoins { get; }
        public GameResultRequest(string resultId, string gameId, long score, int earnedCoins)
        {
            if (string.IsNullOrWhiteSpace(resultId) || resultId.Length > 128) throw new ArgumentException(nameof(resultId));
            if (string.IsNullOrWhiteSpace(gameId) || gameId.Length > 128) throw new ArgumentException(nameof(gameId));
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            if (earnedCoins < 0 || earnedCoins > 100) throw new ArgumentOutOfRangeException(nameof(earnedCoins));
            ResultId = resultId; GameId = gameId; Score = score; EarnedCoins = earnedCoins;
        }
    }

    [Serializable]
    public sealed class GameResultReceipt
    {
        public string resultId;
        public string gameId;
        public long score;
        public int earnedCoins;
        public int creditedCoins;
        public long highScore;
        public GameResultReceipt Copy() => new GameResultReceipt
        { resultId = resultId, gameId = gameId, score = score, earnedCoins = earnedCoins, creditedCoins = creditedCoins, highScore = highScore };
    }
}
