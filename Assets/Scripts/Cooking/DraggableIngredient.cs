using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    public enum IngredientType
    {
        Tomato,
        Cheese,
        Meat,
        Mushroom,
        Onion,
        Herbs
    }

    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Ingredient Identity")]
        [Tooltip("The type of ingredient used for recipe validation.")]
        [SerializeField] private IngredientType ingredientType = IngredientType.Tomato;

        [Tooltip("Display name shown in UI/recipes.")]
        [SerializeField] private string ingredientName = "Tomato";

        [Header("Drag Visual Feedback")]
        [Tooltip("Alpha transparency applied to the CanvasGroup while dragging.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float dragAlpha = 0.75f;

        [Tooltip("Scale multiplier applied to the RectTransform when picked up.")]
        [SerializeField] private Vector3 dragScale = new Vector3(1.1f, 1.1f, 1f);

        [Header("Snap Back Animation")]
        [Tooltip("Duration in seconds for the smooth return animation if dropped on an invalid target.")]
        [SerializeField] private float snapBackDuration = 0.2f;

        [Tooltip("Animation curve controlling the return easing.")]
        [SerializeField] private AnimationCurve snapBackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Cached components
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Image image;
        private Canvas rootCanvas;

        // Hierarchy and state tracking
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalScale;
        private bool isDroppedOnStation;
        private Coroutine snapBackCoroutine;

        public IngredientType Type => ingredientType;
        public string IngredientName => ingredientName;
        public bool IsBeingDragged { get; private set; }

        /// <summary>
        /// Fires when the ingredient is successfully placed and accepted into a cooking station.
        /// </summary>
        public event System.Action<DraggableIngredient> onAccepted;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();

            // Find the outermost Canvas responsible for rendering this element
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                rootCanvas = rootCanvas.rootCanvas;
            }

            originalScale = rectTransform.localScale;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDroppedOnStation) return;

            // Stop any ongoing snap-back coroutine
            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            IsBeingDragged = true;

            if (rootCanvas == null)
            {
                var parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;
            }

            // 1. Cache return position and hierarchy
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;

            // 2. Reparent to root canvas so this element renders above all UI panels
            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
            }
            transform.SetAsLastSibling();

            // 3. Disable raycast blocking so drop zones underneath can receive IDropHandler events
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = dragAlpha;
            rectTransform.localScale = Vector3.Scale(originalScale, dragScale);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsBeingDragged || isDroppedOnStation) return;

            // Scale movement delta according to canvas scale factor to support any resolution/DPI
            float scaleFactor = (rootCanvas != null) ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsBeingDragged) return;
            IsBeingDragged = false;

            // Re-enable raycast blocking
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            rectTransform.localScale = originalScale;

            // If the ingredient was successfully consumed or placed into a station, do not snap back
            if (isDroppedOnStation) return;

            // Invalid drop: Smoothly snap back to original position
            ReturnToOrigin();
        }

        /// <summary>
        /// Called by CookingStation when this ingredient is accepted.
        /// </summary>
        public void NotifyAcceptedByStation(Transform targetContainer)
        {
            isDroppedOnStation = true;
            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1f;
            rectTransform.localScale = originalScale;

            if (targetContainer != null)
            {
                transform.SetParent(targetContainer, false);
                rectTransform.anchoredPosition = Vector2.zero;
            }

            onAccepted?.Invoke(this);
        }

        /// <summary>
        /// Restores the ingredient to its original shelf/pantry slot.
        /// </summary>
        public void ReturnToOrigin(bool instant = false)
        {
            if (instant)
            {
                if (originalParent != null)
                {
                    transform.SetParent(originalParent, false);
                    transform.SetSiblingIndex(originalSiblingIndex);
                }
                rectTransform.anchoredPosition = originalAnchoredPosition;
                rectTransform.localScale = originalScale;
                return;
            }

            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = StartCoroutine(SnapBackRoutine());
        }

        private IEnumerator SnapBackRoutine()
        {
            Vector2 startPos = rectTransform.position;
            Vector3 targetWorldPos;

            if (originalParent != null)
            {
                // Calculate target world position based on original parent and anchored position
                targetWorldPos = originalParent.TransformPoint(originalAnchoredPosition);
            }
            else
            {
                targetWorldPos = rectTransform.position;
            }

            float elapsed = 0f;
            while (elapsed < snapBackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / snapBackDuration);
                float curveT = snapBackCurve.Evaluate(t);

                rectTransform.position = Vector3.Lerp(startPos, targetWorldPos, curveT);
                yield return null;
            }

            // Restore original hierarchy and exact position
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localScale = originalScale;
            snapBackCoroutine = null;
        }

        /// <summary>
        /// Resets the ingredient state for reuse (e.g., when clearing cooking pot).
        /// </summary>
        public void ResetState()
        {
            isDroppedOnStation = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            ReturnToOrigin(true);
        }
    }
}
