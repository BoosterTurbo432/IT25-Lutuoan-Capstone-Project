using System.Runtime.InteropServices;
using UnityEngine;

namespace CookingSystem
{
    /// <summary>
    /// Cross-platform persistent key-value storage for PC Desktop, Android, and Apple (iOS/iPadOS) WebGL browsers.
    /// Synchronously writes to browser localStorage + PlayerPrefs (+ Emscripten IDBFS flush) so data survives
    /// page refreshes and mobile browser tab termination.
    /// </summary>
    public static class BrowserStorageBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int Lutuoan_SaveToLocalStorage(string key, string value);

        [DllImport("__Internal")]
        private static extern string Lutuoan_LoadFromLocalStorage(string key);

        [DllImport("__Internal")]
        private static extern void Lutuoan_RemoveFromLocalStorage(string key);

        [DllImport("__Internal")]
        private static extern int Lutuoan_IsMobileBrowser();

        [DllImport("__Internal")]
        private static extern string Lutuoan_PromptMobileText(string title, string defaultValue);

        [DllImport("__Internal")]
        private static extern void Lutuoan_SyncIdbfs();
#endif

        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            string safeVal = value ?? string.Empty;

            PlayerPrefs.SetString(key, safeVal);
            PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
            Lutuoan_SaveToLocalStorage(key, safeVal);
            Lutuoan_SyncIdbfs();
#endif
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

#if UNITY_WEBGL && !UNITY_EDITOR
            string webVal = Lutuoan_LoadFromLocalStorage(key);
            if (!string.IsNullOrEmpty(webVal))
            {
                return webVal;
            }
#endif
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public static bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

#if UNITY_WEBGL && !UNITY_EDITOR
            string webVal = Lutuoan_LoadFromLocalStorage(key);
            if (!string.IsNullOrEmpty(webVal)) return true;
#endif
            return PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
            Lutuoan_RemoveFromLocalStorage(key);
            Lutuoan_SyncIdbfs();
#endif
        }

        public static bool IsMobileBrowser()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Lutuoan_IsMobileBrowser() == 1 || Application.isMobilePlatform;
#else
            return Application.isMobilePlatform;
#endif
        }

        public static bool TryPromptMobileInput(string title, string currentValue, out string result)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string prompted = Lutuoan_PromptMobileText(title ?? "Enter Name (Max 8 Characters):", currentValue ?? string.Empty);
            if (prompted != null)
            {
                result = prompted;
                return true;
            }
#endif
            result = currentValue;
            return false;
        }
    }
}
