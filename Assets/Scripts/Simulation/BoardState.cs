using System;

namespace EchoShift.Simulation
{
    /// <summary>
    /// Everything that changes during play. Copied once per turn so Undo can restore it exactly.
    /// </summary>
    public sealed class BoardState
    {
        public int Turn;
        public Vec2i Player;
        public Vec2i[] Echoes = new Vec2i[0];

        /// <summary>Per device: pressed (plate), on (switch), open (door, bridge), running (timed switch).</summary>
        public bool[] DeviceActive = new bool[0];
        public int[] DeviceTimer = new int[0];
        public bool[] ChannelActive = new bool[LevelDef.ChannelCount];

        /// <summary>Per tile: 0 intact, 1 stepped on, 2 broken. Only used by Crumble tiles.</summary>
        public byte[] Crumble = new byte[0];

        public Vec2i[] PlatformPos = new Vec2i[0];
        public int[] PlatformDir = new int[0];

        public bool ExitOpen;
        public bool CrystalTaken;
        public GameStatus Status;
        public string FailCause = "";

        public BoardState Clone()
        {
            return new BoardState
            {
                Turn = Turn,
                Player = Player,
                Echoes = (Vec2i[])Echoes.Clone(),
                DeviceActive = (bool[])DeviceActive.Clone(),
                DeviceTimer = (int[])DeviceTimer.Clone(),
                ChannelActive = (bool[])ChannelActive.Clone(),
                Crumble = (byte[])Crumble.Clone(),
                PlatformPos = (Vec2i[])PlatformPos.Clone(),
                PlatformDir = (int[])PlatformDir.Clone(),
                ExitOpen = ExitOpen,
                CrystalTaken = CrystalTaken,
                Status = Status,
                FailCause = FailCause
            };
        }
    }

    public enum SimEventKind : byte
    {
        Moved,
        Bumped,
        Pushed,
        Carried,
        Toggled,
        DeviceChanged,
        ExitChanged,
        Crumbled,
        CrystalTaken,
        Won,
        Failed
    }

    /// <summary>Something that happened during a turn, for sound, haptics and analytics.</summary>
    public struct SimEvent
    {
        public SimEventKind Kind;
        /// <summary>-1 for the player, 0..n for echoes, or a device index for device events.</summary>
        public int Index;
        public Vec2i From;
        public Vec2i To;
        public bool Active;

        public SimEvent(SimEventKind kind, int index, Vec2i from, Vec2i to, bool active = false)
        {
            Kind = kind;
            Index = index;
            From = from;
            To = to;
            Active = active;
        }
    }
}
