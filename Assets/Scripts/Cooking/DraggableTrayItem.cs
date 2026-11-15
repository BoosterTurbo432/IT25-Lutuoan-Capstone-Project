using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    public enum TrayItemKind
    {
        None,
        Meal,
        Drink
    }

    /// <summary>
    /// Draggable staged meal or filled drink resting in a FoodDisplaySlot.
    /// Dragged directly onto a CustomerSlotUI to deliver (or TrashCanDropZone to discard).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class DraggableTrayItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private float snapBackDuration = 0.16f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalScale;
        private bool isConsumed;
        private Coroutine snapBackCoroutine;

        public TrayItemKind Kind { get; private set; } = TrayItemKind.None;
        public DishPayload MealPayload { get; private set; }
        public DrinkType DrinkKind { get; private set; } = DrinkType.None;
        public FoodDisplaySlot OwnerSlot { get; private set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (iconImage == null) iconImage = GetComponent<Image>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;

            originalScale = rectTransform.localScale;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }

        public void SetRestPosition(Vector2 newAnchoredPos)
        {
            originalAnchoredPosition = newAnchoredPos;
            if (!isConsumed && rectTransform != null && (originalParent == null || rectTransform.parent == originalParent))
            {
                rectTransform.anchoredPosition = newAnchoredPos;
            }
        }

        public void ConfigureMeal(DishPayload payload, FoodDisplaySlot owner)
        {
            Kind = TrayItemKind.Meal;
            MealPayload = payload;
            DrinkKind = DrinkType.None;
            OwnerSlot = owner;
            isConsumed = false;

            if (iconImage != null && payload != null)
            {
                iconImage.color = payload.placeholderColor;
            }

            if (badgeText != null && payload != null)
            {
                string tag = payload.cookwareUsed == CookwareType.Pot ? "POT" : "PAN";
                badgeText.text = $"{tag}\n{payload.CombinedQuality * 100f:F0}%";
            }

            ResetVisualState();
            gameObject.SetActive(true);
        }

        public void ConfigureDrink(DrinkType drink, FoodDisplaySlot owner)
        {
            Kind = TrayItemKind.Drink;
            MealPayload = null;
            DrinkKind = drink;
            OwnerSlot = owner;
            isConsumed = false;

            if (iconImage != null)
            {
                iconImage.color = RecipeDatabase.GetDrinkColor(drink);
            }

            if (badgeText != null)
            {
                badgeText.text = "DRINK";
            }

            ResetVisualState();
            gameObject.SetActive(true);
        }

        public void ClearItem()
        {
            Kind = TrayItemKind.None;
            MealPayload = null;
            DrinkKind = DrinkType.None;
            isConsumed = false;
            ReturnToSlotImmediate();
            gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isConsumed || Kind == TrayItemKind.None) return;

            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            if (rootCanvas == null)
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;
            }

            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;

            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
            }
            transform.SetAsLastSibling();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
            rectTransform.localScale = originalScale * 1.12f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isConsumed || Kind == TrayItemKind.None) return;

            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            rectTransform.localScale = originalScale;

            if (isConsumed) return;
            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = StartCoroutine(SnapBackRoutine());
        }

        public void NotifyConsumed()
        {
            isConsumed = true;
            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            ReturnToSlotImmediate();
            gameObject.SetActive(false);

            if (OwnerSlot != null)
            {
                OwnerSlot.ClearSlot(notify: false);
            }
        }

        private void ResetVisualState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
            }
        }

        private void ReturnToSlotImmediate()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalAnchoredPosition;
                rectTransform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
            }
        }

        private IEnumerator SnapBackRoutine()
        {
            Vector3 startPos = rectTransform.position;
            Vector3 targetPos = originalParent != null
                ? originalParent.TransformPoint(originalAnchoredPosition)
                : startPos;

            float elapsed = 0f;
            while (elapsed < snapBackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rectTransform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(elapsed / snapBackDuration));
                yield return null;
            }

            ReturnToSlotImmediate();
            snapBackCoroutine = null;
        }
    }
}
