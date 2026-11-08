using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PocketArcade.BlockGravity
{
    public readonly struct BlockMove
    {
        public int BlockId { get; }
        public Cell From { get; }
        public Cell To { get; }
        public BlockMove(int id, Cell from, Cell to) { BlockId = id; From = from; To = to; }
    }

    public sealed class GravityResult
    {
        private readonly BoardState board;
        public BoardState Board => board.Copy();
        public ReadOnlyCollection<BlockMove> Moves { get; }
        internal GravityResult(BoardState board, List<BlockMove> moves)
        { this.board = board.Copy(); Moves = moves.AsReadOnly(); }
    }

    public static class GravityResolver
    {
        public static GravityDirection Next(GravityDirection direction)
        {
            Validate(direction);
            return (GravityDirection)(((int)direction + 1) % 4);
        }
        private static void Validate(GravityDirection direction)
        {
            if (direction < GravityDirection.Down || direction > GravityDirection.Right)
                throw new ArgumentOutOfRangeException(nameof(direction));
        }
        public static GravityResult Resolve(BoardState board, GravityDirection direction)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            Validate(direction);
            var result = new BoardState(board.Width, board.Height);
            var moves = new List<BlockMove>();
            bool vertical = direction == GravityDirection.Down || direction == GravityDirection.Up;
            bool reverse = direction == GravityDirection.Up || direction == GravityDirection.Right;
            int lanes = vertical ? board.Width : board.Height;
            int length = vertical ? board.Height : board.Width;
            for (int lane = 0; lane < lanes; lane++)
            {
                int target = 0;
                for (int offset = 0; offset < length; offset++)
                {
                    int sourceAxis = reverse ? length - 1 - offset : offset;
                    var from = vertical ? new Cell(lane, sourceAxis) : new Cell(sourceAxis, lane);
                    int id = board[from.X, from.Y];
                    if (id == 0) continue;
                    int targetAxis = reverse ? length - 1 - target : target;
                    var to = vertical ? new Cell(lane, targetAxis) : new Cell(targetAxis, lane);
                    result[to.X, to.Y] = id;
                    if (!from.Equals(to)) moves.Add(new BlockMove(id, from, to));
                    target++;
                }
            }
            return new GravityResult(result, moves);
        }
    }

    public sealed class LineMatch
    {
        public ReadOnlyCollection<int> Rows { get; }
        public ReadOnlyCollection<int> Columns { get; }
        public ReadOnlyCollection<Cell> Cells { get; }
        public int LineCount => Rows.Count + Columns.Count;
        internal LineMatch(List<int> rows, List<int> columns, List<Cell> cells)
        { Rows = rows.AsReadOnly(); Columns = columns.AsReadOnly(); Cells = cells.AsReadOnly(); }
    }

    public static class LineDetector
    {
        public static LineMatch Find(BoardState board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var rows = new List<int>(); var columns = new List<int>();
            for (int y = 0; y < board.Height; y++)
            {
                bool full = true;
                for (int x = 0; x < board.Width; x++) if (board[x, y] == 0) { full = false; break; }
                if (full) rows.Add(y);
            }
            for (int x = 0; x < board.Width; x++)
            {
                bool full = true;
                for (int y = 0; y < board.Height; y++) if (board[x, y] == 0) { full = false; break; }
                if (full) columns.Add(x);
            }
            var cells = new List<Cell>();
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (rows.Contains(y) || columns.Contains(x)) cells.Add(new Cell(x, y));
            return new LineMatch(rows, columns, cells);
        }
    }

    public static class LineClearResolver
    {
        public static BoardState Clear(BoardState board, out LineMatch match)
        {
            match = LineDetector.Find(board);
            BoardState result = board.Copy();
            foreach (Cell cell in match.Cells) result[cell.X, cell.Y] = 0;
            return result;
        }
    }

    public static class ScoreCalculator
    {
        public static long Placement(int cellCount)
        {
            if (cellCount <= 0) throw new ArgumentOutOfRangeException(nameof(cellCount));
            return cellCount * 10L;
        }
        public static long Clear(int lines, int chain, int combo = 1)
        {
            if (lines < 0) throw new ArgumentOutOfRangeException(nameof(lines));
            if (chain < 1) throw new ArgumentOutOfRangeException(nameof(chain));
            if (combo < 1) throw new ArgumentOutOfRangeException(nameof(combo));
            long value = lines == 0 ? 0 : lines == 1 ? 100 : lines == 2 ? 300 : lines == 3 ? 600 : 1000L + (lines - 4L) * 400;
            return checked(value * chain * combo);
        }
        public static int Coins(long score)
        {
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            return (int)Math.Min(100L, score / 250);
        }
    }
}
