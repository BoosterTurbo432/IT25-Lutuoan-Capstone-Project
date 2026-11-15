using System.Collections.Generic;
using CookingSystem.Core.Responsive;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Switches gamescene.unity smoothly between Desktop/Mobile Landscape (1920x1080)
    /// and Mobile/Narrow Portrait (1080x1920) based on the active browser/device viewport.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSceneResponsiveLayout : MonoBehaviour
    {
        [Header("Root Panels")]
        [SerializeField] private RectTransform topHudBar;
        [SerializeField] private RectTransform dishNotificationBanner;
        [SerializeField] private RectTransform customerZone;
        [SerializeField] private RectTransform countertopArea;

        [Header("Customer Zone")]
        [SerializeField] private RectTransform customerHeader;
        [SerializeField] private RectTransform customerSlotsContainer;
        [SerializeField] private HorizontalLayoutGroup customerSlotsLayout;
        [SerializeField] private RectTransform serviceCounterLedge;

        [Header("Kitchen Zones")]
        [SerializeField] private RectTransform drinkDispenserZone;
        [SerializeField] private RectTransform ingredientTrayZone;
        [SerializeField] private RectTransform stoveAreaZone;
        [SerializeField] private RectTransform utensilsStationZone;
        [SerializeField] private RectTransform foodDisplayTrayZone;
        [SerializeField] private RectTransform utilitySafetyZone;

        private ViewportOrientation? appliedOrientation;
        private Rect appliedSafeArea;

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
            if (topHudBar != null)
            {
                SetBox(topHudBar, new Vector2(0f, 502f), new Vector2(1880f, 62f));
                SetBox(topHudBar.Find("TimerText"), new Vector2(-720f, 0f), new Vector2(280f, 48f));
                SetBox(topHudBar.Find("ScoreText"), new Vector2(-380f, 0f), new Vector2(280f, 48f));
                SetBox(topHudBar.Find("OrdersText"), new Vector2(-30f, 0f), new Vector2(290f, 48f));
                SetBox(topHudBar.Find("ComboText"), new Vector2(340f, 0f), new Vector2(340f, 48f));
                SetBox(topHudBar.Find("RestartHudButton"), new Vector2(675f, 0f), new Vector2(146f, 42f));
                SetBox(topHudBar.Find("MenuHudButton"), new Vector2(842f, 0f), new Vector2(156f, 42f));
            }

            if (dishNotificationBanner != null)
            {
                SetBox(dishNotificationBanner, new Vector2(0f, 456f), new Vector2(780f, 52f));
            }

            if (customerZone != null)
            {
                SetBox(customerZone, new Vector2(0f, 294f), new Vector2(1880f, 336f));
                SetBox(customerHeader, new Vector2(0f, 146f), new Vector2(1240f, 32f));
                SetBox(customerSlotsContainer, new Vector2(0f, -10f), new Vector2(1680f, 272f));
                if (customerSlotsLayout != null) customerSlotsLayout.spacing = 120f;
                SetBox(serviceCounterLedge, new Vector2(0f, -158f), new Vector2(1880f, 20f));
                ApplyCustomerSlotCards(portrait: false);
            }

            if (countertopArea != null)
            {
                SetBox(countertopArea, new Vector2(0f, -210f), new Vector2(1880f, 640f));
            }

            if (drinkDispenserZone != null)
            {
                SetBox(drinkDispenserZone, new Vector2(-832f, 0f), new Vector2(216f, 616f));
                SetBox(drinkDispenserZone.Find("ZoneHeader_DrinkDispenser"), new Vector2(0f, 274f), new Vector2(198f, 46f));
                Transform body = drinkDispenserZone.Find("DispenserBody");
                if (body != null)
                {
                    SetBox(body, new Vector2(0f, 44f), new Vector2(188f, 356f));
                    SetBox(body.Find("DrinkTap_BerryJuice"), new Vector2(-45f, 78f), new Vector2(76f, 148f));
                    SetBox(body.Find("DrinkTap_BerryJuice_Nozzle"), new Vector2(-45f, -8f), new Vector2(22f, 24f));
                    SetBox(body.Find("DrinkTap_CalamansiTea"), new Vector2(45f, 78f), new Vector2(76f, 148f));
                    SetBox(body.Find("DrinkTap_CalamansiTea_Nozzle"), new Vector2(45f, -8f), new Vector2(22f, 24f));
                    SetBox(body.Find("PourProgressBar"), new Vector2(0f, -44f), new Vector2(160f, 16f));
                    SetBox(body.Find("DispenserStatusPill"), new Vector2(0f, -80f), new Vector2(178f, 38f));
                    SetBox(body.Find("StagePendingDrinkButton"), new Vector2(0f, -122f), new Vector2(162f, 34f));
                    SetBox(body.Find("DripTray_Placeholder"), new Vector2(0f, -158f), new Vector2(166f, 20f));
                }
                Transform cupDock = drinkDispenserZone.Find("CupSlot_Dock");
                if (cupDock != null)
                {
                    SetBox(cupDock, new Vector2(0f, -214f), new Vector2(188f, 136f));
                    SetBox(cupDock.Find("CupSlotLabelPill"), new Vector2(0f, -44f), new Vector2(178f, 34f));
                    Transform cup = cupDock.Find("DraggableCup_Item");
                    if (cup != null)
                    {
                        SetSize(cup, new Vector2(72f, 76f));
                        var dc = cup.GetComponent<DraggableCup>();
                        if (dc != null) dc.SetRestPosition(new Vector2(0f, 16f));
                        else SetPos(cup, new Vector2(0f, 16f));
                    }
                }
            }

            if (ingredientTrayZone != null)
            {
                SetBox(ingredientTrayZone, new Vector2(-516f, 0f), new Vector2(380f, 616f));
                SetBox(ingredientTrayZone.Find("ZoneHeader_IngredientTray"), new Vector2(0f, 278f), new Vector2(360f, 32f));
                SetBox(ingredientTrayZone.Find("ZoneSubheaderPill"), new Vector2(0f, 244f), new Vector2(360f, 32f));
                Transform grid = ingredientTrayZone.Find("IngredientSlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -26f), new Vector2(348f, 492f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(165f, 236f);
                        glg.spacing = new Vector2(18f, 16f);
                    }
                    ApplyIngredientSlots(grid, portrait: false);
                }
            }

            if (stoveAreaZone != null)
            {
                SetBox(stoveAreaZone, new Vector2(0f, 98f), new Vector2(620f, 420f));
                SetBox(stoveAreaZone.Find("ZoneHeader_StoveArea"), new Vector2(0f, 186f), new Vector2(594f, 30f));
                Transform grid = stoveAreaZone.Find("BurnersGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -16f), new Vector2(588f, 364f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(284f, 174f);
                        glg.spacing = new Vector2(20f, 14f);
                    }
                }
            }

            if (utensilsStationZone != null)
            {
                SetBox(utensilsStationZone, new Vector2(0f, -214f), new Vector2(620f, 188f));
                SetBox(utensilsStationZone.Find("ZoneHeader_UtensilsStation"), new Vector2(0f, 76f), new Vector2(604f, 28f));
                Transform board = utensilsStationZone.Find("Slot_KnifeAndCuttingBoard");
                if (board != null)
                {
                    SetBox(board, new Vector2(-150f, -12f), new Vector2(292f, 144f));
                    SetBox(board.Find("CuttingBoard_Shape"), new Vector2(0f, 22f), new Vector2(268f, 66f));
                    SetBox(board.Find("IngredientsSummaryText"), new Vector2(0f, 36f), new Vector2(256f, 26f));
                    SetBox(board.Find("StageStatusPill"), new Vector2(0f, 8f), new Vector2(260f, 28f));
                    SetBox(board.Find("PrecisionBarRoot"), new Vector2(0f, -18f), new Vector2(246f, 18f));
                    SetBox(board.Find("ChopButton"), new Vector2(-78f, -50f), new Vector2(88f, 30f));
                    SetBox(board.Find("MixButton"), new Vector2(18f, -50f), new Vector2(88f, 30f));
                    SetBox(board.Find("ResetPrepButton"), new Vector2(104f, -50f), new Vector2(60f, 30f));
                }
                Transform col = utensilsStationZone.Find("UtensilsAssignmentColumn");
                if (col != null)
                {
                    SetBox(col, new Vector2(78f, -12f), new Vector2(142f, 144f));
                    SetBox(col.Find("Slot_StirringLadle"), new Vector2(0f, 37f), new Vector2(138f, 66f));
                    SetBox(col.Find("Slot_FryingSpatula"), new Vector2(0f, -37f), new Vector2(138f, 66f));
                }
                Transform dock = utensilsStationZone.Find("PreparedDishDock");
                if (dock != null)
                {
                    SetBox(dock, new Vector2(228f, -12f), new Vector2(136f, 144f));
                    SetBox(dock.Find("DockLabel"), new Vector2(0f, 48f), new Vector2(128f, 36f));
                    SetBox(dock.Find("PreparedDish_Token"), new Vector2(0f, -14f), new Vector2(106f, 82f));
                }
            }

            if (foodDisplayTrayZone != null)
            {
                SetBox(foodDisplayTrayZone, new Vector2(633f, 70f), new Vector2(614f, 476f));
                SetBox(foodDisplayTrayZone.Find("ZoneHeader_FoodDisplayTray"), new Vector2(0f, 212f), new Vector2(580f, 32f));
                SetBox(foodDisplayTrayZone.Find("ZoneSubheaderPill"), new Vector2(0f, 180f), new Vector2(584f, 30f));
                Transform grid = foodDisplayTrayZone.Find("DisplaySlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -22f), new Vector2(576f, 386f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(182f, 184f);
                        glg.spacing = new Vector2(14f, 14f);
                    }
                    ApplyDisplaySlots(grid, portrait: false);
                }
            }

            if (utilitySafetyZone != null)
            {
                SetBox(utilitySafetyZone, new Vector2(633f, -242f), new Vector2(614f, 124f));
                Transform extDock = utilitySafetyZone.Find("FireExtinguisher_Dock");
                if (extDock != null)
                {
                    SetBox(extDock, new Vector2(-150f, 0f), new Vector2(280f, 102f));
                    SetBox(extDock.Find("ExtDockLabel"), new Vector2(36f, 0f), new Vector2(184f, 74f));
                    Transform tool = extDock.Find("FireExtinguisher_Tool");
                    if (tool != null)
                    {
                        SetSize(tool, new Vector2(58f, 84f));
                        var ft = tool.GetComponent<FireExtinguisherTool>();
                        if (ft != null) ft.SetRestPosition(new Vector2(-94f, 0f));
                        else SetPos(tool, new Vector2(-94f, 0f));
                    }
                }
                Transform trash = utilitySafetyZone.Find("TrashCan_DropZone");
                if (trash != null)
                {
                    SetBox(trash, new Vector2(150f, 0f), new Vector2(280f, 102f));
                    SetBox(trash.Find("TrashLabel"), Vector2.zero, new Vector2(256f, 80f));
                }
            }
        }

        private void ApplyPortraitLayout()
        {
            if (topHudBar != null)
            {
                SetBox(topHudBar, new Vector2(0f, 886f), new Vector2(1052f, 114f));
                SetBox(topHudBar.Find("TimerText"), new Vector2(-340f, 26f), new Vector2(300f, 48f));
                SetBox(topHudBar.Find("ScoreText"), new Vector2(-10f, 26f), new Vector2(300f, 48f));
                SetBox(topHudBar.Find("OrdersText"), new Vector2(340f, 26f), new Vector2(300f, 48f));
                SetBox(topHudBar.Find("ComboText"), new Vector2(-245f, -28f), new Vector2(490f, 44f));
                SetBox(topHudBar.Find("RestartHudButton"), new Vector2(195f, -28f), new Vector2(170f, 44f));
                SetBox(topHudBar.Find("MenuHudButton"), new Vector2(390f, -28f), new Vector2(180f, 44f));
            }

            if (dishNotificationBanner != null)
            {
                SetBox(dishNotificationBanner, new Vector2(0f, 804f), new Vector2(940f, 56f));
            }

            if (customerZone != null)
            {
                SetBox(customerZone, new Vector2(0f, 642f), new Vector2(1052f, 344f));
                SetBox(customerHeader, new Vector2(0f, 148f), new Vector2(990f, 32f));
                SetBox(customerSlotsContainer, new Vector2(0f, -8f), new Vector2(1024f, 268f));
                if (customerSlotsLayout != null) customerSlotsLayout.spacing = 20f;
                SetBox(serviceCounterLedge, new Vector2(0f, -162f), new Vector2(1052f, 20f));
                ApplyCustomerSlotCards(portrait: true);
            }

            if (countertopArea != null)
            {
                SetBox(countertopArea, new Vector2(0f, -248f), new Vector2(1052f, 1384f));
            }

            if (ingredientTrayZone != null)
            {
                SetBox(ingredientTrayZone, new Vector2(-292f, 472f), new Vector2(440f, 412f));
                SetBox(ingredientTrayZone.Find("ZoneHeader_IngredientTray"), new Vector2(0f, 184f), new Vector2(416f, 30f));
                SetBox(ingredientTrayZone.Find("ZoneSubheaderPill"), new Vector2(0f, 150f), new Vector2(416f, 28f));
                Transform grid = ingredientTrayZone.Find("IngredientSlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -22f), new Vector2(416f, 300f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(200f, 142f);
                        glg.spacing = new Vector2(16f, 14f);
                    }
                    ApplyIngredientSlots(grid, portrait: true);
                }
            }

            if (stoveAreaZone != null)
            {
                SetBox(stoveAreaZone, new Vector2(228f, 472f), new Vector2(572f, 412f));
                SetBox(stoveAreaZone.Find("ZoneHeader_StoveArea"), new Vector2(0f, 184f), new Vector2(548f, 30f));
                Transform grid = stoveAreaZone.Find("BurnersGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -18f), new Vector2(548f, 344f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(266f, 164f);
                        glg.spacing = new Vector2(16f, 14f);
                    }
                }
            }

            if (drinkDispenserZone != null)
            {
                SetBox(drinkDispenserZone, new Vector2(-344f, 92f), new Vector2(336f, 316f));
                SetBox(drinkDispenserZone.Find("ZoneHeader_DrinkDispenser"), new Vector2(0f, 136f), new Vector2(316f, 32f));
                Transform body = drinkDispenserZone.Find("DispenserBody");
                if (body != null)
                {
                    SetBox(body, new Vector2(-64f, -18f), new Vector2(184f, 256f));
                    SetBox(body.Find("DrinkTap_BerryJuice"), new Vector2(-44f, 52f), new Vector2(76f, 114f));
                    SetBox(body.Find("DrinkTap_BerryJuice_Nozzle"), new Vector2(-44f, -14f), new Vector2(20f, 18f));
                    SetBox(body.Find("DrinkTap_CalamansiTea"), new Vector2(44f, 52f), new Vector2(76f, 114f));
                    SetBox(body.Find("DrinkTap_CalamansiTea_Nozzle"), new Vector2(44f, -14f), new Vector2(20f, 18f));
                    SetBox(body.Find("PourProgressBar"), new Vector2(0f, -38f), new Vector2(160f, 14f));
                    SetBox(body.Find("DispenserStatusPill"), new Vector2(0f, -72f), new Vector2(172f, 38f));
                    SetBox(body.Find("StagePendingDrinkButton"), new Vector2(0f, -106f), new Vector2(160f, 30f));
                    SetBox(body.Find("DripTray_Placeholder"), new Vector2(0f, -118f), new Vector2(160f, 12f));
                }
                Transform cupDock = drinkDispenserZone.Find("CupSlot_Dock");
                if (cupDock != null)
                {
                    SetBox(cupDock, new Vector2(96f, -18f), new Vector2(118f, 256f));
                    SetBox(cupDock.Find("CupSlotLabelPill"), new Vector2(0f, -68f), new Vector2(110f, 64f));
                    Transform cup = cupDock.Find("DraggableCup_Item");
                    if (cup != null)
                    {
                        SetSize(cup, new Vector2(72f, 78f));
                        var dc = cup.GetComponent<DraggableCup>();
                        if (dc != null) dc.SetRestPosition(new Vector2(0f, 32f));
                        else SetPos(cup, new Vector2(0f, 32f));
                    }
                }
            }

            if (utensilsStationZone != null)
            {
                SetBox(utensilsStationZone, new Vector2(176f, 92f), new Vector2(676f, 316f));
                SetBox(utensilsStationZone.Find("ZoneHeader_UtensilsStation"), new Vector2(0f, 134f), new Vector2(652f, 32f));
                Transform board = utensilsStationZone.Find("Slot_KnifeAndCuttingBoard");
                if (board != null)
                {
                    SetBox(board, new Vector2(-166f, -18f), new Vector2(320f, 240f));
                    SetBox(board.Find("CuttingBoard_Shape"), new Vector2(0f, 44f), new Vector2(292f, 108f));
                    SetBox(board.Find("IngredientsSummaryText"), new Vector2(0f, 68f), new Vector2(280f, 36f));
                    SetBox(board.Find("StageStatusPill"), new Vector2(0f, 20f), new Vector2(280f, 38f));
                    SetBox(board.Find("PrecisionBarRoot"), new Vector2(0f, -28f), new Vector2(276f, 24f));
                    SetBox(board.Find("ChopButton"), new Vector2(-90f, -78f), new Vector2(98f, 46f));
                    SetBox(board.Find("MixButton"), new Vector2(20f, -78f), new Vector2(98f, 46f));
                    SetBox(board.Find("ResetPrepButton"), new Vector2(116f, -78f), new Vector2(68f, 46f));
                }
                Transform col = utensilsStationZone.Find("UtensilsAssignmentColumn");
                if (col != null)
                {
                    SetBox(col, new Vector2(86f, -18f), new Vector2(156f, 240f));
                    SetBox(col.Find("Slot_StirringLadle"), new Vector2(0f, 56f), new Vector2(148f, 104f));
                    SetBox(col.Find("Slot_FryingSpatula"), new Vector2(0f, -56f), new Vector2(148f, 104f));
                }
                Transform dock = utensilsStationZone.Find("PreparedDishDock");
                if (dock != null)
                {
                    SetBox(dock, new Vector2(248f, -18f), new Vector2(144f, 240f));
                    SetBox(dock.Find("DockLabel"), new Vector2(0f, 76f), new Vector2(134f, 50f));
                    SetBox(dock.Find("PreparedDish_Token"), new Vector2(0f, -20f), new Vector2(114f, 114f));
                }
            }

            if (foodDisplayTrayZone != null)
            {
                SetBox(foodDisplayTrayZone, new Vector2(0f, -264f), new Vector2(1024f, 360f));
                SetBox(foodDisplayTrayZone.Find("ZoneHeader_FoodDisplayTray"), new Vector2(0f, 156f), new Vector2(980f, 30f));
                SetBox(foodDisplayTrayZone.Find("ZoneSubheaderPill"), new Vector2(0f, 126f), new Vector2(980f, 28f));
                Transform grid = foodDisplayTrayZone.Find("DisplaySlotsGrid");
                if (grid != null)
                {
                    SetBox(grid, new Vector2(0f, -24f), new Vector2(992f, 264f));
                    var glg = grid.GetComponent<GridLayoutGroup>();
                    if (glg != null)
                    {
                        glg.cellSize = new Vector2(320f, 124f);
                        glg.spacing = new Vector2(16f, 14f);
                    }
                    ApplyDisplaySlots(grid, portrait: true);
                }
            }

            if (utilitySafetyZone != null)
            {
                SetBox(utilitySafetyZone, new Vector2(0f, -540f), new Vector2(1024f, 148f));
                Transform extDock = utilitySafetyZone.Find("FireExtinguisher_Dock");
                if (extDock != null)
                {
                    SetBox(extDock, new Vector2(-256f, 0f), new Vector2(484f, 122f));
                    SetBox(extDock.Find("ExtDockLabel"), new Vector2(48f, 0f), new Vector2(340f, 90f));
                    Transform tool = extDock.Find("FireExtinguisher_Tool");
                    if (tool != null)
                    {
                        SetSize(tool, new Vector2(72f, 92f));
                        var ft = tool.GetComponent<FireExtinguisherTool>();
                        if (ft != null) ft.SetRestPosition(new Vector2(-176f, 0f));
                        else SetPos(tool, new Vector2(-176f, 0f));
                    }
                }
                Transform trash = utilitySafetyZone.Find("TrashCan_DropZone");
                if (trash != null)
                {
                    SetBox(trash, new Vector2(256f, 0f), new Vector2(484f, 122f));
                    SetBox(trash.Find("TrashLabel"), Vector2.zero, new Vector2(440f, 96f));
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

                SetSize(slot, portrait ? new Vector2(328f, 256f) : new Vector2(440f, 264f));

                if (portrait)
                {
                    SetBox(slot.Find("CustomerVisualRoot"), new Vector2(-98f, 28f), new Vector2(96f, 134f));
                    Transform bubble = slot.Find("OrderBubble");
                    if (bubble != null)
                    {
                        SetBox(bubble, new Vector2(56f, 28f), new Vector2(194f, 146f));
                        SetBox(bubble.Find("OrderTitleText"), new Vector2(0f, 56f), new Vector2(182f, 24f));
                        SetBox(bubble.Find("MealOrderText"), new Vector2(0f, 10f), new Vector2(182f, 60f));
                        SetBox(bubble.Find("DrinkOrderText"), new Vector2(0f, -46f), new Vector2(182f, 30f));
                    }
                    SetBox(slot.Find("SlotLabelText"), new Vector2(-98f, -64f), new Vector2(118f, 32f));
                    SetBox(slot.Find("FeedbackText"), new Vector2(56f, -54f), new Vector2(194f, 28f));
                    SetBox(slot.Find("PatienceTimerText"), new Vector2(56f, -80f), new Vector2(194f, 26f));
                    SetBox(slot.Find("PatienceBar"), new Vector2(0f, -108f), new Vector2(304f, 16f));
                }
                else
                {
                    SetBox(slot.Find("CustomerVisualRoot"), new Vector2(-124f, 16f), new Vector2(142f, 168f));
                    Transform bubble = slot.Find("OrderBubble");
                    if (bubble != null)
                    {
                        SetBox(bubble, new Vector2(82f, 32f), new Vector2(236f, 144f));
                        SetBox(bubble.Find("OrderTitleText"), new Vector2(0f, 54f), new Vector2(216f, 24f));
                        SetBox(bubble.Find("MealOrderText"), new Vector2(0f, 10f), new Vector2(216f, 58f));
                        SetBox(bubble.Find("DrinkOrderText"), new Vector2(0f, -44f), new Vector2(216f, 30f));
                    }
                    SetBox(slot.Find("SlotLabelText"), new Vector2(-124f, -82f), new Vector2(166f, 26f));
                    SetBox(slot.Find("FeedbackText"), new Vector2(82f, -54f), new Vector2(236f, 28f));
                    SetBox(slot.Find("PatienceTimerText"), new Vector2(82f, -78f), new Vector2(236f, 24f));
                    SetBox(slot.Find("PatienceBar"), new Vector2(0f, -108f), new Vector2(404f, 16f));
                }
            }
        }

        private static void ApplyIngredientSlots(Transform grid, bool portrait)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform slot = grid.GetChild(i);
                if (slot == null) continue;

                Vector2 socketPos = portrait ? new Vector2(-50f, 0f) : new Vector2(0f, 8f);
                SetBox(slot.Find("SocketRing_Placeholder"), socketPos, portrait ? new Vector2(82f, 82f) : new Vector2(96f, 96f));
                SetBox(slot.Find("CooldownOverlay"), socketPos, portrait ? new Vector2(82f, 82f) : new Vector2(96f, 96f));
                SetBox(slot.Find("CooldownText"), socketPos, new Vector2(82f, 36f));

                SetBox(slot.Find("TitleText"), portrait ? new Vector2(42f, 26f) : new Vector2(0f, 90f),
                    portrait ? new Vector2(106f, 38f) : new Vector2(154f, 34f));
                SetBox(slot.Find("DragHintPill"), portrait ? new Vector2(42f, -24f) : new Vector2(0f, -86f),
                    portrait ? new Vector2(106f, 44f) : new Vector2(156f, 38f));

                var pantry = slot.GetComponent<PantrySlot>();
                if (pantry != null)
                {
                    pantry.SetSpawnAnchoredPosition(socketPos);
                }
            }
        }

        private static void ApplyDisplaySlots(Transform grid, bool portrait)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform slot = grid.GetChild(i);
                if (slot == null) continue;

                Vector2 platePos = portrait ? new Vector2(-96f, 0f) : new Vector2(0f, 22f);
                Vector2 plateSize = portrait ? new Vector2(88f, 88f) : new Vector2(104f, 104f);

                SetBox(slot.Find("Plate_Placeholder"), platePos, plateSize);
                Transform trayItem = slot.Find("TrayItem_Draggable");
                if (trayItem != null)
                {
                    SetSize(trayItem, portrait ? new Vector2(82f, 82f) : new Vector2(96f, 96f));
                    var itemComp = trayItem.GetComponent<DraggableTrayItem>();
                    if (itemComp != null) itemComp.SetRestPosition(platePos);
                    else SetPos(trayItem, platePos);
                }

                SetBox(slot.Find("SlotLabelPill"), portrait ? new Vector2(54f, 0f) : new Vector2(0f, -58f),
                    portrait ? new Vector2(190f, 92f) : new Vector2(172f, 52f));
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
