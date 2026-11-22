using System.Collections;
using System.Collections.Generic;
using CookingSystem.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    public enum SatisfactionReaction
    {
        Happy = 0,
        Neutral = 1,
        Angry = 2
    }

    /// <summary>
    /// Represents a single customer seat in Zone6_CustomerZone.
    /// Features cropped half-body portrait framing, icon-based meal recipe guide,
    /// expressive emoji satisfaction feedback (Satisfaction.png), patience tracking, and drop validation.
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

        [Header("Emoji Satisfaction Feedback (Satisfaction.png)")]
        [SerializeField] private GameObject satisfactionEmojiRoot;
        [SerializeField] private Image satisfactionEmojiImage;
        [SerializeField] private Sprite happyEmojiSprite;
        [SerializeField] private Sprite neutralEmojiSprite;
        [SerializeField] private Sprite angryEmojiSprite;

        [Header("Icon-Based Meal Recipe Guide")]
        [SerializeField] private GameObject recipeIconsContainer;
        [SerializeField] private Image[] recipeIngredientIcons;
        [SerializeField] private TMP_Text[] recipePlusLabels;
        [SerializeField] private GameObject recipeDividerObject;
        [SerializeField] private Image recipeCookwareIcon;
        [SerializeField] private Image drinkOrderIcon;
        [SerializeField] private Sprite potIconSprite;
        [SerializeField] private Sprite panIconSprite;
        [SerializeField] private Sprite berryDrinkIconSprite;
        [SerializeField] private Sprite calamansiDrinkIconSprite;

        [Header("12 Ingredient Recipe Icons")]
        [SerializeField] private Sprite tomatoSprite;
        [SerializeField] private Sprite cheeseSprite;
        [SerializeField] private Sprite meatSprite;
        [SerializeField] private Sprite mushroomSprite;
        [SerializeField] private Sprite onionSprite;
        [SerializeField] private Sprite herbsSprite;
        [SerializeField] private Sprite garlicSprite;
        [SerializeField] private Sprite potatoSprite;
        [SerializeField] private Sprite cabbageSprite;
        [SerializeField] private Sprite carrotsSprite;
        [SerializeField] private Sprite porkSprite;
        [SerializeField] private Sprite bellPepperSprite;

        [Header("Patience UI")]
        [SerializeField] private Image patienceBarFill;
        [SerializeField] private TMP_Text patienceTimerText;

        [Header("Order Timing")]
        [SerializeField] private float defaultPatienceSeconds = 75f;
        [SerializeField] private float drinkFreezeDuration = 14f;
        [SerializeField] [Range(0.05f, 0.5f)] private float drinkRestoreFraction = 0.35f;

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
        private bool receivedDrinkBoost;

        private float patienceDuration;
        private float patienceRemaining;
        private float patienceFreezeTimer;

        private Coroutine leaveCoroutine;
        private Coroutine emojiCoroutine;
        private bool isShowingTempEmoji;

        public bool IsOccupied => isOccupied && !isLeaving;
        public int SlotIndex => slotIndex;
        public bool IsShowingSatisfactionEmoji => satisfactionEmojiRoot != null && satisfactionEmojiRoot.activeSelf;

        public void InitializeEmpty()
        {
            isOccupied = false;
            isLeaving = false;
            orderedRecipe = null;
            orderedDrink = DrinkType.None;
            mealDelivered = false;
            drinkDelivered = false;
            receivedDrinkBoost = false;
            patienceFreezeTimer = 0f;
            isShowingTempEmoji = false;

            if (emojiCoroutine != null)
            {
                StopCoroutine(emojiCoroutine);
                emojiCoroutine = null;
            }
            if (satisfactionEmojiRoot != null) satisfactionEmojiRoot.SetActive(false);

            if (customerVisualRoot != null) customerVisualRoot.SetActive(false);
            if (orderBubbleRoot != null) orderBubbleRoot.gameObject.SetActive(false);
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (cardBackground != null) cardBackground.color = emptyCardColor;
            if (patienceBarFill != null) patienceBarFill.fillAmount = 0f;
            if (patienceTimerText != null) patienceTimerText.text = "Waiting...";
            if (slotLabelText != null) slotLabelText.text = $"Customer {slotIndex}";
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
            if (LevelGameManager.Instance != null &&
                (!LevelGameManager.Instance.IsLevelActive || LevelGameManager.Instance.IsTimerPausedForTutorial))
            {
                return;
            }

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
            UpdateLivePatienceMoodEmoji();
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
            if (emojiCoroutine != null)
            {
                StopCoroutine(emojiCoroutine);
                emojiCoroutine = null;
            }
            isShowingTempEmoji = false;
            if (satisfactionEmojiRoot != null) satisfactionEmojiRoot.SetActive(false);

            orderType = type;
            orderedRecipe = type == OrderType.DrinkOnly ? null : recipe;
            orderedDrink = type == OrderType.MealOnly ? DrinkType.None : drink;
            mealDelivered = type == OrderType.DrinkOnly;
            drinkDelivered = type == OrderType.MealOnly;
            receivedDrinkBoost = false;
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
                    slotLabelText.text = $"Customer {slotIndex}";
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
            ShowTemporaryEmoji(SatisfactionReaction.Angry, 2.0f);
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
                GameAudioManager.Instance?.PlayNotSatisfied();
                DishNotificationUI.Instance?.ShowNotification(msg);
                ShowTemporaryEmoji(SatisfactionReaction.Angry, 1.4f);
                return;
            }

            DishPayload payload = trayItem.MealPayload;
            if (payload == null) return;

            float accuracy = RecipeDatabase.ComputeOrderAccuracy(payload, orderedRecipe);
            float quality = payload.CombinedQuality;
            float patienceRatio = Mathf.Clamp01(patienceRemaining / patienceDuration);
            float drinkBonus = receivedDrinkBoost ? 0.12f : 0f;
            float satisfaction = Mathf.Clamp01((accuracy * 0.55f) + (quality * 0.25f) + (patienceRatio * 0.20f) + drinkBonus);

            // Guarantee fair, rewarding satisfaction whenever the exact dish is served on time
            if (accuracy >= 0.95f && patienceRemaining > 0f)
            {
                float onTimeFloor = receivedDrinkBoost ? 0.88f : 0.78f;
                satisfaction = Mathf.Max(satisfaction, onTimeFloor);
            }

            int basePts = orderedRecipe != null ? orderedRecipe.basePoints : 150;
            int awardedBase = Mathf.Max(30, Mathf.RoundToInt(basePts * Mathf.Max(0.25f, accuracy)));

            trayItem.NotifyConsumed();
            mealDelivered = true;

            bool completesMealDrinkCombo = orderType == OrderType.MealAndDrink && drinkDelivered;
            LevelGameManager.Instance?.RegisterMealDelivery(awardedBase, quality, satisfaction, accuracy >= 0.55f, completesMealDrinkCombo);

            SatisfactionReaction reaction = EvaluateMealReaction(accuracy, satisfaction);
            Color tintColor = GetReactionTintColor(reaction);

            if (completesMealDrinkCombo)
            {
                LevelGameManager.Instance?.RegisterComboCompletion();
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                BeginLeave(reaction, tintColor);
                DishNotificationUI.Instance?.ShowNotification($"Combo Order Served! (+120 Bonus)");
            }
            else if (orderType == OrderType.MealOnly)
            {
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                BeginLeave(reaction, tintColor);
                DishNotificationUI.Instance?.ShowNotification($"Served {payload.dishName}!");
            }
            else
            {
                // Meal served first on a combo order; still waiting for drink
                patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + patienceDuration * 0.25f);
                RefreshOrderBubble();
                RefreshPatienceUI(paused: patienceFreezeTimer > 0f);
                ShowTemporaryEmoji(reaction, 2.0f);
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
                // Hospitality drink restores & freezes customer patience AND boosts meal satisfaction!
                SaveData save = SaveSystem.Current;
                float effectiveRestore = Mathf.Clamp01(drinkRestoreFraction + ProgressionConfig.GetDrinkRestoreBonusFraction(save));
                float effectiveFreeze = drinkFreezeDuration + ProgressionConfig.GetDrinkFreezeBonusSeconds(save);

                trayItem.NotifyConsumed();
                receivedDrinkBoost = true;
                patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + (patienceDuration * effectiveRestore));
                patienceFreezeTimer = effectiveFreeze;
                RefreshPatienceUI(paused: true);
                GameAudioManager.Instance?.PlaySatisfied();
                LevelGameManager.Instance?.RegisterDrinkDelivery(45, isComboWithMeal: false, satisfaction: 0.92f);
                ShowTemporaryEmoji(SatisfactionReaction.Happy, 1.8f);
                return;
            }

            if (drinkDelivered)
            {
                GameAudioManager.Instance?.PlayNotSatisfied();
                DishNotificationUI.Instance?.ShowNotification($"Customer {slotIndex} already has their drink!");
                ShowTemporaryEmoji(SatisfactionReaction.Angry, 1.3f);
                return;
            }

            bool exactDrink = trayItem.DrinkKind == orderedDrink;
            float drinkAccuracy = exactDrink ? 1f : 0.55f;
            int drinkPts = exactDrink ? 80 : 40;

            SaveData hospSave = SaveSystem.Current;
            float restoreFrac = Mathf.Clamp01(drinkRestoreFraction + ProgressionConfig.GetDrinkRestoreBonusFraction(hospSave));
            float freezeSecs = drinkFreezeDuration + ProgressionConfig.GetDrinkFreezeBonusSeconds(hospSave);

            trayItem.NotifyConsumed();
            drinkDelivered = true;
            if (exactDrink) receivedDrinkBoost = true;

            // Restore patience FIRST so the drink genuinely rescues satisfaction even when served late!
            patienceRemaining = Mathf.Min(patienceDuration, patienceRemaining + (patienceDuration * restoreFrac));
            patienceFreezeTimer = freezeSecs;
            RefreshPatienceUI(paused: true);

            float restoredPatienceRatio = Mathf.Clamp01(patienceRemaining / patienceDuration);
            float satisfaction = Mathf.Clamp01((drinkAccuracy * 0.72f) + (restoredPatienceRatio * 0.28f));
            if (exactDrink && patienceRemaining > 0f)
            {
                satisfaction = Mathf.Max(satisfaction, 0.85f);
            }

            SatisfactionReaction drinkReaction = exactDrink
                ? SatisfactionReaction.Happy
                : SatisfactionReaction.Neutral;

            if (orderType == OrderType.DrinkOnly)
            {
                if (exactDrink) GameAudioManager.Instance?.PlaySatisfied();
                else GameAudioManager.Instance?.PlayNotSatisfied();
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: false, satisfaction: satisfaction);
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                BeginLeave(drinkReaction, GetReactionTintColor(drinkReaction));
                DishNotificationUI.Instance?.ShowNotification($"Served {RecipeDatabase.GetDrinkDisplayName(trayItem.DrinkKind)}!");
            }
            else if (mealDelivered)
            {
                // Meal was already delivered -> drink completes combo!
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: true, satisfaction: satisfaction);
                LevelGameManager.Instance?.RegisterCustomerOrderCompleted();
                SatisfactionReaction comboReaction = exactDrink ? SatisfactionReaction.Happy : SatisfactionReaction.Neutral;
                BeginLeave(comboReaction, GetReactionTintColor(comboReaction));
                DishNotificationUI.Instance?.ShowNotification($"Meal + Drink Combo Complete! (+120 Bonus)");
            }
            else
            {
                // Drink served before meal on MealAndDrink: freeze patience & record happy boost!
                if (exactDrink) GameAudioManager.Instance?.PlaySatisfied();
                else GameAudioManager.Instance?.PlayNotSatisfied();
                LevelGameManager.Instance?.RegisterDrinkDelivery(drinkPts, isComboWithMeal: false, satisfaction: exactDrink ? satisfaction : -1f);
                RefreshOrderBubble();
                ShowTemporaryEmoji(drinkReaction, 1.8f);
            }
        }

        private void HandlePatienceExpired()
        {
            GameAudioManager.Instance?.PlayNotSatisfied();
            LevelGameManager.Instance?.RegisterCustomerLost();
            DishNotificationUI.Instance?.ShowNotification($"Customer {slotIndex} ran out of patience and left!");
            BeginLeave(SatisfactionReaction.Angry, GetReactionTintColor(SatisfactionReaction.Angry));
        }

        private static SatisfactionReaction EvaluateMealReaction(float accuracy, float satisfaction)
        {
            if (accuracy < 0.55f) return SatisfactionReaction.Angry;
            if (satisfaction >= 0.68f) return SatisfactionReaction.Happy;
            if (satisfaction >= 0.42f) return SatisfactionReaction.Neutral;
            return SatisfactionReaction.Angry;
        }

        private static Color GetReactionTintColor(SatisfactionReaction reaction)
        {
            return reaction switch
            {
                SatisfactionReaction.Happy => new Color(0.22f, 0.80f, 0.46f, 1f),
                SatisfactionReaction.Neutral => new Color(0.96f, 0.68f, 0.14f, 1f),
                _ => new Color(0.94f, 0.27f, 0.27f, 1f)
            };
        }

        private Sprite GetEmojiSprite(SatisfactionReaction reaction)
        {
            return reaction switch
            {
                SatisfactionReaction.Happy => happyEmojiSprite,
                SatisfactionReaction.Neutral => neutralEmojiSprite,
                _ => angryEmojiSprite
            };
        }

        private void UpdateLivePatienceMoodEmoji()
        {
            if (isShowingTempEmoji || isLeaving || satisfactionEmojiRoot == null) return;
            float ratio = patienceDuration > 0f ? Mathf.Clamp01(patienceRemaining / patienceDuration) : 0f;

            if (ratio < 0.30f && ratio > 0f)
            {
                if (!satisfactionEmojiRoot.activeSelf)
                {
                    ApplyEmojiVisual(SatisfactionReaction.Neutral, popAnimate: true);
                }
            }
            else if (satisfactionEmojiRoot.activeSelf)
            {
                satisfactionEmojiRoot.SetActive(false);
            }
        }

        public void ShowTemporaryEmoji(SatisfactionReaction reaction, float duration)
        {
            if (satisfactionEmojiRoot == null || satisfactionEmojiImage == null) return;

            if (emojiCoroutine != null)
            {
                StopCoroutine(emojiCoroutine);
            }
            emojiCoroutine = StartCoroutine(TempEmojiRoutine(reaction, duration));
        }

        private IEnumerator TempEmojiRoutine(SatisfactionReaction reaction, float duration)
        {
            isShowingTempEmoji = true;
            ApplyEmojiVisual(reaction, popAnimate: false);
            yield return StartCoroutine(PopInEmojiRoutine());

            yield return new WaitForSeconds(Mathf.Max(0.25f, duration - 0.24f));

            isShowingTempEmoji = false;
            if (!isLeaving && satisfactionEmojiRoot != null)
            {
                satisfactionEmojiRoot.SetActive(false);
            }
            emojiCoroutine = null;
        }

        private void ApplyEmojiVisual(SatisfactionReaction reaction, bool popAnimate)
        {
            if (satisfactionEmojiRoot == null || satisfactionEmojiImage == null) return;

            Sprite sprite = GetEmojiSprite(reaction);
            if (sprite != null)
            {
                satisfactionEmojiImage.sprite = sprite;
                satisfactionEmojiImage.color = Color.white;
                satisfactionEmojiImage.preserveAspect = true;
            }
            satisfactionEmojiRoot.SetActive(true);

            if (popAnimate)
            {
                if (emojiCoroutine != null) StopCoroutine(emojiCoroutine);
                emojiCoroutine = StartCoroutine(PopInOnlyRoutine());
            }
        }

        private IEnumerator PopInOnlyRoutine()
        {
            yield return StartCoroutine(PopInEmojiRoutine());
            emojiCoroutine = null;
        }

        private IEnumerator PopInEmojiRoutine()
        {
            if (satisfactionEmojiRoot == null) yield break;
            RectTransform rt = satisfactionEmojiRoot.transform as RectTransform;
            if (rt == null) yield break;

            float elapsed = 0f;
            const float popDur = 0.24f;
            while (elapsed < popDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDur);
                float scale = t < 0.65f
                    ? Mathf.Lerp(0.2f, 1.18f, t / 0.65f)
                    : Mathf.Lerp(1.18f, 1.0f, (t - 0.65f) / 0.35f);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private void BeginLeave(SatisfactionReaction reaction, Color color)
        {
            isLeaving = true;
            if (dropHighlightBorder != null) dropHighlightBorder.enabled = false;
            if (emojiCoroutine != null)
            {
                StopCoroutine(emojiCoroutine);
                emojiCoroutine = null;
            }
            isShowingTempEmoji = false;

            if (leaveCoroutine != null)
            {
                StopCoroutine(leaveCoroutine);
            }
            leaveCoroutine = StartCoroutine(LeaveRoutine(reaction, color));
        }

        private IEnumerator LeaveRoutine(SatisfactionReaction reaction, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
            if (cardBackground != null)
            {
                cardBackground.color = Color.Lerp(defaultCardColor, color, 0.32f);
            }

            ApplyEmojiVisual(reaction, popAnimate: false);
            yield return StartCoroutine(PopInEmojiRoutine());

            yield return new WaitForSeconds(1.35f);
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

            bool hasIconGuide = recipeIconsContainer != null && recipeIngredientIcons != null && recipeIngredientIcons.Length > 0;

            if (mealOrderText != null)
            {
                if (orderType == OrderType.DrinkOnly)
                {
                    mealOrderText.text = "Drink Only";
                    if (hasIconGuide)
                    {
                        recipeIconsContainer.SetActive(false);
                    }
                    if (mealIngredientsText != null)
                    {
                        mealIngredientsText.gameObject.SetActive(true);
                        mealIngredientsText.text = "Quick Refresh";
                    }
                }
                else if (mealDelivered)
                {
                    mealOrderText.text = $"<s>{orderedRecipe.displayName}</s>";
                    if (hasIconGuide)
                    {
                        recipeIconsContainer.SetActive(false);
                    }
                    if (mealIngredientsText != null)
                    {
                        mealIngredientsText.gameObject.SetActive(true);
                        mealIngredientsText.text = "<color=#16A34A>Served [OK]</color>";
                    }
                }
                else if (orderedRecipe != null)
                {
                    mealOrderText.text = orderedRecipe.displayName;
                    if (hasIconGuide)
                    {
                        if (mealIngredientsText != null) mealIngredientsText.gameObject.SetActive(false);
                        PopulateRecipeIcons(orderedRecipe);
                    }
                    else if (mealIngredientsText != null)
                    {
                        string ware = orderedRecipe.requiredCookware == CookwareType.Pot ? "Pot" : "Pan";
                        mealIngredientsText.gameObject.SetActive(true);
                        mealIngredientsText.text = $"{orderedRecipe.GetIngredientSummary()} | {ware}";
                    }
                }
            }

            if (drinkOrderText != null)
            {
                if (orderType == OrderType.MealOnly)
                {
                    drinkOrderText.text = "(No Drink)";
                    if (drinkOrderIcon != null) drinkOrderIcon.gameObject.SetActive(false);
                }
                else if (drinkDelivered)
                {
                    drinkOrderText.text = $"<s>{RecipeDatabase.GetDrinkDisplayName(orderedDrink)}</s>";
                    if (drinkOrderIcon != null)
                    {
                        drinkOrderIcon.gameObject.SetActive(true);
                        drinkOrderIcon.sprite = GetDrinkSprite(orderedDrink);
                        drinkOrderIcon.color = new Color(0.7f, 1f, 0.78f, 0.75f);
                    }
                }
                else
                {
                    drinkOrderText.text = RecipeDatabase.GetDrinkDisplayName(orderedDrink);
                    if (drinkOrderIcon != null)
                    {
                        Sprite ds = GetDrinkSprite(orderedDrink);
                        drinkOrderIcon.gameObject.SetActive(ds != null);
                        if (ds != null)
                        {
                            drinkOrderIcon.sprite = ds;
                            drinkOrderIcon.color = Color.white;
                        }
                    }
                }
            }
        }

        private void PopulateRecipeIcons(RecipeDefinition recipe)
        {
            if (recipeIconsContainer == null || recipe == null) return;
            recipeIconsContainer.SetActive(true);

            List<IngredientType> reqs = recipe.requiredIngredients;
            int count = reqs != null ? Mathf.Min(reqs.Count, recipeIngredientIcons.Length) : 0;

            for (int i = 0; i < recipeIngredientIcons.Length; i++)
            {
                Image iconImg = recipeIngredientIcons[i];
                if (iconImg == null) continue;

                if (i < count)
                {
                    Sprite ingSprite = GetIngredientSprite(reqs[i]);
                    iconImg.sprite = ingSprite;
                    iconImg.color = Color.white;
                    iconImg.preserveAspect = true;
                    iconImg.gameObject.SetActive(ingSprite != null);
                }
                else
                {
                    iconImg.gameObject.SetActive(false);
                }
            }

            if (recipePlusLabels != null)
            {
                for (int i = 0; i < recipePlusLabels.Length; i++)
                {
                    if (recipePlusLabels[i] != null)
                    {
                        recipePlusLabels[i].gameObject.SetActive(i < count - 1);
                    }
                }
            }

            if (recipeDividerObject != null)
            {
                recipeDividerObject.SetActive(count > 0 && recipeCookwareIcon != null);
            }

            if (recipeCookwareIcon != null)
            {
                Sprite wareSprite = recipe.requiredCookware == CookwareType.Pan ? panIconSprite : potIconSprite;
                recipeCookwareIcon.sprite = wareSprite;
                recipeCookwareIcon.color = Color.white;
                recipeCookwareIcon.preserveAspect = true;
                recipeCookwareIcon.gameObject.SetActive(wareSprite != null);
            }
        }

        private Sprite GetDrinkSprite(DrinkType drink)
        {
            return drink == DrinkType.CalamansiTea ? calamansiDrinkIconSprite : berryDrinkIconSprite;
        }

        private Sprite GetIngredientSprite(IngredientType type)
        {
            return type switch
            {
                IngredientType.Tomato => tomatoSprite,
                IngredientType.Cheese => cheeseSprite,
                IngredientType.Meat => meatSprite,
                IngredientType.Mushroom => mushroomSprite,
                IngredientType.Onion => onionSprite,
                IngredientType.Herbs => herbsSprite,
                IngredientType.Garlic => garlicSprite,
                IngredientType.Potato => potatoSprite,
                IngredientType.Cabbage => cabbageSprite,
                IngredientType.Carrots => carrotsSprite,
                IngredientType.Pork => porkSprite,
                IngredientType.BellPepper => bellPepperSprite,
                _ => tomatoSprite
            };
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
    }
}
