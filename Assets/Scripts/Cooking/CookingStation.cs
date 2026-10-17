using System;
using System.Collections;
using System.Collections.Generic;
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
        Burned
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

        // Runtime state
        private CookingStationState currentState = CookingStationState.Idle;
        private readonly List<DraggableIngredient> currentIngredients = new List<DraggableIngredient>();
        private Coroutine cookingCoroutine;
        private Image stationImage;

        public string StationName => stationName;
        public CookingStationState CurrentState => currentState;
        public IReadOnlyList<DraggableIngredient> CurrentIngredients => currentIngredients;
        public int IngredientCount => currentIngredients.Count;
        public bool IsFull => currentIngredients.Count >= maxIngredients;

        private void Awake()
        {
            stationImage = GetComponent<Image>();

            // Ensure the station's Image raycastTarget is enabled so IDropHandler can trigger
            if (stationImage != null)
            {
                stationImage.raycastTarget = true;
            }

            if (ingredientsContainer == null)
            {
                ingredientsContainer = transform;
            }

            UpdateProgressUI(0f);
        }

        /// <summary>
        /// Implements IDropHandler. Called automatically by Unity's EventSystem when a dragged object is released over this station.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            // Attempt to retrieve DraggableIngredient component from dropped object
            if (eventData.pointerDrag.TryGetComponent<DraggableIngredient>(out var ingredient))
            {
                if (CanAcceptIngredient(ingredient))
                {
                    AcceptIngredient(ingredient);
                }
                // If CanAcceptIngredient returns false, DraggableIngredient handles its own snap-back on OnEndDrag
            }
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
            onCookingStarted?.Invoke();

            float elapsed = 0f;
            while (elapsed < cookDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / cookDuration);
                UpdateProgressUI(progress);
                onCookingProgress?.Invoke(progress);
                yield return null;
            }

            currentState = CookingStationState.Cooked;
            UpdateProgressUI(1f);
            onCookingCompleted?.Invoke();

            if (canBurn)
            {
                float burnElapsed = 0f;
                while (burnElapsed < burnDuration)
                {
                    if (currentState != CookingStationState.Cooked) yield break;
                    burnElapsed += Time.deltaTime;
                    yield return null;
                }

                currentState = CookingStationState.Burned;
                onFoodBurned?.Invoke();
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
            currentState = CookingStationState.Idle;
            UpdateProgressUI(0f);
            onStationReset?.Invoke();
        }
    }
}
