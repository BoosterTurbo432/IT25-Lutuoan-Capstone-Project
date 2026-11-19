using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Manages the Drink Dispenser station: cup restocking, pouring progress,
    /// and staging filled drinks onto the FoodDisplayTray.
    /// </summary>
    [DisallowMultipleComponent]
    public class DrinkDispenserStation : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float pourDuration = 1.1f;
        [SerializeField] private float cupRestockDuration = 1.0f;

        [Header("References")]
        [SerializeField] private DraggableCup cupItem;
        [SerializeField] private Image pourProgressFill;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button stagePendingDrinkButton;
        [SerializeField] private FoodDisplayTray displayTray;
        [SerializeField] private GameObject berryPourIndicator;
        [SerializeField] private GameObject calamansiPourIndicator;

        private bool isPouring;
        private DrinkType pendingDrink = DrinkType.None;
        private Coroutine pourCoroutine;

        public bool CanStartPour => !isPouring && pendingDrink == DrinkType.None;

        private void Awake()
        {
            if (pourProgressFill != null) pourProgressFill.fillAmount = 0f;

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

            if (pourCoroutine != null) StopCoroutine(pourCoroutine);
            pourCoroutine = StartCoroutine(PourRoutine(drink));
        }

        private IEnumerator PourRoutine(DrinkType drink)
        {
            isPouring = true;
            if (berryPourIndicator != null) berryPourIndicator.SetActive(drink == DrinkType.BerryJuice);
            if (calamansiPourIndicator != null) calamansiPourIndicator.SetActive(drink == DrinkType.CalamansiTea);
            string drinkName = RecipeDatabase.GetDrinkDisplayName(drink);
            UpdateStatus($"Pouring {drinkName}...");

            float elapsed = 0f;
            while (elapsed < pourDuration)
            {
                elapsed += Time.deltaTime;
                if (pourProgressFill != null)
                {
                    pourProgressFill.fillAmount = Mathf.Clamp01(elapsed / pourDuration);
                }
                yield return null;
            }

            if (pourProgressFill != null) pourProgressFill.fillAmount = 0f;
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
