using System.Collections.Generic;

namespace EchoShift.Simulation
{
    public sealed class DeviceDef
    {
        public DeviceKind Kind;
        public Vec2i Pos;
        /// <summary>Channel 0..4, written as a..e in level files.</summary>
        public int Channel;
    }

    public sealed class PlatformDef
    {
        public Vec2i Pos;
        public bool Horizontal;
    }

    /// <summary>Everything that defines one level. Built by <see cref="LevelParser"/>.</summary>
    public sealed class LevelDef
    {
        public const int ChannelCount = 5;

        public string Id = "";
        public string Name = "";
        public int World = 1;
        /// <summary>1-based level number across the whole game.</summary>
        public int Number = 1;

        public int Width;
        public int Height;
        public Tile[] Tiles = new Tile[0];

        public Vec2i Start;
        public Vec2i Exit;
        /// <summary>Channels that must all be active for the exit to open. Empty means always open.</summary>
        public List<int> ExitChannels = new List<int>();

        public bool HasCrystal;
        public Vec2i Crystal;

        public List<DeviceDef> Devices = new List<DeviceDef>();
        public List<PlatformDef> Platforms = new List<PlatformDef>();

        public int EchoLimit = 1;
        public int ParEchoes = 1;
        public int MovesPerRun = 12;
        public int TimerTurns = 4;

        public bool NeedsEcho = true;
        public bool IsBoss;

        public string[] Hints = { "", "", "" };

        /// <summary>One move list per run. The last run finishes the level.</summary>
        public List<List<Move>> Solution = new List<List<Move>>();

        public bool InBounds(Vec2i p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        public int Index(Vec2i p) => p.Y * Width + p.X;
        public Tile TileAt(Vec2i p) => InBounds(p) ? Tiles[Index(p)] : Tile.Wall;

        public bool HasRhythmSpikes
        {
            get
            {
                foreach (var t in Tiles)
                    if (t == Tile.SpikesOdd || t == Tile.SpikesEven) return true;
                return false;
            }
        }

        public static Vec2i WindDirection(Tile t)
        {
            switch (t)
            {
                case Tile.WindUp: return new Vec2i(0, -1);
                case Tile.WindDown: return new Vec2i(0, 1);
                case Tile.WindLeft: return new Vec2i(-1, 0);
                case Tile.WindRight: return new Vec2i(1, 0);
                default: return new Vec2i(0, 0);
            }
        }

        public static bool IsWind(Tile t) =>
            t == Tile.WindUp || t == Tile.WindDown || t == Tile.WindLeft || t == Tile.WindRight;
    }
}
