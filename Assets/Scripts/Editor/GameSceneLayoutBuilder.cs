using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.EditorTools
{
    /// <summary>
    /// Builds and wires the responsive (Desktop + Mobile Landscape/Portrait) playable capstone
    /// showcase layout in Assets/Scenes/gamescene.unity with high-contrast, crisp LiberationSans labels.
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneLayoutBuilder
    {
        public const string GameScenePath = "Assets/Scenes/gamescene.unity";
        private const string LayoutRootName = "CookingStationLayout";

        private const string TomatoPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Tomato.prefab";
        private const string CheesePrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Cheese.prefab";
        private const string MeatPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Meat.prefab";
        private const string MushroomPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Mushroom.prefab";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/Fonts/Stay With Me SDF.asset";

        // High-contrast kitchen zone palette
        private static readonly Color BackdropColor = Hex("1E1916");
        private static readonly Color HudBgColor = Hex("110F0E");
        private static readonly Color PillBgColor = Hex("141211");
        private static readonly Color PillDarkAlpha = new Color(0.07f, 0.06f, 0.06f, 0.90f);

        private static readonly Color CustomerZoneBg = Hex("2B3744");
        private static readonly Color CustomerSlotBg = Hex("394959");
        private static readonly Color CustomerSilhouette = Hex("8FA6BC");
        private static readonly Color OrderBubbleBg = Hex("FFFBEB");
        private static readonly Color CounterLedgeColor = Hex("6F4E37");
        private static readonly Color CounterSurfaceColor = Hex("3A2C24");

        private static readonly Color DispenserZoneBg = Hex("243B47");
        private static readonly Color DispenserBodyColor = Hex("466170");
        private static readonly Color DispenserTrimColor = Hex("2C3F4A");

        private static readonly Color IngredientZoneBg = Hex("2A3F27");
        private static readonly Color IngredientSlotBg = Hex("385034");

        private static readonly Color StoveZoneBg = Hex("332E2C");
        private static readonly Color BurnerCardBg = Hex("221E1C");
        private static readonly Color BurnerRingColor = Hex("141211");
        private static readonly Color BurnerEmberColor = Hex("D95D39");
        private static readonly Color PotColor = Hex("64748B");
        private static readonly Color PanColor = Hex("475569");
        private static readonly Color ServeButtonColor = Hex("16A34A");

        private static readonly Color UtensilsZoneBg = Hex("4F3B2C");
        private static readonly Color UtensilSlotBg = Hex("3B2B20");
        private static readonly Color CuttingBoardColor = Hex("D4A373");
        private static readonly Color MetalColor = Hex("CBD5E1");
        private static readonly Color HandleDarkColor = Hex("1E293B");

        private static readonly Color DisplayZoneBg = Hex("4F3E25");
        private static readonly Color DisplaySlotBg = Hex("3B2D1A");
        private static readonly Color PlateOuterColor = Hex("E2E8F0");
        private static readonly Color PlateInnerColor = Hex("CBD5E1");

        private static readonly Color UtilityZoneBg = Hex("3B2724");
        private static readonly Color ExtinguisherRed = Hex("DC2626");
        private static readonly Color TrashBinColor = Hex("334155");

        private static readonly Color TextLight = Hex("FFFFFF");
        private static readonly Color TextHintYellow = Hex("FDE047");
        private static readonly Color TextMuted = Hex("E2E8F0");
        private static readonly Color TextDark = Hex("111827");

        private static Sprite roundedSprite;
        private static Sprite panelSprite;
        private static Sprite knobSprite;
        private static TMP_FontAsset uiFont;

        static GameSceneLayoutBuilder()
        {
            EditorApplication.delayCall += TryAutoPopulateStarterGameScene;
        }

        private static void TryAutoPopulateStarterGameScene()
        {
            if (PlayerSettings.WebGL.template == "APPLICATION:Default")
            {
                PlayerSettings.WebGL.template = "PROJECT:Responsive";
            }

            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !active.isLoaded || active.path != GameScenePath || active.isDirty) return;

            GameObject[] roots = active.GetRootGameObjects();
            bool hasLayout = roots.Any(r => r.name == LayoutRootName || r.transform.Find(LayoutRootName) != null);
            bool hasStarterMarker = roots.Any(r => r.transform.Find("Assets Waiting") != null);
            bool needsDisplayTrayUpgrade = hasLayout && roots.All(r => r.GetComponentInChildren<FoodDisplayTray>(true) == null);
            bool needsGameplayUpgrade = hasLayout && roots.All(r => r.GetComponentInChildren<LevelGameManager>(true) == null);
            bool needsResponsiveUpgrade = hasLayout && roots.All(r => r.GetComponentInChildren<GameSceneResponsiveLayout>(true) == null);

            if ((hasLayout && !needsDisplayTrayUpgrade && !needsGameplayUpgrade && !needsResponsiveUpgrade) ||
                (!hasLayout && !hasStarterMarker))
            {
                return;
            }

            if (Build(active))
            {
                EditorSceneManager.SaveScene(active, GameScenePath);
                Debug.Log("[GameSceneLayoutBuilder] Auto-upgraded responsive & readable showcase layout in gamescene.unity.");
            }
        }

        [MenuItem("Tools/Game Scene/Build Cooking Station Placeholder Layout")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = OpenGameScene();
            if (!Build(scene)) return;

            EditorSceneManager.SaveScene(scene, GameScenePath);
            Debug.Log("[GameSceneLayoutBuilder] Playable Cooking Station layout built and saved.");
        }

        [MenuItem("Tools/Game Scene/Open Game Scene")]
        public static void OpenGameSceneFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            OpenGameScene();
        }

        public static void BuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = OpenGameScene();
            bool success = Build(scene) && EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(success ? "[GameSceneLayoutBuilder] BUILD OK" : "[GameSceneLayoutBuilder] BUILD FAILED");
            if (!success) EditorApplication.Exit(1);
        }

        private static Scene OpenGameScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == GameScenePath) return active;

            if (File.Exists(GameScenePath))
            {
                return EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, GameScenePath);
            return newScene;
        }

        public static bool Build(Scene scene)
        {
            LoadResources();

            EnsureCamera(scene);
            EnsureEventSystem(scene);
            Canvas canvas = EnsureCanvas(scene);
            RectTransform canvasRect = (RectTransform)canvas.transform;

            RemoveChildIfExists(canvasRect, "Panel");
            RemoveChildIfExists(canvasRect, "Assets Waiting");
            RemoveChildIfExists(canvasRect, LayoutRootName);

            RectTransform layoutRoot = CreateRect(LayoutRootName, canvasRect);
            Stretch(layoutRoot);

            Image backdrop = AddImage(layoutRoot, BackdropColor, null);
            backdrop.raycastTarget = false;

            // Showcase Systems + Responsive Adapter
            GameObject systemsGo = new GameObject("ShowcaseSystems");
            systemsGo.layer = LayerMask.NameToLayer("UI");
            systemsGo.transform.SetParent(layoutRoot, false);
            LevelGameManager levelManager = systemsGo.AddComponent<LevelGameManager>();
            CustomerManager customerManager = systemsGo.AddComponent<CustomerManager>();
            GameSceneResponsiveLayout responsiveLayout = systemsGo.AddComponent<GameSceneResponsiveLayout>();

            // Top HUD Bar
            RectTransform topHud = BuildTopHudBar(layoutRoot, levelManager);

            // 6. Customer Zone
            var customerRefs = BuildCustomerZone(layoutRoot, out List<CustomerSlotUI> customerSlots);
            WireCustomerManager(customerManager, customerSlots);

            // Countertop Area
            RectTransform countertop = CreateRect("Countertop_Area", layoutRoot);
            countertop.anchoredPosition = new Vector2(0f, -210f);
            countertop.sizeDelta = new Vector2(1880f, 640f);
            Image counterImg = AddImage(countertop, CounterSurfaceColor, roundedSprite, 0.4f);
            counterImg.raycastTarget = false;

            // 2. Food Display Tray
            FoodDisplayTray displayTray = BuildFoodDisplayTrayZone(countertop, out RectTransform displayZone);

            // Safety & Waste Utility Zone
            RectTransform utilityZone = BuildUtilitySafetyAndWasteZone(countertop);

            // 5. Drink Dispenser
            RectTransform drinkZone = BuildDrinkDispenserZone(countertop, displayTray);

            // 4. Ingredient Tray
            RectTransform ingredientZone = BuildIngredientTrayZone(countertop);

            // 1. Stove Area
            List<CookingStation> cookingStations = BuildStoveAreaZone(countertop, displayTray, out RectTransform stoveZone);
            WireTrayStations(displayTray, cookingStations);

            // 3. Utensils Station
            RectTransform utensilsZone = BuildUtensilsStationZone(countertop);

            // Dish Notification Banner
            RectTransform banner = BuildDishNotificationBanner(layoutRoot);

            // End-of-Level Summary Modal
            BuildSummaryModal(layoutRoot, levelManager);

            // Wire Responsive Layout Handler
            var respSo = new SerializedObject(responsiveLayout);
            SetRef(respSo, "topHudBar", topHud);
            SetRef(respSo, "dishNotificationBanner", banner);
            SetRef(respSo, "customerZone", customerRefs.zone);
            SetRef(respSo, "countertopArea", countertop);
            SetRef(respSo, "customerHeader", customerRefs.header);
            SetRef(respSo, "customerSlotsContainer", customerRefs.slotsContainer);
            SetRef(respSo, "customerSlotsLayout", customerRefs.hlg);
            SetRef(respSo, "serviceCounterLedge", customerRefs.ledge);
            SetRef(respSo, "drinkDispenserZone", drinkZone);
            SetRef(respSo, "ingredientTrayZone", ingredientZone);
            SetRef(respSo, "stoveAreaZone", stoveZone);
            SetRef(respSo, "utensilsStationZone", utensilsZone);
            SetRef(respSo, "foodDisplayTrayZone", displayZone);
            SetRef(respSo, "utilitySafetyZone", utilityZone);
            respSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static RectTransform BuildTopHudBar(RectTransform parent, LevelGameManager manager)
        {
            RectTransform hud = CreateRect("TopHUD_Bar", parent);
            hud.anchoredPosition = new Vector2(0f, 502f);
            hud.sizeDelta = new Vector2(1880f, 62f);
            AddImage(hud, HudBgColor, roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text timerTmp = CreateLabel("TimerText", hud, "TIME 03:00",
                new Vector2(-720f, 0f), new Vector2(280f, 48f), 28f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Left, 18f);
            TMP_Text scoreTmp = CreateLabel("ScoreText", hud, "SCORE: 0",
                new Vector2(-380f, 0f), new Vector2(280f, 48f), 28f, FontStyles.Bold, TextLight, TextAlignmentOptions.Left, 18f);
            TMP_Text ordersTmp = CreateLabel("OrdersText", hud, "SERVED: 0/8",
                new Vector2(-30f, 0f), new Vector2(290f, 48f), 26f, FontStyles.Bold, Hex("4ADE80"), TextAlignmentOptions.Center, 16f);
            TMP_Text comboTmp = CreateLabel("ComboText", hud, "STREAK: x0  |  COMBOS: 0",
                new Vector2(340f, 0f), new Vector2(340f, 48f), 22f, FontStyles.Bold, Hex("38BDF8"), TextAlignmentOptions.Center, 15f);

            Button restartBtn = CreateActionButton("RestartHudButton", hud, "RESTART",
                new Vector2(675f, 0f), new Vector2(146f, 42f), Hex("475569"), 18f);
            Button menuBtn = CreateActionButton("MenuHudButton", hud, "MAIN MENU",
                new Vector2(842f, 0f), new Vector2(156f, 42f), Hex("334155"), 18f);

            var so = new SerializedObject(manager);
            SetRef(so, "timerText", timerTmp);
            SetRef(so, "scoreText", scoreTmp);
            SetRef(so, "ordersText", ordersTmp);
            SetRef(so, "comboText", comboTmp);
            SetRef(so, "restartHudButton", restartBtn);
            SetRef(so, "mainMenuHudButton", menuBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            return hud;
        }

        private static (RectTransform zone, RectTransform header, RectTransform slotsContainer, HorizontalLayoutGroup hlg, RectTransform ledge)
            BuildCustomerZone(RectTransform parent, out List<CustomerSlotUI> slotComponents)
        {
            RectTransform zone = CreateRect("Zone6_CustomerZone", parent);
            zone.anchoredPosition = new Vector2(0f, 294f);
            zone.sizeDelta = new Vector2(1880f, 336f);
            AddImage(zone, CustomerZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text headerTmp = CreateLabel("ZoneHeader_CustomerZone", zone, "CUSTOMERS  —  DRAG SERVING TRAY ITEMS ONTO MATCHING CUSTOMER",
                new Vector2(0f, 146f), new Vector2(1240f, 32f), 20f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 14f);

            RectTransform slotsContainer = CreateRect("CustomerSlots", zone);
            slotsContainer.anchoredPosition = new Vector2(0f, -10f);
            slotsContainer.sizeDelta = new Vector2(1680f, 272f);

            HorizontalLayoutGroup hlg = slotsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 120f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            slotComponents = new List<CustomerSlotUI>();
            for (int i = 1; i <= 3; i++)
            {
                RectTransform slot = CreateRect($"CustomerSlot_{i}", slotsContainer);
                slot.sizeDelta = new Vector2(440f, 264f);
                Image cardBg = AddImage(slot, CustomerSlotBg, roundedSprite, 0.4f);
                cardBg.raycastTarget = true;

                RectTransform highlight = CreateRect("DropHighlightBorder", slot);
                Stretch(highlight);
                Image highlightImg = AddImage(highlight, new Color(0.22f, 0.80f, 0.46f, 0.35f), roundedSprite, 0.4f);
                highlightImg.raycastTarget = false;
                highlightImg.enabled = false;

                RectTransform spriteHolder = CreateRect("CustomerVisualRoot", slot);
                spriteHolder.anchoredPosition = new Vector2(-124f, 16f);
                spriteHolder.sizeDelta = new Vector2(142f, 168f);
                Image spriteTarget = AddImage(spriteHolder, new Color(1f, 1f, 1f, 0.08f), panelSprite, 0.5f);
                spriteTarget.preserveAspect = true;
                spriteTarget.raycastTarget = false;

                RectTransform head = CreateRect("Placeholder_Head", spriteHolder);
                head.anchoredPosition = new Vector2(0f, 38f);
                head.sizeDelta = new Vector2(58f, 58f);
                Image headImg = AddImage(head, CustomerSilhouette, knobSprite);
                headImg.raycastTarget = false;

                RectTransform torso = CreateRect("Placeholder_Body", spriteHolder);
                torso.anchoredPosition = new Vector2(0f, -34f);
                torso.sizeDelta = new Vector2(92f, 82f);
                Image torsoImg = AddImage(torso, CustomerSilhouette, roundedSprite, 0.4f);
                torsoImg.raycastTarget = false;

                RectTransform bubble = CreateRect("OrderBubble", slot);
                bubble.anchoredPosition = new Vector2(82f, 32f);
                bubble.sizeDelta = new Vector2(236f, 144f);
                Image bubbleImg = AddImage(bubble, OrderBubbleBg, roundedSprite, 0.5f);
                bubbleImg.raycastTarget = false;

                TMP_Text orderTitleTmp = CreateLabel("OrderTitleText", bubble, "ORDER",
                    new Vector2(0f, 54f), new Vector2(216f, 24f), 15f, FontStyles.Bold, Hex("92400E"), TextAlignmentOptions.Center, 12f);
                TMP_Text mealOrderTmp = CreateLabel("MealOrderText", bubble, "Classic Tomato Soup",
                    new Vector2(0f, 10f), new Vector2(216f, 58f), 17f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 13f);
                TMP_Text drinkOrderTmp = CreateLabel("DrinkOrderText", bubble, "+ Berry Juice",
                    new Vector2(0f, -44f), new Vector2(216f, 30f), 16f, FontStyles.Bold, Hex("0369A1"), TextAlignmentOptions.Center, 13f);

                TMP_Text slotLabelTmp = CreateLabel("SlotLabelText", slot, $"Customer {i} [Open]",
                    new Vector2(-124f, -82f), new Vector2(166f, 26f), 15f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 12f);
                TMP_Text feedbackTmp = CreateLabel("FeedbackText", slot, string.Empty,
                    new Vector2(82f, -54f), new Vector2(236f, 28f), 15f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 12f);
                TMP_Text patienceTmp = CreateLabel("PatienceTimerText", slot, "Patience: 55s",
                    new Vector2(82f, -78f), new Vector2(236f, 24f), 15f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 12f);

                RectTransform pTrack = CreateRect("PatienceBar", slot);
                pTrack.anchoredPosition = new Vector2(0f, -108f);
                pTrack.sizeDelta = new Vector2(404f, 16f);
                AddImage(pTrack, Hex("141211"), roundedSprite, 0.5f).raycastTarget = false;

                RectTransform pFillRect = CreateRect("Fill", pTrack);
                Stretch(pFillRect);
                Image pFillImg = AddImage(pFillRect, Hex("22C55E"), roundedSprite, 0.5f);
                pFillImg.type = Image.Type.Filled;
                pFillImg.fillMethod = Image.FillMethod.Horizontal;
                pFillImg.fillOrigin = 0;
                pFillImg.fillAmount = 1f;
                pFillImg.raycastTarget = false;

                CustomerSlotUI slotUi = slot.gameObject.AddComponent<CustomerSlotUI>();
                var slotSo = new SerializedObject(slotUi);
                slotSo.FindProperty("slotIndex").intValue = i;
                SetRef(slotSo, "cardBackground", cardBg);
                SetRef(slotSo, "dropHighlightBorder", highlightImg);
                SetRef(slotSo, "customerVisualRoot", spriteHolder.gameObject);
                SerializedProperty bodyProp = slotSo.FindProperty("customerBodyImages");
                bodyProp.arraySize = 2;
                bodyProp.GetArrayElementAtIndex(0).objectReferenceValue = headImg;
                bodyProp.GetArrayElementAtIndex(1).objectReferenceValue = torsoImg;
                SetRef(slotSo, "orderBubbleRoot", bubbleImg);
                SetRef(slotSo, "slotLabelText", slotLabelTmp);
                SetRef(slotSo, "orderTitleText", orderTitleTmp);
                SetRef(slotSo, "mealOrderText", mealOrderTmp);
                SetRef(slotSo, "drinkOrderText", drinkOrderTmp);
                SetRef(slotSo, "feedbackText", feedbackTmp);
                SetRef(slotSo, "patienceBarFill", pFillImg);
                SetRef(slotSo, "patienceTimerText", patienceTmp);
                slotSo.ApplyModifiedPropertiesWithoutUndo();

                slotComponents.Add(slotUi);
            }

            RectTransform ledge = CreateRect("ServiceCounter_Ledge", zone);
            ledge.anchoredPosition = new Vector2(0f, -158f);
            ledge.sizeDelta = new Vector2(1880f, 20f);
            AddImage(ledge, CounterLedgeColor, roundedSprite, 0.5f).raycastTarget = false;

            return (zone, headerTmp.rectTransform, slotsContainer, hlg, ledge);
        }

        private static void WireCustomerManager(CustomerManager manager, List<CustomerSlotUI> slots)
        {
            var so = new SerializedObject(manager);
            SerializedProperty prop = so.FindProperty("customerSlots");
            prop.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildDrinkDispenserZone(RectTransform parent, FoodDisplayTray displayTray)
        {
            RectTransform zone = CreateRect("Zone5_DrinkDispenser", parent);
            zone.anchoredPosition = new Vector2(-832f, 0f);
            zone.sizeDelta = new Vector2(216f, 616f);
            AddImage(zone, DispenserZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            DrinkDispenserStation station = zone.gameObject.AddComponent<DrinkDispenserStation>();

            CreateLabel("ZoneHeader_DrinkDispenser", zone, "DRINKS",
                new Vector2(0f, 274f), new Vector2(198f, 46f), 22f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 16f);

            RectTransform body = CreateRect("DispenserBody", zone);
            body.anchoredPosition = new Vector2(0f, 44f);
            body.sizeDelta = new Vector2(188f, 356f);
            AddImage(body, DispenserBodyColor, roundedSprite, 0.4f).raycastTarget = false;

            CreateDrinkTap(body, "DrinkTap_BerryJuice", new Vector2(-45f, 78f), RecipeDatabase.GetDrinkColor(DrinkType.BerryJuice),
                "BERRY\nJUICE\n(Drop Cup)", DrinkType.BerryJuice, station);
            CreateDrinkTap(body, "DrinkTap_CalamansiTea", new Vector2(45f, 78f), RecipeDatabase.GetDrinkColor(DrinkType.CalamansiTea),
                "CALAMANSI\nTEA\n(Drop Cup)", DrinkType.CalamansiTea, station);

            RectTransform pTrack = CreateRect("PourProgressBar", body);
            pTrack.anchoredPosition = new Vector2(0f, -44f);
            pTrack.sizeDelta = new Vector2(160f, 16f);
            AddImage(pTrack, Hex("141211"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform pFillRect = CreateRect("Fill", pTrack);
            Stretch(pFillRect);
            Image pFillImg = AddImage(pFillRect, Hex("38BDF8"), roundedSprite, 0.5f);
            pFillImg.type = Image.Type.Filled;
            pFillImg.fillMethod = Image.FillMethod.Horizontal;
            pFillImg.fillOrigin = 0;
            pFillImg.fillAmount = 0f;
            pFillImg.raycastTarget = false;

            TMP_Text statusTmp = CreatePillLabel("DispenserStatusPill", "DispenserStatusText", body, "DRAG CUP TO TAP",
                new Vector2(0f, -80f), new Vector2(178f, 38f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            Button stagePendingBtn = CreateActionButton("StagePendingDrinkButton", body, "PLACE ON TRAY",
                new Vector2(0f, -122f), new Vector2(162f, 34f), ServeButtonColor, 15f);
            stagePendingBtn.gameObject.SetActive(false);

            RectTransform dripTray = CreateRect("DripTray_Placeholder", body);
            dripTray.anchoredPosition = new Vector2(0f, -158f);
            dripTray.sizeDelta = new Vector2(166f, 20f);
            AddImage(dripTray, DispenserTrimColor, roundedSprite, 0.4f).raycastTarget = false;

            RectTransform cupSlot = CreateRect("CupSlot_Dock", zone);
            cupSlot.anchoredPosition = new Vector2(0f, -214f);
            cupSlot.sizeDelta = new Vector2(188f, 136f);
            AddImage(cupSlot, DispenserTrimColor, roundedSprite, 0.4f).raycastTarget = false;

            CreatePillLabel("CupSlotLabelPill", "CupSlotLabel", cupSlot, "DRAG CUP UP -> TAP",
                new Vector2(0f, -44f), new Vector2(178f, 34f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            RectTransform cupRect = CreateRect("DraggableCup_Item", cupSlot);
            cupRect.anchoredPosition = new Vector2(0f, 16f);
            cupRect.sizeDelta = new Vector2(72f, 76f);
            Image cupImg = AddImage(cupRect, PlateOuterColor, roundedSprite, 0.4f);
            cupImg.raycastTarget = true;
            cupRect.gameObject.AddComponent<CanvasGroup>();
            DraggableCup draggableCup = cupRect.gameObject.AddComponent<DraggableCup>();
            CreateLabel("CupText", cupRect, "CUP", Vector2.zero, new Vector2(64f, 32f),
                17f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 14f);

            var stationSo = new SerializedObject(station);
            SetRef(stationSo, "cupItem", draggableCup);
            SetRef(stationSo, "pourProgressFill", pFillImg);
            SetRef(stationSo, "statusText", statusTmp);
            SetRef(stationSo, "stagePendingDrinkButton", stagePendingBtn);
            SetRef(stationSo, "displayTray", displayTray);
            stationSo.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        private static void CreateDrinkTap(
            RectTransform parent,
            string name,
            Vector2 pos,
            Color liquidColor,
            string label,
            DrinkType drinkType,
            DrinkDispenserStation station)
        {
            RectTransform tank = CreateRect(name, parent);
            tank.anchoredPosition = pos;
            tank.sizeDelta = new Vector2(76f, 148f);
            Image tankImg = AddImage(tank, liquidColor, roundedSprite, 0.4f);
            tankImg.raycastTarget = true;

            CreateLabel("Label", tank, label, Vector2.zero, new Vector2(72f, 110f),
                15f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 11f);

            RectTransform nozzle = CreateRect($"{name}_Nozzle", parent);
            nozzle.anchoredPosition = new Vector2(pos.x, pos.y - 86f);
            nozzle.sizeDelta = new Vector2(22f, 24f);
            AddImage(nozzle, MetalColor, roundedSprite, 0.5f).raycastTarget = false;

            DrinkTapDropZone dropZone = tank.gameObject.AddComponent<DrinkTapDropZone>();
            var so = new SerializedObject(dropZone);
            so.FindProperty("drinkType").enumValueIndex = (int)drinkType;
            SetRef(so, "dispenserStation", station);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildIngredientTrayZone(RectTransform parent)
        {
            RectTransform zone = CreateRect("Zone4_IngredientTray", parent);
            zone.anchoredPosition = new Vector2(-516f, 0f);
            zone.sizeDelta = new Vector2(380f, 616f);
            AddImage(zone, IngredientZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("ZoneHeader_IngredientTray", zone, "INGREDIENT TRAY",
                new Vector2(0f, 278f), new Vector2(360f, 32f), 22f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 16f);

            CreatePillLabel("ZoneSubheaderPill", "ZoneSubheader_IngredientTray", zone, "STEP 1: DRAG TO CUTTING BOARD ->",
                new Vector2(0f, 244f), new Vector2(360f, 32f), PillDarkAlpha, 16f, TextHintYellow, 12f);

            RectTransform grid = CreateRect("IngredientSlotsGrid", zone);
            grid.anchoredPosition = new Vector2(0f, -26f);
            grid.sizeDelta = new Vector2(348f, 492f);

            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(165f, 236f);
            glg.spacing = new Vector2(18f, 16f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            CreateIngredientPantrySlot(grid, "Slot_Tomato", "TOMATO", TomatoPrefabPath, Hex("E85D4A"));
            CreateIngredientPantrySlot(grid, "Slot_Cheese", "CHEESE", CheesePrefabPath, Hex("F4C95D"));
            CreateIngredientPantrySlot(grid, "Slot_Meat", "MEAT", MeatPrefabPath, Hex("B85042"));
            CreateIngredientPantrySlot(grid, "Slot_Mushroom", "MUSHROOM", MushroomPrefabPath, Hex("C8AD8D"));

            return zone;
        }

        private static void CreateIngredientPantrySlot(RectTransform parent, string slotName, string displayName, string prefabPath, Color accentColor)
        {
            RectTransform slot = CreateRect(slotName, parent);
            slot.sizeDelta = new Vector2(165f, 236f);
            AddImage(slot, IngredientSlotBg, roundedSprite, 0.4f).raycastTarget = false;

            RectTransform socket = CreateRect("SocketRing_Placeholder", slot);
            socket.anchoredPosition = new Vector2(0f, 8f);
            socket.sizeDelta = new Vector2(96f, 96f);
            AddImage(socket, new Color(accentColor.r, accentColor.g, accentColor.b, 0.30f), knobSprite).raycastTarget = false;

            CreateLabel("TitleText", slot, displayName,
                new Vector2(0f, 90f), new Vector2(154f, 34f), 21f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 15f);

            CreatePillLabel("DragHintPill", "DragHint", slot, "DRAG -> BOARD",
                new Vector2(0f, -86f), new Vector2(156f, 38f), PillDarkAlpha, 16f, TextHintYellow, 12f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot);
                instance.name = prefab.name;
                RectTransform instRect = instance.GetComponent<RectTransform>();
                if (instRect != null)
                {
                    instRect.anchoredPosition = new Vector2(0f, 8f);
                    instRect.localScale = Vector3.one;
                }
            }
            else
            {
                Debug.LogWarning($"[GameSceneLayoutBuilder] Missing ingredient prefab at '{prefabPath}'.");
            }

            RectTransform overlay = CreateRect("CooldownOverlay", slot);
            overlay.anchoredPosition = new Vector2(0f, 8f);
            overlay.sizeDelta = new Vector2(96f, 96f);
            Image overlayImg = AddImage(overlay, new Color(0f, 0f, 0f, 0.72f), knobSprite);
            overlayImg.type = Image.Type.Filled;
            overlayImg.fillMethod = Image.FillMethod.Radial360;
            overlayImg.fillOrigin = 2;
            overlayImg.fillAmount = 0f;
            overlayImg.raycastTarget = false;
            overlay.gameObject.SetActive(false);

            TMP_Text cooldownTmp = CreateLabel("CooldownText", slot, "",
                new Vector2(0f, 8f), new Vector2(90f, 36f), 20f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 14f);
            cooldownTmp.gameObject.SetActive(false);

            PantrySlot pantrySlot = slot.gameObject.AddComponent<PantrySlot>();
            var so = new SerializedObject(pantrySlot);
            SetRef(so, "ingredientPrefab", prefab);
            so.FindProperty("restockDuration").floatValue = 1.6f;
            so.FindProperty("spawnAnchoredPosition").vector2Value = new Vector2(0f, 8f);
            SetRef(so, "cooldownOverlay", overlayImg);
            SetRef(so, "cooldownText", cooldownTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<CookingStation> BuildStoveAreaZone(
            RectTransform parent,
            FoodDisplayTray displayTray,
            out RectTransform zone)
        {
            zone = CreateRect("Zone1_StoveArea", parent);
            zone.anchoredPosition = new Vector2(0f, 98f);
            zone.sizeDelta = new Vector2(620f, 420f);
            AddImage(zone, StoveZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("ZoneHeader_StoveArea", zone, "STOVE AREA  (DROP POT/PAN -> PLATE WITHIN 3s!)",
                new Vector2(0f, 186f), new Vector2(594f, 30f), 19f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 14f);

            RectTransform grid = CreateRect("BurnersGrid", zone);
            grid.anchoredPosition = new Vector2(0f, -16f);
            grid.sizeDelta = new Vector2(588f, 364f);

            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(284f, 174f);
            glg.spacing = new Vector2(20f, 14f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            return new List<CookingStation>
            {
                CreateBurnerSlot(grid, "BurnerSlot_1_Pot", "BURNER 1 (POT)", CookwareType.Pot, displayTray),
                CreateBurnerSlot(grid, "BurnerSlot_2_Pot", "BURNER 2 (POT)", CookwareType.Pot, displayTray),
                CreateBurnerSlot(grid, "BurnerSlot_3_Pan", "BURNER 3 (PAN)", CookwareType.Pan, displayTray),
                CreateBurnerSlot(grid, "BurnerSlot_4_Pan", "BURNER 4 (PAN)", CookwareType.Pan, displayTray),
            };
        }

        private static CookingStation CreateBurnerSlot(
            RectTransform parent,
            string objectName,
            string displayName,
            CookwareType cookware,
            FoodDisplayTray displayTray)
        {
            bool isPan = cookware == CookwareType.Pan;
            RectTransform burner = CreateRect(objectName, parent);
            burner.sizeDelta = new Vector2(284f, 174f);

            Image dropAreaImg = AddImage(burner, BurnerCardBg, roundedSprite, 0.4f);
            dropAreaImg.raycastTarget = true;

            RectTransform outerRing = CreateRect("BurnerRing_Outer", burner);
            outerRing.anchoredPosition = new Vector2(0f, 10f);
            outerRing.sizeDelta = new Vector2(98f, 98f);
            AddImage(outerRing, BurnerRingColor, knobSprite).raycastTarget = false;

            RectTransform coilRing = CreateRect("BurnerRing_Ember", burner);
            coilRing.anchoredPosition = new Vector2(0f, 10f);
            coilRing.sizeDelta = new Vector2(80f, 80f);
            AddImage(coilRing, BurnerEmberColor, knobSprite).raycastTarget = false;

            if (isPan)
            {
                RectTransform handle = CreateRect("PanHandle_Placeholder", burner);
                handle.anchoredPosition = new Vector2(58f, 10f);
                handle.sizeDelta = new Vector2(46f, 12f);
                AddImage(handle, HandleDarkColor, roundedSprite, 0.5f).raycastTarget = false;
            }
            else
            {
                RectTransform leftEar = CreateRect("PotHandle_Left", burner);
                leftEar.anchoredPosition = new Vector2(-42f, 10f);
                leftEar.sizeDelta = new Vector2(16f, 24f);
                AddImage(leftEar, HandleDarkColor, roundedSprite, 0.5f).raycastTarget = false;

                RectTransform rightEar = CreateRect("PotHandle_Right", burner);
                rightEar.anchoredPosition = new Vector2(42f, 10f);
                rightEar.sizeDelta = new Vector2(16f, 24f);
                AddImage(rightEar, HandleDarkColor, roundedSprite, 0.5f).raycastTarget = false;
            }

            RectTransform vessel = CreateRect("Vessel_Placeholder", burner);
            vessel.anchoredPosition = new Vector2(0f, 10f);
            vessel.sizeDelta = new Vector2(72f, 72f);
            AddImage(vessel, isPan ? PanColor : PotColor, knobSprite).raycastTarget = false;

            RectTransform ingredientsContainer = CreateRect("IngredientsContainer", burner);
            ingredientsContainer.anchoredPosition = new Vector2(0f, 10f);
            ingredientsContainer.sizeDelta = new Vector2(200f, 66f);
            HorizontalLayoutGroup ingLayout = ingredientsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            ingLayout.spacing = 6f;
            ingLayout.childAlignment = TextAnchor.MiddleCenter;
            ingLayout.childControlWidth = false;
            ingLayout.childControlHeight = false;

            RectTransform fireOverlay = CreateRect("FireOverlay", burner);
            fireOverlay.anchoredPosition = new Vector2(0f, 10f);
            fireOverlay.sizeDelta = new Vector2(114f, 114f);
            AddImage(fireOverlay, new Color(0.95f, 0.32f, 0.08f, 0.88f), knobSprite).raycastTarget = false;
            RectTransform innerFlame = CreateRect("InnerFlame", fireOverlay);
            innerFlame.anchoredPosition = new Vector2(0f, 8f);
            innerFlame.sizeDelta = new Vector2(68f, 68f);
            AddImage(innerFlame, new Color(0.99f, 0.85f, 0.15f, 0.94f), knobSprite).raycastTarget = false;
            CreateLabel("FireLabel", fireOverlay, "FIRE!", Vector2.zero, new Vector2(96f, 34f),
                20f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 16f);
            fireOverlay.gameObject.SetActive(false);

            RectTransform burntRect = CreateRect("BurntFood_Draggable", burner);
            burntRect.anchoredPosition = new Vector2(0f, 10f);
            burntRect.sizeDelta = new Vector2(104f, 66f);
            Image burntImg = AddImage(burntRect, Hex("1F2937"), roundedSprite, 0.4f);
            burntImg.raycastTarget = true;
            burntRect.gameObject.AddComponent<CanvasGroup>();
            DraggableBurntFood burntHandle = burntRect.gameObject.AddComponent<DraggableBurntFood>();
            CreateLabel("BurntText", burntRect, "BURNT!\nDRAG -> TRASH", Vector2.zero, new Vector2(96f, 54f),
                14f, FontStyles.Bold, Hex("FCA5A5"), TextAlignmentOptions.Center, 11f);
            burntRect.gameObject.SetActive(false);

            TMP_Text titleTmp = CreateLabel("TitleText", burner, displayName,
                new Vector2(0f, 70f), new Vector2(266f, 24f), 17f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 13f);

            TMP_Text statusTmp = CreatePillLabel("StatusPill", "StatusText", burner, $"DROP {cookware.ToString().ToUpper()} HERE",
                new Vector2(0f, -44f), new Vector2(262f, 28f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            RectTransform progressTrack = CreateRect("ProgressBar", burner);
            progressTrack.anchoredPosition = new Vector2(0f, -66f);
            progressTrack.sizeDelta = new Vector2(246f, 10f);
            AddImage(progressTrack, Hex("141211"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform progressFillRect = CreateRect("Fill", progressTrack);
            Stretch(progressFillRect);
            Image fillImg = AddImage(progressFillRect, Hex("F59E0B"), roundedSprite, 0.5f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            RectTransform burnTrack = CreateRect("BurnWarningBar", burner);
            burnTrack.anchoredPosition = new Vector2(0f, -79f);
            burnTrack.sizeDelta = new Vector2(246f, 8f);
            AddImage(burnTrack, Hex("291815"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform burnFillRect = CreateRect("Fill", burnTrack);
            Stretch(burnFillRect);
            Image burnFillImg = AddImage(burnFillRect, Hex("EF4444"), roundedSprite, 0.5f);
            burnFillImg.type = Image.Type.Filled;
            burnFillImg.fillMethod = Image.FillMethod.Horizontal;
            burnFillImg.fillOrigin = 0;
            burnFillImg.fillAmount = 0f;
            burnFillImg.raycastTarget = false;

            Button cookBtn = CreateActionButton("CookButton", burner, "COOK",
                new Vector2(96f, 38f), new Vector2(72f, 28f), Hex("E07A5F"), 14f);
            cookBtn.gameObject.SetActive(false);

            Button serveBtn = CreateActionButton("ServeButton", burner, "PLATE!",
                new Vector2(0f, 10f), new Vector2(126f, 40f), ServeButtonColor, 18f);
            serveBtn.gameObject.SetActive(false);

            CookingStation station = burner.gameObject.AddComponent<CookingStation>();
            var stationSo = new SerializedObject(station);
            stationSo.FindProperty("stationName").stringValue = displayName;
            stationSo.FindProperty("maxIngredients").intValue = 3;
            stationSo.FindProperty("autoStartCookingWhenFull").boolValue = false;
            stationSo.FindProperty("cookDuration").floatValue = 4f;
            stationSo.FindProperty("canBurn").boolValue = true;
            stationSo.FindProperty("burnDuration").floatValue = 3f;
            stationSo.FindProperty("cookwareType").enumValueIndex = (int)cookware;
            stationSo.FindProperty("requirePrepStation").boolValue = true;
            SetRef(stationSo, "displayTray", displayTray);
            SetRef(stationSo, "burntFoodHandle", burntHandle);
            SetRef(stationSo, "fireOverlay", fireOverlay.gameObject);
            SetRef(stationSo, "ingredientsContainer", ingredientsContainer);
            SetRef(stationSo, "progressBarFill", fillImg);
            stationSo.ApplyModifiedPropertiesWithoutUndo();

            CookingStationUI stationUi = burner.gameObject.AddComponent<CookingStationUI>();
            var uiSo = new SerializedObject(stationUi);
            SetRef(uiSo, "statusText", statusTmp);
            SetRef(uiSo, "titleText", titleTmp);
            SetRef(uiSo, "cookButton", cookBtn);
            SetRef(uiSo, "serveButton", serveBtn);
            SetRef(uiSo, "burnWarningFill", burnFillImg);
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        private static RectTransform BuildUtensilsStationZone(RectTransform parent)
        {
            RectTransform zone = CreateRect("Zone3_UtensilsStation", parent);
            zone.anchoredPosition = new Vector2(0f, -214f);
            zone.sizeDelta = new Vector2(620f, 188f);
            AddImage(zone, UtensilsZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("ZoneHeader_UtensilsStation", zone, "PREP BOARD: 1. DROP -> 2. CHOP -> 3. MIX -> 4. PICK POT OR PAN",
                new Vector2(0f, 76f), new Vector2(604f, 28f), 16f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 12f);

            RectTransform boardSlot = CreateRect("Slot_KnifeAndCuttingBoard", zone);
            boardSlot.anchoredPosition = new Vector2(-150f, -12f);
            boardSlot.sizeDelta = new Vector2(292f, 144f);
            Image boardDropBg = AddImage(boardSlot, UtensilSlotBg, roundedSprite, 0.4f);
            boardDropBg.raycastTarget = true;

            RectTransform boardShape = CreateRect("CuttingBoard_Shape", boardSlot);
            boardShape.anchoredPosition = new Vector2(0f, 22f);
            boardShape.sizeDelta = new Vector2(268f, 66f);
            AddImage(boardShape, CuttingBoardColor, roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text ingredientsSummaryTmp = CreateLabel("IngredientsSummaryText", boardSlot, "BOARD: EMPTY (Drop 1-3 Items)",
                new Vector2(0f, 36f), new Vector2(256f, 26f), 15f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 12f);
            TMP_Text stageStatusTmp = CreatePillLabel("StageStatusPill", "StageStatusText", boardSlot, "DRAG INGREDIENTS HERE ->",
                new Vector2(0f, 8f), new Vector2(260f, 28f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            RectTransform precisionRoot = CreateRect("PrecisionBarRoot", boardSlot);
            precisionRoot.anchoredPosition = new Vector2(0f, -18f);
            precisionRoot.sizeDelta = new Vector2(246f, 18f);
            AddImage(precisionRoot, Hex("141211"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform sweetSpot = CreateRect("SweetSpotZone", precisionRoot);
            sweetSpot.anchoredPosition = Vector2.zero;
            sweetSpot.sizeDelta = new Vector2(56f, 16f);
            AddImage(sweetSpot, Hex("22C55E"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform cursorRect = CreateRect("PrecisionCursor", precisionRoot);
            cursorRect.anchoredPosition = Vector2.zero;
            cursorRect.sizeDelta = new Vector2(10f, 22f);
            AddImage(cursorRect, TextLight, roundedSprite, 0.5f).raycastTarget = false;
            precisionRoot.gameObject.SetActive(false);

            Button chopBtn = CreateActionButton("ChopButton", boardSlot, "CHOP",
                new Vector2(-78f, -50f), new Vector2(88f, 30f), Hex("EA580C"), 16f);
            Button mixBtn = CreateActionButton("MixButton", boardSlot, "MIX",
                new Vector2(18f, -50f), new Vector2(88f, 30f), Hex("0284C7"), 16f);
            Button resetBtn = CreateActionButton("ResetPrepButton", boardSlot, "CLR",
                new Vector2(104f, -50f), new Vector2(60f, 30f), Hex("64748B"), 14f);

            RectTransform utensilsCol = CreateRect("UtensilsAssignmentColumn", zone);
            utensilsCol.anchoredPosition = new Vector2(78f, -12f);
            utensilsCol.sizeDelta = new Vector2(142f, 144f);

            Button potBtn = CreateActionButton("Slot_StirringLadle", utensilsCol, "LADLE -> POT",
                new Vector2(0f, 37f), new Vector2(138f, 66f), PotColor, 16f);
            Button panBtn = CreateActionButton("Slot_FryingSpatula", utensilsCol, "SPATULA -> PAN",
                new Vector2(0f, -37f), new Vector2(138f, 66f), PanColor, 16f);

            RectTransform dishDock = CreateRect("PreparedDishDock", zone);
            dishDock.anchoredPosition = new Vector2(228f, -12f);
            dishDock.sizeDelta = new Vector2(136f, 144f);
            AddImage(dishDock, UtensilSlotBg, roundedSprite, 0.4f).raycastTarget = false;
            CreateLabel("DockLabel", dishDock, "DRAG -> STOVE",
                new Vector2(0f, 48f), new Vector2(128f, 36f), 15f, FontStyles.Bold, TextHintYellow, TextAlignmentOptions.Center, 12f);

            RectTransform tokenRect = CreateRect("PreparedDish_Token", dishDock);
            tokenRect.anchoredPosition = new Vector2(0f, -14f);
            tokenRect.sizeDelta = new Vector2(106f, 82f);
            Image tokenImg = AddImage(tokenRect, Hex("E85D4A"), roundedSprite, 0.4f);
            tokenImg.raycastTarget = true;
            tokenRect.gameObject.AddComponent<CanvasGroup>();
            DraggablePreparedDish dishToken = tokenRect.gameObject.AddComponent<DraggablePreparedDish>();
            TMP_Text tokenLabel = CreateLabel("Label", tokenRect, "POT\n100%", Vector2.zero, new Vector2(96f, 70f),
                17f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 13f);
            var tokenSo = new SerializedObject(dishToken);
            SetRef(tokenSo, "iconImage", tokenImg);
            SetRef(tokenSo, "labelText", tokenLabel);
            tokenSo.ApplyModifiedPropertiesWithoutUndo();
            tokenRect.gameObject.SetActive(false);

            PrepStation prepStation = boardSlot.gameObject.AddComponent<PrepStation>();
            var prepSo = new SerializedObject(prepStation);
            prepSo.FindProperty("maxIngredients").intValue = 3;
            prepSo.FindProperty("cursorSweepSpeed").floatValue = 1.45f;
            prepSo.FindProperty("efficiencyTargetSeconds").floatValue = 8f;
            SetRef(prepSo, "stageStatusText", stageStatusTmp);
            SetRef(prepSo, "ingredientsSummaryText", ingredientsSummaryTmp);
            SetRef(prepSo, "chopButton", chopBtn);
            SetRef(prepSo, "mixButton", mixBtn);
            SetRef(prepSo, "assignPotButton", potBtn);
            SetRef(prepSo, "assignPanButton", panBtn);
            SetRef(prepSo, "resetPrepButton", resetBtn);
            SetRef(prepSo, "precisionBarRoot", precisionRoot.gameObject);
            SetRef(prepSo, "precisionCursor", cursorRect);
            prepSo.FindProperty("barTravelWidth").floatValue = 220f;
            SetRef(prepSo, "preparedDishToken", dishToken);
            prepSo.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        private static FoodDisplayTray BuildFoodDisplayTrayZone(RectTransform parent, out RectTransform zone)
        {
            zone = CreateRect("Zone2_FoodDisplayTray", parent);
            zone.anchoredPosition = new Vector2(633f, 70f);
            zone.sizeDelta = new Vector2(614f, 476f);
            AddImage(zone, DisplayZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("ZoneHeader_FoodDisplayTray", zone, "SERVING TRAY  (STAGING COUNTER)",
                new Vector2(0f, 212f), new Vector2(580f, 32f), 20f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 15f);
            CreatePillLabel("ZoneSubheaderPill", "ZoneSubheader_FoodDisplayTray", zone, "DRAG PLATED FOOD OR DRINK -> CUSTOMER",
                new Vector2(0f, 180f), new Vector2(584f, 30f), PillDarkAlpha, 16f, TextHintYellow, 12f);

            RectTransform grid = CreateRect("DisplaySlotsGrid", zone);
            grid.anchoredPosition = new Vector2(0f, -22f);
            grid.sizeDelta = new Vector2(576f, 386f);

            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(182f, 184f);
            glg.spacing = new Vector2(14f, 14f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;

            var displaySlots = new List<FoodDisplaySlot>();
            for (int i = 1; i <= 6; i++)
            {
                RectTransform slot = CreateRect($"DisplaySlot_{i}", grid);
                slot.sizeDelta = new Vector2(182f, 184f);
                AddImage(slot, DisplaySlotBg, roundedSprite, 0.4f).raycastTarget = false;

                RectTransform plateOuter = CreateRect("Plate_Placeholder", slot);
                plateOuter.anchoredPosition = new Vector2(0f, 22f);
                plateOuter.sizeDelta = new Vector2(104f, 104f);
                AddImage(plateOuter, PlateOuterColor, knobSprite).raycastTarget = false;

                RectTransform plateInner = CreateRect("PlateRim_Placeholder", plateOuter);
                plateInner.anchoredPosition = Vector2.zero;
                plateInner.sizeDelta = new Vector2(76f, 76f);
                AddImage(plateInner, PlateInnerColor, knobSprite).raycastTarget = false;

                RectTransform itemRect = CreateRect("TrayItem_Draggable", slot);
                itemRect.anchoredPosition = new Vector2(0f, 22f);
                itemRect.sizeDelta = new Vector2(96f, 96f);
                Image itemImg = AddImage(itemRect, Hex("E85D4A"), knobSprite);
                itemImg.raycastTarget = true;
                itemRect.gameObject.AddComponent<CanvasGroup>();
                DraggableTrayItem trayItem = itemRect.gameObject.AddComponent<DraggableTrayItem>();
                TMP_Text badgeTmp = CreateLabel("BadgeText", itemRect, "POT\n100%", Vector2.zero, new Vector2(88f, 68f),
                    15f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 12f);
                var itemSo = new SerializedObject(trayItem);
                SetRef(itemSo, "iconImage", itemImg);
                SetRef(itemSo, "badgeText", badgeTmp);
                itemSo.ApplyModifiedPropertiesWithoutUndo();

                string defaultLabel = $"TRAY #{i}";
                TMP_Text slotLabel = CreatePillLabel("SlotLabelPill", "SlotLabel", slot, defaultLabel,
                    new Vector2(0f, -58f), new Vector2(172f, 52f), PillDarkAlpha, 15f, TextLight, 11f);

                FoodDisplaySlot displaySlot = slot.gameObject.AddComponent<FoodDisplaySlot>();
                var slotSo = new SerializedObject(displaySlot);
                SetRef(slotSo, "draggableItem", trayItem);
                SetRef(slotSo, "labelText", slotLabel);
                slotSo.FindProperty("defaultLabel").stringValue = defaultLabel;
                slotSo.ApplyModifiedPropertiesWithoutUndo();

                displaySlots.Add(displaySlot);
            }

            FoodDisplayTray tray = zone.gameObject.AddComponent<FoodDisplayTray>();
            var traySo = new SerializedObject(tray);
            SerializedProperty slotsProp = traySo.FindProperty("slots");
            slotsProp.arraySize = displaySlots.Count;
            for (int i = 0; i < displaySlots.Count; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = displaySlots[i];
            }
            traySo.ApplyModifiedPropertiesWithoutUndo();

            return tray;
        }

        private static void WireTrayStations(FoodDisplayTray tray, List<CookingStation> cookingStations)
        {
            var traySo = new SerializedObject(tray);
            SerializedProperty stationsProp = traySo.FindProperty("cookingStations");
            stationsProp.arraySize = cookingStations.Count;
            for (int i = 0; i < cookingStations.Count; i++)
            {
                stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = cookingStations[i];
            }
            traySo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildUtilitySafetyAndWasteZone(RectTransform parent)
        {
            RectTransform zone = CreateRect("UtilityZone_SafetyAndWaste", parent);
            zone.anchoredPosition = new Vector2(633f, -242f);
            zone.sizeDelta = new Vector2(614f, 124f);
            AddImage(zone, UtilityZoneBg, roundedSprite, 0.4f).raycastTarget = false;

            RectTransform extDock = CreateRect("FireExtinguisher_Dock", zone);
            extDock.anchoredPosition = new Vector2(-150f, 0f);
            extDock.sizeDelta = new Vector2(280f, 102f);
            AddImage(extDock, Hex("1E1412"), roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("ExtDockLabel", extDock, "FIRE EXTINGUISHER\nDRAG -> BURNING STOVE",
                new Vector2(36f, 0f), new Vector2(184f, 74f), 16f, FontStyles.Bold, TextLight, TextAlignmentOptions.Left, 12f);

            RectTransform extToolRect = CreateRect("FireExtinguisher_Tool", extDock);
            extToolRect.anchoredPosition = new Vector2(-94f, 0f);
            extToolRect.sizeDelta = new Vector2(58f, 84f);
            Image extImg = AddImage(extToolRect, ExtinguisherRed, roundedSprite, 0.4f);
            extImg.raycastTarget = true;
            extToolRect.gameObject.AddComponent<CanvasGroup>();
            extToolRect.gameObject.AddComponent<FireExtinguisherTool>();
            CreateLabel("ExtBadge", extToolRect, "EXT", Vector2.zero, new Vector2(52f, 32f),
                16f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 13f);

            RectTransform trashRect = CreateRect("TrashCan_DropZone", zone);
            trashRect.anchoredPosition = new Vector2(150f, 0f);
            trashRect.sizeDelta = new Vector2(280f, 102f);
            Image trashImg = AddImage(trashRect, TrashBinColor, roundedSprite, 0.4f);
            trashImg.raycastTarget = true;
            trashRect.gameObject.AddComponent<TrashCanDropZone>();

            CreateLabel("TrashLabel", trashRect, "TRASH CAN\nDROP BURNT FOOD / EXTRA",
                Vector2.zero, new Vector2(256f, 80f), 17f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 13f);

            return zone;
        }

        private static RectTransform BuildDishNotificationBanner(RectTransform parent)
        {
            RectTransform banner = CreateRect("DishNotificationBanner", parent);
            banner.anchoredPosition = new Vector2(0f, 456f);
            banner.sizeDelta = new Vector2(780f, 52f);

            Image bg = AddImage(banner, new Color(0.06f, 0.05f, 0.05f, 0.94f), roundedSprite, 0.4f);
            bg.raycastTarget = false;

            CanvasGroup cg = banner.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            TMP_Text tmp = CreateLabel("NotificationText", banner, string.Empty,
                Vector2.zero, new Vector2(740f, 42f), 22f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 15f);

            DishNotificationUI notification = banner.gameObject.AddComponent<DishNotificationUI>();
            var so = new SerializedObject(notification);
            SetRef(so, "notificationText", tmp);
            so.FindProperty("displayDuration").floatValue = 3.2f;
            so.FindProperty("fadeDuration").floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        private static void BuildSummaryModal(RectTransform parent, LevelGameManager manager)
        {
            RectTransform overlay = CreateRect("EndOfLevelSummaryModal", parent);
            Stretch(overlay);
            Image dimBg = AddImage(overlay, new Color(0.04f, 0.04f, 0.05f, 0.85f), null);
            dimBg.raycastTarget = true;

            RectTransform card = CreateRect("SummaryCard", overlay);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(780f, 540f);
            AddImage(card, Hex("24201E"), roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text titleTmp = CreateLabel("SummaryTitleText", card, "SHIFT COMPLETE!",
                new Vector2(0f, 216f), new Vector2(700f, 54f), 38f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Center, 24f);
            TMP_Text breakdownTmp = CreateLabel("SummaryBreakdownText", card, string.Empty,
                new Vector2(0f, 24f), new Vector2(700f, 260f), 21f, FontStyles.Normal, TextLight, TextAlignmentOptions.Center, 15f);
            TMP_Text finalScoreTmp = CreateLabel("SummaryFinalScoreText", card, "FINAL SCORE: 0",
                new Vector2(0f, -130f), new Vector2(700f, 48f), 32f, FontStyles.Bold, Hex("34D399"), TextAlignmentOptions.Center, 22f);

            Button playAgainBtn = CreateActionButton("PlayAgainButton", card, "PLAY AGAIN",
                new Vector2(-140f, -206f), new Vector2(220f, 56f), ServeButtonColor, 22f);
            Button mainMenuBtn = CreateActionButton("SummaryMainMenuButton", card, "MAIN MENU",
                new Vector2(140f, -206f), new Vector2(220f, 56f), Hex("475569"), 22f);

            var so = new SerializedObject(manager);
            SetRef(so, "summaryModalRoot", overlay.gameObject);
            SetRef(so, "summaryTitleText", titleTmp);
            SetRef(so, "summaryBreakdownText", breakdownTmp);
            SetRef(so, "summaryFinalScoreText", finalScoreTmp);
            SetRef(so, "playAgainButton", playAgainBtn);
            SetRef(so, "summaryMainMenuButton", mainMenuBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlay.gameObject.SetActive(false);
        }

        private static TMP_Text CreatePillLabel(
            string pillName,
            string labelName,
            Transform parent,
            string text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color pillColor,
            float fontSize,
            Color textColor,
            float minFontSize)
        {
            RectTransform pill = CreateRect(pillName, parent);
            pill.anchoredPosition = anchoredPosition;
            pill.sizeDelta = sizeDelta;
            AddImage(pill, pillColor, roundedSprite, 0.5f).raycastTarget = false;

            RectTransform textRect = CreateRect(labelName, pill);
            Stretch(textRect);
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            TextMeshProUGUI tmp = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minFontSize;
            tmp.fontSizeMax = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateActionButton(
            string name,
            Transform parent,
            string text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            Image img = AddImage(rect, color, roundedSprite, 0.5f);
            img.raycastTarget = true;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            CreateLabel("Text", rect, text, Vector2.zero, new Vector2(sizeDelta.x - 10f, sizeDelta.y - 6f),
                fontSize, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, Mathf.Max(11f, fontSize - 5f));
            return btn;
        }

        private static void EnsureCamera(Scene scene)
        {
            Camera existing = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<Camera>())
                .FirstOrDefault(c => c != null);

            if (existing != null)
            {
                existing.orthographic = true;
                existing.backgroundColor = BackdropColor;
                return;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackdropColor;
            cam.depth = -1f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem existing = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<EventSystem>())
                .FirstOrDefault(e => e != null);

            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null &&
                    existing.GetComponent<StandaloneInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            InputSystemUIInputModule module = esGo.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions != null)
            {
                module.actionsAsset = actions;
            }
        }

        private static Canvas EnsureCanvas(Scene scene)
        {
            Canvas canvas = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<Canvas>())
                .Where(c => c != null)
                .OrderByDescending(c => c.name == "Canvas")
                .FirstOrDefault();

            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                canvasGo.layer = LayerMask.NameToLayer("UI");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private static void LoadResources()
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                     ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath)
                     ?? TMP_Settings.defaultFontAsset;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rect, Color color, Sprite sprite, float pixelsPerUnitMultiplier = 1f)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }
            return image;
        }

        private static TMP_Text CreateLabel(
            string name,
            Transform parent,
            string text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment,
            float minFontSize = -1f)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            if (minFontSize > 0f && minFontSize < fontSize)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = minFontSize;
                tmp.fontSizeMax = fontSize;
            }
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[GameSceneLayoutBuilder] Property '{propertyName}' not found on {so.targetObject.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
