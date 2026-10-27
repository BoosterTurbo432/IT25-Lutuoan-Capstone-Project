using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CookingSystem.UI
{
    /// <summary>
    /// Delivers a smooth, cozy "Pan Right" boot-up entrance animation for the Menu UI.
    /// Elements (Logo, Start Cooking, Settings, Credits, Exit buttons) start slightly shifted
    /// to the left and smoothly slide/pan right into place with a warm ease-out curve and alpha fade.
    /// </summary>
    public class UIPanRightIntro : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("The game logo RectTransform.")]
        [SerializeField] private RectTransform logoRect;

        [Tooltip("CanvasGroup for the logo (used for fading in). Added automatically if missing.")]
        [SerializeField] private CanvasGroup logoCanvasGroup;

        [Tooltip("Main menu buttons in the order they should cascade in (Start, Settings, Credits, Exit).")]
        [SerializeField] private List<RectTransform> menuButtons = new List<RectTransform>();

        [Tooltip("Main menu group CanvasGroup that controls background interactivity.")]
        [SerializeField] private CanvasGroup mainMenuGroup;

        [Tooltip("Optional CanvasGroup on the background to smoothly fade in on boot.")]
        [SerializeField] private CanvasGroup backgroundCanvasGroup;

        [Header("Pan Animation Configuration")]
        [Tooltip("Horizontal distance in pixels elements travel from left to right into their final position.")]
        [SerializeField] private float panDistance = 140f;

        [Tooltip("Delay before the Logo begins panning right.")]
        [SerializeField] private float logoDelay = 0.05f;

        [Tooltip("Duration of the Logo pan-right animation in seconds.")]
        [SerializeField] private float logoDuration = 0.75f;

        [Tooltip("Delay before the first menu button begins panning right.")]
        [SerializeField] private float buttonsStartDelay = 0.22f;

        [Tooltip("Staggered delay between each successive menu button.")]
        [SerializeField] private float buttonStaggerDelay = 0.09f;

        [Tooltip("Duration of each button's pan-right animation in seconds.")]
        [SerializeField] private float buttonDuration = 0.58f;

        [Tooltip("Duration of the background image fade-in.")]
        [SerializeField] private float backgroundFadeDuration = 0.50f;

        [Header("Options")]
        [Tooltip("Automatically plays the Pan Right entrance animation on Start.")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("Allow the player to skip the animation immediately with a click, keypress, or gamepad button.")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("Fired when all elements have smoothly settled into place.")]
        public UnityEvent onIntroComplete = new UnityEvent();

        private Vector2 originalLogoPos;
        private readonly List<Vector2> originalButtonPositions = new List<Vector2>();
        private readonly List<CanvasGroup> buttonCanvasGroups = new List<CanvasGroup>();

        private Coroutine introCoroutine;
        private bool isIntroPlaying;
        private bool hasFinished;

        public bool IsIntroPlaying => isIntroPlaying;
        public bool HasFinished => hasFinished;

        private void Awake()
        {
            RecordRestingPositions();
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayIntro();
            }
        }

        private void Update()
        {
            if (isIntroPlaying && allowSkip && WasSkipTriggered())
            {
                SkipToComplete();
            }
        }

        /// <summary>
        /// Records the original resting layout positions of the logo and buttons.
        /// </summary>
        public void RecordRestingPositions()
        {
            originalButtonPositions.Clear();
            buttonCanvasGroups.Clear();

            if (logoRect != null)
            {
                originalLogoPos = logoRect.anchoredPosition;
                if (logoCanvasGroup == null)
                {
                    logoCanvasGroup = logoRect.GetComponent<CanvasGroup>();
                    if (logoCanvasGroup == null) logoCanvasGroup = logoRect.gameObject.AddComponent<CanvasGroup>();
                }
            }

            foreach (RectTransform btn in menuButtons)
            {
                if (btn != null)
                {
                    originalButtonPositions.Add(btn.anchoredPosition);
                    CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                    if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
                    buttonCanvasGroups.Add(cg);
                }
            }
        }

        /// <summary>
        /// Starts the smooth Pan Right entrance animation sequence.
        /// </summary>
        public void PlayIntro()
        {
            if (introCoroutine != null) StopCoroutine(introCoroutine);

            // Block interactivity during the intro cascade
            if (mainMenuGroup != null)
            {
                mainMenuGroup.interactable = false;
                mainMenuGroup.blocksRaycasts = false;
            }

            // Prep initial positions shifted to the left and invisible
            if (logoRect != null)
            {
                logoRect.anchoredPosition = new Vector2(originalLogoPos.x - panDistance, originalLogoPos.y);
                if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;
            }

            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (menuButtons[i] != null)
                {
                    menuButtons[i].anchoredPosition = new Vector2(originalButtonPositions[i].x - panDistance, originalButtonPositions[i].y);
                }
                if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
                {
                    buttonCanvasGroups[i].alpha = 0f;
                }
            }

            if (backgroundCanvasGroup != null)
            {
                backgroundCanvasGroup.alpha = 0f;
            }

            hasFinished = false;
            isIntroPlaying = true;
            introCoroutine = StartCoroutine(IntroSequence());
        }

        private IEnumerator IntroSequence()
        {
            float elapsed = 0f;
            float totalDuration = Mathf.Max(
                logoDelay + logoDuration,
                buttonsStartDelay + (menuButtons.Count * buttonStaggerDelay) + buttonDuration
            );

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Animate background fade
                if (backgroundCanvasGroup != null && backgroundFadeDuration > 0f)
                {
                    float bgT = Mathf.Clamp01(elapsed / backgroundFadeDuration);
                    backgroundCanvasGroup.alpha = bgT;
                }

                // Animate Logo Pan Right
                if (logoRect != null && elapsed >= logoDelay)
                {
                    float logoT = Mathf.Clamp01((elapsed - logoDelay) / logoDuration);
                    float logoEase = EaseOutCubic(logoT);
                    float curX = Mathf.Lerp(originalLogoPos.x - panDistance, originalLogoPos.x, logoEase);
                    logoRect.anchoredPosition = new Vector2(curX, originalLogoPos.y);
                    if (logoCanvasGroup != null) logoCanvasGroup.alpha = logoEase;
                }

                // Animate each Menu Button Pan Right with staggered delay
                for (int i = 0; i < menuButtons.Count; i++)
                {
                    float btnStartTime = buttonsStartDelay + (i * buttonStaggerDelay);
                    if (elapsed >= btnStartTime && menuButtons[i] != null)
                    {
                        float btnT = Mathf.Clamp01((elapsed - btnStartTime) / buttonDuration);
                        float btnEase = EaseOutCubic(btnT);
                        float targetX = originalButtonPositions[i].x;
                        float curX = Mathf.Lerp(targetX - panDistance, targetX, btnEase);
                        menuButtons[i].anchoredPosition = new Vector2(curX, originalButtonPositions[i].y);

                        if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
                        {
                            buttonCanvasGroups[i].alpha = btnEase;
                        }
                    }
                }

                yield return null;
            }

            FinishIntro();
        }

        /// <summary>
        /// Immediately skips to the completed, settled state.
        /// </summary>
        public void SkipToComplete()
        {
            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
                introCoroutine = null;
            }

            FinishIntro();
        }

        private void FinishIntro()
        {
            // Snap all elements cleanly to their final resting positions & full opacity
            if (logoRect != null)
            {
                logoRect.anchoredPosition = originalLogoPos;
                if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f;
            }

            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (menuButtons[i] != null)
                {
                    menuButtons[i].anchoredPosition = originalButtonPositions[i];
                }
                if (i < buttonCanvasGroups.Count && buttonCanvasGroups[i] != null)
                {
                    buttonCanvasGroups[i].alpha = 1f;
                }
            }

            if (backgroundCanvasGroup != null)
            {
                backgroundCanvasGroup.alpha = 1f;
            }

            // Restore full menu interactivity
            if (mainMenuGroup != null)
            {
                mainMenuGroup.interactable = true;
                mainMenuGroup.blocksRaycasts = true;
            }

            isIntroPlaying = false;
            hasFinished = true;
            introCoroutine = null;

            onIntroComplete?.Invoke();
        }

        private static float EaseOutCubic(float t)
        {
            float f = 1f - t;
            return 1f - (f * f * f);
        }

        private bool WasSkipTriggered()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame ||
                                     keyboard.enterKey.wasPressedThisFrame ||
                                     keyboard.escapeKey.wasPressedThisFrame))
            {
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame ||
                                    gamepad.buttonEast.wasPressedThisFrame ||
                                    gamepad.startButton.wasPressedThisFrame))
            {
                return true;
            }
#else
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                return true;
            }
#endif
            return false;
        }
    }
}
