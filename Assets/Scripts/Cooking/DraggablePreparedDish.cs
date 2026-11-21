using System.Collections;
using CookingSystem.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Draggable prepared Pot/Pan dish produced by PrepStation.
    /// Dragged onto a matching CookingStation burner (or TrashCanDropZone to discard).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class DraggablePreparedDish : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI Visuals")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Sprite potSprite;
        [SerializeField] private Sprite panSprite;
        [SerializeField] private float snapBackDuration = 0.18f;

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

        public DishPayload Payload { get; private set; }
        public PrepStation OwnerPrepStation { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;

            originalScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            isInitialized = true;
        }

        public void Initialize(DishPayload payload, PrepStation owner)
        {
            EnsureInitialized();
            Payload = payload;
            OwnerPrepStation = owner;
            isConsumed = false;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (iconImage != null && payload != null)
            {
                Sprite vessel = payload.cookwareUsed == CookwareType.Pan ? panSprite : potSprite;
                if (vessel != null)
                {
                    iconImage.sprite = vessel;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.color = payload.placeholderColor;
                }
            }

            if (labelText != null && payload != null)
            {
                string vesselTag = payload.cookwareUsed == CookwareType.Pot ? "Pot" : "Pan";
                labelText.text = $"{vesselTag}\n{payload.prepQuality * 100f:F0}%";
            }

            gameObject.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (isConsumed || Payload == null) return;
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
            rectTransform.localScale = originalScale * 1.1f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isConsumed || Payload == null) return;

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
            ReturnToDock(instant: false);
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

            ReturnToDock(instant: true);
            gameObject.SetActive(false);

            if (OwnerPrepStation != null)
            {
                OwnerPrepStation.NotifyDishMovedToStove();
            }
        }

        private void ReturnToDock(bool instant)
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
            Vector3 startPos = rectTransform.position;
            Vector3 targetPos = originalParent != null
                ? originalParent.TransformPoint(originalAnchoredPosition)
                : startPos;

            float elapsed = 0f;
            while (elapsed < snapBackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / snapBackDuration);
                rectTransform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localScale = originalScale;
            snapBackCoroutine = null;
        }
    }
}
