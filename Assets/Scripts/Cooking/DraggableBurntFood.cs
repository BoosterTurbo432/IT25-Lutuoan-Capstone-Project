using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Draggable charred food item shown on a CookingStation after fire is extinguished.
    /// Must be dragged into TrashCanDropZone to clean and unlock the stove.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class DraggableBurntFood : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float snapBackDuration = 0.15f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private CookingStation ownerStation;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalScale;
        private bool isTrashed;
        private bool isDragging;
        private bool isInitialized;
        private Coroutine snapBackCoroutine;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (isInitialized) return;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            ownerStation = GetComponentInParent<CookingStation>();

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;

            originalScale = rectTransform.localScale == Vector3.zero ? Vector3.one : rectTransform.localScale;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            isInitialized = true;
        }

        public void SetOwnerStation(CookingStation station)
        {
            ownerStation = station;
        }

        public void ShowReadyToTrash()
        {
            EnsureInitialized();
            isTrashed = false;
            isDragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }
            gameObject.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
            if (isTrashed) return;
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
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
            rectTransform.localScale = originalScale * 1.1f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isTrashed) return;
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

            if (isTrashed) return;
            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = StartCoroutine(SnapBackRoutine());
        }

        public void NotifyTrashed()
        {
            isTrashed = true;
            isDragging = false;
            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localScale = originalScale;
            gameObject.SetActive(false);

            if (ownerStation != null)
            {
                ownerStation.ClearBurntFood();
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
