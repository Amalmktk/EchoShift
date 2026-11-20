using System.Collections.Generic;
using System.Linq;
using EchoShift.Simulation;
using UnityEngine;

namespace EchoShift.Game
{
    /// <summary>
    /// Loads every level file from Resources/Levels at start-up. Monthly updates only add files.
    /// </summary>
    public class LevelCatalog
    {
        public readonly List<LevelDef> All = new List<LevelDef>();

        public void Load()
        {
            All.Clear();
            foreach (var asset in Resources.LoadAll<TextAsset>("Levels"))
            {
                try
                {
                    All.Add(LevelParser.Parse(asset.text));
                }
                catch (LevelParseException e)
                {
                    Debug.LogError("Echo Shift: skipping level file '" + asset.name + "': " + e.Message);
                }
            }
            All.Sort((a, b) => a.Number.CompareTo(b.Number));
        }

        public IEnumerable<int> Worlds => All.Select(l => l.World).Distinct().OrderBy(w => w);

        public List<LevelDef> InWorld(int world) => All.Where(l => l.World == world).ToList();

        public LevelDef ById(string id) => All.FirstOrDefault(l => l.Id == id);

        public LevelDef Next(LevelDef level)
        {
            int i = All.IndexOf(level);
            return i >= 0 && i + 1 < All.Count ? All[i + 1] : null;
        }

        public LevelDef Previous(LevelDef level)
        {
            int i = All.IndexOf(level);
            return i > 0 ? All[i - 1] : null;
        }

        public bool IsWorldUnlocked(int world, SaveSystem save)
        {
            if (world <= Worlds.FirstOrDefault()) return true;
            var previous = InWorld(world - 1);
            if (previous.Count == 0) return false;
            var boss = previous.LastOrDefault(l => l.IsBoss) ?? previous.Last();
            return save.IsCompleted(boss.Id);
        }

        public bool IsLevelUnlocked(LevelDef level, SaveSystem save)
        {
            if (!IsWorldUnlocked(level.World, save)) return false;
            var prev = Previous(level);
            if (prev == null || prev.World != level.World) return true;
            return save.IsCompleted(prev.Id);
        }

        /// <summary>The first unlocked level not yet completed, or the last level.</summary>
        public LevelDef ContinueLevel(SaveSystem save)
        {
            foreach (var l in All)
                if (!save.IsCompleted(l.Id) && IsLevelUnlocked(l, save)) return l;
            return All.LastOrDefault();
        }

        public int StarsInWorld(int world, SaveSystem save)
        {
            int total = 0;
            foreach (var l in InWorld(world))
            {
                var p = save.Progress(l.Id);
                if (p != null) total += p.stars;
            }
            return total;
        }
    }
}
