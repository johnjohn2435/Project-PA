using System;
using PocketArcade.Core.Save;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityResultController
    {
        private readonly Func<GameResultRequest, GameResultReceipt> persist;
        private GameResultReceipt receipt;
        public GameResultRequest Result { get; }
        public GameResultReceipt Receipt => receipt?.Copy();
        public Exception SaveError { get; private set; }
        public bool IsSaving { get; private set; }
        public bool IsSaved => receipt != null;
        public BlockGravityResultController(GameResultRequest result, Func<GameResultRequest, GameResultReceipt> persist)
        { Result = result ?? throw new ArgumentNullException(nameof(result)); this.persist = persist ?? throw new ArgumentNullException(nameof(persist)); }
        public bool TrySave()
        {
            if (IsSaved) return true;
            if (IsSaving) return false;
            IsSaving = true; SaveError = null;
            try
            {
                var saved = persist(Result);
                if (saved == null || saved.resultId != Result.ResultId || saved.gameId != Result.GameId ||
                    saved.score != Result.Score || saved.earnedCoins != Result.EarnedCoins)
                    throw new InvalidOperationException(nameof(saved));
                receipt = saved.Copy(); return true;
            }
            catch (Exception error) { SaveError = error; return false; }
            finally { IsSaving = false; }
        }
    }
}
