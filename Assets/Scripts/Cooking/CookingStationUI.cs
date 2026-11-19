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
        [Tooltip("Optional button to start cooking before reaching maxIngredients.")]
        [SerializeField] private Button cookButton;

        [Tooltip("Optional button to clear/serve cooked food and reset station.")]
        [SerializeField] private Button serveButton;

        [Tooltip("Optional burn grace warning fill image (0..1).")]
        [SerializeField] private Image burnWarningFill;

        [Header("2D Vessel State Sprites")]
        [SerializeField] private Image vesselImage;
        [SerializeField] private Sprite idleVesselSprite;
        [SerializeField] private Sprite cookingVesselSprite;
        [SerializeField] private Sprite burnedVesselSprite;

        private CookingStation station;

        private void Awake()
        {
            station = GetComponent<CookingStation>();

            if (cookButton != null)
            {
                cookButton.onClick.AddListener(OnCookButtonClicked);
                cookButton.gameObject.SetActive(false);
            }

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
            SetVesselSprite(idleVesselSprite);
        }

        private void OnEnable()
        {
            if (station == null) station = GetComponent<CookingStation>();

            station.onIngredientAdded.AddListener(OnIngredientAdded);
            station.onCookingStarted.AddListener(OnCookingStarted);
            station.onCookingProgress.AddListener(OnCookingProgress);
            station.onCookingCompleted.AddListener(OnCookingCompleted);
            station.onFoodBurned.AddListener(OnFoodBurned);
            station.onBurnWarningProgress.AddListener(OnBurnWarningProgress);
            station.onStoveCaughtFire.AddListener(OnStoveCaughtFire);
            station.onFireExtinguished.AddListener(OnFireExtinguished);
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
            station.onBurnWarningProgress.RemoveListener(OnBurnWarningProgress);
            station.onStoveCaughtFire.RemoveListener(OnStoveCaughtFire);
            station.onFireExtinguished.RemoveListener(OnFireExtinguished);
            station.onStationReset.RemoveListener(OnStationReset);
        }

        private void OnIngredientAdded(DraggableIngredient ingredient)
        {
            UpdateStatusText();
            UpdateCookButtonVisibility();
        }

        private void OnCookingStarted()
        {
            if (statusText != null)
            {
                statusText.text = "<color=#FFA500>Cooking in progress...</color>";
            }
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(false);
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
            if (progress >= 0.35f)
            {
                SetVesselSprite(cookingVesselSprite);
            }
        }

        private void OnCookingCompleted()
        {
            SetVesselSprite(cookingVesselSprite);
            if (statusText != null)
            {
                string graceLabel = station.EffectiveBurnDuration.ToString("0.#");
                statusText.text = station.RequirePrepStation
                    ? $"<color=#00FF7F>Ready! Click Plate! ({graceLabel}s)</color>"
                    : "<color=#00FF7F>Dish Ready! Click Serve</color>";
            }
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(false);
            }
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(true);
            }
        }

        private void OnBurnWarningProgress(float progress)
        {
            if (burnWarningFill != null)
            {
                burnWarningFill.fillAmount = progress;
            }
            if (statusText != null && station.CurrentState == CookingStationState.Cooked)
            {
                float secondsLeft = Mathf.Max(0f, station.BurnDuration * (1f - progress));
                statusText.text = $"<color=#F59E0B>Plate Now! ({secondsLeft:F1}s)</color>";
            }
        }

        private void OnStoveCaughtFire()
        {
            SetVesselSprite(burnedVesselSprite);
            if (burnWarningFill != null) burnWarningFill.fillAmount = 1f;
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (serveButton != null) serveButton.gameObject.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "<color=#EF4444>Fire! Drag Extinguisher</color>";
            }
        }

        private void OnFireExtinguished()
        {
            if (serveButton != null) serveButton.gameObject.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "<color=#FB923C>Burnt! Drag to Trash</color>";
            }
        }

        private void OnFoodBurned()
        {
            SetVesselSprite(burnedVesselSprite);
            if (station.CurrentState == CookingStationState.OnFire)
            {
                OnStoveCaughtFire();
                return;
            }
            if (statusText != null)
            {
                statusText.text = "<color=#FF4500>Burned! Click Serve to Clean</color>";
            }
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(false);
            }
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(true);
            }
        }

        private void OnStationReset()
        {
            SetVesselSprite(idleVesselSprite);
            if (burnWarningFill != null) burnWarningFill.fillAmount = 0f;
            UpdateStatusText();
            UpdateCookButtonVisibility();
            if (serveButton != null)
            {
                serveButton.gameObject.SetActive(false);
            }
        }

        private void OnCookButtonClicked()
        {
            station.StartCooking();
        }

        private void OnServeButtonClicked()
        {
            string dishName = station.ServeDish();
            if (DishNotificationUI.Instance != null && !string.IsNullOrEmpty(dishName))
            {
                DishNotificationUI.Instance.ShowDishServed(dishName);
            }
        }

        private void UpdateCookButtonVisibility()
        {
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(station.CurrentState == CookingStationState.Idle && station.IngredientCount > 0);
            }
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                statusText.text = station.RequirePrepStation
                    ? $"Drop {station.Cookware} Here"
                    : $"Ingredients: {station.IngredientCount} (Drop to Cook)";
            }
        }

        private void SetVesselSprite(Sprite sprite)
        {
            if (vesselImage != null && sprite != null)
            {
                vesselImage.sprite = sprite;
                vesselImage.color = Color.white;
                vesselImage.preserveAspect = true;
            }
        }
    }
}
