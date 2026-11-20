using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EchoShift.Simulation;
using NUnit.Framework;

namespace EchoShift.Tests
{
    /// <summary>Checks every level file in Assets/Resources/Levels.</summary>
    public class LevelTests
    {
        static string LevelDir()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Resources", "Levels")))
                dir = dir.Parent;
            if (dir == null) throw new DirectoryNotFoundException("Could not find Assets/Resources/Levels above the test folder");
            return Path.Combine(dir.FullName, "Assets", "Resources", "Levels");
        }

        public static IEnumerable<string> LevelFiles()
        {
            return Directory.GetFiles(LevelDir(), "*.txt").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n);
        }

        static LevelDef Load(string name) => LevelParser.Parse(File.ReadAllText(Path.Combine(LevelDir(), name + ".txt")));

        [TestCaseSource(nameof(LevelFiles))]
        public void Parses_and_matches_file_name(string name)
        {
            var level = Load(name);
            Assert.AreEqual(name, level.Id);
            Assert.IsNotEmpty(level.Name);
            Assert.IsTrue(level.Hints.All(h => h.Length > 0), "every level needs three hints");
            Assert.LessOrEqual(level.ParEchoes, level.EchoLimit);
            Assert.LessOrEqual(level.Width, 9);
            Assert.LessOrEqual(level.Height, 12);
            if (level.World == 1)
            {
                Assert.LessOrEqual(level.Width, 7);
                Assert.LessOrEqual(level.Height, 9);
            }
        }

        [TestCaseSource(nameof(LevelFiles))]
        public void Saved_solution_wins_with_three_stars(string name)
        {
            var level = Load(name);
            Assert.IsNotEmpty(level.Solution, "level needs a solution");
            Assert.LessOrEqual(level.Solution.Count - 1, level.EchoLimit, "solution uses too many echoes");

            var sim = new GridSimulation(level);
            for (int r = 0; r < level.Solution.Count; r++)
            {
                var run = level.Solution[r];
                Assert.LessOrEqual(run.Count, level.MovesPerRun, "run " + (r + 1) + " is longer than the move limit");
                bool last = r == level.Solution.Count - 1;
                foreach (var m in run)
                {
                    Assert.AreEqual(StepResult.Ok, sim.Step(m));
                    Assert.AreNotEqual(GameStatus.Failed, sim.State.Status, "run " + (r + 1) + " fails on turn " + sim.State.Turn + " (" + sim.State.FailCause + ")");
                    if (sim.State.Status == GameStatus.Won)
                    {
                        Assert.IsTrue(last, "run " + (r + 1) + " already wins");
                        break;
                    }
                }
                if (!last) Assert.IsTrue(sim.MakeEcho(), "could not make echo after run " + (r + 1));
            }

            Assert.AreEqual(GameStatus.Won, sim.State.Status, "solution does not reach the open exit");
            Assert.AreEqual(3, sim.Stars(), "solution should earn three stars");
        }

        [TestCaseSource(nameof(LevelFiles))]
        public void Needs_echo_levels_cannot_be_won_alone(string name)
        {
            var level = Load(name);
            if (!level.NeedsEcho) Assert.Pass("level does not claim to need an echo");
            string path = SolveWithoutEcho(level);
            Assert.IsNull(path, "level is marked needs_echo but can be won with no echo: " + path);
        }

        [Test]
        public void Ids_and_numbers_are_unique_and_contiguous()
        {
            var levels = LevelFiles().Select(Load).ToList();
            Assert.AreEqual(levels.Count, levels.Select(l => l.Id).Distinct().Count(), "duplicate ids");
            var numbers = levels.Select(l => l.Number).OrderBy(n => n).ToList();
            CollectionAssert.AreEqual(Enumerable.Range(1, levels.Count).ToList(), numbers, "level numbers must run 1..N without gaps");
        }

        /// <summary>Breadth-first search over player-only play. Returns a winning move string, or null.</summary>
        static string SolveWithoutEcho(LevelDef level)
        {
            var frontier = new List<string> { "" };
            var seen = new HashSet<string> { Key(new GridSimulation(level), level) };
            const string moves = "UDLRW";

            for (int depth = 0; depth < level.MovesPerRun && frontier.Count > 0; depth++)
            {
                var next = new List<string>();
                foreach (var path in frontier)
                {
                    foreach (char c in moves)
                    {
                        var sim = Replay(level, path + c);
                        if (sim.State.Status == GameStatus.Won) return path + c;
                        if (sim.State.Status == GameStatus.Failed) continue;
                        if (seen.Add(Key(sim, level))) next.Add(path + c);
                    }
                }
                frontier = next;
            }
            return null;
        }

        static GridSimulation Replay(LevelDef level, string path)
        {
            var sim = new GridSimulation(level);
            foreach (char c in path)
            {
                Moves.TryParse(c, out var m);
                sim.Step(m);
                if (sim.State.Status != GameStatus.Playing) break;
            }
            return sim;
        }

        static string Key(GridSimulation sim, LevelDef level)
        {
            var s = sim.State;
            var sb = new StringBuilder();
            sb.Append(s.Player).Append('|');
            foreach (var a in s.DeviceActive) sb.Append(a ? '1' : '0');
            sb.Append('|');
            foreach (var t in s.DeviceTimer) sb.Append(t).Append(',');
            sb.Append('|');
            foreach (var c in s.Crumble) sb.Append(c);
            sb.Append('|');
            for (int i = 0; i < s.PlatformPos.Length; i++) sb.Append(s.PlatformPos[i]).Append(s.PlatformDir[i]);
            if (level.HasRhythmSpikes) sb.Append('|').Append(s.Turn % 2);
            return sb.ToString();
        }
    }
}
