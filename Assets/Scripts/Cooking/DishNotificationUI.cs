using System.Collections;
using TMPro;
using UnityEngine;

namespace CookingSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class DishNotificationUI : MonoBehaviour
    {
        public static DishNotificationUI Instance { get; private set; }

        [Header("UI Components")]
        [Tooltip("The text component that displays the dish announcement.")]
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Animation Settings")]
        [Tooltip("How long the banner remains fully visible in seconds.")]
        [SerializeField] private float displayDuration = 3.5f;

        [Tooltip("Fade in/out duration in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private Coroutine notificationCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Displays the required banner: You made a dish called "[dishName]"
        /// </summary>
        public void ShowDishServed(string dishName)
        {
            ShowNotification($"You made a dish called \"{dishName}\"");
        }

        public void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }
            notificationCoroutine = StartCoroutine(DisplayRoutine());
        }

        private IEnumerator DisplayRoutine()
        {
            // Fade In
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Wait display duration
            yield return new WaitForSecondsRealtime(displayDuration);

            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            notificationCoroutine = null;
        }
    }
}
