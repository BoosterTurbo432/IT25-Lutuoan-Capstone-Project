using System.Collections.Generic;
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
        [SerializeField] private float cursorSweepSpeed = 1.45f;
        [SerializeField] private float efficiencyTargetSeconds = 8f;

        [Header("UI Controls")]
        [SerializeField] private TextMeshProUGUI stageStatusText;
        [SerializeField] private TextMeshProUGUI ingredientsSummaryText;
        [SerializeField] private Button chopButton;
        [SerializeField] private Button mixButton;
        [SerializeField] private Button assignPotButton;
        [SerializeField] private Button assignPanButton;
        [SerializeField] private Button resetPrepButton;

        [Header("Precision Timing Bar")]
        [SerializeField] private GameObject precisionBarRoot;
        [SerializeField] private RectTransform precisionCursor;
        [SerializeField] private float barTravelWidth = 190f;

        [Header("Output Draggable Token")]
        [SerializeField] private DraggablePreparedDish preparedDishToken;

        private readonly List<IngredientType> currentIngredients = new List<IngredientType>();
        private PrepStage currentStage = PrepStage.CollectingIngredients;
        private float stageStartTime;
        private float prepStartTime;
        private float chopScore;
        private float mixScore;
        private float cursorPhase;

        public PrepStage CurrentStage => currentStage;

        private void Awake()
        {
            if (chopButton != null) chopButton.onClick.AddListener(HandleChopClicked);
            if (mixButton != null) mixButton.onClick.AddListener(HandleMixClicked);
            if (assignPotButton != null) assignPotButton.onClick.AddListener(() => HandleAssignCookware(CookwareType.Pot));
            if (assignPanButton != null) assignPanButton.onClick.AddListener(() => HandleAssignCookware(CookwareType.Pan));
            if (resetPrepButton != null) resetPrepButton.onClick.AddListener(ResetPrep);

            if (preparedDishToken != null)
            {
                preparedDishToken.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            RefreshUI();
        }

        private void Update()
        {
            if (currentStage == PrepStage.Chopping || currentStage == PrepStage.Mixing)
            {
                cursorPhase += Time.deltaTime * cursorSweepSpeed;
                float normalized = Mathf.PingPong(cursorPhase, 1f);
                if (precisionCursor != null)
                {
                    float x = (normalized - 0.5f) * barTravelWidth;
                    precisionCursor.anchoredPosition = new Vector2(x, precisionCursor.anchoredPosition.y);
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

            if (currentStage == PrepStage.CollectingIngredients)
            {
                currentStage = PrepStage.Chopping;
                cursorPhase = 0f;
            }

            RefreshUI();
        }

        private void HandleChopClicked()
        {
            if (currentStage != PrepStage.Chopping || currentIngredients.Count == 0) return;

            float accuracy = EvaluateTimingBarScore();
            float speedFactor = EvaluateSpeedFactor(Time.time - stageStartTime, 4f);
            chopScore = Mathf.Clamp01(accuracy * 0.75f + speedFactor * 0.25f);

            currentStage = PrepStage.Mixing;
            stageStartTime = Time.time;
            cursorPhase = 0.2f;
            RefreshUI();
        }

        private void HandleMixClicked()
        {
            if (currentStage != PrepStage.Mixing) return;

            float accuracy = EvaluateTimingBarScore();
            float speedFactor = EvaluateSpeedFactor(Time.time - stageStartTime, 4f);
            mixScore = Mathf.Clamp01(accuracy * 0.75f + speedFactor * 0.25f);

            currentStage = PrepStage.AssigningCookware;
            stageStartTime = Time.time;
            RefreshUI();
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

            if (preparedDishToken != null)
            {
                preparedDishToken.gameObject.SetActive(false);
            }

            RefreshUI();
        }

        private float EvaluateTimingBarScore()
        {
            float normalized = Mathf.PingPong(cursorPhase, 1f);
            float distanceFromCenter = Mathf.Abs(normalized - 0.5f);
            if (distanceFromCenter <= 0.16f) return 1.0f;   // Green sweet spot
            if (distanceFromCenter <= 0.32f) return 0.78f;  // Yellow zone
            return 0.52f;                                   // Outer edge
        }

        private static float EvaluateSpeedFactor(float elapsed, float targetSeconds)
        {
            if (elapsed <= targetSeconds) return 1f;
            float overtime = elapsed - targetSeconds;
            return Mathf.Clamp(1f - (overtime / (targetSeconds * 1.5f)), 0.45f, 1f);
        }

        private void RefreshUI()
        {
            if (ingredientsSummaryText != null)
            {
                if (currentIngredients.Count == 0)
                {
                    ingredientsSummaryText.text = "BOARD: EMPTY (Drop 1-3 Items)";
                }
                else
                {
                    ingredientsSummaryText.text = "BOARD: " + string.Join(" + ", currentIngredients);
                }
            }

            if (stageStatusText != null)
            {
                stageStatusText.text = currentStage switch
                {
                    PrepStage.CollectingIngredients => "DRAG INGREDIENTS HERE ->",
                    PrepStage.Chopping => "CLICK CHOP ON GREEN!",
                    PrepStage.Mixing => $"CHOP {chopScore * 100f:F0}% | CLICK MIX!",
                    PrepStage.AssigningCookware => $"MIX {mixScore * 100f:F0}% | PICK POT/PAN",
                    PrepStage.ReadyForStove => "DRAG TOKEN -> STOVE!",
                    _ => string.Empty
                };
            }

            bool showPrecisionBar = currentStage == PrepStage.Chopping || currentStage == PrepStage.Mixing;
            if (precisionBarRoot != null) precisionBarRoot.SetActive(showPrecisionBar);

            if (chopButton != null)
            {
                chopButton.gameObject.SetActive(currentStage == PrepStage.Chopping);
                chopButton.interactable = currentStage == PrepStage.Chopping && currentIngredients.Count > 0;
            }

            if (mixButton != null)
            {
                mixButton.gameObject.SetActive(currentStage == PrepStage.Mixing);
                mixButton.interactable = currentStage == PrepStage.Mixing;
            }

            bool canAssign = currentStage == PrepStage.AssigningCookware;
            if (assignPotButton != null) assignPotButton.interactable = canAssign;
            if (assignPanButton != null) assignPanButton.interactable = canAssign;

            if (resetPrepButton != null)
            {
                resetPrepButton.interactable = currentIngredients.Count > 0 || currentStage != PrepStage.CollectingIngredients;
            }
        }
    }
}
