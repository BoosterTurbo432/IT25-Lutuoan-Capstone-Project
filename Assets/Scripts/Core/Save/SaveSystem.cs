using System;
using System.IO;
using UnityEngine;

namespace CookingSystem
{
    /// <summary>
    /// Lightweight single-slot JSON save system stored in <see cref="Application.persistentDataPath"/>.
    /// Writes are crash-safe (temp file + backup of the last good save), and a corrupt main file
    /// automatically falls back to the backup.
    ///
    /// Typical gameplay usage:
    /// <code>
    /// SaveData data = SaveSystem.GetOrCreateCurrent();
    /// data.coins += 10;
    /// SaveSystem.Save();
    /// </code>
    /// </summary>
    public static class SaveSystem
    {
        public const string SaveFileName = "savegame.json";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private static string BackupFilePath => SaveFilePath + ".bak";
        private static string TempFilePath => SaveFilePath + ".tmp";

        /// <summary>The save data of the running session (null until a game is started or loaded).</summary>
        public static SaveData Current { get; private set; }

        public static event Action<SaveData> Saved;
        public static event Action<SaveData> Loaded;

        // Keeps statics clean when "Enter Play Mode Options" disables domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Saved = null;
            Loaded = null;
        }

        /// <summary>True if a save (or its backup) exists on disk.</summary>
        public static bool HasSave()
        {
            return File.Exists(SaveFilePath) || File.Exists(BackupFilePath);
        }

        /// <summary>Starts a fresh in-memory session. Nothing is written until <see cref="Save"/> is called.</summary>
        public static SaveData StartNewGame()
        {
            Current = SaveData.CreateNew();
            return Current;
        }

        /// <summary>
        /// Returns the active session, creating one if needed (honours the main menu's New Game / Load Save choice).
        /// Safe to call from gameplay even when the scene was started directly in the editor.
        /// </summary>
        public static SaveData GetOrCreateCurrent()
        {
            if (Current != null) return Current;
            if (GameLaunchOptions.LoadFromSave && TryLoad(out SaveData loaded)) return loaded;
            return StartNewGame();
        }

        /// <summary>Writes <see cref="Current"/> to disk. Returns false (and logs) on failure.</summary>
        public static bool Save()
        {
            if (Current == null)
            {
                Debug.LogWarning("[SaveSystem] Nothing to save. Call StartNewGame() or TryLoad() first.");
                return false;
            }

            try
            {
                Current.lastSavedAtUtc = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(Current, prettyPrint: true);

                Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
                File.WriteAllText(TempFilePath, json);                                      // 1. Write the full file first.
                if (File.Exists(SaveFilePath)) File.Copy(SaveFilePath, BackupFilePath, true); // 2. Keep the last good save.
                File.Copy(TempFilePath, SaveFilePath, true);                                // 3. Swap in the new save.
                File.Delete(TempFilePath);

                Saved?.Invoke(Current);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Failed to save to '{SaveFilePath}': {exception}");
                return false;
            }
        }

        /// <summary>Loads the save from disk and makes it the active session.</summary>
        public static bool TryLoad(out SaveData data)
        {
            if (!TryPeek(out data)) return false;

            Current = data;
            Loaded?.Invoke(data);
            return true;
        }

        /// <summary>Reads the save from disk without making it active (e.g. to show "Last saved" in a menu).</summary>
        public static bool TryPeek(out SaveData data)
        {
            if (TryReadFile(SaveFilePath, out data)) return true;

            if (TryReadFile(BackupFilePath, out data))
            {
                Debug.LogWarning("[SaveSystem] Main save unreadable, recovered from backup.");
                return true;
            }

            return false;
        }

        /// <summary>Deletes the save, its backup, and clears the active session.</summary>
        public static void Delete()
        {
            TryDeleteFile(SaveFilePath);
            TryDeleteFile(BackupFilePath);
            TryDeleteFile(TempFilePath);
            Current = null;
        }

        private static bool TryReadFile(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;

                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return false;

                data.Upgrade();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Could not read '{path}': {exception.Message}");
                data = null;
                return false;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Could not delete '{path}': {exception.Message}");
            }
        }
    }
}
