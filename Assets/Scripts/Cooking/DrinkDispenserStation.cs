using System.Collections;
using CookingSystem.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Manages the Drink Dispenser station: cup restocking, animated frame-by-frame pouring
    /// (berry.png / Calamansi.png) + circular Progress Icon ring, and staging filled drinks onto FoodDisplayTray.
    /// </summary>
    [DisallowMultipleComponent]
    public class DrinkDispenserStation : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float pourDuration = 1.1f;
        [SerializeField] private float cupRestockDuration = 1.0f;

        [Header("References")]
        [SerializeField] private DraggableCup cupItem;
        [SerializeField] private GameObject pourProgressRoot;
        [SerializeField] private Image pourProgressFill;
        [SerializeField] private TMP_Text pourProgressPercentText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button stagePendingDrinkButton;
        [SerializeField] private FoodDisplayTray displayTray;
        [SerializeField] private GameObject berryPourIndicator;
        [SerializeField] private GameObject calamansiPourIndicator;
        [SerializeField] private UISpriteSheetAnimator berryPourAnimator;
        [SerializeField] private UISpriteSheetAnimator calamansiPourAnimator;

        private bool isPouring;
        private DrinkType pendingDrink = DrinkType.None;
        private Coroutine pourCoroutine;

        public bool CanStartPour => !isPouring && pendingDrink == DrinkType.None;

        private void Awake()
        {
            if (pourProgressFill != null) pourProgressFill.fillAmount = 0f;
            if (pourProgressRoot != null) pourProgressRoot.SetActive(false);

            if (berryPourIndicator != null) berryPourIndicator.SetActive(false);
            if (calamansiPourIndicator != null) calamansiPourIndicator.SetActive(false);

            if (stagePendingDrinkButton != null)
            {
                stagePendingDrinkButton.onClick.AddListener(TryStagePendingDrink);
                stagePendingDrinkButton.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            UpdateStatus("Drag Cup to Tap");
        }

        public void StartPour(DrinkType drink)
        {
            if (!CanStartPour || drink == DrinkType.None) return;

            GameAudioManager.Instance?.PlayDrink();
            if (pourCoroutine != null) StopCoroutine(pourCoroutine);
            pourCoroutine = StartCoroutine(PourRoutine(drink));
        }

        private IEnumerator PourRoutine(DrinkType drink)
        {
            isPouring = true;
            bool isBerry = drink == DrinkType.BerryJuice;
            if (berryPourIndicator != null) berryPourIndicator.SetActive(isBerry);
            if (calamansiPourIndicator != null) calamansiPourIndicator.SetActive(!isBerry);
            if (pourProgressRoot != null) pourProgressRoot.SetActive(true);

            UISpriteSheetAnimator activeAnim = isBerry ? berryPourAnimator : calamansiPourAnimator;
            if (activeAnim != null)
            {
                activeAnim.SetNormalizedProgress(0f);
            }

            string drinkName = RecipeDatabase.GetDrinkDisplayName(drink);
            UpdateStatus($"Pouring {drinkName}...");

            float elapsed = 0f;
            while (elapsed < pourDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / pourDuration);

                if (pourProgressFill != null)
                {
                    pourProgressFill.fillAmount = progress;
                }
                if (pourProgressPercentText != null)
                {
                    pourProgressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
                }
                if (activeAnim != null)
                {
                    activeAnim.SetNormalizedProgress(progress);
                }

                yield return null;
            }

            if (activeAnim != null) activeAnim.SetNormalizedProgress(1f);
            if (pourProgressFill != null) pourProgressFill.fillAmount = 0f;
            if (pourProgressPercentText != null) pourProgressPercentText.text = string.Empty;
            if (pourProgressRoot != null) pourProgressRoot.SetActive(false);
            if (berryPourIndicator != null) berryPourIndicator.SetActive(false);
            if (calamansiPourIndicator != null) calamansiPourIndicator.SetActive(false);
            isPouring = false;

            if (displayTray != null && displayTray.TryPlaceDrink(drink))
            {
                DishNotificationUI.Instance?.ShowNotification($"Staged {drinkName} on Display Tray!");
                yield return StartCoroutine(RestockCupRoutine());
            }
            else
            {
                pendingDrink = drink;
                UpdateStatus($"{drinkName} Ready!\nTray Full");
                if (stagePendingDrinkButton != null)
                {
                    stagePendingDrinkButton.gameObject.SetActive(true);
                }
            }

            pourCoroutine = null;
        }

        private void TryStagePendingDrink()
        {
            if (pendingDrink == DrinkType.None || displayTray == null) return;

            if (displayTray.TryPlaceDrink(pendingDrink))
            {
                string drinkName = RecipeDatabase.GetDrinkDisplayName(pendingDrink);
                pendingDrink = DrinkType.None;
                if (stagePendingDrinkButton != null)
                {
                    stagePendingDrinkButton.gameObject.SetActive(false);
                }
                DishNotificationUI.Instance?.ShowNotification($"Staged {drinkName} on Display Tray!");
                StartCoroutine(RestockCupRoutine());
            }
            else
            {
                DishNotificationUI.Instance?.ShowNotification("Display Tray full! Clear or serve a slot first.");
            }
        }

        private IEnumerator RestockCupRoutine()
        {
            UpdateStatus("Restocking Cup...");
            yield return new WaitForSeconds(cupRestockDuration);

            if (cupItem != null)
            {
                cupItem.ResetToSlot();
            }
            UpdateStatus("Drag Cup to Tap");
        }

        private void UpdateStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }
    }
}
