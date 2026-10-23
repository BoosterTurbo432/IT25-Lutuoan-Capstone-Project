using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// A modal popup with a fade + "pop" scale transition driven by a CanvasGroup.
    /// Close buttons / backdrop clicks raise <see cref="CloseRequested"/> so a manager can
    /// keep track of the popup stack. If nothing is listening, the popup simply hides itself.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPopupPanel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The visible window/card that pops in. Defaults to this RectTransform.")]
        [SerializeField] private RectTransform window;

        [Tooltip("Receives keyboard/gamepad focus when the popup opens.")]
        [SerializeField] private Selectable firstSelected;

        [Tooltip("Buttons that close this popup (X, Back, Cancel...).")]
        [SerializeField] private Button[] closeButtons;

        [Tooltip("Optional full-screen backdrop Button; clicking outside the window closes the popup.")]
        [SerializeField] private Button backdropButton;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.25f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.85f;

        [Header("Events")]
        public UnityEvent onShown;
        public UnityEvent onHidden;

        /// <summary>Raised when a close button, the backdrop, or <see cref="RequestClose"/> asks to close.</summary>
        public event Action<UIPopupPanel> CloseRequested;

        public bool IsOpen { get; private set; }
        public Selectable FirstSelected => firstSelected;

        private CanvasGroup canvasGroup;
        private Vector3 windowBaseScale = Vector3.one;
        private Coroutine transitionRoutine;
        private bool isSuspended;
        private bool selectFirstOnShown;
        private bool isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            // If deactivated externally mid-transition, make sure state stays consistent.
            transitionRoutine = null;
        }

        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            canvasGroup = GetComponent<CanvasGroup>();
            if (window == null) window = transform as RectTransform;
            windowBaseScale = window.localScale;

            if (closeButtons != null)
            {
                foreach (Button button in closeButtons)
                {
                    if (button != null) button.onClick.AddListener(RequestClose);
                }
            }

            if (backdropButton != null) backdropButton.onClick.AddListener(RequestClose);
        }

        /// <summary>Opens the popup with a pop-in animation.</summary>
        /// <param name="selectFirst">Give focus to <see cref="FirstSelected"/> once shown (for keyboard/gamepad users).</param>
        public void Show(bool selectFirst = true)
        {
            Initialize();
            if (IsOpen) return;

            if (!gameObject.activeSelf)
            {
                canvasGroup.alpha = 0f;
                window.localScale = windowBaseScale * hiddenScale;
            }

            IsOpen = true;
            selectFirstOnShown = selectFirst;
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // Render above any other open popup.

            canvasGroup.blocksRaycasts = true; // Block clicks immediately...
            canvasGroup.interactable = false;  // ...but only allow interaction after the pop-in (prevents double-click-through).

            StartTransition(1f, 1f, showDuration, EaseOutBack, OnShowComplete);
        }

        /// <summary>Closes the popup with a fade-out animation, then deactivates it.</summary>
        public void Hide()
        {
            Initialize();
            if (!IsOpen) return;

            IsOpen = false;
            isSuspended = false;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false; // Let clicks reach the menu right away for a snappy feel.

            StartTransition(0f, hiddenScale, hideDuration, EaseInQuad, OnHideComplete);
        }

        /// <summary>Immediately hides and deactivates the popup without animation or events.</summary>
        public void HideInstant()
        {
            Initialize();
            StopTransition();

            IsOpen = false;
            isSuspended = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            window.localScale = windowBaseScale * hiddenScale;
            gameObject.SetActive(false);
        }

        /// <summary>Temporarily disables interaction (e.g. while another popup is stacked on top).</summary>
        public void SetSuspended(bool suspended)
        {
            Initialize();
            isSuspended = suspended;
            UpdateInteractable();
        }

        /// <summary>Asks the owner to close this popup. Hook this to any extra close buttons.</summary>
        public void RequestClose()
        {
            if (!IsOpen) return;

            if (CloseRequested != null) CloseRequested.Invoke(this);
            else Hide();
        }

        private void OnShowComplete()
        {
            UpdateInteractable();

            if (selectFirstOnShown && !isSuspended && firstSelected != null && firstSelected.IsInteractable()
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }

            onShown?.Invoke();
        }

        private void OnHideComplete()
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }

        private void UpdateInteractable()
        {
            canvasGroup.interactable = IsOpen && !isSuspended && transitionRoutine == null;
        }

        private void StartTransition(float targetAlpha, float targetScaleFactor, float duration,
            Func<float, float> ease, Action onComplete)
        {
            StopTransition();

            // Coroutines can't run on inactive hierarchies (e.g. a disabled parent) - apply the end state instantly.
            if (!gameObject.activeInHierarchy || duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                window.localScale = windowBaseScale * targetScaleFactor;
                onComplete?.Invoke();
                return;
            }

            transitionRoutine = StartCoroutine(TransitionRoutine(targetAlpha, targetScaleFactor, duration, ease, onComplete));
        }

        private void StopTransition()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
        }

        private IEnumerator TransitionRoutine(float targetAlpha, float targetScaleFactor, float duration,
            Func<float, float> ease, Action onComplete)
        {
            // Start from the current values so interrupted transitions (open -> close quickly) stay smooth.
            float startAlpha = canvasGroup.alpha;
            Vector3 startScale = window.localScale;
            Vector3 endScale = windowBaseScale * targetScaleFactor;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Works even when the game is paused.
                float t = Mathf.Clamp01(elapsed / duration);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                window.localScale = Vector3.LerpUnclamped(startScale, endScale, ease(t));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            window.localScale = endScale;
            transitionRoutine = null;
            onComplete?.Invoke();
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private static float EaseInQuad(float t)
        {
            return t * t;
        }
    }
}
