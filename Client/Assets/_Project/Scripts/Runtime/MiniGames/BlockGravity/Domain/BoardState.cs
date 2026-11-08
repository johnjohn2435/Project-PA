using System;

namespace PocketArcade.BlockGravity
{
    public sealed class BoardState
    {
        private readonly int[] cells;
        public int Width { get; }
        public int Height { get; }
        public BoardState(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width; Height = height;
            cells = new int[checked(width * height)];
        }
        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public int this[int x, int y]
        {
            get { Check(x, y); return cells[y * Width + x]; }
            set
            {
                Check(x, y);
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                cells[y * Width + x] = value;
            }
        }
        private void Check(int x, int y)
        {
            if (!Contains(x, y)) throw new ArgumentOutOfRangeException(nameof(x));
        }
        public int OccupiedCount
        {
            get { int count = 0; foreach (int value in cells) if (value != 0) count++; return count; }
        }
        public BoardState Copy()
        {
            var copy = new BoardState(Width, Height);
            Array.Copy(cells, copy.cells, cells.Length);
            return copy;
        }
    }
}
