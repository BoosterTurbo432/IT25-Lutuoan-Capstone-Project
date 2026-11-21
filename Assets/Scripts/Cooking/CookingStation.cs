using System;
using System.Collections;
using System.Collections.Generic;
using CookingSystem.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    public enum CookingStationState
    {
        Idle,
        Cooking,
        Cooked,
        Burned,
        OnFire
    }

    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class CookingStation : MonoBehaviour, IDropHandler
    {
        [Header("Station Configuration")]
        [Tooltip("Identifier name for this cooking station (e.g. Pot, Frying Pan, Oven).")]
        [SerializeField] private string stationName = "Cooking Pot";

        [Tooltip("Maximum number of ingredients this station can hold at once.")]
        [Range(1, 10)]
        [SerializeField] private int maxIngredients = 3;

        [Tooltip("Allowed ingredient types for this station. If empty, all types are accepted.")]
        [SerializeField] private List<IngredientType> acceptedIngredientTypes = new List<IngredientType>();

        [Tooltip("If true, cooking starts automatically once at least one ingredient is added. If false, call StartCooking() manually (e.g. via button).")]
        [SerializeField] private bool autoStartCookingWhenFull = true;

        [Header("Showcase Integration")]
        [Tooltip("Required cookware type for prepared dishes.")]
        [SerializeField] private CookwareType cookwareType = CookwareType.Pot;

        [Tooltip("If true, raw ingredients must go through PrepStation before cooking.")]
        [SerializeField] private bool requirePrepStation = false;

        [Tooltip("Optional staging tray where cooked meals are plated instead of disappearing immediately.")]
        [SerializeField] private FoodDisplayTray displayTray;

        [Tooltip("Optional burnt food draggable object revealed after extinguishing a stove fire.")]
        [SerializeField] private DraggableBurntFood burntFoodHandle;

        [Tooltip("Optional fire flame overlay activated when this burner catches fire.")]
        [SerializeField] private GameObject fireOverlay;

        [Range(0.05f, 0.5f)]
        [SerializeField] private float fireCustomerPatiencePenalty = 0.15f;

        [Header("Cooking Timer")]
        [Tooltip("Time in seconds required to finish cooking.")]
        [SerializeField] private float cookDuration = 5f;

        [Tooltip("If enabled, cooked food will eventually burn if not collected in time.")]
        [SerializeField] private bool canBurn = false;

        [Tooltip("Time in seconds after completion before the dish is considered burned.")]
        [SerializeField] private float burnDuration = 5f;

        [Header("Hierarchy & Visual Slots")]
        [Tooltip("Transform container where dropped ingredients will be visually parented.")]
        [SerializeField] private Transform ingredientsContainer;

        [Tooltip("Optional UI Image or Slider fill for cooking progress (0 to 1).")]
        [SerializeField] private Image progressBarFill;

        [Tooltip("Highlight glow image activated when this burner matches prepped ingredients.")]
        [SerializeField] private Image recommendationGlow;

        [Header("State Events")]
        [Tooltip("Fires when a valid ingredient is dropped into the station.")]
        public UnityEvent<DraggableIngredient> onIngredientAdded;

        [Tooltip("Fires when cooking initiates.")]
        public UnityEvent onCookingStarted;

        [Tooltip("Fires every frame while cooking, passing normalized progress (0.0 to 1.0).")]
        public UnityEvent<float> onCookingProgress;

        [Tooltip("Fires when cooking successfully finishes.")]
        public UnityEvent onCookingCompleted;

        [Tooltip("Fires if the cooked item burns.")]
        public UnityEvent onFoodBurned;

        [Tooltip("Fires when the station is cleared and ready for new ingredients.")]
        public UnityEvent onStationReset;

        [Tooltip("Fires when a completed dish is served, passing the generated dish name.")]
        public UnityEvent<string> onDishServed;

        [Tooltip("Fires every frame during the burn grace countdown with normalized burn progress (0..1).")]
        public UnityEvent<float> onBurnWarningProgress;

        [Tooltip("Fires when the stove catches fire after leaving food unattended.")]
        public UnityEvent onStoveCaughtFire;

        [Tooltip("Fires when the fire extinguisher puts out a stove fire.")]
        public UnityEvent onFireExtinguished;

        private DishPayload currentPayload;
        private float currentBurnProgress;

        public CookwareType Cookware => cookwareType;
        public bool RequirePrepStation => requirePrepStation;
        public float BurnDuration => EffectiveBurnDuration;
        public float EffectiveCookDuration => Mathf.Max(1.2f, cookDuration * ProgressionConfig.GetCookDurationMultiplier(SaveSystem.Current));
        public float EffectiveBurnDuration => burnDuration + ProgressionConfig.GetBurnGraceBonusSeconds(SaveSystem.Current);
        public float CurrentBurnProgress => currentBurnProgress;

        // Runtime state
        private static readonly List<CookingStation> ActiveStations = new List<CookingStation>();
        private CookingStationState currentState = CookingStationState.Idle;
        private readonly List<DraggableIngredient> currentIngredients = new List<DraggableIngredient>();
        private Coroutine cookingCoroutine;
        private Image stationImage;
        private bool isRecommendedGlowActive;
        private Color recommendationBaseColor = new Color(0.98f, 0.78f, 0.22f, 0.7f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStationRegistry()
        {
            ActiveStations.Clear();
        }

        public static void HighlightRecommendedStations(CookwareType? recommendedCookware, bool isReadyForDrop)
        {
            Color glowColor = isReadyForDrop
                ? new Color(0.29f, 0.87f, 0.50f, 0.85f)
                : new Color(0.98f, 0.78f, 0.22f, 0.65f);

            for (int i = 0; i < ActiveStations.Count; i++)
            {
                CookingStation s = ActiveStations[i];
                if (s == null) continue;
                bool match = recommendedCookware.HasValue &&
                             s.currentState == CookingStationState.Idle &&
                             s.cookwareType == recommendedCookware.Value;
                s.SetRecommendationHighlight(match, glowColor);
            }
        }

        public string StationName => stationName;
        public CookingStationState CurrentState => currentState;
        public IReadOnlyList<DraggableIngredient> CurrentIngredients => currentIngredients;
        public int IngredientCount => currentIngredients.Count;
        public bool IsFull => currentIngredients.Count >= maxIngredients;

        private void Awake()
        {
            stationImage = GetComponent<Image>();

            if (!ActiveStations.Contains(this))
            {
                ActiveStations.Add(this);
            }

            // Ensure the station's Image raycastTarget is enabled so IDropHandler can trigger
            if (stationImage != null)
            {
                stationImage.raycastTarget = true;
            }

            if (ingredientsContainer == null)
            {
                ingredientsContainer = transform;
            }

            SetRecommendationHighlight(false, recommendationBaseColor);
            UpdateProgressUI(0f);
        }

        private void OnDestroy()
        {
            ActiveStations.Remove(this);
        }

        private void Update()
        {
            if (isRecommendedGlowActive && recommendationGlow != null)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.2f);
                Color c = recommendationBaseColor;
                c.a = Mathf.Clamp01(recommendationBaseColor.a * pulse);
                recommendationGlow.color = c;
            }
        }

        public void SetRecommendationHighlight(bool active, Color glowColor)
        {
            isRecommendedGlowActive = active;
            recommendationBaseColor = glowColor;
            if (recommendationGlow != null)
            {
                recommendationGlow.enabled = active;
                recommendationGlow.color = glowColor;
            }
        }

        /// <summary>
        /// Implements IDropHandler. Called automatically by Unity's EventSystem when a dragged object is released over this station.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            if (eventData.pointerDrag.TryGetComponent<FireExtinguisherTool>(out _))
            {
                if (currentState == CookingStationState.OnFire)
                {
                    ExtinguishFire();
                }
                else
                {
                    DishNotificationUI.Instance?.ShowNotification($"{stationName} is not on fire!");
                }
                return;
            }

            if (eventData.pointerDrag.TryGetComponent<DraggablePreparedDish>(out var preparedDish))
            {
                AcceptPreparedDish(preparedDish);
                return;
            }

            // Attempt to retrieve DraggableIngredient component from dropped object
            if (eventData.pointerDrag.TryGetComponent<DraggableIngredient>(out var ingredient))
            {
                if (requirePrepStation)
                {
                    DishNotificationUI.Instance?.ShowNotification("Prep raw ingredients on the Cutting Board first!");
                    return;
                }

                if (CanAcceptIngredient(ingredient))
                {
                    AcceptIngredient(ingredient);
                }
                // If CanAcceptIngredient returns false, DraggableIngredient handles its own snap-back on OnEndDrag
            }
        }

        public bool AcceptPreparedDish(DraggablePreparedDish preparedDish)
        {
            if (preparedDish == null || preparedDish.Payload == null) return false;

            if (currentState == CookingStationState.OnFire)
            {
                DishNotificationUI.Instance?.ShowNotification($"{stationName} is on fire! Use the Fire Extinguisher first.");
                return false;
            }

            if (currentState == CookingStationState.Burned)
            {
                DishNotificationUI.Instance?.ShowNotification($"{stationName} has burnt food! Drag it to the Trash Can.");
                return false;
            }

            if (currentState != CookingStationState.Idle)
            {
                DishNotificationUI.Instance?.ShowNotification($"{stationName} is currently busy!");
                return false;
            }

            if (preparedDish.Payload.cookwareUsed != cookwareType)
            {
                DishNotificationUI.Instance?.ShowNotification($"Needs a {preparedDish.Payload.cookwareUsed} burner!");
                return false;
            }

            currentPayload = preparedDish.Payload;
            preparedDish.NotifyConsumed();
            GameAudioManager.Instance?.PlayTapPickup();
            StartPreparedCooking();
            return true;
        }

        private void StartPreparedCooking()
        {
            if (currentState != CookingStationState.Idle || currentPayload == null) return;

            if (cookingCoroutine != null)
            {
                StopCoroutine(cookingCoroutine);
            }

            cookingCoroutine = StartCoroutine(CookingRoutine());
        }

        public void ExtinguishFire()
        {
            if (currentState != CookingStationState.OnFire) return;

            currentState = CookingStationState.Burned;
            if (fireOverlay != null) fireOverlay.SetActive(false);

            if (burntFoodHandle != null)
            {
                burntFoodHandle.SetOwnerStation(this);
                burntFoodHandle.ShowReadyToTrash();
            }

            RefreshFireAlertAudio();
            GameAudioManager.Instance?.PlayClick(0.85f, 0.92f);
            onFireExtinguished?.Invoke();
            DishNotificationUI.Instance?.ShowNotification($"{stationName} fire out! Drag burnt food into the Trash Can.");
        }

        private static void RefreshFireAlertAudio()
        {
            bool anyOnFire = false;
            for (int i = 0; i < ActiveStations.Count; i++)
            {
                if (ActiveStations[i] != null && ActiveStations[i].currentState == CookingStationState.OnFire)
                {
                    anyOnFire = true;
                    break;
                }
            }

            if (!anyOnFire)
            {
                GameAudioManager.Instance?.StopOvercookedAlert();
            }
        }

        public void ClearBurntFood()
        {
            if (currentState == CookingStationState.OnFire)
            {
                DishNotificationUI.Instance?.ShowNotification("Extinguish the fire first!");
                return;
            }

            if (currentState != CookingStationState.Burned) return;

            ResetStation(destroyIngredients: true);
            DishNotificationUI.Instance?.ShowNotification($"{stationName} cleaned and ready!");
        }

        /// <summary>
        /// Validates whether the given ingredient can be added to this station.
        /// </summary>
        public bool CanAcceptIngredient(DraggableIngredient ingredient)
        {
            if (ingredient == null) return false;

            // Station must be in Idle state to accept ingredients
            if (currentState != CookingStationState.Idle) return false;

            // Cannot exceed maximum capacity
            if (IsFull) return false;

            // Type whitelist validation
            if (acceptedIngredientTypes.Count > 0 && !acceptedIngredientTypes.Contains(ingredient.Type))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Consumes and registers the ingredient into the cooking station.
        /// </summary>
        private void AcceptIngredient(DraggableIngredient ingredient)
        {
            currentIngredients.Add(ingredient);
            ingredient.NotifyAcceptedByStation(ingredientsContainer);

            onIngredientAdded?.Invoke(ingredient);

            if (autoStartCookingWhenFull && IsFull)
            {
                StartCooking();
            }
        }

        /// <summary>
        /// Starts the cooking process if in Idle state and contains ingredients.
        /// </summary>
        public void StartCooking()
        {
            if (currentState != CookingStationState.Idle) return;
            if (currentIngredients.Count == 0) return;

            if (cookingCoroutine != null)
            {
                StopCoroutine(cookingCoroutine);
            }

            cookingCoroutine = StartCoroutine(CookingRoutine());
        }

        private IEnumerator CookingRoutine()
        {
            currentState = CookingStationState.Cooking;
            HighlightRecommendedStations(null, false);
            onCookingStarted?.Invoke();

            float targetCookDuration = EffectiveCookDuration;
            float elapsed = 0f;
            while (elapsed < targetCookDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / targetCookDuration);
                UpdateProgressUI(progress);
                onCookingProgress?.Invoke(progress);
                yield return null;
            }

            currentState = CookingStationState.Cooked;
            currentBurnProgress = 0f;
            UpdateProgressUI(1f);
            GameAudioManager.Instance?.PlayCooked();
            onCookingCompleted?.Invoke();

            if (canBurn)
            {
                float targetBurnDuration = EffectiveBurnDuration;
                float burnElapsed = 0f;
                while (burnElapsed < targetBurnDuration)
                {
                    if (currentState != CookingStationState.Cooked) yield break;
                    burnElapsed += Time.deltaTime;
                    currentBurnProgress = Mathf.Clamp01(burnElapsed / targetBurnDuration);
                    onBurnWarningProgress?.Invoke(currentBurnProgress);
                    yield return null;
                }

                currentState = CookingStationState.OnFire;
                if (fireOverlay != null) fireOverlay.SetActive(true);
                GameAudioManager.Instance?.StartOvercookedAlert();
                onStoveCaughtFire?.Invoke();
                onFoodBurned?.Invoke();
                CustomerManager.Instance?.ApplyFirePenalty(fireCustomerPatiencePenalty);
                DishNotificationUI.Instance?.ShowNotification($"{stationName} CAUGHT FIRE! Drag Fire Extinguisher!");
            }

            cookingCoroutine = null;
        }

        private void UpdateProgressUI(float progress)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progress;
            }
        }

        /// <summary>
        /// Generates a dish name based on the current recipe ingredients.
        /// </summary>
        public string GenerateDishName()
        {
            if (currentIngredients.Count == 0) return "Carinderia Special Ulam";

            var types = new List<IngredientType>(currentIngredients.Count);
            foreach (var ing in currentIngredients)
            {
                if (ing != null) types.Add(ing.Type);
            }

            return RecipeDatabase.GetFallbackDishName(types, cookwareType);
        }

        /// <summary>
        /// Serves or plates the cooked dish, emits onDishServed, and resets the station.
        /// </summary>
        public string ServeDish()
        {
            if (currentState == CookingStationState.OnFire)
            {
                DishNotificationUI.Instance?.ShowNotification($"{stationName} is ON FIRE! Use Fire Extinguisher first.");
                return string.Empty;
            }

            if (currentState == CookingStationState.Burned)
            {
                DishNotificationUI.Instance?.ShowNotification("Burnt food cannot be served! Drag it to the Trash Can.");
                return string.Empty;
            }

            if (currentState != CookingStationState.Cooked)
            {
                return string.Empty;
            }

            DishPayload finalPayload = BuildFinalPayload();
            string dishName = finalPayload.dishName;

            if (displayTray != null && !displayTray.TryPlaceMeal(finalPayload))
            {
                DishNotificationUI.Instance?.ShowNotification("Food Display Tray is full! Serve or discard an item first.");
                return string.Empty;
            }

            ResetStation(destroyIngredients: true);
            onDishServed?.Invoke(dishName);
            return dishName;
        }

        private DishPayload BuildFinalPayload()
        {
            float cookQual = Mathf.Clamp01(1f - (currentBurnProgress * 0.25f));
            if (currentPayload != null)
            {
                return new DishPayload
                {
                    dishName = currentPayload.dishName,
                    recipeId = currentPayload.recipeId,
                    cookwareUsed = currentPayload.cookwareUsed,
                    ingredients = new List<IngredientType>(currentPayload.ingredients),
                    prepQuality = currentPayload.prepQuality,
                    cookQuality = cookQual,
                    placeholderColor = currentPayload.placeholderColor
                };
            }

            return new DishPayload
            {
                dishName = GenerateDishName(),
                recipeId = string.Empty,
                cookwareUsed = cookwareType,
                prepQuality = 1f,
                cookQuality = cookQual,
                placeholderColor = new Color(0.88f, 0.48f, 0.37f, 1f)
            };
        }

        /// <summary>
        /// Clears all ingredients and resets station back to Idle.
        /// </summary>
        public void ResetStation(bool destroyIngredients = true)
        {
            if (cookingCoroutine != null)
            {
                StopCoroutine(cookingCoroutine);
                cookingCoroutine = null;
            }

            for (int i = currentIngredients.Count - 1; i >= 0; i--)
            {
                if (currentIngredients[i] != null)
                {
                    if (destroyIngredients)
                    {
                        Destroy(currentIngredients[i].gameObject);
                    }
                    else
                    {
                        currentIngredients[i].ResetState();
                    }
                }
            }

            currentIngredients.Clear();
            currentPayload = null;
            currentBurnProgress = 0f;
            currentState = CookingStationState.Idle;
            if (fireOverlay != null) fireOverlay.SetActive(false);
            if (burntFoodHandle != null) burntFoodHandle.gameObject.SetActive(false);
            RefreshFireAlertAudio();
            UpdateProgressUI(0f);
            onStationReset?.Invoke();
        }
    }
}
