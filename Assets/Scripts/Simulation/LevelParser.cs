using System;
using System.Collections.Generic;
using System.Globalization;

namespace EchoShift.Simulation
{
    public sealed class LevelParseException : Exception
    {
        public LevelParseException(string message) : base(message) { }
    }

    /// <summary>
    /// Reads the plain-text level format described in docs/LEVELS.md.
    /// </summary>
    public static class LevelParser
    {
        public static LevelDef Parse(string text)
        {
            var level = new LevelDef();
            var mapRows = new List<string>();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool inMap = false;
            bool sawEchoes = false, sawPar = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                string line = raw.TrimEnd();
                if (line.TrimStart().StartsWith("//")) continue;

                if (inMap)
                {
                    if (line.Length == 0 || IsKeyLine(line))
                    {
                        inMap = false;
                    }
                    else
                    {
                        mapRows.Add(line.Trim());
                        continue;
                    }
                }

                if (line.Trim().Length == 0) continue;

                int colon = line.IndexOf(':');
                if (colon <= 0) throw new LevelParseException(Where(level, i) + "expected 'key: value' but found '" + line + "'");
                string key = line.Substring(0, colon).Trim().ToLowerInvariant();
                string value = line.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "id": level.Id = value; break;
                    case "name": level.Name = value; break;
                    case "world": level.World = ParseInt(value, level, i); break;
                    case "number": level.Number = ParseInt(value, level, i); break;
                    case "echoes": level.EchoLimit = ParseInt(value, level, i); sawEchoes = true; break;
                    case "par": level.ParEchoes = ParseInt(value, level, i); sawPar = true; break;
                    case "moves": level.MovesPerRun = ParseInt(value, level, i); break;
                    case "timer": level.TimerTurns = ParseInt(value, level, i); break;
                    case "needs_echo": level.NeedsEcho = ParseBool(value, level, i); break;
                    case "boss": level.IsBoss = ParseBool(value, level, i); break;
                    case "exit": level.ExitChannels = ParseChannels(value, level, i); break;
                    case "hint1": level.Hints[0] = value; break;
                    case "hint2": level.Hints[1] = value; break;
                    case "hint3": level.Hints[2] = value; break;
                    case "solution": level.Solution = ParseSolution(value, level, i); break;
                    case "map":
                        inMap = true;
                        if (value.Length > 0) throw new LevelParseException(Where(level, i) + "map rows start on the line after 'map:'");
                        break;
                    default:
                        throw new LevelParseException(Where(level, i) + "unknown key '" + key + "'");
                }
            }

            if (string.IsNullOrEmpty(level.Id)) throw new LevelParseException("level has no id");
            if (mapRows.Count == 0) throw new LevelParseException(level.Id + ": level has no map");
            if (!sawEchoes) level.EchoLimit = level.NeedsEcho ? 1 : 0;
            if (!sawPar) level.ParEchoes = level.EchoLimit;

            BuildMap(level, mapRows);
            return level;
        }

        static bool IsKeyLine(string line)
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) return false;
            for (int i = 0; i < colon; i++)
            {
                char c = line[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == ' ';
                if (!ok) return false;
            }
            return char.IsLetter(line.TrimStart()[0]);
        }

        static void BuildMap(LevelDef level, List<string> rows)
        {
            level.Height = rows.Count;
            level.Width = rows[0].Length;
            level.Tiles = new Tile[level.Width * level.Height];
            bool sawStart = false, sawExit = false;

            for (int y = 0; y < rows.Count; y++)
            {
                if (rows[y].Length != level.Width)
                    throw new LevelParseException(level.Id + ": map row " + (y + 1) + " is " + rows[y].Length + " wide, expected " + level.Width);

                for (int x = 0; x < level.Width; x++)
                {
                    char c = rows[y][x];
                    var p = new Vec2i(x, y);
                    Tile tile = Tile.Floor;

                    switch (c)
                    {
                        case '.': break;
                        case '#': tile = Tile.Wall; break;
                        case '~': tile = Tile.Gap; break;
                        case '^': tile = Tile.Spikes; break;
                        case '!': tile = Tile.SpikesOdd; break;
                        case ';': tile = Tile.SpikesEven; break;
                        case '%': tile = Tile.Crumble; break;
                        case 'u': tile = Tile.WindUp; break;
                        case 'v': tile = Tile.WindDown; break;
                        case '<': tile = Tile.WindLeft; break;
                        case '>': tile = Tile.WindRight; break;
                        case 'S':
                            if (sawStart) throw new LevelParseException(level.Id + ": more than one start 'S'");
                            level.Start = p; sawStart = true; break;
                        case 'X':
                            if (sawExit) throw new LevelParseException(level.Id + ": more than one exit 'X'");
                            level.Exit = p; sawExit = true; break;
                        case '*':
                            if (level.HasCrystal) throw new LevelParseException(level.Id + ": more than one crystal '*'");
                            level.HasCrystal = true; level.Crystal = p; break;
                        case '=':
                            tile = Tile.Gap;
                            level.Platforms.Add(new PlatformDef { Pos = p, Horizontal = true });
                            break;
                        case '|':
                            tile = Tile.Gap;
                            level.Platforms.Add(new PlatformDef { Pos = p, Horizontal = false });
                            break;
                        default:
                            if (c >= 'a' && c <= 'e') AddDevice(level, DeviceKind.Plate, p, c - 'a');
                            else if (c >= 'A' && c <= 'E') AddDevice(level, DeviceKind.Door, p, c - 'A');
                            else if (c >= '1' && c <= '5') AddDevice(level, DeviceKind.Switch, p, c - '1');
                            else if (c >= '6' && c <= '9') AddDevice(level, DeviceKind.TimedSwitch, p, c - '6');
                            else if (c == '0') AddDevice(level, DeviceKind.TimedSwitch, p, 4);
                            else if (c >= 'f' && c <= 'j') { tile = Tile.Gap; AddDevice(level, DeviceKind.Bridge, p, c - 'f'); }
                            else throw new LevelParseException(level.Id + ": unknown map character '" + c + "' at row " + (y + 1) + ", column " + (x + 1));
                            break;
                    }

                    level.Tiles[level.Index(p)] = tile;
                }
            }

            if (!sawStart) throw new LevelParseException(level.Id + ": map has no start 'S'");
            if (!sawExit) throw new LevelParseException(level.Id + ": map has no exit 'X'");
        }

        static void AddDevice(LevelDef level, DeviceKind kind, Vec2i p, int channel)
        {
            level.Devices.Add(new DeviceDef { Kind = kind, Pos = p, Channel = channel });
        }

        static List<int> ParseChannels(string value, LevelDef level, int line)
        {
            var list = new List<int>();
            foreach (var part in value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.Length != 1 || part[0] < 'a' || part[0] > 'e')
                    throw new LevelParseException(Where(level, line) + "exit channels must be letters a..e, found '" + part + "'");
                list.Add(part[0] - 'a');
            }
            return list;
        }

        public static List<List<Move>> ParseSolution(string value, LevelDef level, int line)
        {
            var runs = new List<List<Move>>();
            foreach (var part in value.Split('|'))
            {
                var run = new List<Move>();
                foreach (char c in part)
                {
                    if (char.IsWhiteSpace(c)) continue;
                    if (!Moves.TryParse(c, out var m))
                        throw new LevelParseException(Where(level, line) + "solution has unknown move '" + c + "'");
                    run.Add(m);
                }
                runs.Add(run);
            }
            return runs;
        }

        static int ParseInt(string value, LevelDef level, int line)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                throw new LevelParseException(Where(level, line) + "expected a number, found '" + value + "'");
            return n;
        }

        static bool ParseBool(string value, LevelDef level, int line)
        {
            switch (value.ToLowerInvariant())
            {
                case "true": case "yes": case "1": return true;
                case "false": case "no": case "0": return false;
                default: throw new LevelParseException(Where(level, line) + "expected true or false, found '" + value + "'");
            }
        }

        static string Where(LevelDef level, int line) =>
            (string.IsNullOrEmpty(level.Id) ? "level" : level.Id) + " line " + (line + 1) + ": ";
    }
}
