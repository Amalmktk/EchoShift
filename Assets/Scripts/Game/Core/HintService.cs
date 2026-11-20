using System.Collections.Generic;
using System.Text;
using EchoShift.Simulation;

namespace EchoShift.Game
{
    /// <summary>
    /// Three hint steps per level. Step 1 is free; steps 2 and 3 cost one hint each.
    /// Unlocked steps stay unlocked for the rest of the session.
    /// </summary>
    public class HintService
    {
        readonly SaveSystem _save;
        readonly Dictionary<string, int> _unlocked = new Dictionary<string, int>();

        public HintService(SaveSystem save)
        {
            _save = save;
        }

        public int Balance => _save.Data.hints;

        public int UnlockedSteps(string levelId) => _unlocked.TryGetValue(levelId, out var n) ? n : 0;

        public int NextStep(string levelId) => UnlockedSteps(levelId) + 1;

        /// <summary>True if the next step can be shown right now without an ad.</summary>
        public bool CanUnlockNext(string levelId)
        {
            int next = NextStep(levelId);
            if (next > 3) return false;
            return next == 1 || Balance > 0;
        }

        /// <summary>Unlocks the next step, spending a hint when needed. Returns false if the player has none.</summary>
        public bool UnlockNext(string levelId)
        {
            int next = NextStep(levelId);
            if (next > 3) return false;
            if (next > 1)
            {
                if (Balance <= 0) return false;
                _save.Data.hints--;
                _save.Save();
            }
            _unlocked[levelId] = next;
            return true;
        }

        public void Grant(int count)
        {
            _save.Data.hints += count;
            _save.Save();
        }

        public bool DailyHintAvailable => _save.Data.lastDailyHint != SaveSystem.Today();

        public bool ClaimDailyHint()
        {
            if (!DailyHintAvailable) return false;
            _save.Data.lastDailyHint = SaveSystem.Today();
            Grant(GameConfig.DailyFreeHints);
            return true;
        }

        /// <summary>Hint 3: the hint text plus the saved solution as arrows.</summary>
        public static string SolutionText(LevelDef level)
        {
            var sb = new StringBuilder();
            sb.Append(level.Hints[2]);
            sb.Append("\n\n");
            for (int r = 0; r < level.Solution.Count; r++)
            {
                var arrows = new StringBuilder();
                foreach (var m in level.Solution[r]) arrows.Append(Arrow(m)).Append(' ');
                string line = Strings.T("hint.run", r + 1, arrows.ToString().Trim());
                if (r < level.Solution.Count - 1) line += ", " + Strings.T("hint.then_echo");
                sb.Append(line).Append('\n');
            }
            return sb.ToString().TrimEnd();
        }

        static string Arrow(Move m)
        {
            switch (m)
            {
                case Move.Up: return "↑";
                case Move.Down: return "↓";
                case Move.Left: return "←";
                case Move.Right: return "→";
                default: return "Wait";
            }
        }
    }
}
