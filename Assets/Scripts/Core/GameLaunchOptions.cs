using UnityEngine;

namespace CookingSystem
{
    /// <summary>
    /// Carries the player's main-menu choice (New Game vs Load Save) into the gameplay scene.
    /// Read <see cref="LoadFromSave"/> from your game/save manager when the gameplay scene starts.
    /// </summary>
    public static class GameLaunchOptions
    {
        /// <summary>True when the player chose "Load Save", false for "New Game".</summary>
        public static bool LoadFromSave { get; set; }

        // Keeps statics clean when "Enter Play Mode Options" disables domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LoadFromSave = false;
        }
    }
}
