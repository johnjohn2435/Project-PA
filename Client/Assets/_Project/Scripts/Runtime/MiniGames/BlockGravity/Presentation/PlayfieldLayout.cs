using System;

namespace PocketArcade.BlockGravity
{
    public readonly struct LayoutRect
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;
        public LayoutRect(double x, double y, double width, double height)
        {
            if (!Finite(x) || !Finite(y) || !Finite(width) || !Finite(height) || width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            X = x; Y = y; Width = width; Height = height;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class PlayfieldLayout
    {
        public const double CellWorldSize = 1;
        public const int BlockPixelsPerUnit = 128;
        public LayoutRect Hud { get; }
        public LayoutRect Board { get; }
        public LayoutRect Tray { get; }
        public double CellPixels => Board.Width / 8;
        public double WorldUnitsPerPixel => CellWorldSize / CellPixels;
        public double OrthographicSize { get; }
        public double CameraX { get; }
        public double CameraY { get; }
        private PlayfieldLayout(double screenWidth, double screenHeight, LayoutRect safe)
        {
            double scale = Math.Min(safe.Width / 1080, safe.Height / 1920);
            double side = 60 * scale, gap = 64 * scale;
            double hudHeight = 260 * scale, trayHeight = 360 * scale;
            double boardSize = Math.Min(safe.Width - 2 * side,
                safe.Height - hudHeight - trayHeight - 2 * gap - 120 * scale);
            double contentHeight = hudHeight + gap + boardSize + gap + trayHeight;
            double bottom = safe.Y + (safe.Height - contentHeight) / 2;
            Tray = new LayoutRect(safe.X + side, bottom, safe.Width - 2 * side, trayHeight);
            Board = new LayoutRect(safe.CenterX - boardSize / 2, bottom + trayHeight + gap, boardSize, boardSize);
            Hud = new LayoutRect(safe.X + side, Board.Y + boardSize + gap, safe.Width - 2 * side, hudHeight);
            OrthographicSize = screenHeight * WorldUnitsPerPixel / 2;
            // Board center is world (0,0), camera covers the full screen including unsafe regions.
            CameraX = (screenWidth / 2 - Board.CenterX) * WorldUnitsPerPixel;
            CameraY = (screenHeight / 2 - Board.CenterY) * WorldUnitsPerPixel;
        }
        public static PlayfieldLayout Calculate(double screenWidth, double screenHeight, LayoutRect safe)
        {
            if (double.IsNaN(screenWidth) || double.IsInfinity(screenWidth) || screenWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            if (double.IsNaN(screenHeight) || double.IsInfinity(screenHeight) || screenHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenHeight));
            // Also rejects default(LayoutRect), which bypasses its constructor.
            if (safe.Width <= 0 || safe.Height <= 0 || safe.X < 0 || safe.Y < 0 ||
                safe.X + safe.Width > screenWidth || safe.Y + safe.Height > screenHeight)
                throw new ArgumentOutOfRangeException(nameof(safe));
            return new PlayfieldLayout(screenWidth, screenHeight, safe);
        }
        public Cell WorldToCell(double worldX, double worldY) => new Cell((int)Math.Floor(worldX + 4), (int)Math.Floor(worldY + 4));
        public double CellCenterX(int column) => column - 3.5;
        public double CellCenterY(int row) => row - 3.5;
    }
}
