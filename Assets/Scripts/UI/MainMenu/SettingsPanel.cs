using CookingSystem.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// Audio & display settings controller: Master Volume, Music Volume, SFX Volume, and Fullscreen.
    /// Reusable across both Main Menu SettingsPopup and in-game PauseMenuModal; all instances stay
    /// synced via GameAudioManager.VolumesChanged and persist via PlayerPrefs + BrowserStorageBridge.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public const string MasterVolumeKey = GameAudioManager.MasterVolumeKey;
        public const string MusicVolumeKey = GameAudioManager.MusicVolumeKey;
        public const string SfxVolumeKey = GameAudioManager.SfxVolumeKey;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [Tooltip("Optional label showing the master volume percentage.")]
        [SerializeField] private TMP_Text masterVolumeLabel;

        [SerializeField] private Slider musicVolumeSlider;
        [Tooltip("Optional label showing the music volume percentage.")]
        [SerializeField] private TMP_Text musicVolumeLabel;

        [SerializeField] private Slider sfxVolumeSlider;
        [Tooltip("Optional label showing the SFX volume percentage.")]
        [SerializeField] private TMP_Text sfxVolumeLabel;

        [Header("Display")]
        [SerializeField] private Toggle fullscreenToggle;

        private bool isSyncingControls;

        private void Awake()
        {
            ConfigureSlider(masterVolumeSlider, OnMasterVolumeChanged);
            ConfigureSlider(musicVolumeSlider, OnMusicVolumeChanged);
            ConfigureSlider(sfxVolumeSlider, OnSfxVolumeChanged);

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void OnEnable()
        {
            GameAudioManager.VolumesChanged += SyncFromAudioManager;
            SyncFromAudioManager();
        }

        private void OnDisable()
        {
            GameAudioManager.VolumesChanged -= SyncFromAudioManager;
            GameAudioManager.Instance?.SaveVolumePreferences();
        }

        private static void ConfigureSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(callback);
        }

        private void SyncFromAudioManager()
        {
            isSyncingControls = true;

            GameAudioManager audio = GameAudioManager.Instance;
            float master = audio != null ? audio.MasterVolume : AudioListener.volume;
            float music = audio != null ? audio.MusicVolume : PlayerPrefs.GetFloat(MusicVolumeKey, 0.75f);
            float sfx = audio != null ? audio.SfxVolume : PlayerPrefs.GetFloat(SfxVolumeKey, 0.90f);

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(master);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(music);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sfx);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

            UpdateLabel(masterVolumeLabel, master);
            UpdateLabel(musicVolumeLabel, music);
            UpdateLabel(sfxVolumeLabel, sfx);

            isSyncingControls = false;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (isSyncingControls) return;
            GameAudioManager audio = GameAudioManager.Instance;
            if (audio != null) audio.SetMasterVolume(value);
            else
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat(MasterVolumeKey, value);
            }
            UpdateLabel(masterVolumeLabel, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (isSyncingControls) return;
            GameAudioManager audio = GameAudioManager.Instance;
            if (audio != null) audio.SetMusicVolume(value);
            else PlayerPrefs.SetFloat(MusicVolumeKey, value);
            UpdateLabel(musicVolumeLabel, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (isSyncingControls) return;
            GameAudioManager audio = GameAudioManager.Instance;
            if (audio != null) audio.SetSfxVolume(value, playPreview: true);
            else PlayerPrefs.SetFloat(SfxVolumeKey, value);
            UpdateLabel(sfxVolumeLabel, value);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            if (isSyncingControls) return;
            Screen.fullScreen = isFullscreen;
        }

        private static void UpdateLabel(TMP_Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }
    }
}
