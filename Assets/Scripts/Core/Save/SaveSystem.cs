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
        private const string BrowserSaveKey = "lutuoan_savegame_v2";

        private static string GetSaveDirectory()
        {
            string dir = Application.persistentDataPath;
            if (!string.IsNullOrWhiteSpace(dir)) return dir;

            // Fallback if PlayerSettings.productName or companyName had invalid characters / trailing spaces
            string fallback = Path.Combine(Path.GetTempPath(), "IT25_Lutuoan");
            return fallback;
        }

        public static string SaveFilePath => Path.Combine(GetSaveDirectory(), SaveFileName);
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

        /// <summary>True if a save (file or browser storage) exists.</summary>
        public static bool HasSave()
        {
            return File.Exists(SaveFilePath) || File.Exists(BackupFilePath) || BrowserStorageBridge.HasKey(BrowserSaveKey);
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

                string savePath = SaveFilePath;
                string dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(TempFilePath, json);                                      // 1. Write the full file first.
                if (File.Exists(savePath)) File.Copy(savePath, BackupFilePath, true);       // 2. Keep the last good save.
                File.Copy(TempFilePath, savePath, true);                                    // 3. Swap in the new save.
                File.Delete(TempFilePath);

                BrowserStorageBridge.SetString(BrowserSaveKey, json);

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

        /// <summary>Reads the save without making it active (file first, then browser storage fallback).</summary>
        public static bool TryPeek(out SaveData data)
        {
            if (TryReadJson(BrowserStorageBridge.GetString(BrowserSaveKey, string.Empty), out data))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#endif
            }

            if (TryReadFile(SaveFilePath, out SaveData fileData))
            {
                data = fileData;
                return true;
            }

            if (TryReadFile(BackupFilePath, out SaveData bakData))
            {
                Debug.LogWarning("[SaveSystem] Main save unreadable, recovered from backup.");
                data = bakData;
                return true;
            }

            return data != null;
        }

        /// <summary>Deletes the save, its backup, browser storage mirror, and clears the active session.</summary>
        public static void Delete()
        {
            TryDeleteFile(SaveFilePath);
            TryDeleteFile(BackupFilePath);
            TryDeleteFile(TempFilePath);
            BrowserStorageBridge.DeleteKey(BrowserSaveKey);
            Current = null;
        }

        private static bool TryReadFile(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;

            try
            {
                return TryReadJson(File.ReadAllText(path), out data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Could not read '{path}': {exception.Message}");
                data = null;
                return false;
            }
        }

        private static bool TryReadJson(string json, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return false;

                data.Upgrade();
                return true;
            }
            catch
            {
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
