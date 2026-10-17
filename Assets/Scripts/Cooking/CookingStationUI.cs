using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    [RequireComponent(typeof(CookingStation))]
    public class CookingStationUI : MonoBehaviour
    {
        [Header("UI Text Displays")]
        [Tooltip("Displays current station state and ingredient count.")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Displays the station name or recipe title.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Interactive Controls")]
        [Tooltip("Optional button to clear/serve cooked food and reset station.")]
        [SerializeField] private Button serveButton;

        private CookingStation station;

        private void Awake()
        {
            station = GetComponent<CookingStation>();

            if (serveButton != null)
            {
                serveButton.onClick.AddListener(OnServeButtonClicked);
                serveButton.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (titleText != null)
            {
                titleText.text = station.StationName;
            }

            UpdateStatusText();
        }

        private void OnEnable()
        {
            if (station == null) station = GetComponent<CookingStation>();

            station.onIngredientAdded.AddListener(OnIngredientAdded);
            station.onCookingStarted.AddListener(OnCookingStarted);
            station.onCookingProgress.AddListener(OnCookingProgress);
            station.onCookingCompleted.AddListener(OnCookingCompleted);
            station.onFoodBurned.AddListener(OnFoodBurned);
            station.onStationReset.AddListener(OnStationReset);
        }

        private void OnDisable()
        {
            if (station == null) return;

            station.onIngredientAdded.RemoveListener(OnIngredientAdded);
            station.onCookingStarted.RemoveListener(OnCookingStarted);
            station.onCookingProgress.RemoveListener(OnCookingProgress);
            station.onCookingCompleted.RemoveListener(OnCookingCompleted);
            station.onFoodBurned.RemoveListener(OnFoodBurned);
            station.onStationReset.RemoveListener(OnStationReset);
        }

        private void OnIngredientAdded(DraggableIngredient ingredient)
        {
            UpdateStatusText();
        }

        private void OnCookingStarted()
        {
            if (statusText != null)
            {
                statusText.text = "<color=#FFA500>Cooking in progress...</color>";
            }
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(false);
            }
        }

        private void OnCookingProgress(float progress)
        {
            if (statusText != null)
            {
                statusText.text = $"<color=#FFA500>Cooking: {(progress * 100f):F0}%</color>";
            }
        }

        private void OnCookingCompleted()
        {
            if (statusText != null)
            {
                statusText.text = "<color=#00FF7F>Dish Ready! Click Serve to Reset</color>";
            }
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(true);
            }
        }

        private void OnFoodBurned()
        {
            if (statusText != null)
            {
                statusText.text = "<color=#FF4500>Burned! Click Serve to Clean</color>";
            }
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(true);
            }
        }

        private void OnStationReset()
        {
            UpdateStatusText();
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(false);
            }
        }

        private void OnServeButtonClicked()
        {
            string dishName = station.ServeDish();
            if (DishNotificationUI.Instance != null)
            {
                DishNotificationUI.Instance.ShowDishServed(dishName);
            }
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                statusText.text = $"Ingredients: {station.IngredientCount} (Drop items to cook)";
            }
        }
    }
}
