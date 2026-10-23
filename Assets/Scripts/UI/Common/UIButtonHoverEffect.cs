using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// Smooth hover / press / keyboard-select feedback for any UI Selectable (Button, Toggle, ...).
    /// Animates scale and (optionally) colour with frame-rate independent easing on unscaled time,
    /// so it keeps working while the game is paused (Time.timeScale = 0).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class UIButtonHoverEffect : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Scale")]
        [Tooltip("Transform to scale. Defaults to this object's RectTransform.")]
        [SerializeField] private RectTransform scaleTarget;

        [Tooltip("Scale multiplier while hovered / selected.")]
        [SerializeField, Range(1f, 1.5f)] private float hoverScale = 1.08f;

        [Tooltip("Scale multiplier while pressed.")]
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.95f;

        [Header("Colour Tint")]
        [Tooltip("If enabled, the Selectable's built-in Color Tint transition is turned off to avoid conflicts.")]
        [SerializeField] private bool useColorTint = true;

        [Tooltip("Graphic to tint. Defaults to the Selectable's Target Graphic.")]
        [SerializeField] private Graphic tintTarget;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.93f, 0.75f, 1f);
        [SerializeField] private Color pressedColor = new Color(0.9f, 0.78f, 0.55f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.6f);

        [Header("Animation")]
        [Tooltip("Higher = snappier. Values around 10-20 feel good.")]
        [SerializeField, Min(1f)] private float animationSpeed = 14f;

        [Tooltip("Also highlight when selected via keyboard/gamepad navigation (mouse clicks never count as selection highlight).")]
        [SerializeField] private bool highlightOnSelect = true;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;

        private const float ScaleEpsilonSqr = 0.000001f;
        private const float ColorEpsilon = 0.004f;

        private Selectable selectable;
        private Vector3 baseScale;
        private bool isPointerInside;
        private bool isPointerDown;
        private bool isSelected;
        private bool lastInteractable;
        private Coroutine animationRoutine;

        private bool ShouldTint => useColorTint && tintTarget != null;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            if (scaleTarget == null) scaleTarget = transform as RectTransform;
            if (tintTarget == null) tintTarget = selectable.targetGraphic;

            baseScale = scaleTarget.localScale;
            lastInteractable = selectable.interactable;

            // The built-in Color Tint would fight our colour animation on the same Graphic.
            if (ShouldTint && selectable.transition == Selectable.Transition.ColorTint)
            {
                selectable.transition = Selectable.Transition.None;
            }
        }

        private void OnEnable()
        {
            SnapToTargetState();
        }

        private void OnDisable()
        {
            // Coroutines stop automatically on disable; reset so the button never re-appears "stuck" enlarged.
            animationRoutine = null;
            isPointerInside = false;
            isPointerDown = false;
            isSelected = false;
            SnapToTargetState();
        }

        private void Update()
        {
            // Selectable has no "interactable changed" callback, so poll the flag (a cheap bool compare).
            if (selectable.interactable != lastInteractable)
            {
                lastInteractable = selectable.interactable;
                if (!lastInteractable) isPointerDown = false;
                AnimateToTargetState();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            if (selectable.IsInteractable()) PlayHoverSound();
            AnimateToTargetState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            AnimateToTargetState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            isPointerDown = true;
            AnimateToTargetState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            isPointerDown = false;
            AnimateToTargetState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Mouse clicks also "select" a button; only treat keyboard/gamepad/programmatic selection as a highlight.
            if (eventData is PointerEventData) return;

            isSelected = true;
            if (selectable.IsInteractable()) PlayHoverSound();
            AnimateToTargetState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            AnimateToTargetState();
        }

        private void GetTargetState(out Vector3 targetScale, out Color targetColor)
        {
            // Use the Selectable's own flag (not IsInteractable) so buttons behind a blocked CanvasGroup
            // keep their normal look instead of turning grey while a popup is open.
            if (!selectable.interactable)
            {
                targetScale = baseScale;
                targetColor = disabledColor;
                return;
            }

            bool isHighlighted = isPointerInside || (highlightOnSelect && isSelected);

            if (isPointerDown && isPointerInside)
            {
                targetScale = baseScale * pressedScale;
                targetColor = pressedColor;
            }
            else if (isHighlighted)
            {
                targetScale = baseScale * hoverScale;
                targetColor = hoverColor;
            }
            else
            {
                targetScale = baseScale;
                targetColor = normalColor;
            }
        }

        private void AnimateToTargetState()
        {
            if (!isActiveAndEnabled)
            {
                SnapToTargetState();
                return;
            }

            // A single routine re-reads the target every frame, so rapid state changes blend smoothly.
            if (animationRoutine == null)
            {
                animationRoutine = StartCoroutine(AnimateRoutine());
            }
        }

        private IEnumerator AnimateRoutine()
        {
            while (true)
            {
                GetTargetState(out Vector3 targetScale, out Color targetColor);

                // Exponential smoothing: frame-rate independent and naturally eases out.
                float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);

                scaleTarget.localScale = Vector3.Lerp(scaleTarget.localScale, targetScale, t);
                if (ShouldTint) tintTarget.color = Color.Lerp(tintTarget.color, targetColor, t);

                bool scaleDone = (scaleTarget.localScale - targetScale).sqrMagnitude < ScaleEpsilonSqr;
                bool colorDone = !ShouldTint || IsColorClose(tintTarget.color, targetColor);

                if (scaleDone && colorDone)
                {
                    SnapToTargetState();
                    break;
                }

                yield return null;
            }

            animationRoutine = null;
        }

        private void SnapToTargetState()
        {
            if (selectable == null || scaleTarget == null) return;

            GetTargetState(out Vector3 targetScale, out Color targetColor);
            scaleTarget.localScale = targetScale;
            if (ShouldTint) tintTarget.color = targetColor;
        }

        private void PlayHoverSound()
        {
            if (audioSource != null && hoverClip != null)
            {
                audioSource.PlayOneShot(hoverClip);
            }
        }

        private static bool IsColorClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < ColorEpsilon
                && Mathf.Abs(a.g - b.g) < ColorEpsilon
                && Mathf.Abs(a.b - b.b) < ColorEpsilon
                && Mathf.Abs(a.a - b.a) < ColorEpsilon;
        }
    }
}
