using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PocketArcade.BlockGravity
{
    [Serializable]
    public sealed class PieceDefinition
    {
        public string Id { get; }
        public int Weight { get; }
        public PieceShape Shape { get; }
        public PieceDefinition(string id, int weight, params Cell[] cells)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id));
            if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
            Id = id; Weight = weight; Shape = new PieceShape(cells);
        }
    }

    [Serializable]
    public sealed class PieceShape
    {
        public ReadOnlyCollection<Cell> Cells { get; }
        public int Width { get; }
        public int Height { get; }
        public PieceShape(params Cell[] cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Length == 0) throw new ArgumentException(nameof(cells));
            var copy = (Cell[])cells.Clone();
            var unique = new HashSet<Cell>();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            foreach (Cell cell in copy)
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X == int.MaxValue || cell.Y == int.MaxValue || !unique.Add(cell))
                    throw new ArgumentException(nameof(cells));
                minX = Math.Min(minX, cell.X); minY = Math.Min(minY, cell.Y);
                maxX = Math.Max(maxX, cell.X); maxY = Math.Max(maxY, cell.Y);
            }
            if (minX != 0 || minY != 0) throw new ArgumentException(nameof(cells));
            var reached = new HashSet<Cell> { copy[0] };
            var queue = new Queue<Cell>(); queue.Enqueue(copy[0]);
            var offsets = new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) };
            while (queue.Count > 0)
            {
                Cell cell = queue.Dequeue();
                foreach (Cell delta in offsets)
                {
                    var next = new Cell(cell.X + delta.X, cell.Y + delta.Y);
                    if (unique.Contains(next) && reached.Add(next)) queue.Enqueue(next);
                }
            }
            if (reached.Count != copy.Length) throw new ArgumentException(nameof(cells));
            Cells = Array.AsReadOnly(copy); Width = maxX + 1; Height = maxY + 1;
        }
    }

    public static class PlacementValidator
    {
        public static bool CanPlace(BoardState board, PieceShape shape, Cell origin)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (origin.X < 0 || origin.Y < 0 || shape.Width > board.Width || shape.Height > board.Height ||
                origin.X > board.Width - shape.Width || origin.Y > board.Height - shape.Height) return false;
            foreach (Cell cell in shape.Cells)
                if (board[origin.X + cell.X, origin.Y + cell.Y] != 0) return false;
            return true;
        }
        public static bool HasPlacement(BoardState board, PieceShape shape)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            for (int y = 0; y <= board.Height - shape.Height; y++)
                for (int x = 0; x <= board.Width - shape.Width; x++)
                    if (CanPlace(board, shape, new Cell(x, y))) return true;
            return false;
        }
        public static bool HasAnyPlacement(BoardState board, IReadOnlyList<PieceDefinition> tray)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (tray == null) throw new ArgumentNullException(nameof(tray));
            foreach (PieceDefinition piece in tray)
                if (piece != null && HasPlacement(board, piece.Shape)) return true;
            return false;
        }
    }

    public interface IPieceSource { PieceDefinition Next(); }

    public sealed class PieceBag : IPieceSource
    {
        private readonly PieceDefinition[] pool;
        private readonly int totalWeight;
        private readonly Random random;
        public PieceBag(IReadOnlyList<PieceDefinition> pieces, int seed)
        {
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));
            if (pieces.Count == 0) throw new ArgumentException(nameof(pieces));
            pool = new PieceDefinition[pieces.Count];
            var ids = new HashSet<string>();
            for (int i = 0; i < pool.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null || !ids.Add(piece.Id)) throw new ArgumentException(nameof(pieces));
                pool[i] = piece; totalWeight = checked(totalWeight + piece.Weight);
            }
            random = new Random(seed);
        }
        public PieceDefinition Next()
        {
            int choice = random.Next(totalWeight);
            foreach (PieceDefinition piece in pool)
            {
                if (choice < piece.Weight) return piece;
                choice -= piece.Weight;
            }
            throw new InvalidOperationException(nameof(pool));
        }
    }

    public static class DefaultPieceSet
    {
        // Coordinate data, fixed orientation; no rotation is performed by placement.
        public static PieceDefinition[] Create() => new[]
        {
            new PieceDefinition("single", 1, new Cell(0, 0)),
            new PieceDefinition("line2_h", 1, new Cell(0, 0), new Cell(1, 0)),
            new PieceDefinition("line2_v", 1, new Cell(0, 0), new Cell(0, 1)),
            new PieceDefinition("line3_h", 1, new Cell(0, 0), new Cell(1, 0), new Cell(2, 0)),
            new PieceDefinition("line3_v", 1, new Cell(0, 0), new Cell(0, 1), new Cell(0, 2)),
            new PieceDefinition("square4", 1, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1)),
            new PieceDefinition("l3", 1, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1)),
            new PieceDefinition("l4", 1, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(0, 2)),
            new PieceDefinition("corner3", 1, new Cell(0, 0), new Cell(1, 0), new Cell(1, 1)),
            new PieceDefinition("t4", 1, new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(1, 1))
        };
    }

    // Each tray is drawn only after the previous three pieces are consumed.
    // Draw count therefore advances at the same boundaries as completed placements.
    public sealed class ProgressivePieceSource : IPieceSource
    {
        public const int MaxPieceCells = 9;
        private readonly PieceBag[] bags;
        private int draws;

        public ProgressivePieceSource(int seed)
        {
            var normal = DefaultPieceSet.Create();
            var large = new[]
            {
                new PieceDefinition("line4_h", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(3,0)),
                new PieceDefinition("line4_v", 1, new Cell(0,0), new Cell(0,1), new Cell(0,2), new Cell(0,3)),
                new PieceDefinition("rect6_h", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(0,1), new Cell(1,1), new Cell(2,1)),
                new PieceDefinition("rect6_v", 1, new Cell(0,0), new Cell(1,0), new Cell(0,1), new Cell(1,1), new Cell(0,2), new Cell(1,2)),
                new PieceDefinition("square9", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(0,1), new Cell(1,1), new Cell(2,1), new Cell(0,2), new Cell(1,2), new Cell(2,2)),
                new PieceDefinition("corner5", 1, new Cell(0,0), new Cell(1,0), new Cell(2,0), new Cell(0,1), new Cell(0,2))
            };
            var middle = new List<PieceDefinition>();
            var hard = new List<PieceDefinition>();
            foreach (var piece in normal)
            {
                if (piece.Shape.Cells.Count >= 3) middle.Add(piece);
                if (piece.Shape.Cells.Count >= 4) hard.Add(piece);
            }
            for (int i = 0; i < 4; i++) middle.Add(large[i]);
            hard.AddRange(large);
            bags = new[]
            {
                new PieceBag(normal, seed),
                new PieceBag(middle, unchecked(seed + 1)),
                new PieceBag(hard, unchecked(seed + 2)),
                new PieceBag(new[] { large[2], large[3], large[4], large[5] }, unchecked(seed + 3))
            };
        }

        public static int LevelForPlacements(int placements)
        {
            if (placements < 0) throw new ArgumentOutOfRangeException(nameof(placements));
            return placements < 6 ? 1 : placements < 18 ? 2 : placements < 36 ? 3 : 4;
        }

        public PieceDefinition Next()
        {
            var piece = bags[LevelForPlacements(draws) - 1].Next();
            if (draws < 36) draws++;
            return piece;
        }
    }
}
