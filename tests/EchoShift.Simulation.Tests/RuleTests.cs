using EchoShift.Simulation;
using NUnit.Framework;

namespace EchoShift.Tests
{
    /// <summary>Small boards that pin down each rule from docs/LEVELS.md.</summary>
    public class RuleTests
    {
        static GridSimulation Sim(string header, params string[] map)
        {
            var text = "id: test\nname: Test\nhint1: a\nhint2: b\nhint3: c\n" + header + "\nmap:\n" + string.Join("\n", map) + "\nsolution: W\n";
            return new GridSimulation(LevelParser.Parse(text));
        }

        static Vec2i P(int x, int y) => new Vec2i(x, y);

        [Test]
        public void Swipe_moves_one_tile_and_walls_block()
        {
            var sim = Sim("echoes: 0", "#####", "#S.X#", "#####");
            sim.Step(Move.Up);
            Assert.AreEqual(P(1, 1), sim.State.Player);
            sim.Step(Move.Right);
            Assert.AreEqual(P(2, 1), sim.State.Player);
            Assert.AreEqual(2, sim.State.Turn, "a bump still spends a turn");
        }

        [Test]
        public void Reaching_open_exit_wins()
        {
            var sim = Sim("echoes: 0", "#####", "#S.X#", "#####");
            sim.Step(Move.Right);
            sim.Step(Move.Right);
            Assert.AreEqual(GameStatus.Won, sim.State.Status);
            Assert.AreEqual(StepResult.NotPlaying, sim.Step(Move.Left));
        }

        [Test]
        public void Closed_exit_blocks()
        {
            var sim = Sim("echoes: 0\nexit: a", "######", "#S.X.#", "#...a#", "######");
            sim.Step(Move.Right);
            sim.Step(Move.Right);
            Assert.AreEqual(P(2, 1), sim.State.Player);
            Assert.AreEqual(GameStatus.Playing, sim.State.Status);
        }

        [Test]
        public void Undo_restores_the_previous_turn()
        {
            var sim = Sim("echoes: 0", "######", "#S..X#", "######");
            sim.Step(Move.Right);
            Assert.IsTrue(sim.Undo());
            Assert.AreEqual(P(1, 1), sim.State.Player);
            Assert.AreEqual(0, sim.State.Turn);
            Assert.AreEqual(0, sim.CurrentRun.Count);
            Assert.IsFalse(sim.Undo());
        }

        [Test]
        public void Move_limit_stops_the_run()
        {
            var sim = Sim("echoes: 0\nmoves: 2", "#######", "#S...X#", "#######");
            Assert.AreEqual(StepResult.Ok, sim.Step(Move.Right));
            Assert.AreEqual(StepResult.Ok, sim.Step(Move.Right));
            Assert.AreEqual(StepResult.LimitReached, sim.Step(Move.Right));
            Assert.AreEqual(P(3, 1), sim.State.Player);
        }

        [Test]
        public void Echo_repeats_moves_then_stays()
        {
            var sim = Sim("echoes: 1", "#######", "#S...X#", "#######");
            sim.Step(Move.Right);
            sim.Step(Move.Right);
            Assert.IsTrue(sim.MakeEcho());
            Assert.AreEqual(P(1, 1), sim.State.Player);
            Assert.AreEqual(P(1, 1), sim.State.Echoes[0]);

            sim.Step(Move.Wait);
            Assert.AreEqual(P(2, 1), sim.State.Echoes[0]);
            sim.Step(Move.Wait);
            Assert.AreEqual(P(3, 1), sim.State.Echoes[0]);
            sim.Step(Move.Wait);
            Assert.AreEqual(P(3, 1), sim.State.Echoes[0], "an echo with no moves left stays put");
        }

        [Test]
        public void Echo_limit_is_enforced()
        {
            var sim = Sim("echoes: 1", "#######", "#S...X#", "#######");
            Assert.IsFalse(sim.MakeEcho(), "an empty run can't become an echo");
            sim.Step(Move.Right);
            Assert.IsTrue(sim.MakeEcho());
            sim.Step(Move.Right);
            Assert.IsFalse(sim.MakeEcho());
        }

        [Test]
        public void Echo_bumps_into_closed_door_and_carries_on()
        {
            var sim = Sim("echoes: 1", "#######", "#S.A.X#", "#....a#", "#######");
            sim.Step(Move.Right);
            sim.Step(Move.Right); // blocked by the closed door
            sim.Step(Move.Down);
            Assert.AreEqual(P(2, 2), sim.State.Player);
            Assert.IsTrue(sim.MakeEcho());

            sim.Step(Move.Wait);
            sim.Step(Move.Wait);
            Assert.AreEqual(P(2, 1), sim.State.Echoes[0], "the echo bumped the door and stayed");
            sim.Step(Move.Wait);
            Assert.AreEqual(P(2, 2), sim.State.Echoes[0], "then carried on with its next move");
        }

        [Test]
        public void Door_opens_the_turn_after_its_plate_is_pressed()
        {
            var sim = Sim("echoes: 0", "######", "#Sa.X#", "#.#A##", "#....#", "######");
            sim.Step(Move.Right); // onto the plate
            Assert.IsTrue(sim.State.ChannelActive[0]);
            int door = sim.DeviceAt(P(3, 2));
            Assert.IsTrue(sim.State.DeviceActive[door]);
        }

        [Test]
        public void Door_never_closes_on_someone_standing_in_it()
        {
            var sim = Sim("echoes: 0", "#####", "#Sa##", "##A##", "##X##", "#####");
            sim.Step(Move.Right); // plate
            sim.Step(Move.Down);  // into the open door; plate released
            int door = sim.DeviceAt(P(2, 2));
            Assert.IsTrue(sim.State.DeviceActive[door]);
            sim.Step(Move.Down);
            Assert.AreEqual(GameStatus.Won, sim.State.Status);
        }

        [Test]
        public void Switch_toggles_when_bumped_and_is_not_walkable()
        {
            var sim = Sim("echoes: 0", "######", "#S1.X#", "#.A..#", "######");
            sim.Step(Move.Right);
            Assert.AreEqual(P(1, 1), sim.State.Player);
            Assert.IsTrue(sim.State.ChannelActive[0]);
            sim.Step(Move.Right);
            Assert.IsFalse(sim.State.ChannelActive[0], "a second bump turns it off");
        }

        [Test]
        public void Timed_switch_stays_on_for_timer_turns()
        {
            var sim = Sim("echoes: 0\ntimer: 3", "######", "#S6.X#", "######");
            sim.Step(Move.Right);
            Assert.IsTrue(sim.State.ChannelActive[0], "turn 1");
            sim.Step(Move.Wait);
            Assert.IsTrue(sim.State.ChannelActive[0], "turn 2");
            sim.Step(Move.Wait);
            Assert.IsTrue(sim.State.ChannelActive[0], "turn 3");
            sim.Step(Move.Wait);
            Assert.IsFalse(sim.State.ChannelActive[0], "turn 4");
        }

        [Test]
        public void Spikes_fail_the_player_but_not_echoes()
        {
            var sim = Sim("echoes: 1", "#######", "#S^..X#", "#.....#", "#######");
            sim.Step(Move.Right);
            Assert.AreEqual(GameStatus.Failed, sim.State.Status);
            Assert.AreEqual("spikes", sim.State.FailCause);
            Assert.IsTrue(sim.Undo());
            Assert.AreEqual(GameStatus.Playing, sim.State.Status, "undo recovers from a failure");
        }

        [Test]
        public void Rhythm_spikes_follow_turn_parity()
        {
            var sim = Sim("echoes: 0", "######", "#S!.X#", "######");
            sim.Step(Move.Right); // turn 1, odd spikes are up
            Assert.AreEqual(GameStatus.Failed, sim.State.Status);
            sim.Undo();
            sim.Step(Move.Wait);  // turn 1
            sim.Step(Move.Right); // turn 2, odd spikes are down
            Assert.AreEqual(GameStatus.Playing, sim.State.Status);
            Assert.AreEqual(P(2, 1), sim.State.Player);
        }

        [Test]
        public void Crumbling_floor_breaks_after_stepping_off()
        {
            var sim = Sim("echoes: 0", "######", "#S%.X#", "######");
            sim.Step(Move.Right);
            Assert.AreEqual(P(2, 1), sim.State.Player);
            sim.Step(Move.Right);
            sim.Step(Move.Left);
            Assert.AreEqual(P(3, 1), sim.State.Player, "the broken tile is now a gap");
        }

        [Test]
        public void Wind_pushes_one_extra_tile()
        {
            var sim = Sim("echoes: 0", "#######", "#S>..X#", "#######");
            sim.Step(Move.Right);
            Assert.AreEqual(P(3, 1), sim.State.Player);
        }

        [Test]
        public void Platform_carries_the_player_and_bounces()
        {
            var sim = Sim("echoes: 0", "#######", "#S=~.X#", "#######");
            sim.Step(Move.Right);
            Assert.AreEqual(P(3, 1), sim.State.Player, "carried one tile");
            sim.Step(Move.Right);
            Assert.AreEqual(P(4, 1), sim.State.Player);
            Assert.AreEqual(P(2, 1), sim.State.PlatformPos[0], "bounced back along its lane");
        }

        [Test]
        public void Bridge_spans_a_gap_while_its_channel_is_on()
        {
            var sim = Sim("echoes: 0", "######", "#Sf.X#", "#a...#", "######");
            sim.Step(Move.Right);
            Assert.AreEqual(P(1, 1), sim.State.Player, "bridge is retracted");
            sim.Undo();
            sim.Step(Move.Down);  // onto the plate
            sim.Step(Move.Up);    // bridge is open from last turn, plate released now
            sim.Step(Move.Right);
            Assert.AreEqual(P(1, 1), sim.State.Player, "bridge retracted once the plate was released");
        }

        [Test]
        public void Stars_follow_crystal_and_par()
        {
            var level = LevelParser.Parse("id: t\nname: T\nhint1: a\nhint2: b\nhint3: c\nechoes: 2\npar: 1\nmap:\n#####\n#S*X#\n#####\nsolution: RR\n");
            Assert.AreEqual(1, StarRules.Compute(level, 0, false));
            Assert.AreEqual(3, StarRules.Compute(level, 1, true));
            Assert.AreEqual(2, StarRules.Compute(level, 2, true));
        }
    }
}
