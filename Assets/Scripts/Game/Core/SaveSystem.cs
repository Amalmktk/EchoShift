using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EchoShift.Game
{
    [Serializable]
    public class LevelProgress
    {
        public string id;
        public bool completed;
        public int stars;
        public int bestMoves;
        public bool crystal;
    }

    [Serializable]
    public class SettingsData
    {
        public bool music = true;
        public bool sfx = true;
        public bool vibration = true;
        public bool reduceMotion;
        public bool dpad;
        public int buttonSize = 1;
        public bool leftHanded;
        public string language = "en";
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public int hints = GameConfig.StartingHints;
        public string lastDailyHint = "";
        public bool tutorialDone;
        public bool removeAds;
        public int interstitialsShown;
        public bool removeAdsOfferShown;
        public int levelsCompleted;
        public string lastReviewAsk = "";
        public SettingsData settings = new SettingsData();
    }

    /// <summary>
    /// One JSON file in persistentDataPath. Writes go to a temp file first, so a crash
    /// mid-write never corrupts the save.
    /// </summary>
    public class SaveSystem
    {
        public SaveData Data { get; private set; }
        public event Action Changed;

        string PathMain => Path.Combine(Application.persistentDataPath, "save.json");
        string PathTemp => Path.Combine(Application.persistentDataPath, "save.json.tmp");
        string PathBackup => Path.Combine(Application.persistentDataPath, "save.json.bak");

        public void Load()
        {
            Data = TryRead(PathMain) ?? TryRead(PathBackup) ?? new SaveData();
            Migrate(Data);
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Echo Shift: could not read save at " + path + ": " + e.Message);
                return null;
            }
        }

        static void Migrate(SaveData data)
        {
            if (data.levels == null) data.levels = new List<LevelProgress>();
            if (data.settings == null) data.settings = new SettingsData();
            // Future versions: upgrade older saves here, then bump data.version.
            data.version = 1;
        }

        public void Save()
        {
            try
            {
                var json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(PathTemp, json);
                if (File.Exists(PathMain)) File.Replace(PathTemp, PathMain, PathBackup);
                else File.Move(PathTemp, PathMain);
            }
            catch (Exception e)
            {
                Debug.LogError("Echo Shift: could not write save: " + e.Message);
            }
            Changed?.Invoke();
        }

        public LevelProgress Progress(string levelId)
        {
            foreach (var p in Data.levels)
                if (p.id == levelId) return p;
            return null;
        }

        public bool IsCompleted(string levelId)
        {
            var p = Progress(levelId);
            return p != null && p.completed;
        }

        /// <summary>Records a win, keeping the best result. Returns true if this is the first completion.</summary>
        public bool RecordWin(string levelId, int stars, int moves, bool crystal)
        {
            var p = Progress(levelId);
            bool first = p == null || !p.completed;
            if (p == null)
            {
                p = new LevelProgress { id = levelId };
                Data.levels.Add(p);
            }
            p.completed = true;
            p.stars = Mathf.Max(p.stars, stars);
            p.crystal |= crystal;
            p.bestMoves = p.bestMoves == 0 ? moves : Mathf.Min(p.bestMoves, moves);
            if (first) Data.levelsCompleted++;
            Save();
            return first;
        }

        public void ResetProgress()
        {
            var keep = Data;
            Data = new SaveData
            {
                removeAds = keep.removeAds,
                hints = keep.hints,
                settings = keep.settings,
                interstitialsShown = keep.interstitialsShown,
                removeAdsOfferShown = keep.removeAdsOfferShown,
                lastReviewAsk = keep.lastReviewAsk,
                tutorialDone = false
            };
            Save();
        }

        public static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}
