using UnityEngine;

namespace CookingSystem.Audio
{
    public enum MusicTrack
    {
        None = 0,
        MainMenu = 1,
        CampaignGame = 2,
        EndlessGame = 3
    }

    public enum SfxType
    {
        Click = 0,
        Chop = 1,
        Mix = 2,
        Cooked = 3,
        Overcooked = 4,
        Drink = 5,
        Satisfied = 6,
        NotSatisfied = 7,
        ComboStreak = 8,
        Victory = 9,
        Fail = 10
    }

    /// <summary>
    /// Holds direct references to all BGM and SFX clips from Assets/Luto SFX/.
    /// Stored at Assets/Resources/GameAudioLibrary.asset so GameAudioManager can load it
    /// reliably from any scene in Editor, Desktop, and WebGL builds.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Lutuoan/Audio Library")]
    public class GameAudioLibrary : ScriptableObject
    {
        [Header("Background Music")]
        public AudioClip mainMenuBg;
        public AudioClip gameBg;
        public AudioClip endlessModeBg;

        [Header("UI & Kitchen SFX")]
        public AudioClip click;
        public AudioClip chop;
        public AudioClip mix;
        public AudioClip cooked;
        public AudioClip overcooked;
        public AudioClip drink;
        public AudioClip satisfied;
        public AudioClip notSatisfied;
        public AudioClip comboStreak;
        public AudioClip victory;
        public AudioClip fail;

        [Header("Relative Mix Balance (0..1)")]
        [Range(0f, 1f)] public float mainMenuBgScale = 0.55f;
        [Range(0f, 1f)] public float gameBgScale = 0.48f;
        [Range(0f, 1f)] public float endlessBgScale = 0.50f;
        [Range(0f, 1f)] public float clickScale = 0.78f;
        [Range(0f, 1f)] public float chopScale = 0.90f;
        [Range(0f, 1f)] public float mixScale = 0.88f;
        [Range(0f, 1f)] public float cookedScale = 0.92f;
        [Range(0f, 1f)] public float overcookedScale = 0.72f;
        [Range(0f, 1f)] public float drinkScale = 0.88f;
        [Range(0f, 1f)] public float satisfiedScale = 0.90f;
        [Range(0f, 1f)] public float notSatisfiedScale = 0.92f;
        [Range(0f, 1f)] public float comboStreakScale = 0.92f;
        [Range(0f, 1f)] public float victoryScale = 0.95f;
        [Range(0f, 1f)] public float failScale = 0.95f;

        public AudioClip GetMusicClip(MusicTrack track, out float balanceScale)
        {
            switch (track)
            {
                case MusicTrack.MainMenu:
                    balanceScale = mainMenuBgScale;
                    return mainMenuBg;
                case MusicTrack.CampaignGame:
                    balanceScale = gameBgScale;
                    return gameBg;
                case MusicTrack.EndlessGame:
                    balanceScale = endlessBgScale;
                    return endlessModeBg;
                default:
                    balanceScale = 0f;
                    return null;
            }
        }

        public AudioClip GetSfxClip(SfxType sfx, out float balanceScale)
        {
            switch (sfx)
            {
                case SfxType.Click:
                    balanceScale = clickScale;
                    return click;
                case SfxType.Chop:
                    balanceScale = chopScale;
                    return chop;
                case SfxType.Mix:
                    balanceScale = mixScale;
                    return mix;
                case SfxType.Cooked:
                    balanceScale = cookedScale;
                    return cooked;
                case SfxType.Overcooked:
                    balanceScale = overcookedScale;
                    return overcooked;
                case SfxType.Drink:
                    balanceScale = drinkScale;
                    return drink;
                case SfxType.Satisfied:
                    balanceScale = satisfiedScale;
                    return satisfied;
                case SfxType.NotSatisfied:
                    balanceScale = notSatisfiedScale;
                    return notSatisfied;
                case SfxType.ComboStreak:
                    balanceScale = comboStreakScale;
                    return comboStreak;
                case SfxType.Victory:
                    balanceScale = victoryScale;
                    return victory;
                case SfxType.Fail:
                    balanceScale = failScale;
                    return fail;
                default:
                    balanceScale = 1f;
                    return null;
            }
        }
    }
}
