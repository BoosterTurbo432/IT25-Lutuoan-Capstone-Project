using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookingSystem
{
    /// <summary>
    /// Static bridge that passes target scene and options to the dedicated loading screen.
    /// Manages both the initial game boot (forwarding to mainmenuscene) and in-game
    /// transitions (forwarding to gamescene or other scenes).
    /// Safe for WebGL single-threaded execution and domain-reload disabled Play Mode.
    /// </summary>
    public static class LoadingBridge
    {
        public const string DefaultLoadingScene = "loadingscreen";
        public const string DefaultMainMenuScene = "mainmenuscene";
        public const string DefaultGameplayScene = "gamescene";

        /// <summary>
        /// Destination scene to be loaded by the loading screen.
        /// Defaults to "gamescene" in Editor play mode so direct testing of loadingscreen loads the gameplay scene.
        /// Defaults to "mainmenuscene" on cold boot in standalone player builds.
        /// </summary>
#if UNITY_EDITOR
        public static string TargetSceneName { get; set; } = DefaultGameplayScene;
#else
        public static string TargetSceneName { get; set; } = DefaultMainMenuScene;
#endif

        /// <summary>
        /// True when the loading screen is executing the cold game boot into the main menu.
        /// </summary>
        public static bool IsInitialBoot =>
            string.Equals(TargetSceneName, DefaultMainMenuScene, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Optional custom status message override (e.g. "Loading Saved Kitchen...").
        /// </summary>
        public static string CustomStatusMessage { get; set; }

        /// <summary>
        /// Initiates a transition through the loading screen to the target scene.
        /// </summary>
        /// <param name="targetSceneName">Scene to load after loading completes.</param>
        /// <param name="loadFromSave">Carries save option forward into GameLaunchOptions.</param>
        /// <param name="loadingSceneName">Name of the loading screen scene.</param>
        public static void LoadScene(string targetSceneName, bool loadFromSave = false, string loadingSceneName = DefaultLoadingScene)
        {
            TargetSceneName = string.IsNullOrEmpty(targetSceneName) ? DefaultGameplayScene : targetSceneName;
            GameLaunchOptions.LoadFromSave = loadFromSave;

            bool canLoadLoadingScene = Application.CanStreamedLevelBeLoaded(loadingSceneName);
#if UNITY_EDITOR
            if (!canLoadLoadingScene && System.IO.File.Exists($"Assets/Scenes/{loadingSceneName}.unity"))
            {
                canLoadLoadingScene = true;
            }
#endif
            if (canLoadLoadingScene)
            {
                SceneManager.LoadScene(loadingSceneName);
            }
            else
            {
                Debug.LogWarning($"[LoadingBridge] Loading scene '{loadingSceneName}' is not in Build Profiles. " +
                                 $"Loading target scene '{TargetSceneName}' directly.");
                SceneManager.LoadScene(TargetSceneName);
            }
        }

        /// <summary>
        /// Asynchronously transitions through the loading screen to the target scene.
        /// </summary>
        public static void LoadSceneAsync(string targetSceneName, bool loadFromSave = false, string loadingSceneName = DefaultLoadingScene)
        {
            TargetSceneName = string.IsNullOrEmpty(targetSceneName) ? DefaultGameplayScene : targetSceneName;
            GameLaunchOptions.LoadFromSave = loadFromSave;

            bool canLoadLoadingScene = Application.CanStreamedLevelBeLoaded(loadingSceneName);
#if UNITY_EDITOR
            if (!canLoadLoadingScene && System.IO.File.Exists($"Assets/Scenes/{loadingSceneName}.unity"))
            {
                canLoadLoadingScene = true;
            }
#endif
            if (canLoadLoadingScene)
            {
                SceneManager.LoadSceneAsync(loadingSceneName);
            }
            else
            {
                Debug.LogWarning($"[LoadingBridge] Loading scene '{loadingSceneName}' is not in Build Profiles. " +
                                 $"Loading target scene '{TargetSceneName}' directly.");
                SceneManager.LoadSceneAsync(TargetSceneName);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
#if UNITY_EDITOR
            TargetSceneName = DefaultGameplayScene;
#else
            TargetSceneName = DefaultMainMenuScene;
#endif
            CustomStatusMessage = null;
        }
    }
}
