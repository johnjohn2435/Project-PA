using System;

namespace PocketArcade.BlockGravity
{
    [Serializable]
    public readonly struct Cell : IEquatable<Cell>
    {
        public int X { get; }
        public int Y { get; }
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
    }

    public enum GravityDirection { Down, Left, Up, Right }
    public enum SessionState { WaitingForInput, AwaitingPresentation, GameOver }
    public enum TurnStage { Placing, ResolvingGravity, ClearingLines, ChangingGravity, RefillTray, CheckingGameOver }
    public enum PlacementFailure { None, Busy, GameOver, InvalidSlot, UsedSlot, InvalidPosition }
}
