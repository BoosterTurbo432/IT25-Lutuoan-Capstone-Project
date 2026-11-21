using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Simplified 4-step interactive Day 1 guided tutorial:
    /// 1. Drop Ingredient -> 2. Chop & Mix -> 3. Utensil, Cook & Plate -> 4. Serve Customer.
    /// Pauses only the shift countdown timer & customer patience until the first dish is served or skipped.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidedTutorialController : MonoBehaviour
    {
        public enum TutorialStep
        {
            DropIngredient = 0,
            ChopAndMix = 1,
            CookAndPlate = 2,
            ServeCustomer = 3,
            Completed = 4
        }

        [Header("Banner UI")]
        [SerializeField] private GameObject tutorialBannerRoot;
        [SerializeField] private TMP_Text stepBadgeText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Button skipTutorialButton;

        [Header("Pulsing Highlight Frame")]
        [SerializeField] private RectTransform highlightRingRect;
        [SerializeField] private Image highlightRingImage;

        [Header("Scene Station References")]
        [SerializeField] private LevelGameManager levelManager;
        [SerializeField] private PrepStation prepStation;
        [SerializeField] private FoodDisplayTray displayTray;
        [SerializeField] private RectTransform ingredientTrayRect;
        [SerializeField] private RectTransform cuttingBoardRect;
        [SerializeField] private RectTransform chopButtonRect;
        [SerializeField] private RectTransform mixButtonRect;
        [SerializeField] private RectTransform ladleButtonRect;
        [SerializeField] private RectTransform spatulaButtonRect;
        [SerializeField] private RectTransform stoveAreaRect;
        [SerializeField] private RectTransform displayTrayRect;
        [SerializeField] private RectTransform customerSlot1Rect;

        private TutorialStep currentStep = TutorialStep.Completed;
        private bool isRunning;
        private bool eventsBound;

        public bool IsRunning => isRunning;

        public event System.Action onTutorialCompleted;

        private void Awake()
        {
            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            if (tutorialBannerRoot != null) tutorialBannerRoot.SetActive(false);
            if (highlightRingRect != null) highlightRingRect.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (eventsBound) return;
            eventsBound = true;

            if (prepStation != null)
            {
                prepStation.onIngredientsChanged += HandleIngredientsChanged;
                prepStation.onStageChanged += HandlePrepStageChanged;
            }
            if (displayTray != null)
            {
                displayTray.onMealPlaced += HandleMealPlaced;
            }
            LevelGameManager mgr = levelManager != null ? levelManager : LevelGameManager.Instance;
            if (mgr != null)
            {
                mgr.onAnyOrderProgress += HandleCustomerServed;
            }
        }

        private void UnbindEvents()
        {
            if (!eventsBound) return;
            eventsBound = false;

            if (prepStation != null)
            {
                prepStation.onIngredientsChanged -= HandleIngredientsChanged;
                prepStation.onStageChanged -= HandlePrepStageChanged;
            }
            if (displayTray != null)
            {
                displayTray.onMealPlaced -= HandleMealPlaced;
            }
            LevelGameManager mgr = levelManager != null ? levelManager : LevelGameManager.Instance;
            if (mgr != null)
            {
                mgr.onAnyOrderProgress -= HandleCustomerServed;
            }
        }

        public void BeginTutorial()
        {
            BindEvents();
            isRunning = true;
            SetStep(TutorialStep.DropIngredient);

            if (tutorialBannerRoot != null) tutorialBannerRoot.SetActive(true);
            if (highlightRingRect != null) highlightRingRect.gameObject.SetActive(true);
        }

        public void SkipTutorial()
        {
            if (!isRunning) return;
            FinishTutorial(wasSkipped: true);
        }

        /// <summary>
        /// Direct hook called by LevelGameManager whenever a meal or customer order is served,
        /// guaranteeing tutorial completion cannot miss an event ordering edge case.
        /// </summary>
        public void NotifyDishServed()
        {
            if (!isRunning) return;
            FinishTutorial(wasSkipped: false);
        }

        private void Update()
        {
            if (!isRunning) return;
            UpdateHighlightFrame();
        }

        private void HandleIngredientsChanged(int count)
        {
            if (!isRunning) return;
            if (currentStep == TutorialStep.DropIngredient && count >= 1)
            {
                SetStep(TutorialStep.ChopAndMix);
            }
        }

        private void HandlePrepStageChanged(PrepStage stage)
        {
            if (!isRunning) return;

            if (currentStep <= TutorialStep.ChopAndMix && stage == PrepStage.AssigningCookware)
            {
                SetStep(TutorialStep.CookAndPlate);
            }
            else if (currentStep == TutorialStep.ChopAndMix && (stage == PrepStage.Chopping || stage == PrepStage.Mixing))
            {
                RefreshStepBannerText();
            }
        }

        private void HandleMealPlaced()
        {
            if (!isRunning) return;
            if (currentStep <= TutorialStep.ServeCustomer)
            {
                SetStep(TutorialStep.ServeCustomer);
            }
        }

        private void HandleCustomerServed()
        {
            if (!isRunning) return;
            FinishTutorial(wasSkipped: false);
        }

        private void SetStep(TutorialStep step)
        {
            currentStep = step;
            RefreshStepBannerText();
        }

        private void RefreshStepBannerText()
        {
            int stepNumber = Mathf.Clamp((int)currentStep + 1, 1, 4);

            if (stepBadgeText != null)
            {
                stepBadgeText.text = $"Guide {stepNumber}/4 (Timer Paused)";
            }

            if (instructionText != null)
            {
                switch (currentStep)
                {
                    case TutorialStep.DropIngredient:
                        instructionText.text = "Drag Tomatoes from the tray onto the Cutting Board!";
                        break;
                    case TutorialStep.ChopAndMix:
                        instructionText.text = (prepStation != null && prepStation.CurrentStage == PrepStage.Mixing)
                            ? "Now click MIX when the marker hits the green zone!"
                            : "Click CHOP when the white marker hits the green zone!";
                        break;
                    case TutorialStep.CookAndPlate:
                        instructionText.text = "Pick glowing Ladle/Spatula, drag to a glowing Burner, then click Plate!";
                        break;
                    case TutorialStep.ServeCustomer:
                        instructionText.text = "Drag the dish from Display Tray onto Customer 1 to start your shift!";
                        break;
                    default:
                        instructionText.text = string.Empty;
                        break;
                }
            }
        }

        private void FinishTutorial(bool wasSkipped)
        {
            if (!isRunning) return;
            isRunning = false;
            currentStep = TutorialStep.Completed;
            UnbindEvents();

            if (tutorialBannerRoot != null) tutorialBannerRoot.SetActive(false);
            if (highlightRingRect != null) highlightRingRect.gameObject.SetActive(false);

            DishNotificationUI.Instance?.ShowNotification(
                wasSkipped
                    ? "Tutorial Skipped — Day 1 Shift Timer Started!"
                    : "Great Job! Shift Timer Started — Serve the remaining customers!");

            onTutorialCompleted?.Invoke();
        }

        private void UpdateHighlightFrame()
        {
            if (highlightRingRect == null) return;

            RectTransform target = ResolveCurrentTargetRect();
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                highlightRingRect.gameObject.SetActive(false);
                return;
            }

            highlightRingRect.gameObject.SetActive(true);
            Transform ringParent = highlightRingRect.parent;
            if (ringParent != null)
            {
                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);

                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 local = ringParent.InverseTransformPoint(corners[i]);
                    min.x = Mathf.Min(min.x, local.x);
                    min.y = Mathf.Min(min.y, local.y);
                    max.x = Mathf.Max(max.x, local.x);
                    max.y = Mathf.Max(max.y, local.y);
                }

                float pulseExpand = 6f + 6f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f));
                highlightRingRect.anchoredPosition = (min + max) * 0.5f;
                highlightRingRect.sizeDelta = new Vector2((max.x - min.x) + pulseExpand * 2f, (max.y - min.y) + pulseExpand * 2f);
            }

            if (highlightRingImage != null)
            {
                highlightRingImage.color = new Color(0.99f, 0.85f, 0.22f, 0.20f + 0.16f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f)));
            }
        }

        private RectTransform ResolveCurrentTargetRect()
        {
            float toggle = Mathf.Sin(Time.unscaledTime * 2.2f);
            switch (currentStep)
            {
                case TutorialStep.DropIngredient:
                    return toggle >= 0f ? ingredientTrayRect : cuttingBoardRect;
                case TutorialStep.ChopAndMix:
                    return (prepStation != null && prepStation.CurrentStage == PrepStage.Mixing)
                        ? mixButtonRect
                        : chopButtonRect;
                case TutorialStep.CookAndPlate:
                    if (prepStation != null && prepStation.CurrentStage == PrepStage.AssigningCookware)
                    {
                        return prepStation.RecommendedCookware == CookwareType.Pan ? spatulaButtonRect : ladleButtonRect;
                    }
                    return stoveAreaRect;
                case TutorialStep.ServeCustomer:
                    return toggle >= 0f ? displayTrayRect : customerSlot1Rect;
                default:
                    return null;
            }
        }
    }
}
