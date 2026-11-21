using System.Collections;
using CookingSystem.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Draggable fire extinguisher tool. When dropped onto a burning CookingStation,
    /// extinguishes the fire and snaps cleanly back to its dock.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class FireExtinguisherTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float snapBackDuration = 0.16f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;

        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 originalAnchoredPosition;
        private Vector3 originalScale;
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
            if (!isDragging && rectTransform != null && (originalParent == null || rectTransform.parent == originalParent))
            {
                rectTransform.anchoredPosition = newAnchoredPos;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInitialized();
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
            if (!isDragging) return;
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
