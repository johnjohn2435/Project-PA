using System;
using System.Collections.Generic;
using NUnit.Framework;
using PocketArcade.BlockGravity;

namespace PocketArcade.Tests.BlockGravity
{
    public sealed class BlockGravityTests
    {
        private static PieceDefinition Single => new PieceDefinition("single", 1, new Cell(0, 0));
        private static BoardState Board(int width, int height, params Cell[] occupied)
        {
            var board = new BoardState(width, height);
            for (int i = 0; i < occupied.Length; i++) board[occupied[i].X, occupied[i].Y] = i + 1;
            return board;
        }
        private static void EqualBoards(BoardState expected, BoardState actual)
        {
            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            for (int y = 0; y < expected.Height; y++)
                for (int x = 0; x < expected.Width; x++) Assert.That(actual[x, y], Is.EqualTo(expected[x, y]));
        }
        private sealed class FixedSource : IPieceSource
        {
            private readonly PieceDefinition[] pieces;
            public int Draws { get; private set; }
            public FixedSource(params PieceDefinition[] pieces) { this.pieces = pieces; }
            public PieceDefinition Next() { return pieces[Draws++ % pieces.Length]; }
        }

        [Test] public void EmptyBoardAndCopyAreIndependent()
        {
            var board = new BoardState(8, 8);
            Assert.That(board.OccupiedCount, Is.Zero);
            board[2, 3] = 5;
            var copy = board.Copy(); copy[2, 3] = 7;
            Assert.That(board[2, 3], Is.EqualTo(5));
            Assert.That(copy.OccupiedCount, Is.EqualTo(1));
        }
        [TestCase(-1, 0)] [TestCase(8, 0)] [TestCase(0, -1)] [TestCase(0, 8)]
        public void BoardRejectsOutsideAccess(int x, int y)
        { Assert.Throws<ArgumentOutOfRangeException>(() => { int unused = new BoardState(8, 8)[x, y]; }); }
        [Test] public void BoardRejectsInvalidSizeAndNegativeIds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardState(0, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardState(8, -1));
            var board = new BoardState(8, 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => board[0, 0] = -1);
        }
        [Test] public void ShapeCopiesAndValidatesCoordinates()
        {
            var cells = new[] { new Cell(0, 0), new Cell(1, 0) };
            var shape = new PieceShape(cells); cells[0] = new Cell(9, 9);
            Assert.That(shape.Cells[0], Is.EqualTo(new Cell(0, 0)));
            Assert.Throws<ArgumentException>(() => new PieceShape());
            Assert.Throws<ArgumentException>(() => new PieceShape(new Cell(0, 0), new Cell(0, 0)));
            Assert.Throws<ArgumentException>(() => new PieceShape(new Cell(0, 0), new Cell(2, 0)));
            Assert.Throws<ArgumentException>(() => new PieceShape(new Cell(1, 1)));
            Assert.Throws<ArgumentException>(() => new PieceShape(new Cell(-1, 0)));
        }
        [TestCase(0, 0, true)] [TestCase(6, 7, true)] [TestCase(7, 7, false)]
        [TestCase(-1, 0, false)] [TestCase(0, 8, false)] [TestCase(int.MaxValue, 0, false)]
        public void PlacementHonorsBoundsWithoutOverflow(int x, int y, bool expected)
        {
            var shape = new PieceShape(new Cell(0, 0), new Cell(1, 0));
            Assert.That(PlacementValidator.CanPlace(new BoardState(8, 8), shape, new Cell(x, y)), Is.EqualTo(expected));
        }
        [Test] public void PlacementUsesActualShapeWithoutRotating()
        {
            var board = Board(2, 3, new Cell(1, 0), new Cell(1, 1), new Cell(1, 2));
            var vertical = new PieceShape(new Cell(0, 0), new Cell(0, 1), new Cell(0, 2));
            var horizontal = new PieceShape(new Cell(0, 0), new Cell(1, 0), new Cell(2, 0));
            Assert.That(PlacementValidator.HasPlacement(board, vertical), Is.True);
            Assert.That(PlacementValidator.HasPlacement(board, horizontal), Is.False);
            Assert.That(PlacementValidator.CanPlace(board, Single.Shape, new Cell(1, 1)), Is.False);
        }
        [Test] public void TrayChecksAllRemainingPiecesAndIgnoresUsedSlots()
        {
            var board = Board(2, 2, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1));
            var domino = new PieceDefinition("domino", 1, new Cell(0, 0), new Cell(1, 0));
            Assert.That(PlacementValidator.HasAnyPlacement(board, new[] { domino, null, Single }), Is.True);
            Assert.That(PlacementValidator.HasAnyPlacement(board, new[] { domino, null, domino }), Is.False);
        }
        [TestCase(GravityDirection.Down, 1, 0, 1, 1)]
        [TestCase(GravityDirection.Up, 1, 2, 1, 3)]
        [TestCase(GravityDirection.Left, 0, 1, 1, 1)]
        [TestCase(GravityDirection.Right, 2, 1, 3, 1)]
        public void GravityCompactsWithoutReordering(GravityDirection direction, int ax, int ay, int bx, int by)
        {
            bool vertical = direction == GravityDirection.Down || direction == GravityDirection.Up;
            var board = vertical ? Board(4, 4, new Cell(1, 1), new Cell(1, 3)) : Board(4, 4, new Cell(1, 1), new Cell(3, 1));
            var original = board.Copy();
            var moved = GravityResolver.Resolve(board, direction);
            Assert.That(moved.Board[ax, ay], Is.EqualTo(1));
            Assert.That(moved.Board[bx, by], Is.EqualTo(2));
            Assert.That(moved.Board.OccupiedCount, Is.EqualTo(2));
            EqualBoards(original, board);
            EqualBoards(moved.Board, GravityResolver.Resolve(moved.Board, direction).Board);
            Assert.That(GravityResolver.Resolve(moved.Board, direction).Moves.Count, Is.Zero);
        }
        [TestCase(GravityDirection.Down)] [TestCase(GravityDirection.Left)]
        [TestCase(GravityDirection.Up)] [TestCase(GravityDirection.Right)]
        public void GravityPreservesRandomBoardsAndEmptyBoards(GravityDirection direction)
        {
            Assert.That(GravityResolver.Resolve(new BoardState(8, 8), direction).Board.OccupiedCount, Is.Zero);
            var random = new Random(37);
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var board = new BoardState(8, 8); var expected = new HashSet<int>();
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                    if (random.Next(2) == 0) { int id = y * 8 + x + 1; board[x, y] = id; expected.Add(id); }
                var moved = GravityResolver.Resolve(board, direction); var actual = new HashSet<int>();
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (moved.Board[x, y] != 0) actual.Add(moved.Board[x, y]);
                Assert.That(actual.SetEquals(expected), Is.True);
                foreach (BlockMove move in moved.Moves)
                { Assert.That(board[move.From.X, move.From.Y], Is.EqualTo(move.BlockId)); Assert.That(moved.Board[move.To.X, move.To.Y], Is.EqualTo(move.BlockId)); }
                EqualBoards(moved.Board, GravityResolver.Resolve(moved.Board, direction).Board);
            }
        }
        [Test] public void GravityCyclesInSpecifiedOrderAndRejectsInvalidValues()
        {
            var direction = GravityDirection.Down;
            foreach (var expected in new[] { GravityDirection.Left, GravityDirection.Up, GravityDirection.Right, GravityDirection.Down })
            { direction = GravityResolver.Next(direction); Assert.That(direction, Is.EqualTo(expected)); }
            Assert.Throws<ArgumentOutOfRangeException>(() => GravityResolver.Resolve(new BoardState(8, 8), (GravityDirection)99));
        }
        [Test] public void SimultaneousCrossClearsIntersectionOnlyOnce()
        {
            var board = Board(3, 3, new Cell(0, 1), new Cell(1, 1), new Cell(2, 1), new Cell(1, 0), new Cell(1, 2));
            var cleared = LineClearResolver.Clear(board, out LineMatch lines);
            Assert.That(lines.LineCount, Is.EqualTo(2)); Assert.That(lines.Cells.Count, Is.EqualTo(5));
            Assert.That(cleared.OccupiedCount, Is.Zero); Assert.That(board.OccupiedCount, Is.EqualTo(5));
        }
        [TestCase(true)] [TestCase(false)]
        public void DetectsSingleRowOrColumn(bool row)
        {
            var board = new BoardState(3, 3);
            for (int i = 0; i < 3; i++) if (row) board[i, 1] = i + 1; else board[1, i] = i + 1;
            var match = LineDetector.Find(board);
            Assert.That(match.LineCount, Is.EqualTo(1)); Assert.That(match.Cells.Count, Is.EqualTo(3));
            Assert.That(match.Rows.Count, Is.EqualTo(row ? 1 : 0));
        }
        [Test] public void ClearThenGravityCanCreateAnotherLineOnUnsettledInput()
        {
            var board = Board(3, 3, new Cell(0, 2), new Cell(1, 2), new Cell(2, 2), new Cell(0, 0), new Cell(1, 1), new Cell(2, 0));
            var cleared = LineClearResolver.Clear(board, out LineMatch first);
            Assert.That(first.LineCount, Is.EqualTo(1));
            var moved = GravityResolver.Resolve(cleared, GravityDirection.Down);
            Assert.That(LineDetector.Find(moved.Board).LineCount, Is.EqualTo(1));
        }
        [TestCase(0, 0)] [TestCase(1, 100)] [TestCase(2, 300)] [TestCase(3, 600)]
        [TestCase(4, 1000)] [TestCase(5, 1400)] [TestCase(8, 2600)]
        public void ScoreMatchesLineTable(int lines, int expected)
        { Assert.That(ScoreCalculator.Clear(lines, 1), Is.EqualTo(expected)); Assert.That(ScoreCalculator.Clear(lines, 3), Is.EqualTo(expected * 3L)); }
        [Test] public void ScorePlacementRewardsAndInvalidArguments()
        {
            Assert.That(ScoreCalculator.Placement(4), Is.EqualTo(40));
            Assert.That(ScoreCalculator.Coins(249), Is.Zero); Assert.That(ScoreCalculator.Coins(250), Is.EqualTo(1));
            Assert.That(ScoreCalculator.Coins(long.MaxValue), Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreCalculator.Clear(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreCalculator.Clear(1, 0));
        }
        [Test] public void SeededWeightedBagIsReproducibleAndCopiesPool()
        {
            var pool = DefaultPieceSet.Create(); var a = new PieceBag(pool, 42); var b = new PieceBag(pool, 42);
            pool[0] = null;
            for (int i = 0; i < 300; i++) Assert.That(a.Next().Id, Is.EqualTo(b.Next().Id));
            Assert.Throws<ArgumentException>(() => new PieceBag(new PieceDefinition[0], 1));
            Assert.Throws<ArgumentException>(() => new PieceBag(new[] { Single, Single }, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PieceDefinition("invalid", 0, new Cell(0, 0)));
        }
        [Test] public void DefaultPoolHasTenUniqueFixedOrientationsAndFourCellT()
        {
            var pool = DefaultPieceSet.Create(); var shapes = new HashSet<string>();
            Assert.That(pool.Length, Is.EqualTo(10));
            foreach (var piece in pool)
            {
                var coords = new List<string>(); foreach (Cell c in piece.Shape.Cells) coords.Add(c.X + ":" + c.Y);
                coords.Sort(StringComparer.Ordinal); Assert.That(shapes.Add(string.Join(";", coords)), Is.True);
                if (piece.Id == "t4") Assert.That(piece.Shape.Cells.Count, Is.EqualTo(4));
            }
        }
        [Test] public void TurnScoresSplitsPieceIntoCellsAndLocksUntilAcknowledged()
        {
            var diagonalL = new PieceDefinition("l", 1, new Cell(0, 0), new Cell(1, 0), new Cell(1, 1));
            var session = new BlockGravitySession(new BlockGravityConfig(), new FixedSource(diagonalL));
            Assert.That(session.TryPlace(0, new Cell(2, 4), out TurnResult turn), Is.EqualTo(PlacementFailure.None));
            Assert.That(session.Score, Is.EqualTo(30)); Assert.That(session.Board[2, 0], Is.Not.Zero);
            Assert.That(session.Board[3, 0], Is.Not.Zero); Assert.That(session.Board[3, 1], Is.Not.Zero);
            Assert.That(session.TryPlace(1, new Cell(0, 0), out _), Is.EqualTo(PlacementFailure.Busy));
            Assert.That(turn.Frames[0].Stage, Is.EqualTo(TurnStage.Placing));
            Assert.That(turn.Frames[turn.Frames.Count - 1].Stage, Is.EqualTo(TurnStage.CheckingGameOver));
            Assert.That(session.CompletePresentation(), Is.True); Assert.That(session.CompletePresentation(), Is.False);
        }
        [Test] public void InvalidTurnLeavesBoardScoreTrayAndRandomSourceUntouched()
        {
            var source = new FixedSource(Single); var session = new BlockGravitySession(new BlockGravityConfig(), source);
            Assert.That(session.TryPlace(0, new Cell(8, 0), out TurnResult turn), Is.EqualTo(PlacementFailure.InvalidPosition));
            Assert.That(turn, Is.Null); Assert.That(source.Draws, Is.EqualTo(3));
            Assert.That(session.Score, Is.Zero); Assert.That(session.Placements, Is.Zero); Assert.That(session.Board.OccupiedCount, Is.Zero);
            Assert.That(session.TryPlace(-1, new Cell(0, 0), out _), Is.EqualTo(PlacementFailure.InvalidSlot));
        }
        [Test] public void TrayRefillsOnlyAfterThirdPieceAndSnapshotsAreIndependent()
        {
            var source = new FixedSource(Single); var session = new BlockGravitySession(new BlockGravityConfig(), source);
            var oldTray = session.Tray;
            for (int i = 0; i < 3; i++)
            {
                Assert.That(session.TryPlace(i, new Cell(i, 7), out TurnResult turn), Is.EqualTo(PlacementFailure.None));
                Assert.That(source.Draws, Is.EqualTo(i == 2 ? 6 : 3));
                var copy = session.Board; copy[7, 7] = 999; Assert.That(session.Board[7, 7], Is.Zero);
                var frameCopy = turn.Frames[0].Board; frameCopy[7, 7] = 999;
                Assert.That(turn.Frames[0].Board[7, 7], Is.Zero);
                session.CompletePresentation();
                if (i == 0) Assert.That(session.TryPlace(0, new Cell(7, 7), out _), Is.EqualTo(PlacementFailure.UsedSlot));
            }
            Assert.That(oldTray[0], Is.Not.Null); foreach (var piece in session.Tray) Assert.That(piece, Is.Not.Null);
        }
        [Test] public void FifthPlacementChangesGravityAfterResolutionAndRefillPrecedesGameOverCheck()
        {
            var session = new BlockGravitySession(new BlockGravityConfig(), new FixedSource(Single));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(session.TryPlace(i % 3, new Cell(i, 7), out TurnResult turn), Is.EqualTo(PlacementFailure.None));
                if (i == 4)
                {
                    var stages = new List<TurnStage>(); foreach (var frame in turn.Frames) stages.Add(frame.Stage);
                    int change = stages.IndexOf(TurnStage.ChangingGravity);
                    Assert.That(change, Is.GreaterThan(stages.IndexOf(TurnStage.ResolvingGravity)));
                    Assert.That(stages[change + 1], Is.EqualTo(TurnStage.ResolvingGravity));
                    Assert.That(session.Gravity, Is.EqualTo(GravityDirection.Left));
                    Assert.That(session.RemainingUntilGravityChange, Is.EqualTo(5));
                }
                else Assert.That(session.Gravity, Is.EqualTo(GravityDirection.Down));
                session.CompletePresentation();
            }
        }
        [Test] public void SimultaneousLinesScoreBeforeGameOverAndCanFreeFullBoard()
        {
            var initial = Board(2, 2, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1));
            var session = new BlockGravitySession(new BlockGravityConfig(2, 2), new FixedSource(Single), initial);
            Assert.That(session.TryPlace(0, new Cell(1, 1), out TurnResult turn), Is.EqualTo(PlacementFailure.None));
            Assert.That(session.Board.OccupiedCount, Is.Zero); Assert.That(session.Score, Is.EqualTo(1010));
            Assert.That(turn.IsGameOver, Is.False); session.CompletePresentation();
            Assert.That(session.State, Is.EqualTo(SessionState.WaitingForInput));
        }
        [Test] public void GameOverWaitsForPresentationAndRewardCanOnlyBeClaimedOnce()
        {
            var single = Single; var big = new PieceDefinition("big", 1, new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0));
            var session = new BlockGravitySession(new BlockGravityConfig(3, 3), new FixedSource(single, big, big));
            Assert.That(session.TryClaimReward(out _), Is.False);
            session.TryPlace(0, new Cell(1, 1), out TurnResult turn);
            Assert.That(turn.IsGameOver, Is.True); Assert.That(session.TryClaimReward(out _), Is.False);
            session.CompletePresentation(); Assert.That(session.State, Is.EqualTo(SessionState.GameOver));
            Assert.That(session.TryClaimReward(out int coins), Is.True); Assert.That(coins, Is.Zero);
            Assert.That(session.TryClaimReward(out _), Is.False);
            Assert.That(session.TryPlace(1, new Cell(0, 0), out _), Is.EqualTo(PlacementFailure.GameOver));
        }
        [Test] public void RefillBeforeGameOverAllowsNewSmallPieces()
        {
            var source = new FixedSource(Single); var session = new BlockGravitySession(new BlockGravityConfig(2, 2), source);
            for (int i = 0; i < 3; i++)
            {
                session.TryPlace(i, new Cell(0, 1), out TurnResult turn); session.CompletePresentation();
                Assert.That(turn.IsGameOver, Is.False);
                if (i == 2)
                {
                    Assert.That(turn.Frames[turn.Frames.Count - 2].Stage, Is.EqualTo(TurnStage.RefillTray));
                    Assert.That(turn.Frames[turn.Frames.Count - 1].Stage, Is.EqualTo(TurnStage.CheckingGameOver));
                }
            }
        }
        [Test] public void FreshSessionResetsProgressAndCopiesInitialBoard()
        {
            var initial = Board(8, 8, new Cell(0, 0));
            var session = new BlockGravitySession(new BlockGravityConfig(), new FixedSource(Single), initial);
            initial[0, 0] = 0; Assert.That(session.Board[0, 0], Is.Not.Zero);
            var fresh = new BlockGravitySession(new BlockGravityConfig(), new FixedSource(Single));
            Assert.That(fresh.Score, Is.Zero); Assert.That(fresh.Placements, Is.Zero);
            Assert.That(fresh.Gravity, Is.EqualTo(GravityDirection.Down)); Assert.That(fresh.Board.OccupiedCount, Is.Zero);
        }
        [Test] public void SeededPlaythroughsPreserveInvariantsAndTerminateEachTurn()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                var session = new BlockGravitySession(new BlockGravityConfig(), new PieceBag(DefaultPieceSet.Create(), seed));
                long previous = 0;
                for (int t = 0; t < 100 && session.State != SessionState.GameOver; t++)
                {
                    bool placed = false;
                    for (int slot = 0; slot < 3 && !placed; slot++)
                    {
                        var piece = session.Tray[slot]; if (piece == null) continue;
                        for (int y = 0; y < 8 && !placed; y++) for (int x = 0; x < 8 && !placed; x++)
                        {
                            if (!PlacementValidator.CanPlace(session.Board, piece.Shape, new Cell(x, y))) continue;
                            Assert.That(session.TryPlace(slot, new Cell(x, y), out TurnResult turn), Is.EqualTo(PlacementFailure.None));
                            Assert.That(turn.Frames.Count, Is.LessThan(140));
                            Assert.That(session.Score, Is.GreaterThan(previous)); previous = session.Score;
                            Assert.That(LineDetector.Find(session.Board).LineCount, Is.Zero);
                            session.CompletePresentation(); placed = true;
                        }
                    }
                    Assert.That(placed, Is.True);
                }
            }
        }

        [TestCase(1080, 1920, 0, 0, 1080, 1920)]
        [TestCase(1080, 2340, 0, 80, 1080, 2200)]
        [TestCase(1080, 2400, 0, 100, 1080, 2240)]
        [TestCase(720, 1280, 0, 0, 720, 1280)]
        [TestCase(1200, 1920, 20, 40, 1160, 1800)]
        public void LayoutFitsSafeAreaAndProjectsBoardCenterCorrectly(int w, int h, int x, int y, int sw, int sh)
        {
            var safe = new LayoutRect(x, y, sw, sh); var layout = PlayfieldLayout.Calculate(w, h, safe);
            Assert.That(layout.Board.Width, Is.EqualTo(layout.Board.Height));
            foreach (var rect in new[] { layout.Hud, layout.Board, layout.Tray })
            {
                Assert.That(rect.X, Is.GreaterThanOrEqualTo(x)); Assert.That(rect.Y, Is.GreaterThanOrEqualTo(y));
                Assert.That(rect.X + rect.Width, Is.LessThanOrEqualTo(x + sw + 0.00001));
                Assert.That(rect.Y + rect.Height, Is.LessThanOrEqualTo(y + sh + 0.00001));
            }
            Assert.That(layout.Tray.Y + layout.Tray.Height, Is.LessThan(layout.Board.Y));
            Assert.That(layout.Board.Y + layout.Board.Height, Is.LessThan(layout.Hud.Y));
            double projectedX = w / 2.0 - layout.CameraX / layout.WorldUnitsPerPixel;
            double projectedY = h / 2.0 - layout.CameraY / layout.WorldUnitsPerPixel;
            Assert.That(projectedX, Is.EqualTo(layout.Board.CenterX).Within(0.00001));
            Assert.That(projectedY, Is.EqualTo(layout.Board.CenterY).Within(0.00001));
            Assert.That(8 / layout.WorldUnitsPerPixel, Is.EqualTo(layout.Board.Width).Within(0.00001));
        }
        [Test] public void LayoutMapsCellCentersAndRejectsInvalidSafeArea()
        {
            var layout = PlayfieldLayout.Calculate(1080, 1920, new LayoutRect(0, 0, 1080, 1920));
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                Assert.That(layout.WorldToCell(layout.CellCenterX(x), layout.CellCenterY(y)), Is.EqualTo(new Cell(x, y)));
            Assert.That(layout.WorldToCell(4, 0).X, Is.EqualTo(8));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayfieldLayout.Calculate(1080, 1920, default(LayoutRect)));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayfieldLayout.Calculate(1080, 1920, new LayoutRect(-1, 0, 100, 100)));
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayfieldLayout.Calculate(1080, 1920, new LayoutRect(0, 0, 2000, 100)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutRect(double.NaN, 0, 1, 1));
        }
    }
}
