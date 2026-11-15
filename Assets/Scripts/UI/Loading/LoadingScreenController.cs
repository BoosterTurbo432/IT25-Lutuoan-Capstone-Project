using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// Dedicated Loading Screen Controller optimized for WebGL deployments.
    /// Manages initial game boot (forwarding to mainmenuscene) and in-game transitions
    /// (forwarding to gamescene). Features smooth asynchronous scene activation,
    /// progressive bar easing, educational culinary tips, and memory cleanup cycles.
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("Root & Transitions")]
        [Tooltip("Master CanvasGroup for smooth scene fade-in and fade-out.")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Tooltip("Duration of the fade-in at the start of loading.")]
        [SerializeField] private float fadeInDuration = 0.25f;

        [Tooltip("Duration of the fade-out before activating the destination scene.")]
        [SerializeField] private float fadeOutDuration = 0f;

        [Header("Progress Bar")]
        [Tooltip("Slider displaying load progress (0.0 to 1.0).")]
        [SerializeField] private Slider progressBar;

        [Tooltip("Direct Image fill reference (optional fallback if Slider is not used).")]
        [SerializeField] private Image progressFillImage;

        [Tooltip("Text showing load percentage (e.g. '75%').")]
        [SerializeField] private TMP_Text percentText;

        [Tooltip("Speed at which the displayed progress bar smoothly catches up to actual load progress.")]
        [SerializeField] private float progressSmoothingSpeed = 2.4f;

        [Header("Status & Cooking Tips")]
        [Tooltip("Label displaying the current loading stage or culinary task.")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("Label displaying educational culinary trivia/tips.")]
        [SerializeField] private TMP_Text tipText;

        [Tooltip("CanvasGroup wrapping the tip label for smooth cross-fading when tips rotate.")]
        [SerializeField] private CanvasGroup tipCanvasGroup;

        [Tooltip("Seconds before switching to the next educational tip.")]
        [SerializeField] private float tipRotateInterval = 3.2f;

        [Header("Cooking Animation")]
        [Tooltip("Optional icon (e.g. chef utensil / pot) that gently bobs while loading.")]
        [SerializeField] private RectTransform cookingIcon;

        [Tooltip("Vertical bobbing amplitude in pixels.")]
        [SerializeField] private float iconBobAmplitude = 8f;

        [Tooltip("Speed of the bobbing motion.")]
        [SerializeField] private float iconBobSpeed = 4f;

        [Header("Optimization & Timing (WebGL)")]
        [Tooltip("Minimum time the loading screen is displayed on initial boot to prevent single-frame flickering.")]
        [SerializeField] private float minBootDisplayTime = 0.9f;

        [Tooltip("Minimum time the loading screen is displayed during in-game scene transitions.")]
        [SerializeField] private float minTransitionDisplayTime = 0.65f;

        [Tooltip("Execute memory cleanup (UnloadUnusedAssets + GC) before scene activation.")]
        [SerializeField] private bool cleanMemoryBeforeActivation = true;

        // Randomly cycled tips: Lutuoan gameplay tips, Filipino chef wisdom, and Philippine carinderia fun facts
        private static readonly string[] CulinaryTips = new[]
        {
            // Lutuoan Game Tips
            "Lutuoan Tip — Drag and drop ingredients into an idle pot, and serve cooked dishes fast to beat the leaderboard!",
            "Lutuoan Tip — Keep an eye on your cooking pot timer! Pulling a dish on time keeps it from burning.",
            "Lutuoan Tip — Mix Tomato, Meat, Cheese, and Mushroom across both cooking pots to unlock richer house dishes.",
            "Lutuoan Tip — Run two pots at once to prep multiple orders faster and shave seconds off your clear time.",
            "Lutuoan Tip — Pantry slots restock automatically, so keep your eyes on the pots and your hands moving!",
            "Lutuoan Tip — Combining Meat, Tomato, and Cheese creates a Hearty Meat & Tomato Casserole!",

            // Filipino Chef Wisdom & Quotes
            "Chef Wisdom — As Chef Nora Daza popularized in Filipino home cooking, careful prep and measured heat turn simple ingredients into classics.",
            "Chef Wisdom — Chef Claude Tayag often celebrates kapampangan-style patience: slow sautéing of bawang, sibuyas, and kamatis builds deep flavor.",
            "Chef Wisdom — Chef Margarita Forés championed fresh local produce—the brighter your market ingredients, the less seasoning a dish needs.",
            "Chef Wisdom — Following Chef Tatung Sarthou's Simpol approach, balancing sour, salty, sweet, and umami brings every ulam together.",
            "Chef Wisdom — Chef JP Anglo reminds young cooks that bold local flavors—like calamansi, vinegar, and native chilies—make a dish unforgettable.",
            "Chef Wisdom — In the spirit of Chef Boy Logro, great kitchen energy starts with clean mise en place before the stove even heats up.",

            // Philippine Carinderia Fun Facts
            "Carinderia Fun Fact — The word 'carinderia' comes from 'cari' (a meat-and-vegetable stew like kare-kare) and '-eria' (the place where it is sold).",
            "Carinderia Fun Fact — Carinderias are nicknamed 'turo-turo' ('point-point') because diners simply point at the glass-covered trays of cooked ulam!",
            "Carinderia Fun Fact — Ask for 'sabaw' at a carinderia and many cooks will happily ladle free hot sinigang or nilaga broth over your rice.",
            "Carinderia Fun Fact — Ginisang monggo is famously a Friday carinderia tradition across the Philippines, often paired with fried galunggong or pork.",
            "Carinderia Fun Fact — Many carinderias serve 'kalahati' (half-orders), letting you mix two different ulam on one plate of steamed rice.",
            "Carinderia Fun Fact — Adobo is a carinderia staple because its vinegar, soy sauce, and garlic base stays delicious throughout a busy lunch rush.",
            "Carinderia Fun Fact — Dinuguan paired with soft, slightly sweet steamed puto is one of the most iconic combos on a carinderia counter.",
            "Carinderia Fun Fact — Every carinderia has a 'sawsawan' station—calamansi, patis, sili, and spiced vinegar—to customize every bite.",
            "Carinderia Fun Fact — Tortang talong (smoky grilled eggplant omelette) and lumpiang togue are beloved budget heroes of Filipino carinderias.",
            "Carinderia Fun Fact — Early-morning carinderias often serve lugaw, goto, and tapsilog before bringing out the big kaldero pots for lunch!"
        };

        private string targetScene;
        private bool isInitialBoot;
        private float displayedProgress;
        private Vector2 originalIconPosition;
        private readonly List<int> shuffledTipOrder = new List<int>();
        private int shuffledCursor;
        private int lastTipIndex = -1;
        private Coroutine loadRoutine;

        private void Awake()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
            }

            if (cookingIcon != null)
            {
                originalIconPosition = cookingIcon.anchoredPosition;
            }

            // Determine destination scene from LoadingBridge or fallback to gameplay scene
            targetScene = LoadingBridge.TargetSceneName;
            if (string.IsNullOrEmpty(targetScene))
            {
#if UNITY_EDITOR
                targetScene = LoadingBridge.DefaultGameplayScene;
#else
                targetScene = LoadingBridge.DefaultMainMenuScene;
#endif
            }

            isInitialBoot = LoadingBridge.IsInitialBoot;
            ReshuffleTips();
        }

        private void Start()
        {
            loadRoutine = StartCoroutine(LoadSceneSequence());
        }

        private void OnDestroy()
        {
            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }
        }

        private const float CleanupBarShare = 0.15f;
        private const float PreloadEndBarShare = 0.90f;
        private const float FadeOutEndBarShare = 0.95f;

        private IEnumerator LoadSceneSequence()
        {
            float minDuration = isInitialBoot ? minBootDisplayTime : minTransitionDisplayTime;
            float elapsedTime = 0f;
            float tipTimer = 0f;
            displayedProgress = 0f;

            UpdateProgressVisuals(0f);
            SetStatusMessage(GetProgressStatus(0f));
            SetTipText(GetNextRandomTip());

            if (string.Equals(targetScene, LoadingBridge.DefaultLoadingScene, StringComparison.OrdinalIgnoreCase))
            {
                targetScene = LoadingBridge.DefaultMainMenuScene;
            }

            // Phase 1: Smooth Fade-In (0% -> ~8% of bar) before memory cleanup
            if (rootCanvasGroup != null && fadeInDuration > 0f)
            {
                float fadeT = 0f;
                while (fadeT < fadeInDuration)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    fadeT += dt;
                    elapsedTime += dt;
                    tipTimer += dt;
                    float fadeFraction = Mathf.Clamp01(fadeT / fadeInDuration);
                    rootCanvasGroup.alpha = fadeFraction;
                    displayedProgress = Mathf.Max(displayedProgress, fadeFraction * (CleanupBarShare * 0.5f));
                    UpdateProgressVisuals(displayedProgress);
                    SetStatusMessage(GetProgressStatus(displayedProgress));
                    AnimateCookingIcon(elapsedTime);
                    yield return null;
                }
                rootCanvasGroup.alpha = 1f;
            }
            else
            {
                if (rootCanvasGroup != null)
                {
                    rootCanvasGroup.alpha = 1f;
                }
                // Let the loading screen render one frame before running memory cleanup
                yield return null;
            }

            // Phase 2: WebGL Memory Optimization Cycle BEFORE starting LoadSceneAsync.
            // Calling UnloadUnusedAssets while LoadSceneAsync is paused at allowSceneActivation = false
            // deadlocks Unity's PreloadManager async operation queue.
            if (cleanMemoryBeforeActivation)
            {
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            // Phase 3: Asynchronous Scene Preload (-> 90% of bar)
            AsyncOperation asyncOp = null;
            try
            {
                asyncOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadingScreenController] Failed to begin loading '{targetScene}': {ex.Message}");
            }

            if (asyncOp == null)
            {
                Debug.LogError($"[LoadingScreenController] Scene '{targetScene}' could not be loaded async. Aborting to main menu.");
                SceneManager.LoadScene(LoadingBridge.DefaultMainMenuScene);
                yield break;
            }

            // Hold activation until preload, minimum display time, and smooth progress complete
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                elapsedTime += dt;
                tipTimer += dt;

                // Unity asyncOp.progress stops at 0.9 while allowSceneActivation is false
                float rawPreload = Mathf.Clamp01(asyncOp.progress / 0.9f);
                float timeFraction = minDuration > 0f ? Mathf.Clamp01(elapsedTime / minDuration) : 1f;
                float preloadTarget = Mathf.Lerp(CleanupBarShare, PreloadEndBarShare, Mathf.Min(rawPreload, timeFraction));

                displayedProgress = Mathf.Max(
                    displayedProgress,
                    Mathf.MoveTowards(displayedProgress, preloadTarget, progressSmoothingSpeed * dt));

                UpdateProgressVisuals(displayedProgress);
                SetStatusMessage(GetProgressStatus(displayedProgress));
                AnimateCookingIcon(elapsedTime);

                // Rotate educational tips periodically with smooth cross-fade
                if (tipTimer >= tipRotateInterval)
                {
                    tipTimer = 0f;
                    yield return StartCoroutine(CycleNextTip());
                }

                if ((asyncOp.progress >= 0.899f || asyncOp.isDone) && elapsedTime >= minDuration && displayedProgress >= PreloadEndBarShare - 0.001f)
                {
                    break;
                }

                yield return null;
            }

            displayedProgress = Mathf.Max(displayedProgress, PreloadEndBarShare);
            UpdateProgressVisuals(displayedProgress);

            // Phase 4: Optional Fade-Out (if configured) before scene activation
            if (rootCanvasGroup != null && fadeOutDuration > 0f)
            {
                float fadeOutT = 0f;
                while (fadeOutT < fadeOutDuration)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    fadeOutT += dt;
                    elapsedTime += dt;
                    float fadeFraction = Mathf.Clamp01(fadeOutT / fadeOutDuration);
                    rootCanvasGroup.alpha = 1f - fadeFraction;
                    displayedProgress = Mathf.Lerp(PreloadEndBarShare, FadeOutEndBarShare, fadeFraction);
                    UpdateProgressVisuals(displayedProgress);
                    AnimateCookingIcon(elapsedTime);
                    yield return null;
                }
                rootCanvasGroup.alpha = 0f;
            }

            // Phase 5: Activate Destination Scene (90% -> 100% tracks real scene activation)
            SetStatusMessage(isInitialBoot ? "Welcome to the Kitchen!" : "Ready to Cook!");
            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                elapsedTime += dt;
                float activationFraction = Mathf.Clamp01((asyncOp.progress - 0.9f) / 0.1f);
                float activationTarget = Mathf.Lerp(PreloadEndBarShare, 1f, activationFraction);
                displayedProgress = Mathf.Max(
                    displayedProgress,
                    Mathf.MoveTowards(displayedProgress, activationTarget, progressSmoothingSpeed * dt));
                UpdateProgressVisuals(displayedProgress);
                AnimateCookingIcon(elapsedTime);
                yield return null;
            }

            UpdateProgressVisuals(1f);
        }

        private void UpdateProgressVisuals(float progress)
        {
            float clamped = Mathf.Clamp01(progress);

            if (progressBar != null)
            {
                progressBar.value = clamped;
            }

            if (progressFillImage != null)
            {
                progressFillImage.enabled = clamped > 0.001f;
                progressFillImage.fillAmount = clamped;
            }

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
            }
        }

        private string GetProgressStatus(float progress)
        {
            if (!string.IsNullOrEmpty(LoadingBridge.CustomStatusMessage))
            {
                return LoadingBridge.CustomStatusMessage;
            }

            if (isInitialBoot)
            {
                if (progress < 0.25f) return "Lighting the stoves & preheating...";
                if (progress < 0.55f) return "Washing & stocking fresh pantry ingredients...";
                if (progress < 0.85f) return "Sharpening culinary knives & setting stations...";
                return "Preparing the Main Menu...";
            }
            else
            {
                if (progress < 0.25f) return "Gathering kitchen equipment...";
                if (progress < 0.55f) return "Prepping recipe ingredients...";
                if (progress < 0.85f) return "Simmering stock & warming cooking pans...";
                return "Ready to serve!";
            }
        }

        private void SetStatusMessage(string message)
        {
            if (statusText != null && statusText.text != message)
            {
                statusText.text = message;
            }
        }

        private void SetTipText(string tip)
        {
            if (tipText != null)
            {
                tipText.text = tip;
            }
        }

        private IEnumerator CycleNextTip()
        {
            if (tipText == null || CulinaryTips.Length <= 1) yield break;

            // Fade out tip
            if (tipCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.2f)
                {
                    t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    tipCanvasGroup.alpha = Mathf.Clamp01(1f - (t / 0.2f));
                    yield return null;
                }
                tipCanvasGroup.alpha = 0f;
            }

            // Switch to next shuffled tip
            SetTipText(GetNextRandomTip());

            // Fade in tip
            if (tipCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.2f)
                {
                    t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    tipCanvasGroup.alpha = Mathf.Clamp01(t / 0.2f);
                    yield return null;
                }
                tipCanvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Returns the next randomized tip from a shuffled bag so tips don't repeat consecutively.
        /// </summary>
        public string GetNextRandomTip()
        {
            if (CulinaryTips.Length == 0) return string.Empty;

            if (shuffledCursor >= shuffledTipOrder.Count)
            {
                ReshuffleTips();
            }

            int index = shuffledTipOrder[shuffledCursor++];
            lastTipIndex = index;
            return CulinaryTips[index];
        }

        private void ReshuffleTips()
        {
            shuffledTipOrder.Clear();
            for (int i = 0; i < CulinaryTips.Length; i++)
            {
                shuffledTipOrder.Add(i);
            }

            for (int i = shuffledTipOrder.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = shuffledTipOrder[i];
                shuffledTipOrder[i] = shuffledTipOrder[j];
                shuffledTipOrder[j] = temp;
            }

            // Avoid repeating the immediately preceding tip across reshuffle boundaries
            if (shuffledTipOrder.Count > 1 && shuffledTipOrder[0] == lastTipIndex)
            {
                int swapIdx = UnityEngine.Random.Range(1, shuffledTipOrder.Count);
                int temp = shuffledTipOrder[0];
                shuffledTipOrder[0] = shuffledTipOrder[swapIdx];
                shuffledTipOrder[swapIdx] = temp;
            }

            shuffledCursor = 0;
        }

        private void AnimateCookingIcon(float elapsed)
        {
            if (cookingIcon == null) return;

            float yOffset = Mathf.Sin(elapsed * iconBobSpeed) * iconBobAmplitude;
            cookingIcon.anchoredPosition = new Vector2(originalIconPosition.x, originalIconPosition.y + yOffset);
        }
    }
}
