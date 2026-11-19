using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Represents a single customer seat in Zone6_CustomerZone.
    /// Enforces a strict non-overlapping RectTransform drop zone, tracks randomized orders,
    /// depletes patience over time, and computes delivery satisfaction.
    /// </summary>
    public class CustomerSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Customer Visuals")]
        [SerializeField] private int slotIndex = 1;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image dropHighlightBorder;
        [SerializeField] private GameObject customerVisualRoot;
        [SerializeField] private Image customerPortraitImage;
        [SerializeField] private Image[] customerBodyImages;
        [SerializeField] private Image orderBubbleRoot;

        [Header("Order & Status Text")]
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private TMP_Text orderTitleText;
        [SerializeField] private TMP_Text mealOrderText;
        [SerializeField] private TMP_Text mealIngredientsText;
        [SerializeField] private TMP_Text drinkOrderText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Patience UI")]
        [SerializeField] private Image patienceBarFill;
        [SerializeField] private TMP_Text patienceTimerText;

        [Header("Order Timing")]
        [SerializeField] private float defaultPatienceSeconds = 55f;
        [SerializeField] private float drinkFreezeDuration = 10f;
        [SerializeField] [Range(0.05f, 0.5f)] private float drinkRestoreFraction = 0.25f;

        private Color defaultCardColor = new Color(0.18f, 0.13f, 0.10f, 0.28f);
        private Color hoverCardColor = new Color(0.28f, 0.22f, 0.16f, 0.55f);
        private Color emptyCardColor = new Color(0.15f, 0.11f, 0.09f, 0.14f);

        private bool isOccupied;
        private bool isLeaving;
        private OrderType orderType;
        private RecipeDefinition orderedRecipe;
        private DrinkType orderedDrink;
        private bool mealDelivered;
        private bool drinkDelivered;

        private float patienceDuration;
        private float patienceRemaining;
        private float patienceFreezeTimer;

        private Coroutine leaveCoroutine;
        private Coroutine feedbackCoroutine;

        public bool IsOccupied => isOccupied && !isLeaving;
        public int SlotIndex => slotIndex;

        public void InitializeEmpty()
        {
            isOccupied = false;
            isLeaving = false;
            orderedRecipe = null;
            orderedDrink = DrinkType.None;
            mealDelivered = false;
            drinkDelivered = false;
            patienceFreezeTimer = 0f;

            if (customerVisualRoot != null) customerVisualRoot.SetActive(false);
            if (orderBubbleRoot != null) orderBubbleRoot.gameObject.SetActive(false);
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (cardBackground != null) cardBackground.color = emptyCardColor;
            if (patienceBarFill != null) patienceBarFill.fillAmount = 0f;
            if (patienceTimerText != null) patienceTimerText.text = "Waiting for customer...";
            if (slotLabelText != null) slotLabelText.text = $"Customer {slotIndex}  [Open]";
            if (feedbackText != null) feedbackText.text = string.Empty;
        }

        private void Start()
        {
            if (!isOccupied && !isLeaving)
            {
                InitializeEmpty();
            }
        }

        private void Update()
        {
            if (!isOccupied || isLeaving) return;
            if (LevelGameManager.Instance != null && !LevelGameManager.Instance.IsLevelActive) return;

            if (patienceFreezeTimer > 0f)
            {
                patienceFreezeTimer -= Time.deltaTime;
                RefreshPatienceUI(paused: true);
                return;
            }

            patienceRemaining -= Time.deltaTime;
            if (patienceRemaining <= 0f)
            {
                patienceRemaining = 0f;
                RefreshPatienceUI(paused: false);
                HandlePatienceExpired();
                return;
            }

            RefreshPatienceUI(paused: false);
        }

        public void AssignOrder(
            OrderType type,
            RecipeDefinition recipe,
            DrinkType drink,
            Color customerColor,
            float customPatience = -1f,
            string customerDisplayName = null,
            Sprite customerPortrait = null)
        {
            if (leaveCoroutine != null)
            {
                StopCoroutine(leaveCoroutine);
                leaveCoroutine = null;
            }

            orderType = type;
            orderedRecipe = type == OrderType.DrinkOnly ? null : recipe;
            orderedDrink = type == OrderType.MealOnly ? DrinkType.None : drink;
            mealDelivered = type == OrderType.DrinkOnly;
            drinkDelivered = type == OrderType.MealOnly;
            isOccupied = true;
            isLeaving = false;
            patienceFreezeTimer = 0f;

            float baseTime = customPatience > 0f ? customPatience : defaultPatienceSeconds;
            if (type == OrderType.MealAndDrink) baseTime += 10f;
            else if (type == OrderType.DrinkOnly) baseTime -= 12f;

            patienceDuration = Mathf.Max(25f, baseTime);
            patienceRemaining = patienceDuration;

            if (customerVisualRoot != null) customerVisualRoot.SetActive(true);
            if (orderBubbleRoot != null) orderBubbleRoot.gameObject.SetActive(true);
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (cardBackground != null) cardBackground.color = defaultCardColor;

            if (customerPortraitImage != null)
            {
                if (customerPortrait != null)
                {
                    customerPortraitImage.sprite = customerPortrait;
                    customerPortraitImage.color = Color.white;
                    customerPortraitImage.preserveAspect = true;
                    customerPortraitImage.enabled = true;
                }
                else
                {
                    customerPortraitImage.enabled = false;
                }
            }

            if (customerBodyImages != null)
            {
                bool showSilhouette = customerPortrait == null;
                for (int i = 0; i < customerBodyImages.Length; i++)
                {
                    if (customerBodyImages[i] != null)
                    {
                        customerBodyImages[i].enabled = showSilhouette;
                        customerBodyImages[i].color = customerColor;
                    }
                }
            }

            if (slotLabelText != null)
            {
                if (!string.IsNullOrEmpty(customerDisplayName))
                {
                    slotLabelText.text = customerDisplayName;
                }
                else
                {
                    slotLabelText.text = type switch
                    {
                        OrderType.MealAndDrink => $"Customer {slotIndex}  [COMBO ORDER]",
                        OrderType.DrinkOnly => $"Customer {slotIndex}  [DRINK ORDER]",
                        _ => $"Customer {slotIndex}  [MEAL ORDER]"
                    };
                }
            }

            if (feedbackText != null) feedbackText.text = string.Empty;
            RefreshOrderBubble();
            RefreshPatienceUI(paused: false);
        }

        public void ApplyPatiencePenalty(float fraction, string reason = "Stove Fire!")
        {
            if (!isOccupied || isLeaving) return;

            patienceFreezeTimer = 0f;
            float penalty = patienceDuration * Mathf.Clamp01(fraction);
            patienceRemaining = Mathf.Max(0f, patienceRemaining - penalty);
            ShowTemporaryFeedback($"<color=#EF4444>{reason} -{Mathf.RoundToInt(fraction * 100f)}%</color>", 2.2f);
            RefreshPatienceUI(paused: false);

            if (patienceRemaining <= 0f)
            {
                HandlePatienceExpired();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsOccupied) return;
            if (eventData.pointerDrag == null) return;
            if (eventData.pointerDrag.GetComponent<DraggableTrayItem>() == null) return;

            if (dropHighlightBorder != null) dropHighlightBorder.enabled = true;
            if (cardBackground != null) cardBackground.color = hoverCardColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (cardBackground != null) cardBackground.color = isOccupied ? defaultCardColor : emptyCardColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (cardBackground != null) cardBackground.color = isOccupied ? defaultCardColor : emptyCardColor;

            if (!IsOccupied) return;
            if (LevelGameManager.Instance != null && !LevelGameManager.Instance.IsLevelActive) return;

            // Strict non-overlapping bounds check prevents snapping to an adjacent customer
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    (RectTransform)transform,
                    eventData.position,
                    eventData.enterEventCamera))
            {
                return;
            }

            if (eventData.pointerDrag == null) return;
            if (!eventData.pointerDrag.TryGetComponent<DraggableTrayItem>(out var trayItem)) return;
            if (trayItem.Kind == TrayItemKind.None) return;

            if (trayItem.Kind == TrayItemKind.Meal)
            {
                HandleMealDrop(trayItem);
            }
            else if (trayItem.Kind == TrayItemKind.Drink)
            {
                HandleDrinkDrop(trayItem);
            }
        }

        private void HandleMealDrop(DraggableTrayItem trayItem)
        {
            if (orderType == OrderType.DrinkOnly || mealDelivered)
            {
                string msg = orderType == OrderType.DrinkOnly
                    ? $"Customer {slotIndex} only ordered a drink!"
                    : $"Customer {slotIndex} already received their meal!";
                DishNotificationUI.Instance?.ShowNotification(msg);
                ShowTemporaryFeedback("<color=#F59E0B>Wrong item for order</color>", 1.8f);
                return;
            }

            DishPayload payload = trayItem.MealPayload;
            if (payload == null) return;

            float accuracy = RecipeDatabase.ComputeOrderAccuracy(payload, orderedRecipe);
            float quality = payload.CombinedQuality;
            float patienceRatio = Mathf.Clamp01(patienceRemaining / patienceDuration);
            float satisfaction = Mathf.Clamp01((accuracy * 0.45f) + (quality * 0.35f) + (patienceRatio * 0.20f));

            int basePts = orderedRecipe != null ? orderedRecipe.basePoints : 150;
            int awardedBase = Mathf.Max(30, Mathf.RoundToInt(basePts * Mathf.Max(0.25f, accuracy)));

            trayItem.NotifyConsumed();
            mealDelivered = true;

            LevelGameManager.Instance?.RegisterMealDelivery(awardedBase, quality, satisfaction);

            string qualityTag = satisfaction >= 0.85f ? "Perfect!" : (accuracy >= 0.99f ? "Great!" : "Wrong Dish");
            Color msgColor = satisfaction >= 0.7f ? new Color(0.22f, 0.80f, 0.46f, 1f) : new Color(0.96f, 0.62f, 0.14f, 1f);

            if (orderType == OrderType.MealAndDrink && drinkDelivered)
            {
                LevelGameManager.Instance?.RegisterComboCompletion();
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                BeginLeave($"{qualityTag} + Combo!\nSatisfaction {satisfaction * 100f:F0}%", msgColor);
                DishNotificationUI.Instance?.ShowNotification($"Combo Order Served! Satisfaction: {satisfaction * 100f:F0}%");
            }
            else if (orderType == OrderType.MealOnly)
            {
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                BeginLeave($"{qualityTag}\nSatisfaction {satisfaction * 100f:F0}%", msgColor);
                DishNotificationUI.Instance?.ShowNotification($"Served {payload.dishName}! Satisfaction: {satisfaction * 100f:F0}%");
            }
            else
            {
                // Meal served first on a combo order; still waiting for drink
                patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + patienceDuration * 0.15f);
                RefreshOrderBubble();
                RefreshPatienceUI(paused: patienceFreezeTimer > 0f);
                ShowTemporaryFeedback($"<color=#34D399>Meal OK ({satisfaction * 100f:F0}%) - Need Drink!</color>", 2.5f);
            }
        }

        private void HandleDrinkDrop(DraggableTrayItem trayItem)
        {
            if (drinkDelivered && orderType == OrderType.DrinkOnly)
            {
                return;
            }

            if (orderType == OrderType.MealOnly)
            {
                // Hospitality drink still pauses/restores customer patience!
                SaveData save = SaveSystem.Current;
                float effectiveRestore = Mathf.Clamp01(drinkRestoreFraction + ProgressionConfig.GetDrinkRestoreBonusFraction(save));
                float effectiveFreeze = drinkFreezeDuration + ProgressionConfig.GetDrinkFreezeBonusSeconds(save);

                trayItem.NotifyConsumed();
                patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + (patienceDuration * effectiveRestore));
                patienceFreezeTimer = effectiveFreeze;
                RefreshPatienceUI(paused: true);
                LevelGameManager.Instance?.RegisterDrinkDelivery(35, isComboWithMeal: false, satisfaction: -1f);
                ShowTemporaryFeedback("<color=#38BDF8>Drink Refreshed Patience!</color>", 2.0f);
                return;
            }

            if (drinkDelivered)
            {
                DishNotificationUI.Instance?.ShowNotification($"Customer {slotIndex} already has their drink!");
                return;
            }

            bool exactDrink = trayItem.DrinkKind == orderedDrink;
            float drinkAccuracy = exactDrink ? 1f : 0.45f;
            float patienceRatio = Mathf.Clamp01(patienceRemaining / patienceDuration);
            float satisfaction = Mathf.Clamp01((drinkAccuracy * 0.65f) + (patienceRatio * 0.35f));
            int drinkPts = exactDrink ? 80 : 35;

            SaveData hospSave = SaveSystem.Current;
            float restoreFrac = Mathf.Clamp01(drinkRestoreFraction + ProgressionConfig.GetDrinkRestoreBonusFraction(hospSave));
            float freezeSecs = drinkFreezeDuration + ProgressionConfig.GetDrinkFreezeBonusSeconds(hospSave);

            trayItem.NotifyConsumed();
            drinkDelivered = true;

            patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + (patienceDuration * restoreFrac));
            patienceFreezeTimer = freezeSecs;
            RefreshPatienceUI(paused: true);

            if (orderType == OrderType.DrinkOnly)
            {
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: false, satisfaction: satisfaction);
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                string tag = exactDrink ? "Refreshed!" : "Wrong Drink";
                Color c = exactDrink ? new Color(0.22f, 0.80f, 0.46f, 1f) : new Color(0.96f, 0.62f, 0.14f, 1f);
                BeginLeave($"{tag}\nSatisfaction {satisfaction * 100f:F0}%", c);
                DishNotificationUI.Instance?.ShowNotification($"Served {RecipeDatabase.GetDrinkDisplayName(trayItem.DrinkKind)}!");
            }
            else if (mealDelivered)
            {
                // Meal was already delivered -> drink completes combo!
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: true, satisfaction: satisfaction);
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                string tag = exactDrink ? "Combo Complete! +120" : "Served + Combo!";
                BeginLeave($"{tag}\nDrink Match {drinkAccuracy * 100f:F0}%", new Color(0.22f, 0.80f, 0.46f, 1f));
                DishNotificationUI.Instance?.ShowNotification($"Meal + Drink Combo Complete! (+120 Bonus)");
            }
            else
            {
                // Drink served before meal on MealAndDrink: freeze patience while waiting for meal
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: false, satisfaction: -1f);
                RefreshOrderBubble();
                ShowTemporaryFeedback("<color=#38BDF8>Drink Served! Patience Paused</color>", 2.5f);
            }
        }

        private void HandlePatienceExpired()
        {
            LevelGameManager.Instance?.RegisterCustomerLost();
            DishNotificationUI.Instance?.ShowNotification($"Customer {slotIndex} ran out of patience and left!");
            BeginLeave("LEFT ANGRY!\nPatience 0%", new Color(0.94f, 0.27f, 0.27f, 1f));
        }

        private void BeginLeave(string message, Color color)
        {
            isLeaving = true;
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
                feedbackCoroutine = null;
            }
            if (leaveCoroutine != null)
            {
                StopCoroutine(leaveCoroutine);
            }
            leaveCoroutine = StartCoroutine(LeaveRoutine(message, color));
        }

        private IEnumerator LeaveRoutine(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.color = color;
                feedbackText.text = message;
            }
            if (cardBackground != null)
            {
                cardBackground.color = Color.Lerp(defaultCardColor, color, 0.35f);
            }

            yield return new WaitForSeconds(1.6f);
            InitializeEmpty();
            leaveCoroutine = null;
        }

        private void RefreshOrderBubble()
        {
            if (orderTitleText != null)
            {
                orderTitleText.text = orderType switch
                {
                    OrderType.MealAndDrink => "Combo Order",
                    OrderType.DrinkOnly => "Drink Order",
                    _ => "Meal Order"
                };
            }

            if (mealOrderText != null)
            {
                if (orderType == OrderType.DrinkOnly)
                {
                    mealOrderText.text = mealIngredientsText != null ? "Drink Only" : string.Empty;
                    if (mealIngredientsText != null) mealIngredientsText.text = "Quick Refresh";
                }
                else if (mealDelivered)
                {
                    mealOrderText.text = $"<s>{orderedRecipe.displayName}</s>";
                    if (mealIngredientsText != null) mealIngredientsText.text = "<color=#16A34A>Served [OK]</color>";
                }
                else
                {
                    string ware = orderedRecipe.requiredCookware == CookwareType.Pot ? "Pot" : "Pan";
                    if (mealIngredientsText != null)
                    {
                        mealOrderText.text = orderedRecipe.displayName;
                        mealIngredientsText.text = $"{orderedRecipe.GetIngredientSummary()} | {ware}";
                    }
                    else
                    {
                        mealOrderText.text = $"{orderedRecipe.displayName}\n<color=#334155>({orderedRecipe.GetIngredientSummary()} | {ware})</color>";
                    }
                }
            }

            if (drinkOrderText != null)
            {
                if (orderType == OrderType.MealOnly)
                {
                    drinkOrderText.text = mealIngredientsText != null ? "(No Drink)" : string.Empty;
                }
                else if (drinkDelivered)
                {
                    drinkOrderText.text = $"<s>{RecipeDatabase.GetDrinkDisplayName(orderedDrink)}</s> <color=#16A34A>[OK]</color>";
                }
                else
                {
                    drinkOrderText.text = $"+ {RecipeDatabase.GetDrinkDisplayName(orderedDrink)}";
                }
            }
        }

        private void RefreshPatienceUI(bool paused)
        {
            float ratio = patienceDuration > 0f ? Mathf.Clamp01(patienceRemaining / patienceDuration) : 0f;

            if (patienceBarFill != null)
            {
                patienceBarFill.fillAmount = ratio;
                if (paused)
                {
                    patienceBarFill.color = new Color(0.22f, 0.74f, 0.97f, 1f);
                }
                else if (ratio > 0.5f)
                {
                    patienceBarFill.color = new Color(0.20f, 0.76f, 0.45f, 1f);
                }
                else if (ratio > 0.25f)
                {
                    patienceBarFill.color = new Color(0.96f, 0.62f, 0.04f, 1f);
                }
                else
                {
                    patienceBarFill.color = new Color(0.94f, 0.27f, 0.27f, 1f);
                }
            }

            if (patienceTimerText != null)
            {
                if (paused)
                {
                    patienceTimerText.text = $"Paused ({patienceFreezeTimer:F0}s)";
                }
                else
                {
                    patienceTimerText.text = $"Patience: {Mathf.CeilToInt(patienceRemaining)}s";
                }
            }
        }

        private void ShowTemporaryFeedback(string message, float duration)
        {
            if (feedbackText == null) return;
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            feedbackCoroutine = StartCoroutine(TempFeedbackRoutine(message, duration));
        }

        private IEnumerator TempFeedbackRoutine(string message, float duration)
        {
            feedbackText.color = Color.white;
            feedbackText.text = message;
            yield return new WaitForSeconds(duration);
            if (!isLeaving && feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
            feedbackCoroutine = null;
        }
    }
}
