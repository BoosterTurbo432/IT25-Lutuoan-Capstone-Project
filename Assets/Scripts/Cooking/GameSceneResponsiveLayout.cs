using CookingSystem.Core.Responsive;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Switches gamescene.unity smoothly between the Carinderia Desktop/Landscape layout (1920x1080)
    /// matching the visualization and Mobile/Narrow Portrait (1080x1920) based on the active viewport.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSceneResponsiveLayout : MonoBehaviour
    {
        [Header("Root Panels")]
        [SerializeField] private RectTransform wallBackdrop;
        [SerializeField] private RectTransform topHudBar;
        [SerializeField] private RectTransform dishNotificationBanner;
        [SerializeField] private RectTransform customerZone;
        [SerializeField] private RectTransform countertopArea;
        [SerializeField] private RectTransform plasticMatRect;

        [Header("Customer Zone")]
        [SerializeField] private RectTransform customerSlotsContainer;
        [SerializeField] private HorizontalLayoutGroup customerSlotsLayout;
        [SerializeField] private RectTransform serviceCounterLedge;

        [Header("Kitchen Zones")]
        [SerializeField] private RectTransform drinkDispenserZone;
        [SerializeField] private RectTransform ingredientTrayZone;
        [SerializeField] private RectTransform stoveAreaZone;
        [SerializeField] private RectTransform stoveArtRoot;
        [SerializeField] private RectTransform utensilsStationZone;
        [SerializeField] private RectTransform prepTrayRoot;
        [SerializeField] private RectTransform foodDisplayTrayZone;
        [SerializeField] private RectTransform utilitySafetyZone;

        [Header("Modals & Guided Tutorial")]
        [SerializeField] private RectTransform tutorialCardRect;
        [SerializeField] private GridLayoutGroup tutorialStepsGrid;
        [SerializeField] private RectTransform guidedTutorialBanner;

        private ViewportOrientation? appliedOrientation;
        private Rect appliedSafeArea;
        private int activeIngredientTier = 1;

        public void ConfigureIngredientTier(int unlockDay)
        {
            activeIngredientTier = Mathf.Clamp(unlockDay, 1, 3);
            if (ingredientTrayZone != null)
            {
                Transform grid = ingredientTrayZone.Find("IngredientSlotsGrid");
                if (grid != null)
                {
                    int visibleCount = activeIngredientTier switch
                    {
                        1 => 4,
                        2 => 8,
                        _ => 12
                    };
                    for (int i = 0; i < grid.childCount; i++)
                    {
                        grid.GetChild(i).gameObject.SetActive(i < visibleCount);
                    }
                    bool portrait = appliedOrientation == ViewportOrientation.Portrait;
                    ApplyIngredientGridLayout(portrait);
                }
            }
        }

        private void OnEnable()
        {
            ResponsiveViewportManager mgr = ResponsiveViewportManager.GetOrCreate();
            mgr.RegisterAdaptiveSceneHandler(GetReferenceResolutionForOrientation, ApplyViewportProfile);
        }

        private void OnDisable()
        {
            if (ResponsiveViewportManager.Instance != null)
            {
                ResponsiveViewportManager.Instance.UnregisterAdaptiveSceneHandler(ApplyViewportProfile);
            }
        }

        private static Vector2 GetReferenceResolutionForOrientation(ViewportOrientation orientation)
        {
            return orientation == ViewportOrientation.Portrait
                ? new Vector2(1080f, 1920f)
                : new Vector2(1920f, 1080f);
        }

        public void ApplyViewportProfile(ViewportProfile profile)
        {
            if (appliedOrientation == profile.orientation && appliedSafeArea == profile.safeArea)
            {
                return;
            }

            appliedOrientation = profile.orientation;
            appliedSafeArea = profile.safeArea;

            if (profile.orientation == ViewportOrientation.Portrait)
            {
                ApplyPortraitLayout();
            }
            else
            {
                ApplyLandscapeLayout();
            }

            ApplySafeAreaOffset(profile);
            Canvas.ForceUpdateCanvases();
        }

        private void ApplyLandscapeLayout()
        {
            if (wallBackdrop != null)
            {
                SetBox(wallBackdrop, new Vector2(0f, 320f), new Vector2(2600f, 480f));
            }

            if (topHudBar != null)
            {
                SetBox(topHudBar, new Vector2(0f, 502f), new Vector2(1880f, 66f));
                SetBox(topHudBar.Find("UI_ScorePill"), new Vector2(-702f, 0f), new Vector2(420f, 58f));
                SetBox(topHudBar.Find("UI_TimerBarBg"), new Vector2(-12f, 0f), new Vector2(900f, 62f));
                SetBox(topHudBar.Find("HelpHudButton"), new Vector2(548f, 0f), new Vector2(160f, 56f));
                SetBox(topHudBar.Find("PauseHudButton"), new Vector2(782f, 0f), new Vector2(260f, 56f));
            }

            if (dishNotificationBanner != null)
            {
                SetBox(dishNotificationBanner, new Vector2(0f, 444f), new Vector2(760f, 48f));
            }

            if (guidedTutorialBanner != null)
            {
                SetBox(guidedTutorialBanner, new Vector2(0f, 434f), new Vector2(1040f, 68f));
            }

            if (customerZone != null)
            {
                SetBox(customerZone, new Vector2(0f, 294f), new Vector2(1880f, 336f));
                SetBox(customerSlotsContainer, new Vector2(0f, -2f), new Vector2(1760f, 276f));
                if (customerSlotsLayout != null) customerSlotsLayout.spacing = 70f;
                SetBox(serviceCounterLedge, new Vector2(0f, -158f), new Vector2(2600f, 24f));
                ApplyCustomerSlotCards(portrait: false);
            }

            if (countertopArea != null)
            {
                SetBox(countertopArea, new Vector2(0f, -210f), new Vector2(1920f, 650f));
                if (plasticMatRect != null) SetBox(plasticMatRect, new Vector2(0f, 6f), new Vector2(1840f, 608f));
            }

            // Left Column: Drink Dispenser stacked above Ingredient Tray [-908 .. -492]
            if (drinkDispenserZone != null)
            {
                SetBox(drinkDispenserZone, new Vector2(-700f, 148f), new Vector2(416f, 298f));
                Transform artGroup = drinkDispenserZone.Find("DispenserArtGroup");
                if (artGroup != null)
                {
                    SetBox(artGroup, new Vector2(-68f, 8f), new Vector2(246f, 238f));
                    artGroup.localScale = new Vector3(0.95f, 0.95f, 1f);
                }
                Transform cupDock = drinkDispenserZone.Find("CupSlot_Dock");
                if (cupDock != null)
                {
                    SetBox(cupDock, new Vector2(126f, -14f), new Vector2(132f, 224f));
                    Transform cup = cupDock.Find("DraggableCup_Item");
                    if (cup != null)
                    {
                        SetSize(cup, new Vector2(84f, 126f));
                        var dc = cup.GetComponent<DraggableCup>();
                        if (dc != null) dc.SetRestPosition(new Vector2(0f, 18f));
                        else SetPos(cup, new Vector2(0f, 18f));
                    }
                }
            }

            if (ingredientTrayZone != null)
            {
                SetBox(ingredientTrayZone, new Vector2(-700f, -160f), new Vector2(416f, 290f));
                ApplyIngredientGridLayout(portrait: false);
            }

            // Center Column: 4-Burner Stove stacked above Kitchen Prep Tray [-472 .. +380]
            if (stoveAreaZone != null)
            {
                SetBox(stoveAreaZone, new Vector2(-46f, 126f), new Vector2(780f, 360f));
                if (stoveArtRoot != null)
                {
                    SetBox(stoveArtRoot, new Vector2(0f, -4f), new Vector2(520f, 354f));
                    stoveArtRoot.localScale = Vector3.one;
                }
            }

            if (utensilsStationZone != null)
            {
                SetBox(utensilsStationZone, new Vector2(-46f, -186f), new Vector2(852f, 248f));
                if (prepTrayRoot != null)
                {
                    SetBox(prepTrayRoot, Vector2.zero, new Vector2(852f, 248f));
                    prepTrayRoot.localScale = Vector3.one;
                }
            }

            // Right Column: 3x2 Display Tray stacked above Extinguisher & Trash Can [+400 .. +908]
            if (foodDisplayTrayZone != null)
            {
                SetBox(foodDisplayTrayZone, new Vector2(654f, 96f), new Vector2(508f, 414f));
                Transform grid = foodDisplayTrayZone.Find("DisplaySlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -16f), new Vector2(486f, 344f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(154f, 162f);
                        glg.spacing = new Vector2(10f, 12f);
                    }
                    ApplyDisplaySlots(grid, portrait: false);
                }
            }

            if (utilitySafetyZone != null)
            {
                SetBox(utilitySafetyZone, new Vector2(654f, -204f), new Vector2(508f, 190f));
                Transform extDock = utilitySafetyZone.Find("FireExtinguisher_Dock");
                if (extDock != null)
                {
                    SetBox(extDock, new Vector2(-120f, 0f), new Vector2(248f, 158f));
                    Transform tool = extDock.Find("FireExtinguisher_Tool");
                    if (tool != null)
                    {
                        SetSize(tool, new Vector2(214f, 118f));
                        var ft = tool.GetComponent<FireExtinguisherTool>();
                        if (ft != null) ft.SetRestPosition(new Vector2(0f, 12f));
                        else SetPos(tool, new Vector2(0f, 12f));
                    }
                }
                Transform trash = utilitySafetyZone.Find("TrashCan_DropZone");
                if (trash != null)
                {
                    SetBox(trash, new Vector2(130f, 0f), new Vector2(224f, 166f));
                }
            }

            if (tutorialCardRect != null)
            {
                SetBox(tutorialCardRect, Vector2.zero, new Vector2(1460f, 820f));
                Transform cookGrid = tutorialCardRect.Find("InnerPanel/HowToCookSection/CookStepsGrid");
                if (cookGrid != null && cookGrid.TryGetComponent<GridLayoutGroup>(out var cglg))
                {
                    cglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    cglg.constraintCount = 4;
                    cglg.cellSize = new Vector2(326f, 246f);
                    cglg.spacing = new Vector2(14f, 12f);
                }
                Transform custGrid = tutorialCardRect.Find("InnerPanel/HowCustomersSection/CustomerCardsGrid");
                if (custGrid != null && custGrid.TryGetComponent<GridLayoutGroup>(out var ulg))
                {
                    ulg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    ulg.constraintCount = 3;
                    ulg.cellSize = new Vector2(440f, 246f);
                    ulg.spacing = new Vector2(16f, 12f);
                }
            }
        }

        private void ApplyPortraitLayout()
        {
            if (wallBackdrop != null)
            {
                SetBox(wallBackdrop, new Vector2(0f, 680f), new Vector2(1600f, 560f));
            }

            if (topHudBar != null)
            {
                SetBox(topHudBar, new Vector2(0f, 886f), new Vector2(1052f, 124f));
                SetBox(topHudBar.Find("UI_ScorePill"), new Vector2(-266f, 28f), new Vector2(480f, 54f));
                SetBox(topHudBar.Find("UI_TimerBarBg"), new Vector2(0f, -30f), new Vector2(1012f, 56f));
                SetBox(topHudBar.Find("HelpHudButton"), new Vector2(96f, 28f), new Vector2(196f, 54f));
                SetBox(topHudBar.Find("PauseHudButton"), new Vector2(362f, 28f), new Vector2(288f, 54f));
            }

            if (dishNotificationBanner != null)
            {
                SetBox(dishNotificationBanner, new Vector2(0f, 800f), new Vector2(920f, 52f));
            }

            if (guidedTutorialBanner != null)
            {
                SetBox(guidedTutorialBanner, new Vector2(0f, 462f), new Vector2(1010f, 70f));
            }

            if (customerZone != null)
            {
                SetBox(customerZone, new Vector2(0f, 642f), new Vector2(1052f, 344f));
                SetBox(customerSlotsContainer, new Vector2(0f, -4f), new Vector2(1036f, 272f));
                if (customerSlotsLayout != null) customerSlotsLayout.spacing = 16f;
                SetBox(serviceCounterLedge, new Vector2(0f, -162f), new Vector2(1600f, 22f));
                ApplyCustomerSlotCards(portrait: true);
            }

            if (countertopArea != null)
            {
                SetBox(countertopArea, new Vector2(0f, -248f), new Vector2(1080f, 1396f));
                if (plasticMatRect != null) SetBox(plasticMatRect, new Vector2(0f, 0f), new Vector2(1032f, 1320f));
            }

            // Portrait Top Row: Ingredient Tray (left) + 4-Burner Stove (right)
            if (ingredientTrayZone != null)
            {
                SetBox(ingredientTrayZone, new Vector2(-318f, 474f), new Vector2(406f, 396f));
                ApplyIngredientGridLayout(portrait: true);
            }

            if (stoveAreaZone != null)
            {
                SetBox(stoveAreaZone, new Vector2(204f, 474f), new Vector2(616f, 396f));
                if (stoveArtRoot != null)
                {
                    SetBox(stoveArtRoot, new Vector2(0f, -4f), new Vector2(528f, 362f));
                    stoveArtRoot.localScale = new Vector3(0.96f, 0.96f, 1f);
                }
            }

            // Portrait Mid Row: Drink Dispenser (left) + Kitchen Prep Tray (right)
            if (drinkDispenserZone != null)
            {
                SetBox(drinkDispenserZone, new Vector2(-346f, 108f), new Vector2(340f, 286f));
                Transform artGroup = drinkDispenserZone.Find("DispenserArtGroup");
                if (artGroup != null)
                {
                    SetBox(artGroup, new Vector2(-58f, 8f), new Vector2(246f, 238f));
                    artGroup.localScale = new Vector3(0.84f, 0.88f, 1f);
                }
                Transform cupDock = drinkDispenserZone.Find("CupSlot_Dock");
                if (cupDock != null)
                {
                    SetBox(cupDock, new Vector2(104f, -12f), new Vector2(114f, 224f));
                    Transform cup = cupDock.Find("DraggableCup_Item");
                    if (cup != null)
                    {
                        SetSize(cup, new Vector2(78f, 118f));
                        var dc = cup.GetComponent<DraggableCup>();
                        if (dc != null) dc.SetRestPosition(new Vector2(0f, 18f));
                        else SetPos(cup, new Vector2(0f, 18f));
                    }
                }
            }

            if (utensilsStationZone != null)
            {
                SetBox(utensilsStationZone, new Vector2(180f, 108f), new Vector2(688f, 286f));
                if (prepTrayRoot != null)
                {
                    SetBox(prepTrayRoot, Vector2.zero, new Vector2(860f, 252f));
                    prepTrayRoot.localScale = new Vector3(0.79f, 0.96f, 1f);
                }
            }

            // Portrait Lower Row: 3x2 Display Tray
            if (foodDisplayTrayZone != null)
            {
                SetBox(foodDisplayTrayZone, new Vector2(0f, -242f), new Vector2(1024f, 360f));
                Transform grid = foodDisplayTrayZone.Find("DisplaySlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -16f), new Vector2(990f, 300f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(318f, 142f);
                        glg.spacing = new Vector2(14f, 12f);
                    }
                    ApplyDisplaySlots(grid, portrait: true);
                }
            }

            // Portrait Bottom Row: Extinguisher + Trash Can
            if (utilitySafetyZone != null)
            {
                SetBox(utilitySafetyZone, new Vector2(0f, -536f), new Vector2(1024f, 186f));
                Transform extDock = utilitySafetyZone.Find("FireExtinguisher_Dock");
                if (extDock != null)
                {
                    SetBox(extDock, new Vector2(-235f, 0f), new Vector2(460f, 158f));
                    Transform tool = extDock.Find("FireExtinguisher_Tool");
                    if (tool != null)
                    {
                        SetSize(tool, new Vector2(260f, 136f));
                        var ft = tool.GetComponent<FireExtinguisherTool>();
                        if (ft != null) ft.SetRestPosition(new Vector2(-46f, 8f));
                        else SetPos(tool, new Vector2(-46f, 8f));
                    }
                }
                Transform trash = utilitySafetyZone.Find("TrashCan_DropZone");
                if (trash != null)
                {
                    SetBox(trash, new Vector2(235f, 0f), new Vector2(460f, 166f));
                }
            }

            if (tutorialCardRect != null)
            {
                SetBox(tutorialCardRect, Vector2.zero, new Vector2(1010f, 1520f));
                Transform cookGrid = tutorialCardRect.Find("InnerPanel/HowToCookSection/CookStepsGrid");
                if (cookGrid != null && cookGrid.TryGetComponent<GridLayoutGroup>(out var cglg))
                {
                    cglg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    cglg.constraintCount = 2;
                    cglg.cellSize = new Vector2(452f, 246f);
                    cglg.spacing = new Vector2(14f, 14f);
                }
                Transform custGrid = tutorialCardRect.Find("InnerPanel/HowCustomersSection/CustomerCardsGrid");
                if (custGrid != null && custGrid.TryGetComponent<GridLayoutGroup>(out var ulg))
                {
                    ulg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    ulg.constraintCount = 1;
                    ulg.cellSize = new Vector2(920f, 162f);
                    ulg.spacing = new Vector2(12f, 12f);
                }
            }
        }

        private void ApplyCustomerSlotCards(bool portrait)
        {
            if (customerSlotsContainer == null) return;

            for (int i = 0; i < customerSlotsContainer.childCount; i++)
            {
                Transform slot = customerSlotsContainer.GetChild(i);
                if (slot == null) continue;

                SetSize(slot, portrait ? new Vector2(334f, 262f) : new Vector2(510f, 268f));

                if (portrait)
                {
                    SetBox(slot.Find("CustomerVisualRoot"), new Vector2(-94f, -6f), new Vector2(128f, 224f));
                    SetBox(slot.Find("SatisfactionEmojiBadge"), new Vector2(-42f, 78f), new Vector2(60f, 60f));
                    SetBox(slot.Find("NameplatePill"), new Vector2(-94f, -110f), new Vector2(128f, 32f));
                    SetBox(slot.Find("OrderBubble"), new Vector2(66f, 8f), new Vector2(186f, 236f));
                    SetBox(slot.Find("PatienceTimerPill"), new Vector2(66f, -118f), new Vector2(184f, 24f));
                    SetBox(slot.Find("FeedbackText"), new Vector2(0f, 114f), new Vector2(316f, 28f));
                }
                else
                {
                    SetBox(slot.Find("CustomerVisualRoot"), new Vector2(-146f, -6f), new Vector2(196f, 240f));
                    SetBox(slot.Find("SatisfactionEmojiBadge"), new Vector2(-62f, 84f), new Vector2(74f, 74f));
                    SetBox(slot.Find("NameplatePill"), new Vector2(-146f, -114f), new Vector2(182f, 34f));
                    SetBox(slot.Find("OrderBubble"), new Vector2(96f, 8f), new Vector2(248f, 254f));
                    SetBox(slot.Find("PatienceTimerPill"), new Vector2(96f, -122f), new Vector2(246f, 26f));
                    SetBox(slot.Find("FeedbackText"), new Vector2(0f, 116f), new Vector2(460f, 28f));
                }
            }
        }

        private void ApplyIngredientGridLayout(bool portrait)
        {
            if (ingredientTrayZone == null) return;
            Transform grid = ingredientTrayZone.Find("IngredientSlotsGrid");
            if (grid == null) return;

            Transform headerPill = ingredientTrayZone.Find("IngredientHeaderPill");
            if (headerPill != null)
            {
                SetBox(headerPill, new Vector2(0f, portrait ? 176f : 124f), new Vector2(384f, 26f));
            }

            SetBox(grid, new Vector2(0f, -14f), portrait ? new Vector2(384f, 338f) : new Vector2(386f, 238f));

            int activeSlots = 0;
            for (int i = 0; i < grid.childCount; i++)
            {
                if (grid.GetChild(i).gameObject.activeSelf) activeSlots++;
            }

            bool compactMultiRow = activeSlots > 4;
            bool threeRows = activeSlots > 8;

            var glg = grid.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.childAlignment = TextAnchor.UpperCenter;
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                if (!compactMultiRow)
                {
                    glg.constraintCount = 2;
                    glg.cellSize = portrait ? new Vector2(184f, 156f) : new Vector2(184f, 108f);
                    glg.spacing = portrait ? new Vector2(12f, 14f) : new Vector2(12f, 10f);
                }
                else if (!threeRows)
                {
                    glg.constraintCount = 4;
                    glg.cellSize = portrait ? new Vector2(90f, 158f) : new Vector2(90f, 112f);
                    glg.spacing = portrait ? new Vector2(6f, 14f) : new Vector2(6f, 8f);
                }
                else
                {
                    glg.constraintCount = 4;
                    glg.cellSize = portrait ? new Vector2(90f, 104f) : new Vector2(90f, 74f);
                    glg.spacing = portrait ? new Vector2(6f, 10f) : new Vector2(6f, 6f);
                }
            }

            ApplyIngredientSlots(grid, portrait, compactMultiRow, threeRows);
            if (grid is RectTransform gridRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRt);
            }
        }

        private static void ApplyIngredientSlots(Transform grid, bool portrait, bool compactMultiRow = false, bool threeRows = false)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform slot = grid.GetChild(i);
                if (slot == null) continue;

                Vector2 socketPos;
                Vector2 iconSize;
                Vector2 titlePos;
                Vector2 titleSize;
                bool showDragPill = !compactMultiRow;

                if (!compactMultiRow)
                {
                    socketPos = portrait ? new Vector2(0f, 22f) : new Vector2(-40f, 0f);
                    iconSize = portrait ? new Vector2(86f, 86f) : new Vector2(82f, 82f);
                    titlePos = portrait ? new Vector2(0f, -34f) : new Vector2(44f, 18f);
                    titleSize = portrait ? new Vector2(166f, 26f) : new Vector2(88f, 28f);
                }
                else if (!threeRows)
                {
                    socketPos = portrait ? new Vector2(0f, 20f) : new Vector2(0f, 14f);
                    iconSize = portrait ? new Vector2(68f, 68f) : new Vector2(60f, 60f);
                    titlePos = portrait ? new Vector2(0f, -36f) : new Vector2(0f, -34f);
                    titleSize = new Vector2(86f, 24f);
                }
                else
                {
                    socketPos = portrait ? new Vector2(0f, 14f) : new Vector2(0f, 11f);
                    iconSize = portrait ? new Vector2(56f, 56f) : new Vector2(44f, 44f);
                    titlePos = portrait ? new Vector2(0f, -30f) : new Vector2(0f, -22f);
                    titleSize = new Vector2(84f, 20f);
                }

                SetBox(slot.Find("SocketRing_Placeholder"), socketPos, iconSize);
                SetBox(slot.Find("CooldownOverlay"), socketPos, iconSize);
                SetBox(slot.Find("CooldownText"), socketPos, new Vector2(Mathf.Min(78f, iconSize.x), 30f));

                SetBox(slot.Find("TitleText"), titlePos, titleSize);
                Transform dragPill = slot.Find("DragHintPill");
                if (dragPill != null)
                {
                    dragPill.gameObject.SetActive(showDragPill);
                    if (showDragPill)
                    {
                        SetBox(dragPill, portrait ? new Vector2(0f, -60f) : new Vector2(44f, -18f),
                            portrait ? new Vector2(162f, 26f) : new Vector2(88f, 28f));
                    }
                }

                for (int c = 0; c < slot.childCount; c++)
                {
                    Transform child = slot.GetChild(c);
                    if (child != null && child.GetComponent<DraggableIngredient>() != null)
                    {
                        if (child is RectTransform drt)
                        {
                            drt.anchorMin = new Vector2(0.5f, 0.5f);
                            drt.anchorMax = new Vector2(0.5f, 0.5f);
                            drt.pivot = new Vector2(0.5f, 0.5f);
                            drt.anchoredPosition = socketPos;
                            drt.sizeDelta = iconSize;
                        }
                    }
                }

                var pantry = slot.GetComponent<PantrySlot>();
                if (pantry != null)
                {
                    pantry.SetSpawnAnchoredPosition(socketPos);
                    pantry.SetOverrideIconSize(iconSize);
                }
            }
        }

        private static void ApplyDisplaySlots(Transform grid, bool portrait)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform slot = grid.GetChild(i);
                if (slot == null) continue;

                Vector2 trayPos = portrait ? new Vector2(-78f, 4f) : new Vector2(0f, 18f);
                Vector2 traySize = portrait ? new Vector2(126f, 114f) : new Vector2(124f, 112f);

                SetBox(slot.Find("TrayFrame_Image"), trayPos, traySize);
                Transform trayItem = slot.Find("TrayItem_Draggable");
                if (trayItem != null)
                {
                    SetSize(trayItem, portrait ? new Vector2(118f, 108f) : new Vector2(114f, 104f));
                    var itemComp = trayItem.GetComponent<DraggableTrayItem>();
                    if (itemComp != null) itemComp.SetRestPosition(trayPos);
                    else SetPos(trayItem, trayPos);
                }

                SetBox(slot.Find("SlotLabelPill"), portrait ? new Vector2(70f, 0f) : new Vector2(0f, -54f),
                    portrait ? new Vector2(158f, 92f) : new Vector2(148f, 46f));
            }
        }

        private void ApplySafeAreaOffset(ViewportProfile profile)
        {
            if (topHudBar == null || profile.height <= 0) return;

            float topInsetPx = Mathf.Max(0f, profile.height - profile.safeArea.yMax);
            if (topInsetPx <= 1f) return;

            float refHeight = profile.orientation == ViewportOrientation.Portrait ? 1920f : 1080f;
            float topInsetUnits = (topInsetPx / profile.height) * refHeight;
            Vector2 pos = topHudBar.anchoredPosition;
            pos.y -= Mathf.Min(topInsetUnits, 60f);
            topHudBar.anchoredPosition = pos;
        }

        private static void SetBox(Transform target, Vector2 pos, Vector2 size)
        {
            if (target is RectTransform rt)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
        }

        private static void SetPos(Transform target, Vector2 pos)
        {
            if (target is RectTransform rt) rt.anchoredPosition = pos;
        }

        private static void SetSize(Transform target, Vector2 size)
        {
            if (target is RectTransform rt) rt.sizeDelta = size;
        }
    }
}
