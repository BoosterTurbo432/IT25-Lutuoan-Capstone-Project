using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Simplified 4-step interactive Day 1 guided tutorial with BOTH:
    /// - Flashing golden target highlight ring
    /// - Animated guideline hand cursor + tap ripple + dotted drag arc that demonstrates
    ///   exact drag/tap actions for visual learners.
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

        private enum CursorMotionMode
        {
            Hidden,
            Tap,
            Drag,
            HoverWait
        }

        [Header("Banner UI")]
        [SerializeField] private GameObject tutorialBannerRoot;
        [SerializeField] private TMP_Text stepBadgeText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Button skipTutorialButton;

        [Header("Pulsing Highlight Frame")]
        [SerializeField] private RectTransform highlightRingRect;
        [SerializeField] private Image highlightRingImage;

        [Header("Animated Guideline Hand Cursor")]
        [SerializeField] private RectTransform cursorRoot;
        [SerializeField] private Image cursorImage;
        [SerializeField] private RectTransform cursorTapRing;
        [SerializeField] private Image cursorTapRingImage;
        [SerializeField] private RectTransform[] cursorTrailDots;

        [Header("Scene Station References")]
        [SerializeField] private LevelGameManager levelManager;
        [SerializeField] private PrepStation prepStation;
        [SerializeField] private FoodDisplayTray displayTray;
        [SerializeField] private CookingStation[] cookingStations;
        [SerializeField] private RectTransform tomatoSlotRect;
        [SerializeField] private RectTransform ingredientTrayRect;
        [SerializeField] private RectTransform cuttingBoardRect;
        [SerializeField] private RectTransform chopButtonRect;
        [SerializeField] private RectTransform mixButtonRect;
        [SerializeField] private RectTransform ladleButtonRect;
        [SerializeField] private RectTransform spatulaButtonRect;
        [SerializeField] private RectTransform readyDishDockRect;
        [SerializeField] private RectTransform burner1PotRect;
        [SerializeField] private RectTransform stoveAreaRect;
        [SerializeField] private RectTransform displaySlot1Rect;
        [SerializeField] private RectTransform displayTrayRect;
        [SerializeField] private RectTransform customerSlot1Rect;

        private TutorialStep currentStep = TutorialStep.Completed;
        private bool isRunning;
        private bool eventsBound;

        public bool IsRunning => isRunning;
        public bool IsGuidelineCursorVisible => cursorRoot != null && cursorRoot.gameObject.activeSelf;

        public event System.Action onTutorialCompleted;

        private void Awake()
        {
            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.AddListener(SkipTutorial);
            }

            if (tutorialBannerRoot != null) tutorialBannerRoot.SetActive(false);
            if (highlightRingRect != null) highlightRingRect.gameObject.SetActive(false);
            SetCursorVisible(false);
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
            SetCursorVisible(true);
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

            LevelGameManager mgr = levelManager != null ? levelManager : LevelGameManager.Instance;
            if (mgr != null && mgr.IsHelpOrPauseOpen)
            {
                if (highlightRingRect != null) highlightRingRect.gameObject.SetActive(false);
                SetCursorVisible(false);
                return;
            }

            UpdateHighlightFrame();
            UpdateGuidelineCursor();
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
            else if (currentStep == TutorialStep.CookAndPlate)
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
                            ? "Tap MIX when the white marker hits the green zone!"
                            : "Tap CHOP when the white marker hits the green zone!";
                        break;
                    case TutorialStep.CookAndPlate:
                        if (prepStation != null && prepStation.CurrentStage == PrepStage.AssigningCookware)
                        {
                            instructionText.text = "Tap glowing Ladle (Pot) to assign your cookware!";
                        }
                        else if (prepStation != null && prepStation.CurrentStage == PrepStage.ReadyForStove)
                        {
                            instructionText.text = "Drag Ready Mix onto a glowing Pot burner on the Stove!";
                        }
                        else
                        {
                            instructionText.text = "Wait for the ring to finish, then tap Plate! within 5s!";
                        }
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
            SetCursorVisible(false);

            DishNotificationUI.Instance?.ShowNotification(
                wasSkipped
                    ? "Tutorial Skipped — Day 1 Shift Timer Started!"
                    : "Great Job! Shift Timer Started — Serve the remaining customers!");

            onTutorialCompleted?.Invoke();
        }

        private void SetCursorVisible(bool visible)
        {
            if (cursorRoot != null) cursorRoot.gameObject.SetActive(visible);
            if (cursorTapRing != null && !visible) cursorTapRing.gameObject.SetActive(false);
            if (cursorTrailDots != null && !visible)
            {
                for (int i = 0; i < cursorTrailDots.Length; i++)
                {
                    if (cursorTrailDots[i] != null) cursorTrailDots[i].gameObject.SetActive(false);
                }
            }
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

        private void UpdateGuidelineCursor()
        {
            if (cursorRoot == null) return;

            ResolveCursorAction(out CursorMotionMode mode, out RectTransform fromRect, out RectTransform toRect);
            if (mode == CursorMotionMode.Hidden || fromRect == null || !fromRect.gameObject.activeInHierarchy)
            {
                SetCursorVisible(false);
                return;
            }

            Transform cursorParent = cursorRoot.parent;
            if (cursorParent == null) return;

            cursorRoot.gameObject.SetActive(true);
            Vector2 startPt = GetLocalCenter(fromRect, cursorParent);

            if (mode == CursorMotionMode.Drag && toRect != null && toRect.gameObject.activeInHierarchy)
            {
                Vector2 endPt = GetLocalCenter(toRect, cursorParent);
                AnimateDragCursor(startPt, endPt);
            }
            else if (mode == CursorMotionMode.Tap)
            {
                HideTrailDots();
                AnimateTapCursor(startPt);
            }
            else
            {
                HideTrailDots();
                AnimateHoverWaitCursor(startPt);
            }
        }

        private void AnimateDragCursor(Vector2 startPt, Vector2 endPt)
        {
            // 1.9s repeating drag cycle: [0..0.18] grab press -> [0.18..0.74] smooth curved glide -> [0.74..0.90] release -> [0.90..1.0] fade reset
            const float cycleDur = 1.95f;
            float phase = (Time.unscaledTime % cycleDur) / cycleDur;

            Vector2 midControl = (startPt + endPt) * 0.5f + new Vector2(0f, 54f);
            Vector2 pos;
            float scale = 1f;
            float alpha = 1f;
            float tiltZ = 0f;

            if (phase < 0.18f)
            {
                float t = phase / 0.18f;
                pos = startPt;
                scale = Mathf.Lerp(1.06f, 0.86f, t);
                tiltZ = Mathf.Lerp(4f, -10f, t);
                UpdateTapRing(startPt, t, 0.85f);
            }
            else if (phase < 0.74f)
            {
                float t = Mathf.SmoothStep(0f, 1f, (phase - 0.18f) / 0.56f);
                pos = EvalQuadraticBezier(startPt, midControl, endPt, t);
                scale = 0.88f;
                tiltZ = -10f;
                if (cursorTapRing != null) cursorTapRing.gameObject.SetActive(false);
            }
            else if (phase < 0.90f)
            {
                float t = (phase - 0.74f) / 0.16f;
                pos = endPt;
                scale = Mathf.Lerp(0.88f, 1.04f, t);
                tiltZ = Mathf.Lerp(-10f, 4f, t);
                UpdateTapRing(endPt, t, 0.9f);
            }
            else
            {
                float t = (phase - 0.90f) / 0.10f;
                pos = endPt;
                alpha = Mathf.Lerp(1f, 0f, t);
                if (cursorTapRing != null) cursorTapRing.gameObject.SetActive(false);
            }

            // Offset slightly down-right so fingertip lands directly on target center
            cursorRoot.anchoredPosition = pos + new Vector2(18f, -26f);
            cursorRoot.localScale = new Vector3(scale, scale, 1f);
            cursorRoot.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            if (cursorImage != null) cursorImage.color = new Color(1f, 1f, 1f, alpha);

            // Show subtle glowing dotted path along the drag arc
            if (cursorTrailDots != null)
            {
                for (int i = 0; i < cursorTrailDots.Length; i++)
                {
                    RectTransform dot = cursorTrailDots[i];
                    if (dot == null) continue;
                    float frac = (i + 1f) / (cursorTrailDots.Length + 1f);
                    dot.gameObject.SetActive(true);
                    dot.anchoredPosition = EvalQuadraticBezier(startPt, midControl, endPt, frac);
                    float pulse = 0.35f + 0.55f * Mathf.Clamp01(Mathf.Sin((Time.unscaledTime * 5f) - frac * 3.2f) * 0.5f + 0.5f);
                    Image dotImg = dot.GetComponent<Image>();
                    if (dotImg != null) dotImg.color = new Color(0.99f, 0.88f, 0.28f, pulse);
                }
            }
        }

        private void AnimateTapCursor(Vector2 targetPt)
        {
            const float cycleDur = 0.95f;
            float phase = (Time.unscaledTime % cycleDur) / cycleDur;

            float pressT = phase < 0.35f ? (phase / 0.35f) : (1f - Mathf.Clamp01((phase - 0.35f) / 0.45f));
            float scale = Mathf.Lerp(1.04f, 0.82f, pressT);
            float liftY = Mathf.Lerp(10f, 0f, pressT);
            float tiltZ = Mathf.Lerp(6f, -12f, pressT);

            cursorRoot.anchoredPosition = targetPt + new Vector2(18f, -26f + liftY);
            cursorRoot.localScale = new Vector3(scale, scale, 1f);
            cursorRoot.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            if (cursorImage != null) cursorImage.color = Color.white;

            if (phase >= 0.25f && phase <= 0.88f)
            {
                float ringT = (phase - 0.25f) / 0.63f;
                UpdateTapRing(targetPt, ringT, 0.95f);
            }
            else if (cursorTapRing != null)
            {
                cursorTapRing.gameObject.SetActive(false);
            }
        }

        private void AnimateHoverWaitCursor(Vector2 targetPt)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 4.5f) * 8f;
            cursorRoot.anchoredPosition = targetPt + new Vector2(36f, -24f + bob);
            cursorRoot.localScale = Vector3.one * 0.95f;
            cursorRoot.localRotation = Quaternion.Euler(0f, 0f, 6f);
            if (cursorImage != null) cursorImage.color = new Color(1f, 1f, 1f, 0.85f);
            if (cursorTapRing != null) cursorTapRing.gameObject.SetActive(false);
        }

        private void UpdateTapRing(Vector2 centerPt, float progress01, float maxAlpha)
        {
            if (cursorTapRing == null) return;
            cursorTapRing.gameObject.SetActive(true);
            cursorTapRing.anchoredPosition = centerPt;
            float s = Mathf.Lerp(0.45f, 1.35f, progress01);
            cursorTapRing.localScale = new Vector3(s, s, 1f);
            if (cursorTapRingImage != null)
            {
                cursorTapRingImage.color = new Color(1f, 0.90f, 0.28f, (1f - progress01) * maxAlpha);
            }
        }

        private void HideTrailDots()
        {
            if (cursorTrailDots == null) return;
            for (int i = 0; i < cursorTrailDots.Length; i++)
            {
                if (cursorTrailDots[i] != null) cursorTrailDots[i].gameObject.SetActive(false);
            }
        }

        private static Vector2 EvalQuadraticBezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * control) + (t * t * b);
        }

        private static Vector2 GetLocalCenter(RectTransform target, Transform relativeTo)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            return relativeTo.InverseTransformPoint(worldCenter);
        }

        private void ResolveCursorAction(out CursorMotionMode mode, out RectTransform fromRect, out RectTransform toRect)
        {
            mode = CursorMotionMode.Hidden;
            fromRect = null;
            toRect = null;

            switch (currentStep)
            {
                case TutorialStep.DropIngredient:
                    mode = CursorMotionMode.Drag;
                    fromRect = tomatoSlotRect != null ? tomatoSlotRect : ingredientTrayRect;
                    toRect = cuttingBoardRect;
                    return;

                case TutorialStep.ChopAndMix:
                    mode = CursorMotionMode.Tap;
                    fromRect = (prepStation != null && prepStation.CurrentStage == PrepStage.Mixing)
                        ? mixButtonRect
                        : chopButtonRect;
                    return;

                case TutorialStep.CookAndPlate:
                    if (prepStation != null && prepStation.CurrentStage == PrepStage.AssigningCookware)
                    {
                        mode = CursorMotionMode.Tap;
                        fromRect = prepStation.RecommendedCookware == CookwareType.Pan ? spatulaButtonRect : ladleButtonRect;
                        return;
                    }
                    if (prepStation != null && prepStation.CurrentStage == PrepStage.ReadyForStove)
                    {
                        mode = CursorMotionMode.Drag;
                        fromRect = readyDishDockRect != null ? readyDishDockRect : cuttingBoardRect;
                        toRect = burner1PotRect != null ? burner1PotRect : stoveAreaRect;
                        return;
                    }
                    if (TryFindActiveBurnerTarget(out RectTransform plateBtnRect, out RectTransform cookingBurnerRect))
                    {
                        if (plateBtnRect != null)
                        {
                            mode = CursorMotionMode.Tap;
                            fromRect = plateBtnRect;
                            return;
                        }
                        if (cookingBurnerRect != null)
                        {
                            mode = CursorMotionMode.HoverWait;
                            fromRect = cookingBurnerRect;
                            return;
                        }
                    }
                    mode = CursorMotionMode.Tap;
                    fromRect = burner1PotRect != null ? burner1PotRect : stoveAreaRect;
                    return;

                case TutorialStep.ServeCustomer:
                    mode = CursorMotionMode.Drag;
                    fromRect = displaySlot1Rect != null ? displaySlot1Rect : displayTrayRect;
                    toRect = customerSlot1Rect;
                    return;
            }
        }

        private bool TryFindActiveBurnerTarget(out RectTransform plateButtonRect, out RectTransform cookingBurnerRect)
        {
            plateButtonRect = null;
            cookingBurnerRect = null;

            if (cookingStations == null) return false;
            for (int i = 0; i < cookingStations.Length; i++)
            {
                CookingStation st = cookingStations[i];
                if (st == null) continue;
                if (st.CurrentState == CookingStationState.Cooked)
                {
                    Transform serveBtn = st.transform.Find("ServeButton");
                    plateButtonRect = serveBtn != null ? serveBtn as RectTransform : st.transform as RectTransform;
                    return true;
                }
                if (st.CurrentState == CookingStationState.Cooking && cookingBurnerRect == null)
                {
                    cookingBurnerRect = st.transform as RectTransform;
                }
            }
            return cookingBurnerRect != null;
        }

        private RectTransform ResolveCurrentTargetRect()
        {
            float toggle = Mathf.Sin(Time.unscaledTime * 2.2f);
            switch (currentStep)
            {
                case TutorialStep.DropIngredient:
                    return toggle >= 0f
                        ? (tomatoSlotRect != null ? tomatoSlotRect : ingredientTrayRect)
                        : cuttingBoardRect;
                case TutorialStep.ChopAndMix:
                    return (prepStation != null && prepStation.CurrentStage == PrepStage.Mixing)
                        ? mixButtonRect
                        : chopButtonRect;
                case TutorialStep.CookAndPlate:
                    if (prepStation != null && prepStation.CurrentStage == PrepStage.AssigningCookware)
                    {
                        return prepStation.RecommendedCookware == CookwareType.Pan ? spatulaButtonRect : ladleButtonRect;
                    }
                    if (prepStation != null && prepStation.CurrentStage == PrepStage.ReadyForStove)
                    {
                        return toggle >= 0f
                            ? (readyDishDockRect != null ? readyDishDockRect : cuttingBoardRect)
                            : (burner1PotRect != null ? burner1PotRect : stoveAreaRect);
                    }
                    if (TryFindActiveBurnerTarget(out RectTransform plateRect, out RectTransform cookingRect))
                    {
                        return plateRect != null ? plateRect : cookingRect;
                    }
                    return stoveAreaRect;
                case TutorialStep.ServeCustomer:
                    return toggle >= 0f
                        ? (displaySlot1Rect != null ? displaySlot1Rect : displayTrayRect)
                        : customerSlot1Rect;
                default:
                    return null;
            }
        }
    }
}
