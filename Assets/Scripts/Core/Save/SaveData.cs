using System;
using System.Collections.Generic;
using System.Globalization;

namespace CookingSystem
{
    /// <summary>
    /// Everything that gets written to the save file. Plain serializable fields only (JsonUtility).
    /// To extend: add new public fields with sensible defaults, bump <see cref="LatestVersion"/>,
    /// and handle older files inside <see cref="Upgrade"/>.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int LatestVersion = 2;

        // --- Meta ---
        public int version = LatestVersion;
        public string createdAtUtc;
        public string lastSavedAtUtc;
        public float totalPlayTimeSeconds;

        // --- Progress ---
        public int currentDay = 1;
        public int highestDayCleared;
        public int skillPoints;
        public bool endlessModeUnlocked;
        public int endlessHighScore;
        public int endlessBestServed;

        // --- Carinderia Kitchen Upgrades (Ranks 0..3) ---
        public int prepMasteryLevel;
        public int fastBurnerLevel;
        public int hospitalityLevel;

        public int coins;
        public int highScore;
        public int dishesServed;
        public List<string> unlockedRecipes = new List<string>();
        public List<string> completedLessons = new List<string>();

        public static SaveData CreateNew()
        {
            return new SaveData
            {
                createdAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
        }

        /// <summary>Last save time converted to the player's local time zone.</summary>
        public bool TryGetLastSavedLocalTime(out DateTime localTime)
        {
            if (DateTime.TryParse(lastSavedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime utc))
            {
                localTime = utc.ToLocalTime();
                return true;
            }

            localTime = default;
            return false;
        }

        /// <summary>Repairs missing data and migrates files written by older versions of the game.</summary>
        internal void Upgrade()
        {
            unlockedRecipes ??= new List<string>();
            completedLessons ??= new List<string>();
            if (currentDay < 1) currentDay = 1;
            if (currentDay > 3) currentDay = 3;
            if (highestDayCleared < 0) highestDayCleared = 0;
            if (skillPoints < 0) skillPoints = 0;
            prepMasteryLevel = Math.Max(0, Math.Min(3, prepMasteryLevel));
            fastBurnerLevel = Math.Max(0, Math.Min(3, fastBurnerLevel));
            hospitalityLevel = Math.Max(0, Math.Min(3, hospitalityLevel));
            if (highestDayCleared >= 3) endlessModeUnlocked = true;

            version = LatestVersion;
        }
    }
}
