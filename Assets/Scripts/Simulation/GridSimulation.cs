using System.Collections.Generic;

namespace EchoShift.Simulation
{
    /// <summary>
    /// The whole puzzle as plain C#. No Unity types, so it runs in tests and in the game alike.
    ///
    /// Each turn resolves in a fixed order:
    /// 1. the player moves, 2. echoes move (oldest first), 3. platforms move,
    /// 4. devices update, 5. crumbling tiles break, 6. hazards, 7. crystal, 8. win.
    /// </summary>
    public sealed class GridSimulation
    {
        public const int PlayerIndex = -1;

        public readonly LevelDef Level;
        public BoardState State { get; private set; }

        /// <summary>Committed runs. Echo i replays Echoes[i] from turn 1.</summary>
        public readonly List<List<Move>> Echoes = new List<List<Move>>();
        public readonly List<Move> CurrentRun = new List<Move>();
        public readonly List<SimEvent> Events = new List<SimEvent>();

        readonly Stack<BoardState> _undo = new Stack<BoardState>();

        public GridSimulation(LevelDef level)
        {
            Level = level;
            Reset();
        }

        public int EchoCount => Echoes.Count;
        public int MovesLeft => Level.MovesPerRun - CurrentRun.Count;
        public bool CanUndo => _undo.Count > 0;
        public bool CanEcho =>
            State.Status == GameStatus.Playing && CurrentRun.Count > 0 && Echoes.Count < Level.EchoLimit;

        // ------------------------------------------------------------------ commands

        public StepResult Step(Move move)
        {
            Events.Clear();
            if (State.Status != GameStatus.Playing) return StepResult.NotPlaying;
            if (CurrentRun.Count >= Level.MovesPerRun) return StepResult.LimitReached;

            _undo.Push(State.Clone());
            CurrentRun.Add(move);
            Resolve(move);
            return StepResult.Ok;
        }

        public bool Undo()
        {
            Events.Clear();
            if (_undo.Count == 0) return false;
            State = _undo.Pop();
            CurrentRun.RemoveAt(CurrentRun.Count - 1);
            return true;
        }

        /// <summary>Turns the current run into an echo, then sends everyone back to the start.</summary>
        public bool MakeEcho()
        {
            Events.Clear();
            if (!CanEcho) return false;
            Echoes.Add(new List<Move>(CurrentRun));
            CurrentRun.Clear();
            _undo.Clear();
            Reset();
            return true;
        }

        /// <summary>Removes every echo and resets the room.</summary>
        public void Restart()
        {
            Events.Clear();
            Echoes.Clear();
            CurrentRun.Clear();
            _undo.Clear();
            Reset();
        }

        public int Stars() => StarRules.Compute(Level, Echoes.Count, State.CrystalTaken);

        // ------------------------------------------------------------------ setup

        void Reset()
        {
            var s = new BoardState
            {
                Turn = 0,
                Player = Level.Start,
                Echoes = new Vec2i[Echoes.Count],
                DeviceActive = new bool[Level.Devices.Count],
                DeviceTimer = new int[Level.Devices.Count],
                ChannelActive = new bool[LevelDef.ChannelCount],
                Crumble = new byte[Level.Width * Level.Height],
                PlatformPos = new Vec2i[Level.Platforms.Count],
                PlatformDir = new int[Level.Platforms.Count],
                Status = GameStatus.Playing
            };
            for (int i = 0; i < s.Echoes.Length; i++) s.Echoes[i] = Level.Start;
            for (int i = 0; i < Level.Platforms.Count; i++)
            {
                s.PlatformPos[i] = Level.Platforms[i].Pos;
                s.PlatformDir[i] = 1;
            }
            State = s;
            UpdateDevices(false);
        }

        // ------------------------------------------------------------------ turn

        void Resolve(Move playerMove)
        {
            var s = State;
            s.Turn++;
            int moveIndex = s.Turn - 1;

            MoveEntity(PlayerIndex, playerMove);
            for (int i = 0; i < Echoes.Count; i++)
            {
                var run = Echoes[i];
                MoveEntity(i, moveIndex < run.Count ? run[moveIndex] : Move.Wait);
            }

            AdvancePlatforms();
            UpdateDevices(true);
            UpdateCrumble();

            // Hazards only affect the player. Echoes are ghosts.
            if (IsSpikeUp(s.Player, s.Turn))
            {
                Fail("spikes");
                return;
            }
            if (!IsGroundUnder(s.Player))
            {
                Fail("fell");
                return;
            }

            if (Level.HasCrystal && !s.CrystalTaken && s.Player == Level.Crystal)
            {
                s.CrystalTaken = true;
                Events.Add(new SimEvent(SimEventKind.CrystalTaken, PlayerIndex, s.Player, s.Player));
            }

            if (s.Player == Level.Exit && s.ExitOpen)
            {
                s.Status = GameStatus.Won;
                Events.Add(new SimEvent(SimEventKind.Won, PlayerIndex, s.Player, s.Player));
            }
        }

        void Fail(string cause)
        {
            State.Status = GameStatus.Failed;
            State.FailCause = cause;
            Events.Add(new SimEvent(SimEventKind.Failed, PlayerIndex, State.Player, State.Player));
        }

        Vec2i GetPos(int entity) => entity == PlayerIndex ? State.Player : State.Echoes[entity];

        void SetPos(int entity, Vec2i p)
        {
            if (entity == PlayerIndex) State.Player = p;
            else State.Echoes[entity] = p;
        }

        void MoveEntity(int entity, Move move)
        {
            if (move == Move.Wait) return;

            var from = GetPos(entity);
            var target = from + Moves.Delta(move);

            int dev = DeviceAt(target);
            if (dev >= 0 && (Level.Devices[dev].Kind == DeviceKind.Switch || Level.Devices[dev].Kind == DeviceKind.TimedSwitch))
            {
                BumpSwitch(dev);
                Events.Add(new SimEvent(SimEventKind.Bumped, entity, from, target));
                return;
            }

            if (!IsWalkable(target))
            {
                Events.Add(new SimEvent(SimEventKind.Bumped, entity, from, target));
                return;
            }

            SetPos(entity, target);
            MarkSteppedOff(from);
            Events.Add(new SimEvent(SimEventKind.Moved, entity, from, target));

            // Wind pushes once, only when entering a wind tile.
            var tile = Level.TileAt(target);
            if (LevelDef.IsWind(tile))
            {
                var pushTo = target + LevelDef.WindDirection(tile);
                int pushDev = DeviceAt(pushTo);
                bool pushBlockedBySwitch = pushDev >= 0 &&
                    (Level.Devices[pushDev].Kind == DeviceKind.Switch || Level.Devices[pushDev].Kind == DeviceKind.TimedSwitch);
                if (!pushBlockedBySwitch && IsWalkable(pushTo))
                {
                    SetPos(entity, pushTo);
                    Events.Add(new SimEvent(SimEventKind.Pushed, entity, target, pushTo));
                }
            }
        }

        void BumpSwitch(int dev)
        {
            var d = Level.Devices[dev];
            if (d.Kind == DeviceKind.Switch)
            {
                State.DeviceActive[dev] = !State.DeviceActive[dev];
            }
            else
            {
                // +1 because the device update later this turn counts one down.
                State.DeviceTimer[dev] = Level.TimerTurns + 1;
            }
            Events.Add(new SimEvent(SimEventKind.Toggled, dev, d.Pos, d.Pos, State.DeviceActive[dev]));
        }

        void MarkSteppedOff(Vec2i p)
        {
            if (Level.TileAt(p) != Tile.Crumble) return;
            int i = Level.Index(p);
            if (State.Crumble[i] == 0) State.Crumble[i] = 1;
        }

        void AdvancePlatforms()
        {
            var s = State;
            for (int k = 0; k < Level.Platforms.Count; k++)
            {
                var def = Level.Platforms[k];
                var pos = s.PlatformPos[k];
                var step = def.Horizontal ? new Vec2i(s.PlatformDir[k], 0) : new Vec2i(0, s.PlatformDir[k]);
                var next = pos + step;
                if (!CanPlatformEnter(next, k))
                {
                    s.PlatformDir[k] = -s.PlatformDir[k];
                    next = pos - step;
                    if (!CanPlatformEnter(next, k)) continue;
                }

                // Carry anything standing on the platform.
                if (s.Player == pos)
                {
                    s.Player = next;
                    Events.Add(new SimEvent(SimEventKind.Carried, PlayerIndex, pos, next));
                }
                for (int e = 0; e < s.Echoes.Length; e++)
                {
                    if (s.Echoes[e] != pos) continue;
                    s.Echoes[e] = next;
                    Events.Add(new SimEvent(SimEventKind.Carried, e, pos, next));
                }
                s.PlatformPos[k] = next;
            }
        }

        bool CanPlatformEnter(Vec2i p, int self)
        {
            if (!Level.InBounds(p) || Level.TileAt(p) != Tile.Gap) return false;
            if (DeviceAt(p) >= 0) return false;
            for (int k = 0; k < State.PlatformPos.Length; k++)
                if (k != self && State.PlatformPos[k] == p) return false;
            return true;
        }

        void UpdateDevices(bool emitEvents)
        {
            var s = State;
            var devices = Level.Devices;

            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                if (d.Kind == DeviceKind.Plate)
                {
                    s.DeviceActive[i] = IsOccupied(d.Pos);
                }
                else if (d.Kind == DeviceKind.TimedSwitch)
                {
                    if (s.DeviceTimer[i] > 0) s.DeviceTimer[i]--;
                    s.DeviceActive[i] = s.DeviceTimer[i] > 0;
                }
            }

            for (int c = 0; c < LevelDef.ChannelCount; c++) s.ChannelActive[c] = false;
            for (int i = 0; i < devices.Count; i++)
            {
                var k = devices[i].Kind;
                if ((k == DeviceKind.Plate || k == DeviceKind.Switch || k == DeviceKind.TimedSwitch) && s.DeviceActive[i])
                    s.ChannelActive[devices[i].Channel] = true;
            }

            for (int i = 0; i < devices.Count; i++)
            {
                var d = devices[i];
                if (d.Kind != DeviceKind.Door && d.Kind != DeviceKind.Bridge) continue;
                bool want = s.ChannelActive[d.Channel];
                // A door or bridge never closes on something standing in it.
                if (!want && s.DeviceActive[i] && IsOccupied(d.Pos)) want = true;
                if (want != s.DeviceActive[i])
                {
                    s.DeviceActive[i] = want;
                    if (emitEvents) Events.Add(new SimEvent(SimEventKind.DeviceChanged, i, d.Pos, d.Pos, want));
                }
            }

            bool open = true;
            foreach (var c in Level.ExitChannels)
                if (!s.ChannelActive[c]) open = false;
            if (!open && s.ExitOpen && IsOccupied(Level.Exit)) open = true;
            if (open != s.ExitOpen)
            {
                s.ExitOpen = open;
                if (emitEvents) Events.Add(new SimEvent(SimEventKind.ExitChanged, -1, Level.Exit, Level.Exit, open));
            }
        }

        void UpdateCrumble()
        {
            var s = State;
            for (int i = 0; i < s.Crumble.Length; i++)
            {
                if (s.Crumble[i] != 1) continue;
                var p = new Vec2i(i % Level.Width, i / Level.Width);
                if (IsOccupied(p)) continue;
                s.Crumble[i] = 2;
                Events.Add(new SimEvent(SimEventKind.Crumbled, -1, p, p));
            }
        }

        // ------------------------------------------------------------------ queries

        public int DeviceAt(Vec2i p)
        {
            var devices = Level.Devices;
            for (int i = 0; i < devices.Count; i++)
                if (devices[i].Pos == p) return i;
            return -1;
        }

        public int PlatformAt(Vec2i p)
        {
            for (int k = 0; k < State.PlatformPos.Length; k++)
                if (State.PlatformPos[k] == p) return k;
            return -1;
        }

        public bool IsOccupied(Vec2i p)
        {
            if (State.Player == p) return true;
            foreach (var e in State.Echoes)
                if (e == p) return true;
            return false;
        }

        /// <summary>True if something can stand on this tile right now.</summary>
        public bool IsGroundUnder(Vec2i p)
        {
            if (!Level.InBounds(p)) return false;
            var tile = Level.TileAt(p);
            if (tile == Tile.Wall) return false;

            if (tile == Tile.Gap)
            {
                if (PlatformAt(p) >= 0) return true;
                int dev = DeviceAt(p);
                return dev >= 0 && Level.Devices[dev].Kind == DeviceKind.Bridge && State.DeviceActive[dev];
            }

            if (tile == Tile.Crumble && State.Crumble[Level.Index(p)] == 2)
                return PlatformAt(p) >= 0;

            return true;
        }

        /// <summary>True if the player or an echo may step onto this tile this turn.</summary>
        public bool IsWalkable(Vec2i p)
        {
            if (!IsGroundUnder(p)) return false;
            int dev = DeviceAt(p);
            if (dev >= 0)
            {
                var d = Level.Devices[dev];
                if (d.Kind == DeviceKind.Switch || d.Kind == DeviceKind.TimedSwitch) return false;
                if (d.Kind == DeviceKind.Door && !State.DeviceActive[dev]) return false;
            }
            if (p == Level.Exit && !State.ExitOpen) return false;
            return true;
        }

        public bool IsSpikeUp(Vec2i p, int turn)
        {
            var tile = Level.TileAt(p);
            if (tile == Tile.Spikes) return true;
            if (tile == Tile.SpikesOdd) return turn % 2 == 1;
            if (tile == Tile.SpikesEven) return turn % 2 == 0;
            return false;
        }
    }

    public static class StarRules
    {
        /// <summary>One star for finishing, two with the crystal, three with the crystal at or under par echoes.</summary>
        public static int Compute(LevelDef level, int echoesUsed, bool crystalTaken)
        {
            int stars = 1;
            if (!level.HasCrystal || crystalTaken) stars = 2;
            if (stars == 2 && echoesUsed <= level.ParEchoes) stars = 3;
            return stars;
        }
    }
}
