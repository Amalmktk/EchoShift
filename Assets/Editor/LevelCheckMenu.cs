using EchoShift.Simulation;
using UnityEditor;
using UnityEngine;

namespace EchoShift.EditorTools
{
    /// <summary>Replays every level's saved solution inside Unity. The same check runs on GitHub for each push.</summary>
    public static class LevelCheckMenu
    {
        [MenuItem("Echo Shift/Check All Levels", priority = 40)]
        public static void CheckAll()
        {
            int ok = 0, failed = 0;
            foreach (var asset in Resources.LoadAll<TextAsset>("Levels"))
            {
                string error = Check(asset.text, out string label);
                if (error == null)
                {
                    ok++;
                }
                else
                {
                    failed++;
                    Debug.LogError("Echo Shift level check FAILED: " + label + ": " + error, asset);
                }
            }
            string summary = "Echo Shift: " + ok + " levels passed, " + failed + " failed.";
            if (failed == 0) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        static string Check(string text, out string label)
        {
            LevelDef level;
            label = "?";
            try
            {
                level = LevelParser.Parse(text);
            }
            catch (LevelParseException e)
            {
                return e.Message;
            }
            label = level.Number + " " + level.Id + " " + level.Name;

            var sim = new GridSimulation(level);
            for (int r = 0; r < level.Solution.Count; r++)
            {
                bool last = r == level.Solution.Count - 1;
                foreach (var m in level.Solution[r])
                {
                    if (sim.Step(m) != StepResult.Ok) return "run " + (r + 1) + " could not take a move";
                    if (sim.State.Status == GameStatus.Failed) return "run " + (r + 1) + " fails (" + sim.State.FailCause + ")";
                    if (sim.State.Status == GameStatus.Won) break;
                }
                if (!last && !sim.MakeEcho()) return "could not make echo after run " + (r + 1);
            }
            if (sim.State.Status != GameStatus.Won) return "solution does not win";
            if (sim.Stars() != 3) return "solution earns " + sim.Stars() + " stars, expected 3";
            return null;
        }
    }
}
