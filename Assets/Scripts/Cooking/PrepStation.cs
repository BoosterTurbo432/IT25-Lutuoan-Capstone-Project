using System.Collections.Generic;
using CookingSystem.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    public enum PrepStage
    {
        CollectingIngredients,
        Chopping,
        Mixing,
        AssigningCookware,
        ReadyForStove
    }

    /// <summary>
    /// Sequential Prep & Utensils Station:
    /// 1. Drop 1-3 ingredients onto the Cutting Board.
    /// 2. Chop (timed precision bar + efficiency rating).
    /// 3. Mix (timed precision bar + efficiency rating).
    /// 4. Assign Cookware (Stirring Ladle = Pot, Frying Spatula = Pan).
    /// 5. Drag the prepared cookware token onto a matching Stove burner.
    /// </summary>
    [DisallowMultipleComponent]
    public class PrepStation : MonoBehaviour, IDropHandler
    {
        [Header("Station Limits & Timing")]
        [SerializeField] private int maxIngredients = 3;
        [SerializeField] private float cursorSweepSpeed = 1.05f;
        [SerializeField] private float efficiencyTargetSeconds = 14f;

        [Header("UI Controls")]
        [SerializeField] private TextMeshProUGUI stageStatusText;
        [SerializeField] private TextMeshProUGUI ingredientsSummaryText;
        [SerializeField] private Button chopButton;
        [SerializeField] private Button mixButton;
        [SerializeField] private Button assignPotButton;
        [SerializeField] private Button assignPanButton;
        [SerializeField] private Button resetPrepButton;

        [Header("Smart Cookware Highlights")]
        [SerializeField] private Image potRecommendGlow;
        [SerializeField] private Image panRecommendGlow;

        [Header("Precision Timing Bar")]
        [SerializeField] private GameObject precisionBarRoot;
        [SerializeField] private RectTransform precisionCursor;
        [SerializeField] private float barTravelWidth = 190f;

        [Header("Output Draggable Token")]
        [SerializeField] private DraggablePreparedDish preparedDishToken;

        [Header("Resolution-Safe Prep Action Animations")]
        [SerializeField] private UISpriteSheetAnimator prepActionAnimator;
        [SerializeField] private Sprite[] chopAnimFrames;
        [SerializeField] private Sprite[] mixAnimFrames;
        [SerializeField] private GameObject emptyBoardSlotsRoot;
        [SerializeField] private GameObject placedMiniIconsRoot;
        [SerializeField] private Image[] placedMiniIcons;

        [Header("Board Ingredient Slots (Optional 2D Icons)")]
        [SerializeField] private Image[] ingredientSlotIcons;
        [SerializeField] private TextMeshProUGUI[] ingredientSlotLabels;
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

        private readonly List<IngredientType> currentIngredients = new List<IngredientType>();
        private PrepStage currentStage = PrepStage.CollectingIngredients;
        private float stageStartTime;
        private float prepStartTime;
        private float chopScore;
        private float mixScore;
        private float cursorPhase;
        private float sweetSpotHalfWidth = 0.16f;
        private float yellowHalfWidth = 0.32f;
        private float effectiveSweepSpeed = 1.45f;

        public PrepStage CurrentStage => currentStage;
        public int IngredientCount => currentIngredients.Count;
        public CookwareType? RecommendedCookware { get; private set; }

        public event System.Action<int> onIngredientsChanged;
        public event System.Action<PrepStage> onStageChanged;

        private void Awake()
        {
            if (chopButton != null)
            {
                chopButton.onClick.AddListener(HandleChopClicked);
                UIClickAudioHook.SetSuppressed(chopButton, true);
            }
            if (mixButton != null)
            {
                mixButton.onClick.AddListener(HandleMixClicked);
                UIClickAudioHook.SetSuppressed(mixButton, true);
            }
            if (assignPotButton != null) assignPotButton.onClick.AddListener(() => HandleAssignCookware(CookwareType.Pot));
            if (assignPanButton != null) assignPanButton.onClick.AddListener(() => HandleAssignCookware(CookwareType.Pan));
            if (resetPrepButton != null) resetPrepButton.onClick.AddListener(ResetPrep);

            if (preparedDishToken != null)
            {
                preparedDishToken.gameObject.SetActive(false);
            }

            if (prepActionAnimator != null)
            {
                prepActionAnimator.StopAndHide();
            }

            if (precisionBarRoot != null)
            {
                Button barBtn = precisionBarRoot.GetComponent<Button>();
                if (barBtn != null)
                {
                    barBtn.onClick.AddListener(HandlePrecisionBarClick);
                    UIClickAudioHook.SetSuppressed(barBtn, true);
                }
            }
        }

        private void HandlePrecisionBarClick()
        {
            if (currentStage == PrepStage.Chopping) HandleChopClicked();
            else if (currentStage == PrepStage.Mixing) HandleMixClicked();
        }

        private void Start()
        {
            ApplyUpgradeBonuses();
            RefreshUI();
        }

        public void ApplyUpgradeBonuses()
        {
            SaveData save = SaveSystem.GetOrCreateCurrent();
            sweetSpotHalfWidth = ProgressionConfig.GetPrepSweetSpotHalfWidth(save);
            yellowHalfWidth = ProgressionConfig.GetPrepYellowHalfWidth(save);
            effectiveSweepSpeed = cursorSweepSpeed * ProgressionConfig.GetPrepCursorSpeedMultiplier(save);

            if (precisionBarRoot != null)
            {
                Transform sweetTransform = precisionBarRoot.transform.Find("SweetSpotZone");
                if (sweetTransform is RectTransform sweetRect)
                {
                    float targetWidth = Mathf.Clamp(barTravelWidth * (sweetSpotHalfWidth * 2f), 60f, barTravelWidth);
                    sweetRect.sizeDelta = new Vector2(targetWidth, sweetRect.sizeDelta.y);
                }
            }
        }

        private void Update()
        {
            if (currentStage == PrepStage.Chopping || currentStage == PrepStage.Mixing)
            {
                cursorPhase += Time.deltaTime * effectiveSweepSpeed;
                float normalized = Mathf.PingPong(cursorPhase, 1f);
                if (precisionCursor != null)
                {
                    float x = (normalized - 0.5f) * barTravelWidth;
                    precisionCursor.anchoredPosition = new Vector2(x, precisionCursor.anchoredPosition.y);
                }
            }

            if (currentStage == PrepStage.AssigningCookware && RecommendedCookware.HasValue)
            {
                float alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.2f);
                Image activeGlow = RecommendedCookware.Value == CookwareType.Pot ? potRecommendGlow : panRecommendGlow;
                if (activeGlow != null && activeGlow.enabled)
                {
                    Color c = activeGlow.color;
                    c.a = alpha;
                    activeGlow.color = c;
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            if (!eventData.pointerDrag.TryGetComponent<DraggableIngredient>(out var ingredient)) return;

            if (currentStage != PrepStage.CollectingIngredients && currentStage != PrepStage.Chopping)
            {
                DishNotificationUI.Instance?.ShowNotification("Finish or Reset current prep before adding ingredients!");
                return;
            }

            if (currentIngredients.Count >= maxIngredients)
            {
                DishNotificationUI.Instance?.ShowNotification($"Cutting board full (max {maxIngredients} ingredients)!");
                return;
            }

            if (currentIngredients.Count == 0)
            {
                prepStartTime = Time.time;
                stageStartTime = Time.time;
            }

            currentIngredients.Add(ingredient.Type);
            ingredient.NotifyAcceptedByStation(null);
            Destroy(ingredient.gameObject);
            GameAudioManager.Instance?.PlayTapPickup();

            if (currentStage == PrepStage.CollectingIngredients)
            {
                currentStage = PrepStage.Chopping;
                cursorPhase = 0f;
                StartPrepAnimation(chopAnimFrames, 6.5f);
            }

            RefreshUI();
            onIngredientsChanged?.Invoke(currentIngredients.Count);
            onStageChanged?.Invoke(currentStage);
        }

        private void HandleChopClicked()
        {
            if (currentStage != PrepStage.Chopping || currentIngredients.Count == 0) return;

            GameAudioManager.Instance?.PlayChop();
            float accuracy = EvaluateTimingBarScore();
            float speedFactor = EvaluateSpeedFactor(Time.time - stageStartTime, 8f);
            chopScore = Mathf.Clamp01(accuracy * 0.80f + speedFactor * 0.20f);

            currentStage = PrepStage.Mixing;
            stageStartTime = Time.time;
            cursorPhase = 0.2f;
            StartPrepAnimation(mixAnimFrames, 7.5f);
            RefreshUI();
            onStageChanged?.Invoke(currentStage);
        }

        private void HandleMixClicked()
        {
            if (currentStage != PrepStage.Mixing) return;

            GameAudioManager.Instance?.PlayMix();
            float accuracy = EvaluateTimingBarScore();
            float speedFactor = EvaluateSpeedFactor(Time.time - stageStartTime, 8f);
            mixScore = Mathf.Clamp01(accuracy * 0.80f + speedFactor * 0.20f);

            currentStage = PrepStage.AssigningCookware;
            stageStartTime = Time.time;
            PlayPrepFinishBurst(mixAnimFrames);
            RefreshUI();
            onStageChanged?.Invoke(currentStage);
        }

        private void StartPrepAnimation(Sprite[] frames, float fps)
        {
            if (prepActionAnimator == null || frames == null || frames.Length == 0) return;
            prepActionAnimator.SetFrames(frames, fps, shouldLoop: true);
            prepActionAnimator.PlayLoop(fps);
        }

        private void PlayPrepFinishBurst(Sprite[] frames)
        {
            if (prepActionAnimator == null || frames == null || frames.Length == 0) return;
            prepActionAnimator.SetFrames(frames, 10f, shouldLoop: false);
            prepActionAnimator.PlayOneShot(durationSeconds: 0.36f, fadeOutTail: true, hideWhenDone: true, startScale: 0.96f, endScale: 1.08f);
        }

        private void HandleAssignCookware(CookwareType cookware)
        {
            if (currentStage != PrepStage.AssigningCookware) return;

            RecipeDefinition matched = RecipeDatabase.FindBestMatch(currentIngredients, cookware, out float cookwareAccuracy);
            float totalPrepDuration = Time.time - prepStartTime;
            float efficiencyBonus = EvaluateSpeedFactor(totalPrepDuration, efficiencyTargetSeconds);

            float combinedPrepQuality = Mathf.Clamp01(
                (chopScore * 0.35f) +
                (mixScore * 0.35f) +
                (cookwareAccuracy * 0.20f) +
                (efficiencyBonus * 0.10f));

            var payload = new DishPayload
            {
                recipeId = matched != null ? matched.recipeId : string.Empty,
                dishName = matched != null ? matched.displayName : RecipeDatabase.GetFallbackDishName(currentIngredients, cookware),
                cookwareUsed = cookware,
                ingredients = new List<IngredientType>(currentIngredients),
                prepQuality = combinedPrepQuality,
                cookQuality = 1f,
                placeholderColor = matched != null ? matched.placeholderColor : RecipeDatabase.GetFallbackColor(currentIngredients)
            };

            currentStage = PrepStage.ReadyForStove;
            if (preparedDishToken != null)
            {
                preparedDishToken.Initialize(payload, this);
            }

            if (cookwareAccuracy < 0.7f)
            {
                DishNotificationUI.Instance?.ShowNotification($"Assigned {cookware}, but recipe prefers {(cookware == CookwareType.Pot ? "Pan" : "Pot")}!");
            }

            RefreshUI();
            onStageChanged?.Invoke(currentStage);
        }

        public void NotifyDishMovedToStove()
        {
            ResetPrep();
        }

        public void ResetPrep()
        {
            currentIngredients.Clear();
            currentStage = PrepStage.CollectingIngredients;
            chopScore = 0f;
            mixScore = 0f;
            cursorPhase = 0f;
            RecommendedCookware = null;

            if (preparedDishToken != null)
            {
                preparedDishToken.gameObject.SetActive(false);
            }

            if (prepActionAnimator != null)
            {
                prepActionAnimator.StopAndHide();
            }

            CookingStation.HighlightRecommendedStations(null, false);
            RefreshUI();
            onIngredientsChanged?.Invoke(0);
            onStageChanged?.Invoke(currentStage);
        }

        private float EvaluateTimingBarScore()
        {
            float normalized = Mathf.PingPong(cursorPhase, 1f);
            float distanceFromCenter = Mathf.Abs(normalized - 0.5f);
            if (distanceFromCenter <= sweetSpotHalfWidth) return 1.0f;   // Green sweet spot (upgradable)
            if (distanceFromCenter <= yellowHalfWidth) return 0.88f;     // Yellow zone (upgradable)
            return 0.72f;                                                 // Outer edge (forgiving for new players)
        }

        private static float EvaluateSpeedFactor(float elapsed, float targetSeconds)
        {
            if (elapsed <= targetSeconds) return 1f;
            float overtime = elapsed - targetSeconds;
            return Mathf.Clamp(1f - (overtime / (targetSeconds * 2.5f)), 0.75f, 1f);
        }

        private void RefreshUI()
        {
            bool hasMiniIconBar = placedMiniIconsRoot != null && placedMiniIcons != null && placedMiniIcons.Length > 0;
            bool hasIngredients = currentIngredients.Count > 0;

            if (emptyBoardSlotsRoot != null)
            {
                emptyBoardSlotsRoot.SetActive(!hasIngredients);
            }

            if (placedMiniIconsRoot != null)
            {
                placedMiniIconsRoot.SetActive(hasIngredients);
            }

            if (hasMiniIconBar)
            {
                for (int i = 0; i < placedMiniIcons.Length; i++)
                {
                    Image mini = placedMiniIcons[i];
                    if (mini == null) continue;
                    bool filled = i < currentIngredients.Count;
                    mini.gameObject.SetActive(filled);
                    if (filled)
                    {
                        Sprite s = GetSpriteForIngredient(currentIngredients[i]);
                        mini.sprite = s;
                        mini.color = Color.white;
                        mini.preserveAspect = true;
                    }
                }
            }

            if (ingredientsSummaryText != null)
            {
                // When compact mini ingredient icons are present, hide text overlay so the board stays clean
                ingredientsSummaryText.gameObject.SetActive(!hasMiniIconBar);
                if (!hasMiniIconBar)
                {
                    ingredientsSummaryText.text = currentIngredients.Count == 0
                        ? "Board: Drop 1-3 Items"
                        : "Board: " + string.Join(" + ", currentIngredients);
                }
            }

            RecommendedCookware = null;
            string recommendedLabel = "Pick Pot or Pan";
            if (currentStage == PrepStage.AssigningCookware &&
                RecipeDatabase.TryGetRecommendedCookware(currentIngredients, out CookwareType recWare, out _))
            {
                RecommendedCookware = recWare;
                recommendedLabel = recWare == CookwareType.Pot
                    ? $"Mix {mixScore * 100f:F0}% | Recommended: Ladle (Pot)"
                    : $"Mix {mixScore * 100f:F0}% | Recommended: Spatula (Pan)";
            }

            if (stageStatusText != null)
            {
                stageStatusText.text = currentStage switch
                {
                    PrepStage.CollectingIngredients => "Drag Ingredients Here",
                    PrepStage.Chopping => "Click Chop (or Bar) on Green!",
                    PrepStage.Mixing => $"Chop {chopScore * 100f:F0}% | Click Mix on Green!",
                    PrepStage.AssigningCookware => recommendedLabel,
                    PrepStage.ReadyForStove => "Drag Vessel to Glowing Burner!",
                    _ => string.Empty
                };
            }

            bool showPrecisionBar = currentStage == PrepStage.Chopping || currentStage == PrepStage.Mixing;
            if (precisionBarRoot != null) precisionBarRoot.SetActive(showPrecisionBar);

            if (chopButton != null)
            {
                chopButton.gameObject.SetActive(true);
                chopButton.interactable = currentStage == PrepStage.Chopping && currentIngredients.Count > 0;
            }

            if (mixButton != null)
            {
                mixButton.gameObject.SetActive(true);
                mixButton.interactable = currentStage == PrepStage.Mixing;
            }

            bool canAssign = currentStage == PrepStage.AssigningCookware;
            if (assignPotButton != null) assignPotButton.interactable = canAssign;
            if (assignPanButton != null) assignPanButton.interactable = canAssign;

            if (potRecommendGlow != null)
            {
                potRecommendGlow.enabled = canAssign && RecommendedCookware == CookwareType.Pot;
            }
            if (panRecommendGlow != null)
            {
                panRecommendGlow.enabled = canAssign && RecommendedCookware == CookwareType.Pan;
            }

            if (canAssign && RecommendedCookware.HasValue)
            {
                CookingStation.HighlightRecommendedStations(RecommendedCookware.Value, isReadyForDrop: false);
            }
            else if (currentStage == PrepStage.ReadyForStove && preparedDishToken != null && preparedDishToken.Payload != null)
            {
                CookingStation.HighlightRecommendedStations(preparedDishToken.Payload.cookwareUsed, isReadyForDrop: true);
            }
            else
            {
                CookingStation.HighlightRecommendedStations(null, isReadyForDrop: false);
            }

            if (resetPrepButton != null)
            {
                resetPrepButton.interactable = currentIngredients.Count > 0 || currentStage != PrepStage.CollectingIngredients;
            }

            if (ingredientSlotIcons != null)
            {
                for (int i = 0; i < ingredientSlotIcons.Length; i++)
                {
                    Image slotImg = ingredientSlotIcons[i];
                    TextMeshProUGUI slotLbl = ingredientSlotLabels != null && i < ingredientSlotLabels.Length ? ingredientSlotLabels[i] : null;
                    bool hasIng = i < currentIngredients.Count;

                    if (slotImg != null)
                    {
                        if (hasIng)
                        {
                            slotImg.sprite = GetSpriteForIngredient(currentIngredients[i]);
                            slotImg.color = Color.white;
                            slotImg.preserveAspect = true;
                            slotImg.enabled = slotImg.sprite != null;
                        }
                        else
                        {
                            slotImg.enabled = false;
                        }
                    }

                    if (slotLbl != null)
                    {
                        slotLbl.gameObject.SetActive(!hasIng);
                    }
                }
            }
        }

        private Sprite GetSpriteForIngredient(IngredientType type)
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
                _ => null
            };
        }
    }
}
