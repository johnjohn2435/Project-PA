using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PocketArcade.BlockGravity
{
    public sealed class BlockGravityConfig
    {
        public int Width { get; }
        public int Height { get; }
        public int PlacementsPerGravityChange { get; }
        public GravityDirection StartDirection { get; }
        public BlockGravityConfig(int width = 8, int height = 8, int placementsPerGravityChange = 5,
            GravityDirection startDirection = GravityDirection.Down)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (placementsPerGravityChange <= 0) throw new ArgumentOutOfRangeException(nameof(placementsPerGravityChange));
            GravityResolver.Next(startDirection);
            Width = width; Height = height; PlacementsPerGravityChange = placementsPerGravityChange;
            StartDirection = startDirection;
        }
    }

    public sealed class TurnFrame
    {
        private readonly BoardState board;
        public TurnStage Stage { get; }
        public BoardState Board => board.Copy();
        public GravityDirection Gravity { get; }
        public long Score { get; }
        public int Chain { get; }
        public int Combo { get; }
        public LineMatch Lines { get; }
        public ReadOnlyCollection<BlockMove> Moves { get; }
        internal TurnFrame(TurnStage stage, BoardState board, GravityDirection gravity, long score, int chain = 0,
            LineMatch lines = null, ReadOnlyCollection<BlockMove> moves = null, int combo = 0)
        {
            Stage = stage; this.board = board.Copy(); Gravity = gravity; Score = score; Chain = chain; Combo = combo;
            Lines = lines; Moves = moves ?? Array.AsReadOnly(new BlockMove[0]);
        }
    }

    public sealed class TurnResult
    {
        public ReadOnlyCollection<TurnFrame> Frames { get; }
        public bool IsGameOver { get; }
        public long ScoreDelta { get; }
        internal TurnResult(List<TurnFrame> frames, bool gameOver, long scoreDelta)
        { Frames = frames.AsReadOnly(); IsGameOver = gameOver; ScoreDelta = scoreDelta; }
    }

    public sealed class BlockGravitySession
    {
        private readonly BlockGravityConfig config;
        private readonly IPieceSource source;
        private BoardState board;
        private PieceDefinition[] tray;
        private bool pendingGameOver;
        private bool rewardClaimed;
        private bool clearedInTray;
        private int nextBlockId;
        public SessionState State { get; private set; }
        public BoardState Board => board.Copy();
        public ReadOnlyCollection<PieceDefinition> Tray => Array.AsReadOnly((PieceDefinition[])tray.Clone());
        public GravityDirection Gravity { get; private set; }
        public GravityDirection NextGravity => GravityResolver.Next(Gravity);
        public int Placements { get; private set; }
        public int RemainingUntilGravityChange => config.PlacementsPerGravityChange - Placements % config.PlacementsPerGravityChange;
        public long Score { get; private set; }
        public int Combo { get; private set; }

        public BlockGravitySession(BlockGravityConfig config, IPieceSource source, BoardState initialBoard = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            if (initialBoard != null && (initialBoard.Width != config.Width || initialBoard.Height != config.Height))
                throw new ArgumentException(nameof(initialBoard));
            board = initialBoard?.Copy() ?? new BoardState(config.Width, config.Height);
            nextBlockId = 1;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++) nextBlockId = Math.Max(nextBlockId, checked(board[x, y] + 1));
            Gravity = config.StartDirection;
            tray = DrawTray();
            State = PlacementValidator.HasAnyPlacement(board, tray) ? SessionState.WaitingForInput : SessionState.GameOver;
        }

        private PieceDefinition[] DrawTray()
        {
            var result = new PieceDefinition[3];
            for (int i = 0; i < result.Length; i++)
                result[i] = source.Next() ?? throw new InvalidOperationException(nameof(source));
            return result;
        }

        public PlacementFailure TryPlace(int slot, Cell origin, out TurnResult result)
        {
            result = null;
            if (State == SessionState.AwaitingPresentation) return PlacementFailure.Busy;
            if (State == SessionState.GameOver) return PlacementFailure.GameOver;
            if (slot < 0 || slot >= tray.Length) return PlacementFailure.InvalidSlot;
            PieceDefinition piece = tray[slot];
            if (piece == null) return PlacementFailure.UsedSlot;
            if (!PlacementValidator.CanPlace(board, piece.Shape, origin)) return PlacementFailure.InvalidPosition;

            // Work on copies; only a fully resolved turn is committed to session state.
            BoardState working = board.Copy();
            int blockId = nextBlockId;
            foreach (Cell cell in piece.Shape.Cells) working[origin.X + cell.X, origin.Y + cell.Y] = checked(blockId++);
            long score = checked(Score + ScoreCalculator.Placement(piece.Shape.Cells.Count));
            GravityDirection gravity = Gravity;
            int combo = Combo;
            bool clearedThisTurn = false;
            var frames = new List<TurnFrame> { new TurnFrame(TurnStage.Placing, working, gravity, score, combo: combo) };
            int chain = 0;
            Resolve(ref working, gravity, ref score, ref chain, ref combo, ref clearedThisTurn, frames);
            int placements = checked(Placements + 1);
            if (placements % config.PlacementsPerGravityChange == 0)
            {
                gravity = GravityResolver.Next(gravity);
                frames.Add(new TurnFrame(TurnStage.ChangingGravity, working, gravity, score, combo: combo));
                // Gravity-change clears continue the same turn's chain multiplier.
                Resolve(ref working, gravity, ref score, ref chain, ref combo, ref clearedThisTurn, frames);
            }
            bool nextClearedInTray = clearedInTray || clearedThisTurn;
            var nextTray = (PieceDefinition[])tray.Clone();
            nextTray[slot] = null;
            if (Array.TrueForAll(nextTray, entry => entry == null))
            {
                if (!nextClearedInTray) combo = 0;
                nextClearedInTray = false;
                nextTray = DrawTray();
                frames.Add(new TurnFrame(TurnStage.RefillTray, working, gravity, score, combo: combo));
            }
            bool gameOver = !PlacementValidator.HasAnyPlacement(working, nextTray);
            frames.Add(new TurnFrame(TurnStage.CheckingGameOver, working, gravity, score, combo: combo));
            result = new TurnResult(frames, gameOver, score - Score);
            Combo = combo; clearedInTray = nextClearedInTray;
            board = working; tray = nextTray; Score = score; Gravity = gravity;
            Placements = placements; nextBlockId = blockId; pendingGameOver = gameOver;
            State = SessionState.AwaitingPresentation;
            return PlacementFailure.None;
        }

        private static void Resolve(ref BoardState working, GravityDirection gravity, ref long score, ref int chain,
            ref int combo, ref bool clearedThisTurn, List<TurnFrame> frames)
        {
            while (true)
            {
                GravityResult movement = GravityResolver.Resolve(working, gravity);
                working = movement.Board;
                frames.Add(new TurnFrame(TurnStage.ResolvingGravity, working, gravity, score, moves: movement.Moves, combo: combo));
                BoardState cleared = LineClearResolver.Clear(working, out LineMatch lines);
                if (lines.LineCount == 0) return;
                if (!clearedThisTurn) { combo = checked(combo + 1); clearedThisTurn = true; }
                chain++;
                score = checked(score + ScoreCalculator.Clear(lines.LineCount, chain, combo));
                working = cleared;
                frames.Add(new TurnFrame(TurnStage.ClearingLines, working, gravity, score, chain, lines, combo: combo));
            }
        }

        public bool CompletePresentation()
        {
            if (State != SessionState.AwaitingPresentation) return false;
            State = pendingGameOver ? SessionState.GameOver : SessionState.WaitingForInput;
            return true;
        }

        public bool TryClaimReward(out int coins)
        {
            coins = 0;
            if (State != SessionState.GameOver || rewardClaimed) return false;
            coins = ScoreCalculator.Coins(Score); rewardClaimed = true;
            return true;
        }
    }
}
