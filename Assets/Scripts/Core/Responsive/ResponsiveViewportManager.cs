using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.Core.Responsive
{
    public enum ViewportOrientation
    {
        Landscape,
        Portrait
    }

    public enum DeviceFormFactor
    {
        Desktop,
        Mobile
    }

    public struct ViewportProfile
    {
        public int width;
        public int height;
        public float aspectRatio;
        public ViewportOrientation orientation;
        public DeviceFormFactor deviceType;
        public Rect safeArea;
    }

    /// <summary>
    /// Automatically detects browser/device viewport dimensions, aspect ratio, and
    /// Desktop vs Mobile form factor across all scenes. Adapts CanvasScalers so UI never
    /// clips on 16:10, 4:3, 16:9, 21:9, or Mobile Portrait/Landscape screens.
    /// </summary>
    [DisallowMultipleComponent]
    public class ResponsiveViewportManager : MonoBehaviour
    {
        public static ResponsiveViewportManager Instance { get; private set; }

        private const float PortraitAspectThreshold = 0.95f;
        private static readonly Vector2 LandscapeReference = new Vector2(1920f, 1080f);
        private static readonly Vector2 PortraitReference = new Vector2(1080f, 1920f);

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private Rect lastSafeArea;
        private bool forceRefreshNextFrame = true;

        private Func<ViewportOrientation, Vector2> customReferenceProvider;
        private Action<ViewportProfile> customLayoutHandler;

        public ViewportProfile CurrentProfile { get; private set; }
        public bool IsPortrait => CurrentProfile.orientation == ViewportOrientation.Portrait;
        public bool IsMobile => CurrentProfile.deviceType == DeviceFormFactor.Mobile;

        public event Action<ViewportProfile> OnViewportChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int GetBrowserIsMobile();

        [DllImport("__Internal")]
        private static extern int GetBrowserInnerWidth();

        [DllImport("__Internal")]
        private static extern int GetBrowserInnerHeight();
#endif

        public static ResponsiveViewportManager GetOrCreate()
        {
            EnsureInstance();
            return Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null) return;

            var existing = FindAnyObjectByType<ResponsiveViewportManager>();
            if (existing != null)
            {
                Instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            var go = new GameObject("ResponsiveViewportManager");
            Instance = go.AddComponent<ResponsiveViewportManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            forceRefreshNextFrame = true;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void RegisterAdaptiveSceneHandler(
            Func<ViewportOrientation, Vector2> referenceResolutionProvider,
            Action<ViewportProfile> onApplyLayout)
        {
            customReferenceProvider = referenceResolutionProvider;
            customLayoutHandler = onApplyLayout;
            forceRefreshNextFrame = true;
            EvaluateViewport(force: true);
        }

        public void UnregisterAdaptiveSceneHandler(Action<ViewportProfile> onApplyLayout)
        {
            if (customLayoutHandler == onApplyLayout)
            {
                customReferenceProvider = null;
                customLayoutHandler = null;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            customReferenceProvider = null;
            customLayoutHandler = null;
            forceRefreshNextFrame = true;
        }

        private void Update()
        {
            EvaluateViewport(forceRefreshNextFrame);
        }

        public void EvaluateViewport(bool force = false)
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            Rect safeArea = Screen.safeArea;

#if UNITY_WEBGL && !UNITY_EDITOR
            int browserW = GetBrowserInnerWidth();
            int browserH = GetBrowserInnerHeight();
            if (browserW > 0 && browserH > 0 && (Mathf.Abs(width - browserW) > 2 || Mathf.Abs(height - browserH) > 2))
            {
                // Keep Screen metrics authoritative for Canvas, while browser dimensions break any stale resize delay
                width = Mathf.Max(width, 1);
                height = Mathf.Max(height, 1);
            }
#endif

            if (!force &&
                width == lastScreenWidth &&
                height == lastScreenHeight &&
                safeArea == lastSafeArea)
            {
                return;
            }

            forceRefreshNextFrame = false;
            lastScreenWidth = width;
            lastScreenHeight = height;
            lastSafeArea = safeArea;

            float aspect = width / (float)height;
            ViewportOrientation orientation = aspect < PortraitAspectThreshold
                ? ViewportOrientation.Portrait
                : ViewportOrientation.Landscape;

            DeviceFormFactor deviceType = DetectDeviceType(width, height);

            var profile = new ViewportProfile
            {
                width = width,
                height = height,
                aspectRatio = aspect,
                orientation = orientation,
                deviceType = deviceType,
                safeArea = safeArea
            };

            CurrentProfile = profile;
            ApplyCanvasScalers(profile);
            customLayoutHandler?.Invoke(profile);
            OnViewportChanged?.Invoke(profile);
        }

        private void ApplyCanvasScalers(ViewportProfile profile)
        {
            Vector2 targetReference = customReferenceProvider != null
                ? customReferenceProvider(profile.orientation)
                : (profile.orientation == ViewportOrientation.Portrait ? LandscapeReference : LandscapeReference);

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas) continue;

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) continue;

                if (customReferenceProvider == null)
                {
                    // Non-adaptive scenes keep full horizontal fit without clipping on 4:3, 16:10, ultrawide, or mobile
                    targetReference = LandscapeReference;
                }

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = targetReference;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }

        private static DeviceFormFactor DetectDeviceType(int width, int height)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GetBrowserIsMobile() != 0)
            {
                return DeviceFormFactor.Mobile;
            }
#endif
            if (Application.isMobilePlatform)
            {
                return DeviceFormFactor.Mobile;
            }

            if (Input.touchSupported && Mathf.Min(width, height) <= 1000)
            {
                return DeviceFormFactor.Mobile;
            }

            return DeviceFormFactor.Desktop;
        }
    }
}
