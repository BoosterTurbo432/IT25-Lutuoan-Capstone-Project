using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// Minimal, extendable settings content: master volume + fullscreen, persisted via PlayerPrefs.
    /// Place on the Settings popup (next to <see cref="UIPopupPanel"/>).
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public const string MasterVolumeKey = "Settings.MasterVolume";

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [Tooltip("Optional label showing the volume percentage.")]
        [SerializeField] private TMP_Text masterVolumeLabel;

        [Header("Display")]
        [SerializeField] private Toggle fullscreenToggle;

        // Applies saved settings at launch, even if the Settings popup is never opened.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplySavedSettings()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        }

        private void Awake()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void OnEnable()
        {
            // Sync controls with the real values every time the panel opens.
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            UpdateVolumeLabel(AudioListener.volume);
        }

        private void OnDisable()
        {
            PlayerPrefs.Save(); // Persist once when the panel closes instead of on every slider tick.
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            UpdateVolumeLabel(value);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void UpdateVolumeLabel(float value)
        {
            if (masterVolumeLabel != null)
            {
                masterVolumeLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }
    }
}
