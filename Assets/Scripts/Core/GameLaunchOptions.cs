using UnityEngine;

namespace CookingSystem
{
    public enum GameMode
    {
        CampaignDay = 0,
        Endless = 1
    }

    /// <summary>
    /// Carries the player's main-menu / upgrade-shop selection into the gameplay and shop scenes.
    /// </summary>
    public static class GameLaunchOptions
    {
        /// <summary>True when the player chose "Load Save", false for "New Game".</summary>
        public static bool LoadFromSave { get; set; }

        /// <summary>Selected gameplay mode (3-Day Campaign vs Endless Rush).</summary>
        public static GameMode SelectedMode { get; set; } = GameMode.CampaignDay;

        /// <summary>Explicit campaign day override (1..3). 0 resolves from SaveData.currentDay.</summary>
        public static int SelectedDay { get; set; }

        /// <summary>Day number just completed before entering UpgradeShopScene (1..3, or 0).</summary>
        public static int LastClearedDay { get; set; }

        // Keeps statics clean when "Enter Play Mode Options" disables domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LoadFromSave = false;
            SelectedMode = GameMode.CampaignDay;
            SelectedDay = 0;
            LastClearedDay = 0;
        }
    }
}
