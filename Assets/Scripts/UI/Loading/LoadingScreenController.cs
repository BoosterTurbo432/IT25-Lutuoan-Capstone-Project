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
        [SerializeField] private float fadeOutDuration = 0.35f;

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

        // Educational culinary tips matching the capstone theme
        private static readonly string[] CulinaryTips = new[]
        {
            "Mise en place (everything in its place): Prepping ingredients before cooking prevents burning and stress!",
            "Preheating your pan ensures a golden, even sear and keeps delicate foods from sticking.",
            "Season in layers: Adding seasonings at each stage builds balanced and deep flavours.",
            "Keep knives sharp! A dull knife requires excessive force and is much more likely to slip.",
            "Pat wet proteins dry with a clean paper towel before searing to get a crispy golden crust.",
            "Avoid overcrowding the pan! Crowded ingredients steam in their own moisture instead of browning.",
            "Always wash hands, prep boards, and utensils after handling raw ingredients.",
            "Taste as you cook: Flavours develop and concentrate as liquids simmer and reduce.",
            "Resting cooked meats allows natural juices to redistribute evenly rather than spilling out.",
            "A touch of acid (lemon juice or vinegar) brightens heavy sauces and balances rich fats."
        };

        private string targetScene;
        private bool isInitialBoot;
        private float displayedProgress;
        private Vector2 originalIconPosition;
        private int currentTipIndex;
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
            currentTipIndex = UnityEngine.Random.Range(0, CulinaryTips.Length);
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

        private IEnumerator LoadSceneSequence()
        {
            float minDuration = isInitialBoot ? minBootDisplayTime : minTransitionDisplayTime;
            float elapsedTime = 0f;
            float tipTimer = 0f;
            displayedProgress = 0f;

            UpdateProgressVisuals(0f);
            SetStatusMessage(GetProgressStatus(0f));
            SetTipText(CulinaryTips[currentTipIndex]);

            // Phase 1: Smooth Fade-In
            if (rootCanvasGroup != null && fadeInDuration > 0f)
            {
                float fadeT = 0f;
                while (fadeT < fadeInDuration)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    fadeT += dt;
                    elapsedTime += dt;
                    rootCanvasGroup.alpha = Mathf.Clamp01(fadeT / fadeInDuration);
                    AnimateCookingIcon(elapsedTime);
                    yield return null;
                }
                rootCanvasGroup.alpha = 1f;
            }
            else if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 1f;
            }

            // Phase 2: Start Asynchronous Scene Loading
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

            // Keep scene inactive until progress finishes and minimum display time passes
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                elapsedTime += dt;
                tipTimer += dt;

                // Unity asyncOp.progress stops at 0.9 when allowSceneActivation is false
                float rawProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);

                // Smooth progress bar to prevent abrupt visual jumps on WebGL
                displayedProgress = Mathf.MoveTowards(displayedProgress, rawProgress, progressSmoothingSpeed * dt);

                UpdateProgressVisuals(displayedProgress);
                SetStatusMessage(GetProgressStatus(displayedProgress));
                AnimateCookingIcon(elapsedTime);

                // Rotate educational tips periodically with smooth cross-fade
                if (tipTimer >= tipRotateInterval)
                {
                    tipTimer = 0f;
                    yield return StartCoroutine(CycleNextTip());
                }

                // Check completion: async load complete (>=0.9) AND visual bar filled (>=0.999) AND min time elapsed
                if (asyncOp.progress >= 0.9f && displayedProgress >= 0.999f && elapsedTime >= minDuration)
                {
                    break;
                }

                yield return null;
            }

            // Snap visuals to 100% complete
            displayedProgress = 1f;
            UpdateProgressVisuals(1f);
            SetStatusMessage(isInitialBoot ? "Welcome to the Kitchen!" : "Ready to Cook!");

            // Phase 3: WebGL Memory Optimization Cycle
            if (cleanMemoryBeforeActivation)
            {
                // Clean unreferenced assets from memory before activating next scene (vital for WebGL 32-bit heap)
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            // Brief hold on 100% so player sees completion
            yield return new WaitForSecondsRealtime(0.12f);

            // Phase 4: Smooth Fade-Out
            if (rootCanvasGroup != null && fadeOutDuration > 0f)
            {
                float fadeOutT = 0f;
                while (fadeOutT < fadeOutDuration)
                {
                    float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                    fadeOutT += dt;
                    rootCanvasGroup.alpha = Mathf.Clamp01(1f - (fadeOutT / fadeOutDuration));
                    yield return null;
                }
                rootCanvasGroup.alpha = 0f;
            }

            // Phase 5: Activate Destination Scene
            asyncOp.allowSceneActivation = true;
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

            // Switch to next tip
            currentTipIndex = (currentTipIndex + 1) % CulinaryTips.Length;
            SetTipText(CulinaryTips[currentTipIndex]);

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

        private void AnimateCookingIcon(float elapsed)
        {
            if (cookingIcon == null) return;

            float yOffset = Mathf.Sin(elapsed * iconBobSpeed) * iconBobAmplitude;
            cookingIcon.anchoredPosition = new Vector2(originalIconPosition.x, originalIconPosition.y + yOffset);
        }
    }
}
