using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.Audio
{
    /// <summary>
    /// Persistent DontDestroyOnLoad audio controller managing:
    /// - Smooth crossfading BGM tracks (MainMenu, Campaign Game, Endless Mode)
    /// - Dedicated UI click/tap, gameplay SFX, jingle, and stoppable fire-alert channels
    /// - Independent Master, Music, and SFX volume controls persisted across sessions
    /// - Automatic click/tap audio wiring for all interactive Buttons and Toggles across scenes
    /// </summary>
    [DisallowMultipleComponent]
    public class GameAudioManager : MonoBehaviour
    {
        public const string MasterVolumeKey = "Settings.MasterVolume";
        public const string MusicVolumeKey = "Settings.MusicVolume";
        public const string SfxVolumeKey = "Settings.SfxVolume";
        private const string BrowserMasterKey = "lutuoan_vol_master";
        private const string BrowserMusicKey = "lutuoan_vol_music";
        private const string BrowserSfxKey = "lutuoan_vol_sfx";
        public const string ResourceLibraryPath = "GameAudioLibrary";

        private const float DefaultMasterVolume = 1.0f;
        private const float DefaultMusicVolume = 0.75f;
        private const float DefaultSfxVolume = 0.90f;
        private const float DefaultCrossfadeSeconds = 0.45f;

        private static GameAudioManager instance;
        private static bool isQuitting;

        public static GameAudioManager Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    EnsureCreated();
                }
                return instance;
            }
        }

        public static bool HasInstance => instance != null && !isQuitting;
        public static event Action VolumesChanged;

        private GameAudioLibrary library;

        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private AudioSource uiSfxSource;
        private AudioSource gameplaySfxSource;
        private AudioSource jingleSfxSource;
        private AudioSource fireAlertSource;
        private AudioListener fallbackListener;

        private bool usingSourceA = true;
        private MusicTrack currentTrack = MusicTrack.None;
        private float activeTrackBalance = 0.5f;
        private float musicDuckMultiplier = 1f;

        private float masterVolume = DefaultMasterVolume;
        private float musicVolume = DefaultMusicVolume;
        private float sfxVolume = DefaultSfxVolume;

        private Coroutine crossfadeCoroutine;
        private Coroutine duckCoroutine;
        private Coroutine sceneSyncCoroutine;
        private float lastSfxPreviewTime = -10f;
        private float nextMusicWatchdogTime;
        private bool hasHandledSceneLoad;

        private readonly Dictionary<SfxType, float> lastSfxPlayTimes = new Dictionary<SfxType, float>();

        public GameAudioLibrary Library => library;
        public MusicTrack CurrentTrack => currentTrack;
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public bool IsMusicPlaying => ActiveMusicSource != null && ActiveMusicSource.isPlaying;
        public AudioClip ActiveMusicClip => ActiveMusicSource != null ? ActiveMusicSource.clip : null;
        public bool IsFireAlertPlaying => fireAlertSource != null && fireAlertSource.isPlaying;

        private AudioSource ActiveMusicSource => usingSourceA ? musicSourceA : musicSourceB;
        private AudioSource InactiveMusicSource => usingSourceA ? musicSourceB : musicSourceA;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
            VolumesChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapOnLaunch()
        {
            EnsureCreated();
        }

        public static GameAudioManager EnsureCreated()
        {
            if (isQuitting) return null;
            if (instance != null) return instance;

            GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            GameObject go = new GameObject("GameAudioManager");
            instance = go.AddComponent<GameAudioManager>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLibrary();
            CreateAudioSources();
            LoadSavedVolumes();
            ApplyVolumesImmediate();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (hasHandledSceneLoad) return;
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid())
            {
                hasHandledSceneLoad = true;
                SyncAudioListenerForScene();
                RegisterSceneUIButtons();
                SyncMusicForScene(active.name);
            }
        }

        private void Update()
        {
            // Watchdog ensures BGM never stalls after scene AudioListener handoffs or WebGL autoplay unlock.
            if (currentTrack == MusicTrack.None || crossfadeCoroutine != null || AudioListener.pause) return;
            if (Time.unscaledTime < nextMusicWatchdogTime) return;
            nextMusicWatchdogTime = Time.unscaledTime + 0.35f;

            AudioSource activeSrc = ActiveMusicSource;
            if (activeSrc != null && activeSrc.clip != null && !activeSrc.isPlaying)
            {
                activeSrc.volume = ComputeEffectiveMusicVolume(activeTrackBalance);
                activeSrc.Play();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveVolumePreferences();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            SaveVolumePreferences();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                instance = null;
            }
        }

        private void LoadLibrary()
        {
            library = Resources.Load<GameAudioLibrary>(ResourceLibraryPath);
            if (library == null)
            {
                Debug.LogWarning("[GameAudioManager] GameAudioLibrary asset not found in Assets/Resources/GameAudioLibrary.asset. Run Tools > Audio > Rebuild Luto SFX Audio Library.");
            }
        }

        private void CreateAudioSources()
        {
            musicSourceA = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: true, priority: 32);
            musicSourceB = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: true, priority: 32);
            uiSfxSource = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: false, priority: 64);
            gameplaySfxSource = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: false, priority: 96);
            jingleSfxSource = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: false, priority: 48);
            fireAlertSource = ConfigureSource(gameObject.AddComponent<AudioSource>(), loop: true, priority: 80);

            fallbackListener = gameObject.AddComponent<AudioListener>();
            fallbackListener.enabled = false;
        }

        private static AudioSource ConfigureSource(AudioSource src, bool loop, int priority)
        {
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            src.priority = priority;
            src.ignoreListenerPause = true;
            return src;
        }

        private void LoadSavedVolumes()
        {
            masterVolume = ReadStoredFloat(MasterVolumeKey, BrowserMasterKey, DefaultMasterVolume);
            musicVolume = ReadStoredFloat(MusicVolumeKey, BrowserMusicKey, DefaultMusicVolume);
            sfxVolume = ReadStoredFloat(SfxVolumeKey, BrowserSfxKey, DefaultSfxVolume);
        }

        private static float ReadStoredFloat(string prefsKey, string browserKey, float fallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string webRaw = BrowserStorageBridge.GetString(browserKey, string.Empty);
            if (!string.IsNullOrEmpty(webRaw) &&
                float.TryParse(webRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out float webVal))
            {
                return Mathf.Clamp01(webVal);
            }
#endif
            if (PlayerPrefs.HasKey(prefsKey))
            {
                float stored = PlayerPrefs.GetFloat(prefsKey, -1f);
                if (stored >= 0f)
                {
                    return Mathf.Clamp01(stored);
                }
            }

            string legacyRaw = BrowserStorageBridge.GetString(browserKey, string.Empty);
            if (!string.IsNullOrEmpty(legacyRaw) &&
                float.TryParse(legacyRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out float legacyVal))
            {
                return Mathf.Clamp01(legacyVal);
            }

            return Mathf.Clamp01(fallback);
        }

        public void SetMasterVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(masterVolume, clamped)) return;
            masterVolume = clamped;
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            ApplyVolumesImmediate();
            VolumesChanged?.Invoke();
        }

        public void SetMusicVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(musicVolume, clamped)) return;
            musicVolume = clamped;
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            ApplyVolumesImmediate();
            VolumesChanged?.Invoke();
        }

        public void SetSfxVolume(float value, bool playPreview = false)
        {
            float clamped = Mathf.Clamp01(value);
            bool changed = !Mathf.Approximately(sfxVolume, clamped);
            sfxVolume = clamped;
            if (changed)
            {
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                ApplyVolumesImmediate();
                VolumesChanged?.Invoke();
            }

            if (playPreview && sfxVolume > 0.01f && Time.unscaledTime - lastSfxPreviewTime >= 0.18f)
            {
                lastSfxPreviewTime = Time.unscaledTime;
                PlayClick(0.95f, 1f);
            }
        }

        public void SaveVolumePreferences()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();

            BrowserStorageBridge.SetString(BrowserMasterKey, masterVolume.ToString("0.###", CultureInfo.InvariantCulture));
            BrowserStorageBridge.SetString(BrowserMusicKey, musicVolume.ToString("0.###", CultureInfo.InvariantCulture));
            BrowserStorageBridge.SetString(BrowserSfxKey, sfxVolume.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private void ApplyVolumesImmediate()
        {
            AudioListener.volume = masterVolume;

            if (crossfadeCoroutine == null && ActiveMusicSource != null)
            {
                ActiveMusicSource.volume = ComputeEffectiveMusicVolume(activeTrackBalance);
            }
            if (crossfadeCoroutine == null && InactiveMusicSource != null && !InactiveMusicSource.isPlaying)
            {
                InactiveMusicSource.volume = 0f;
            }

            if (uiSfxSource != null) uiSfxSource.volume = sfxVolume;
            if (gameplaySfxSource != null) gameplaySfxSource.volume = sfxVolume;
            if (jingleSfxSource != null) jingleSfxSource.volume = sfxVolume;

            if (fireAlertSource != null && library != null)
            {
                library.GetSfxClip(SfxType.Overcooked, out float alertScale);
                fireAlertSource.volume = sfxVolume * alertScale;
            }
        }

        private float ComputeEffectiveMusicVolume(float balance)
        {
            return Mathf.Clamp01(musicVolume * balance * musicDuckMultiplier);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            hasHandledSceneLoad = true;
            StopOvercookedAlert();
            ResetMusicDucking();
            SyncAudioListenerForScene();
            RegisterSceneUIButtons();

            if (sceneSyncCoroutine != null)
            {
                StopCoroutine(sceneSyncCoroutine);
            }
            sceneSyncCoroutine = StartCoroutine(SyncMusicNextFrameRoutine(scene.name));
        }

        private IEnumerator SyncMusicNextFrameRoutine(string sceneName)
        {
            // Wait one frame so Unity finishes tearing down the old scene's AudioListener
            // and activating the new scene's AudioListener before starting/crossfading BGM.
            yield return null;
            SyncAudioListenerForScene();
            RegisterSceneUIButtons();
            SyncMusicForScene(sceneName);
            sceneSyncCoroutine = null;
        }

        private void SyncAudioListenerForScene()
        {
            if (fallbackListener == null) return;

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            int externalListeners = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i] != fallbackListener && listeners[i].enabled)
                {
                    externalListeners++;
                }
            }

            fallbackListener.enabled = externalListeners == 0;
        }

        public void RegisterSceneUIButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;
                UIClickAudioHook hook = btn.GetComponent<UIClickAudioHook>();
                if (hook == null)
                {
                    btn.gameObject.AddComponent<UIClickAudioHook>();
                }
            }

            Toggle[] toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include);
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle tgl = toggles[i];
                if (tgl == null) continue;
                UIClickAudioHook hook = tgl.GetComponent<UIClickAudioHook>();
                if (hook == null)
                {
                    tgl.gameObject.AddComponent<UIClickAudioHook>();
                }
            }
        }

        public void SyncMusicForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (sceneName.Equals("gamescene", StringComparison.OrdinalIgnoreCase))
            {
                PlayMusicForGameMode(GameLaunchOptions.SelectedMode);
                return;
            }

            if (sceneName.Equals("loadingscreen", StringComparison.OrdinalIgnoreCase))
            {
                // Keep existing track seamless through loading, or gently start menu BGM on initial boot.
                if (currentTrack == MusicTrack.None || !IsMusicPlaying)
                {
                    PlayMusic(MusicTrack.MainMenu, 0.35f);
                }
                return;
            }

            // MainMenu, UpgradeShopScene, LeaderboardScene share the cozy carinderia menu theme without restarting.
            PlayMusic(MusicTrack.MainMenu, DefaultCrossfadeSeconds);
        }

        public void PlayMusicForGameMode(GameMode mode)
        {
            PlayMusic(mode == GameMode.Endless ? MusicTrack.EndlessGame : MusicTrack.CampaignGame, DefaultCrossfadeSeconds);
        }

        public void PlayMusic(MusicTrack track, float fadeDuration = DefaultCrossfadeSeconds)
        {
            if (library == null) LoadLibrary();
            if (library == null) return;

            AudioClip targetClip = library.GetMusicClip(track, out float targetBalance);
            if (targetClip == null) return;

            if (currentTrack == track && ActiveMusicSource != null && ActiveMusicSource.clip == targetClip && ActiveMusicSource.isPlaying)
            {
                activeTrackBalance = targetBalance;
                if (crossfadeCoroutine == null)
                {
                    ActiveMusicSource.volume = ComputeEffectiveMusicVolume(activeTrackBalance);
                }
                return;
            }

            currentTrack = track;
            activeTrackBalance = targetBalance;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                crossfadeCoroutine = null;
            }

            if (fadeDuration <= 0.01f || !IsMusicPlaying)
            {
                AudioSource next = ActiveMusicSource;
                AudioSource other = InactiveMusicSource;
                if (other != null)
                {
                    other.Stop();
                    other.volume = 0f;
                }

                next.clip = targetClip;
                next.loop = true;
                next.time = 0f;
                next.volume = ComputeEffectiveMusicVolume(targetBalance);
                next.Play();
                return;
            }

            crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(targetClip, targetBalance, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(AudioClip nextClip, float nextBalance, float duration)
        {
            AudioSource fromSource = ActiveMusicSource;
            AudioSource toSource = InactiveMusicSource;
            usingSourceA = !usingSourceA;

            float fromStartVol = fromSource != null ? fromSource.volume : 0f;

            toSource.clip = nextClip;
            toSource.loop = true;
            toSource.time = 0f;
            toSource.volume = 0f;
            toSource.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                float t = Mathf.Clamp01(elapsed / duration);
                float targetVol = ComputeEffectiveMusicVolume(nextBalance);
                toSource.volume = Mathf.Lerp(0f, targetVol, t);
                if (fromSource != null && fromSource.isPlaying)
                {
                    fromSource.volume = Mathf.Lerp(fromStartVol, 0f, t);
                }
                yield return null;
            }

            toSource.volume = ComputeEffectiveMusicVolume(nextBalance);
            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }

            crossfadeCoroutine = null;
        }

        public void PlayClick(float volumeScale = 1f, float pitch = 1f)
        {
            PlaySfxInternal(SfxType.Click, uiSfxSource, volumeScale, pitch, cooldownSeconds: 0.045f);
        }

        public void PlayTapPickup()
        {
            PlaySfxInternal(SfxType.Click, uiSfxSource, volumeScale: 0.55f, pitch: 1.08f, cooldownSeconds: 0.05f);
        }

        public void PlayChop() => PlaySfxInternal(SfxType.Chop, gameplaySfxSource, 1f, UnityEngine.Random.Range(0.96f, 1.05f), 0.06f);
        public void PlayMix() => PlaySfxInternal(SfxType.Mix, gameplaySfxSource, 1f, UnityEngine.Random.Range(0.97f, 1.04f), 0.06f);
        public void PlayCooked() => PlaySfxInternal(SfxType.Cooked, gameplaySfxSource, 1f, 1f, 0.10f);
        public void PlayDrink() => PlaySfxInternal(SfxType.Drink, gameplaySfxSource, 1f, 1f, 0.08f);
        public void PlaySatisfied() => PlaySfxInternal(SfxType.Satisfied, gameplaySfxSource, 1f, 1f, 0.10f);
        public void PlayNotSatisfied() => PlaySfxInternal(SfxType.NotSatisfied, gameplaySfxSource, 1f, 1f, 0.10f);
        public void PlayComboStreak() => PlaySfxInternal(SfxType.ComboStreak, gameplaySfxSource, 1f, 1f, 0.12f);

        public void PlayVictory()
        {
            StopOvercookedAlert();
            PlayJingle(SfxType.Victory);
        }

        public void PlayFail()
        {
            StopOvercookedAlert();
            PlayJingle(SfxType.Fail);
        }

        public void StartOvercookedAlert()
        {
            if (library == null) LoadLibrary();
            if (library == null || fireAlertSource == null) return;

            AudioClip clip = library.GetSfxClip(SfxType.Overcooked, out float scale);
            if (clip == null) return;

            fireAlertSource.volume = sfxVolume * scale;
            if (fireAlertSource.clip == clip && fireAlertSource.isPlaying) return;

            fireAlertSource.clip = clip;
            fireAlertSource.loop = true;
            fireAlertSource.time = 0f;
            fireAlertSource.Play();
        }

        public void StopOvercookedAlert()
        {
            if (fireAlertSource != null && fireAlertSource.isPlaying)
            {
                fireAlertSource.Stop();
            }
        }

        private void PlayJingle(SfxType sfx)
        {
            if (library == null) LoadLibrary();
            if (library == null || jingleSfxSource == null) return;

            AudioClip clip = library.GetSfxClip(sfx, out float scale);
            if (clip == null) return;

            jingleSfxSource.Stop();
            jingleSfxSource.pitch = 1f;
            jingleSfxSource.volume = sfxVolume;
            jingleSfxSource.PlayOneShot(clip, Mathf.Clamp01(scale));

            float holdDuration = Mathf.Clamp(clip.length, 1.2f, 2.5f);
            if (duckCoroutine != null) StopCoroutine(duckCoroutine);
            duckCoroutine = StartCoroutine(DuckMusicRoutine(0.32f, holdDuration));
        }

        private IEnumerator DuckMusicRoutine(float duckLevel, float holdSeconds)
        {
            musicDuckMultiplier = duckLevel;
            ApplyVolumesImmediate();

            float elapsed = 0f;
            while (elapsed < holdSeconds)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                yield return null;
            }

            float restoreDuration = 0.65f;
            float t = 0f;
            while (t < restoreDuration)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                musicDuckMultiplier = Mathf.Lerp(duckLevel, 1f, Mathf.Clamp01(t / restoreDuration));
                ApplyVolumesImmediate();
                yield return null;
            }

            musicDuckMultiplier = 1f;
            ApplyVolumesImmediate();
            duckCoroutine = null;
        }

        private void ResetMusicDucking()
        {
            if (duckCoroutine != null)
            {
                StopCoroutine(duckCoroutine);
                duckCoroutine = null;
            }
            musicDuckMultiplier = 1f;
            ApplyVolumesImmediate();
        }

        private void PlaySfxInternal(SfxType sfx, AudioSource channel, float volumeScale, float pitch, float cooldownSeconds)
        {
            if (sfxVolume <= 0.0001f || masterVolume <= 0.0001f) return;
            if (library == null) LoadLibrary();
            if (library == null || channel == null) return;

            float now = Time.unscaledTime;
            if (lastSfxPlayTimes.TryGetValue(sfx, out float lastTime) && (now - lastTime) < cooldownSeconds)
            {
                return;
            }

            AudioClip clip = library.GetSfxClip(sfx, out float balanceScale);
            if (clip == null) return;

            lastSfxPlayTimes[sfx] = now;
            channel.pitch = pitch;
            channel.volume = sfxVolume;
            channel.PlayOneShot(clip, Mathf.Clamp01(balanceScale * volumeScale));
        }
    }

    /// <summary>
    /// Automatically attached to UI Buttons and Toggles by GameAudioManager so every click/tap
    /// across desktop and mobile plays click.wav cleanly unless suppressed for custom SFX (e.g. Chop/Mix).
    /// </summary>
    [DisallowMultipleComponent]
    public class UIClickAudioHook : MonoBehaviour
    {
        [SerializeField] private bool suppressDefaultClick;

        private Button cachedButton;
        private Toggle cachedToggle;
        private bool isBound;

        public bool SuppressDefaultClick
        {
            get => suppressDefaultClick;
            set => suppressDefaultClick = value;
        }

        public static void SetSuppressed(Component target, bool suppressed = true)
        {
            if (target == null) return;
            UIClickAudioHook hook = target.GetComponent<UIClickAudioHook>();
            if (hook == null) hook = target.gameObject.AddComponent<UIClickAudioHook>();
            hook.suppressDefaultClick = suppressed;
        }

        private void Awake()
        {
            BindIfNeeded();
        }

        private void OnEnable()
        {
            BindIfNeeded();
        }

        private void BindIfNeeded()
        {
            if (isBound) return;
            isBound = true;

            cachedButton = GetComponent<Button>();
            if (cachedButton != null)
            {
                cachedButton.onClick.AddListener(HandleButtonClick);
            }

            cachedToggle = GetComponent<Toggle>();
            if (cachedToggle != null)
            {
                cachedToggle.onValueChanged.AddListener(HandleToggleChanged);
            }
        }

        private void OnDestroy()
        {
            if (cachedButton != null) cachedButton.onClick.RemoveListener(HandleButtonClick);
            if (cachedToggle != null) cachedToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandleButtonClick()
        {
            if (suppressDefaultClick) return;
            GameAudioManager.Instance?.PlayClick();
        }

        private void HandleToggleChanged(bool _)
        {
            if (suppressDefaultClick) return;
            GameAudioManager.Instance?.PlayClick(0.85f, 1.05f);
        }
    }
}
