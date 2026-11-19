using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CookingSystem
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
        public float timeSpentSeconds;
        public int dishesServed;
        public int highestCombo;
        public string recordedAtUtc;
    }

    [Serializable]
    internal class LeaderboardTableData
    {
        public int version = 1;
        public string lastPlayerName = "Chef";
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Persistent Endless Mode leaderboard supporting 8-character player names,
    /// Score, Time Spent, Dishes Served, and Highest Combo, saved via BrowserStorageBridge
    /// (PC & Mobile WebGL localStorage + PlayerPrefs + IDBFS).
    /// </summary>
    public static class LeaderboardSystem
    {
        public const int MaxNameLength = 8;
        public const int MaxStoredEntries = 10;
        private const string StorageKey = "lutuoan_endless_leaderboard_v1";

        private static LeaderboardTableData cachedData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cachedData = null;
        }

        public static string LastSubmittedEntryTimestamp { get; private set; }

        public static IReadOnlyList<LeaderboardEntry> GetEntries()
        {
            LeaderboardTableData data = LoadOrCreate();
            return data.entries;
        }

        public static string GetLastPlayerName()
        {
            LeaderboardTableData data = LoadOrCreate();
            return string.IsNullOrWhiteSpace(data.lastPlayerName) ? "Chef" : SanitizeName(data.lastPlayerName);
        }

        public static string SanitizeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return "Chef";
            string trimmed = rawName.Trim();
            if (trimmed.Length > MaxNameLength)
            {
                trimmed = trimmed.Substring(0, MaxNameLength).TrimEnd();
            }
            return string.IsNullOrEmpty(trimmed) ? "Chef" : trimmed;
        }

        /// <summary>
        /// Saves a completed Endless Mode run and returns the 1-based rank (1..10, or 0 if below Top 10).
        /// </summary>
        public static int AddEntry(string rawName, int score, float timeSpentSeconds, int dishesServed, int highestCombo)
        {
            LeaderboardTableData data = LoadOrCreate();
            string cleanName = SanitizeName(rawName);
            data.lastPlayerName = cleanName;

            string timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var entry = new LeaderboardEntry
            {
                playerName = cleanName,
                score = Mathf.Max(0, score),
                timeSpentSeconds = Mathf.Max(0f, timeSpentSeconds),
                dishesServed = Mathf.Max(0, dishesServed),
                highestCombo = Mathf.Max(1, highestCombo),
                recordedAtUtc = timestamp
            };

            data.entries.Add(entry);
            data.entries.Sort((a, b) =>
            {
                int scoreCmp = b.score.CompareTo(a.score);
                if (scoreCmp != 0) return scoreCmp;
                int dishesCmp = b.dishesServed.CompareTo(a.dishesServed);
                if (dishesCmp != 0) return dishesCmp;
                return b.timeSpentSeconds.CompareTo(a.timeSpentSeconds);
            });

            if (data.entries.Count > MaxStoredEntries)
            {
                data.entries.RemoveRange(MaxStoredEntries, data.entries.Count - MaxStoredEntries);
            }

            LastSubmittedEntryTimestamp = timestamp;
            Save(data);

            int rankIndex = data.entries.FindIndex(e => e.recordedAtUtc == timestamp);
            return rankIndex >= 0 ? rankIndex + 1 : 0;
        }

        public static void ClearAll()
        {
            cachedData = new LeaderboardTableData();
            LastSubmittedEntryTimestamp = null;
            BrowserStorageBridge.DeleteKey(StorageKey);
        }

        public static string FormatDuration(float totalSeconds)
        {
            int secs = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
            int mins = secs / 60;
            int rem = secs % 60;
            return $"{mins:00}:{rem:00}";
        }

        private static LeaderboardTableData LoadOrCreate()
        {
            if (cachedData != null) return cachedData;

            string json = BrowserStorageBridge.GetString(StorageKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    LeaderboardTableData parsed = JsonUtility.FromJson<LeaderboardTableData>(json);
                    if (parsed != null)
                    {
                        parsed.entries ??= new List<LeaderboardEntry>();
                        cachedData = parsed;
                        return cachedData;
                    }
                }
                catch
                {
                    // Fallback to empty table if stored payload was corrupt
                }
            }

            cachedData = new LeaderboardTableData();
            return cachedData;
        }

        private static void Save(LeaderboardTableData data)
        {
            cachedData = data;
            string json = JsonUtility.ToJson(data, prettyPrint: false);
            BrowserStorageBridge.SetString(StorageKey, json);
        }
    }
}
