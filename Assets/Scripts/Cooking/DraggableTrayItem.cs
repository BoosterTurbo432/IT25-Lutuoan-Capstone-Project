using System.Collections;
using CookingSystem.Audio;
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

        [Header("2D Artwork Sprites")]
        [SerializeField] private Sprite mealTraySprite;
        [SerializeField] private Sprite berryDrinkSprite;
        [SerializeField] private Sprite calamansiDrinkSprite;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalScale;
        private bool isConsumed;
        private bool isDragging;
        private bool isInitialized;
        private Coroutine snapBackCoroutine;

        public TrayItemKind Kind { get; private set; } = TrayItemKind.None;
        public DishPayload MealPayload { get; private set; }
        public DrinkType DrinkKind { get; private set; } = DrinkType.None;
        public FoodDisplaySlot OwnerSlot { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (iconImage == null) iconImage = GetComponent<Image>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;

            originalScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            isInitialized = true;
        }

        public void SetRestPosition(Vector2 newAnchoredPos)
        {
            EnsureInitialized();
            originalAnchoredPosition = newAnchoredPos;
            if (!isConsumed && !isDragging && rectTransform != null && (originalParent == null || rectTransform.parent == originalParent))
            {
                rectTransform.anchoredPosition = newAnchoredPos;
            }
        }

        public void ConfigureMeal(DishPayload payload, FoodDisplaySlot owner)
        {
            EnsureInitialized();
            Kind = TrayItemKind.Meal;
            MealPayload = payload;
            DrinkKind = DrinkType.None;
            OwnerSlot = owner;
            isConsumed = false;

            if (iconImage != null && payload != null)
            {
                if (mealTraySprite != null)
                {
                    iconImage.sprite = mealTraySprite;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.color = payload.placeholderColor;
                }
            }

            if (badgeText != null && payload != null)
            {
                string tag = payload.cookwareUsed == CookwareType.Pot ? "Pot" : "Pan";
                badgeText.text = $"{tag} {payload.CombinedQuality * 100f:F0}%";
            }

            ResetVisualState();
            gameObject.SetActive(true);
        }

        public void ConfigureDrink(DrinkType drink, FoodDisplaySlot owner)
        {
            EnsureInitialized();
            Kind = TrayItemKind.Drink;
            MealPayload = null;
            DrinkKind = drink;
            OwnerSlot = owner;
            isConsumed = false;

            if (iconImage != null)
            {
                Sprite drinkSprite = drink == DrinkType.CalamansiTea ? calamansiDrinkSprite : berryDrinkSprite;
                if (drinkSprite != null)
                {
                    iconImage.sprite = drinkSprite;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.color = RecipeDatabase.GetDrinkColor(drink);
                }
            }

            if (badgeText != null)
            {
                badgeText.text = "Drink";
            }

            ResetVisualState();
            gameObject.SetActive(true);
        }

        public void ClearItem()
        {
            EnsureInitialized();
            Kind = TrayItemKind.None;
            MealPayload = null;
            DrinkKind = DrinkType.None;
            isConsumed = false;
            isDragging = false;
            ReturnToSlotImmediate();
            gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (isConsumed || Kind == TrayItemKind.None) return;
            if (LevelGameManager.Instance != null && !LevelGameManager.Instance.IsLevelActive) return;

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

            isDragging = true;
            GameAudioManager.Instance?.PlayTapPickup();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
            rectTransform.localScale = originalScale * 1.12f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isConsumed || Kind == TrayItemKind.None) return;

            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

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
            isDragging = false;
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
