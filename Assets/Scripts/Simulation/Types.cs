using System;

namespace EchoShift.Simulation
{
    /// <summary>A tile position on the board. Y grows downwards, so row 0 is the top row.</summary>
    [Serializable]
    public struct Vec2i : IEquatable<Vec2i>
    {
        public int X;
        public int Y;

        public Vec2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vec2i operator +(Vec2i a, Vec2i b) => new Vec2i(a.X + b.X, a.Y + b.Y);
        public static Vec2i operator -(Vec2i a, Vec2i b) => new Vec2i(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Vec2i a, Vec2i b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vec2i a, Vec2i b) => !(a == b);

        public bool Equals(Vec2i other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2i v && Equals(v);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => "(" + X + "," + Y + ")";
    }

    /// <summary>One turn of input. Wait spends a turn without moving.</summary>
    public enum Move : byte
    {
        Up,
        Down,
        Left,
        Right,
        Wait
    }

    public static class Moves
    {
        public static Vec2i Delta(Move m)
        {
            switch (m)
            {
                case Move.Up: return new Vec2i(0, -1);
                case Move.Down: return new Vec2i(0, 1);
                case Move.Left: return new Vec2i(-1, 0);
                case Move.Right: return new Vec2i(1, 0);
                default: return new Vec2i(0, 0);
            }
        }

        public static char ToChar(Move m)
        {
            switch (m)
            {
                case Move.Up: return 'U';
                case Move.Down: return 'D';
                case Move.Left: return 'L';
                case Move.Right: return 'R';
                default: return 'W';
            }
        }

        public static bool TryParse(char c, out Move move)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'U': move = Move.Up; return true;
                case 'D': move = Move.Down; return true;
                case 'L': move = Move.Left; return true;
                case 'R': move = Move.Right; return true;
                case 'W': move = Move.Wait; return true;
                default: move = Move.Wait; return false;
            }
        }
    }

    /// <summary>The static ground of a tile. Devices sit on top of Floor or Gap.</summary>
    public enum Tile : byte
    {
        Floor,
        Wall,
        Gap,
        Spikes,
        /// <summary>Spikes that are up on odd turns (1, 3, 5...).</summary>
        SpikesOdd,
        /// <summary>Spikes that are up on even turns (2, 4, 6...).</summary>
        SpikesEven,
        /// <summary>Breaks into a gap once something has stepped off it.</summary>
        Crumble,
        WindUp,
        WindDown,
        WindLeft,
        WindRight
    }

    public enum DeviceKind : byte
    {
        /// <summary>Active while the player or an echo stands on it.</summary>
        Plate,
        /// <summary>Open while its channel is active. Blocks movement when closed.</summary>
        Door,
        /// <summary>Toggles when bumped. Not walkable.</summary>
        Switch,
        /// <summary>Active for a set number of turns after being bumped. Not walkable.</summary>
        TimedSwitch,
        /// <summary>Spans a gap while its channel is active.</summary>
        Bridge
    }

    public enum GameStatus : byte
    {
        Playing,
        Won,
        Failed
    }

    public enum StepResult : byte
    {
        Ok,
        LimitReached,
        NotPlaying
    }
}
