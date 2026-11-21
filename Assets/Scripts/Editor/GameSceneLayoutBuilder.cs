using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookingSystem.UI;
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
    /// Builds and wires the complete 2D Kitchen showcase scene in Assets/Scenes/gamescene.unity,
    /// matching the user's visualization layout with Carinderia wooden table & plastic mat,
    /// character PNGs, UI.svg-style HUD, 4-burner stove, Kitchen Tray prep, Drink Dispenser,
    /// 6-slot lidded Serving Trays, Fire Extinguisher, and Trash Can.
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneLayoutBuilder
    {
        public const string GameScenePath = "Assets/Scenes/gamescene.unity";
        private const string LayoutRootName = "CookingStationLayout";
        private const string CarinderiaMarkerName = "Carinderia_PlasticMat";

        private const string TomatoPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Tomato.prefab";
        private const string CheesePrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Cheese.prefab";
        private const string MeatPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Meat.prefab";
        private const string MushroomPrefabPath = "Assets/Prefabs/Ingredients/Ingredient_Mushroom.prefab";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/Fonts/Stay With Me SDF.asset";

        // 2D Kitchen Asset Paths
        private const string StovePath = "Assets/2D Kitchen/burnerstove.png";
        private const string OrderListPath = "Assets/2D Kitchen/Customer Order list.png";
        private const string ExtinguisherPath = "Assets/2D Kitchen/FireExtinguisher_Draggable.png";

        private const string DispenserTanksPath = "Assets/2D Kitchen/Drink Dispenser/1.png";
        private const string DispenserFilledCupsPath = "Assets/2D Kitchen/Drink Dispenser/2.png";
        private const string DispenserEmptyCupPath = "Assets/2D Kitchen/Drink Dispenser/3.png";

        private const string IngTomatoPath = "Assets/2D Kitchen/Ingridients Tray Lv 1/tomato.png";
        private const string IngCheesePath = "Assets/2D Kitchen/Ingridients Tray Lv 1/cheese.png";
        private const string IngMeatPath = "Assets/2D Kitchen/Ingridients Tray Lv 1/meat.png";
        private const string IngMushroomPath = "Assets/2D Kitchen/Ingridients Tray Lv 1/mushroom.png";

        // Day 2 & Day 3 Expanded Ingredients
        private const string IngOnionPath = "Assets/2D Kitchen/Ingridients 2/1.png";
        private const string IngGarlicPath = "Assets/2D Kitchen/Ingridients 2/2.png";
        private const string IngGreensPath = "Assets/2D Kitchen/Ingridients 2/3.png";
        private const string IngPotatoPath = "Assets/2D Kitchen/Ingridients 2/4.png";
        private const string IngCabbagePath = "Assets/2D Kitchen/Ingridients 3/cabbage.png";
        private const string IngCarrotsPath = "Assets/2D Kitchen/Ingridients 3/carrots.png";
        private const string IngPorkPath = "Assets/2D Kitchen/Ingridients 3/meat.png";
        private const string IngPepperPath = "Assets/2D Kitchen/Ingridients 3/pepper.png";

        private const string PotIdlePath = "Assets/2D Kitchen/Pots w state/1.png";
        private const string PotCookedPath = "Assets/2D Kitchen/Pots w state/2.png";
        private const string PotBurnedPath = "Assets/2D Kitchen/Pots w state/3.png";

        private const string PanIdlePath = "Assets/2D Kitchen/Pans W State/empty.png";
        private const string PanCookedPath = "Assets/2D Kitchen/Pans W State/cooked.png";
        private const string PanBurnedPath = "Assets/2D Kitchen/Pans W State/overcooked.png";

        private const string TrayEmptyLidPath = "Assets/2D Kitchen/Serving Tray/empty w lid.png";
        private const string TrayOpenEmptyPath = "Assets/2D Kitchen/Serving Tray/empty.png";
        private const string TrayWithDishPath = "Assets/2D Kitchen/Serving Tray/with dish.png";

        private const string TrashClosedPath = "Assets/2D Kitchen/Trash Can W State/closed.png";
        private const string TrashOpenPath = "Assets/2D Kitchen/Trash Can W State/open.png";

        private const string SandokLadlePath = "Assets/Luke's Art Assets/SANDOK.png";
        private const string Spatula2Path = "Assets/Luke's Art Assets/spatula 2.png";

        private static readonly (string name, string path)[] CustomerAssets =
        {
            ("Scarlet", "Assets/2D Kitchen/Character PNG for customers/Scarlet.png"),
            ("Spechan", "Assets/2D Kitchen/Character PNG for customers/Spechan.png"),
            ("Halo", "Assets/2D Kitchen/Character PNG for customers/halo.png"),
            ("Digitan", "Assets/2D Kitchen/Character PNG for customers/digitan.png"),
            ("Macafe", "Assets/2D Kitchen/Character PNG for customers/macafe.png"),
            ("Skye", "Assets/2D Kitchen/Character PNG for customers/Sae Young Skye.png"),
            ("Teto", "Assets/2D Kitchen/Character PNG for customers/teto.png"),
            ("Vulkan", "Assets/2D Kitchen/Character PNG for customers/Vulkan.png")
        };

        private static readonly Color BackdropColor = Hex("1E1916");
        private static readonly Color PillDarkAlpha = new Color(0.07f, 0.06f, 0.06f, 0.88f);
        private static readonly Color ZoneGlassBg = new Color(0.08f, 0.06f, 0.05f, 0.30f);
        private static readonly Color ServingTrayBg = new Color(0.08f, 0.06f, 0.05f, 0.36f);
        private static readonly Color ServeButtonColor = Hex("16A34A");

        private static readonly Color TextLight = Hex("FFFFFF");
        private static readonly Color TextWarmCream = Hex("FFF3D6");
        private static readonly Color TextHintYellow = Hex("FDE047");
        private static readonly Color TextDark = Hex("111827");

        private static Sprite roundedSprite;
        private static Sprite knobSprite;
        private static TMP_FontAsset uiFont;

        // Generated sprites
        private static Sprite wallBackdropSprite;
        private static Sprite woodTableSprite;
        private static Sprite plasticMatSprite;
        private static Sprite scorePillSprite;
        private static Sprite timerBarBgSprite;
        private static Sprite timerTrackSprite;
        private static Sprite timerFillPillSprite;
        private static Sprite patienceFillPillSprite;
        private static Sprite starFilledSprite;
        private static Sprite starEmptySprite;
        private static Sprite btnRestartSprite;
        private static Sprite btnMenuSprite;
        private static Sprite prepFrameSprite;
        private static Sprite prepBoardSprite;
        private static Sprite btnChopSprite;
        private static Sprite btnMixSprite;
        private static Sprite btnClrSprite;
        private static Sprite cardLadleSprite;
        private static Sprite cardSpatulaSprite;
        private static Sprite readyDockSprite;

        // Loaded 2D Kitchen sprites
        private static Sprite stoveSprite;
        private static Sprite orderListSprite;
        private static Sprite extinguisherSprite;
        private static Sprite dispenserTanksSprite;
        private static Sprite drinkBerrySprite;
        private static Sprite drinkCalamansiSprite;
        private static Sprite emptyCupSprite;
        private static Sprite tomatoSprite;
        private static Sprite cheeseSprite;
        private static Sprite meatSprite;
        private static Sprite mushroomSprite;
        private static Sprite onionSprite;
        private static Sprite garlicSprite;
        private static Sprite greensSprite;
        private static Sprite potatoSprite;
        private static Sprite cabbageSprite;
        private static Sprite carrotsSprite;
        private static Sprite porkSprite;
        private static Sprite pepperSprite;
        private static Sprite potIdleSprite;
        private static Sprite potCookedSprite;
        private static Sprite potBurnedSprite;
        private static Sprite panIdleSprite;
        private static Sprite panCookedSprite;
        private static Sprite panBurnedSprite;
        private static Sprite trayClosedLidSprite;
        private static Sprite trayOpenEmptySprite;
        private static Sprite trayWithDishSprite;
        private static Sprite trashClosedSprite;
        private static Sprite trashOpenSprite;
        private static Sprite ladleSprite;
        private static Sprite spatulaSprite;

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
            bool needs2DKitchenUpgrade = hasLayout && roots.All(r => r.transform.Find(LayoutRootName + "/Countertop_Area/" + CarinderiaMarkerName) == null);
            bool needsUtensilIconsUpgrade = hasLayout && roots.All(r =>
                r.transform.Find(LayoutRootName + "/Countertop_Area/Zone3_UtensilsStation/PrepTrayRoot/UtensilsAssignmentColumn/Slot_StirringLadle/LadleIconBox") == null);
            bool needsPillPolishUpgrade = hasLayout && roots.All(r =>
            {
                Transform p = r.transform.Find(LayoutRootName + "/Zone6_CustomerZone/CustomerSlots/CustomerSlot_1/OrderBubble/PatienceBarFill");
                Image img = p != null ? p.GetComponent<Image>() : null;
                return img == null || img.sprite == null || img.sprite.name != "UI_PatienceFillPill";
            });
            bool needsProgressionUpgrade = hasLayout && roots.All(r =>
            {
                Transform bubble = r.transform.Find(LayoutRootName + "/Zone6_CustomerZone/CustomerSlots/CustomerSlot_1/OrderBubble");
                bool orderNeedsReadabilityFix = bubble is RectTransform br && br.sizeDelta.x < 240f;
                return orderNeedsReadabilityFix ||
                       r.transform.Find(LayoutRootName + "/HelpGuideModal") == null ||
                       r.transform.Find(LayoutRootName + "/PauseMenuModal") == null ||
                       r.transform.Find(LayoutRootName + "/PauseMenuModal/PauseCard/InnerSurface/PauseAudioPanel") == null ||
                       r.transform.Find(LayoutRootName + "/GuidedTutorialBanner") == null ||
                       r.transform.Find(LayoutRootName + "/TopHUD_Bar/HelpHudButton") == null ||
                       r.transform.Find(LayoutRootName + "/TopHUD_Bar/PauseHudButton") == null ||
                       r.transform.Find(LayoutRootName + "/EndOfLevelSummaryModal/SummaryCard/SummaryStarRow") == null ||
                       r.transform.Find(LayoutRootName + "/EndOfLevelSummaryModal/SummaryCard/LeaderboardSubmitSection") == null ||
                       r.transform.Find(LayoutRootName + "/LeaderboardPanel") == null ||
                       r.transform.Find(LayoutRootName + "/Countertop_Area/Zone4_IngredientTray/IngredientSlotsGrid/Slot_Pork") == null;
            });

            if ((hasLayout && !needs2DKitchenUpgrade && !needsUtensilIconsUpgrade && !needsPillPolishUpgrade && !needsProgressionUpgrade) || (!hasLayout && !hasStarterMarker))
            {
                return;
            }

            if (Build(active))
            {
                EditorSceneManager.SaveScene(active, GameScenePath);
                Debug.Log("[GameSceneLayoutBuilder] Auto-built 2D Kitchen Carinderia scene in gamescene.unity.");
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
            Debug.Log("[GameSceneLayoutBuilder] 2D Kitchen Carinderia layout built and saved.");
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
            CarinderiaAssetGenerator.EnsureAllGeneratedAssets();
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

            // Carinderia Tiled Wall Backdrop behind customer counter
            RectTransform wallBackdrop = CreateRect("WallBackdrop_Carinderia", layoutRoot);
            wallBackdrop.anchoredPosition = new Vector2(0f, 320f);
            wallBackdrop.sizeDelta = new Vector2(2600f, 480f);
            Image wallImg = AddImage(wallBackdrop, Color.white, wallBackdropSprite);
            wallImg.raycastTarget = false;

            // Showcase Systems + Responsive Layout
            GameObject systemsGo = new GameObject("ShowcaseSystems");
            systemsGo.layer = LayerMask.NameToLayer("UI");
            systemsGo.transform.SetParent(layoutRoot, false);
            LevelGameManager levelManager = systemsGo.AddComponent<LevelGameManager>();
            CustomerManager customerManager = systemsGo.AddComponent<CustomerManager>();
            GameSceneResponsiveLayout responsiveLayout = systemsGo.AddComponent<GameSceneResponsiveLayout>();

            // Top HUD Bar (UI.svg design)
            RectTransform topHud = BuildTopHudBar(layoutRoot, levelManager);

            // Customer Zone (2D character PNGs + Customer Order list.png)
            var customerRefs = BuildCustomerZone(layoutRoot, out List<CustomerSlotUI> customerSlots);
            WireCustomerManager(customerManager, customerSlots);

            // Carinderia Wooden Kitchen Table + Checkered Plastic Mat
            RectTransform countertop = CreateRect("Countertop_Area", layoutRoot);
            countertop.anchoredPosition = new Vector2(0f, -210f);
            countertop.sizeDelta = new Vector2(1920f, 650f);
            AddImage(countertop, Color.white, woodTableSprite).raycastTarget = false;

            RectTransform plasticMat = CreateRect(CarinderiaMarkerName, countertop);
            plasticMat.anchoredPosition = new Vector2(0f, 6f);
            plasticMat.sizeDelta = new Vector2(1840f, 608f);
            AddImage(plasticMat, Color.white, plasticMatSprite).raycastTarget = false;

            // Food Display Tray (Top-Right, 6 closed-lid trays)
            FoodDisplayTray displayTray = BuildFoodDisplayTrayZone(countertop, out RectTransform displayZone);

            // Safety & Waste Utility Zone (Bottom-Right, horizontal Fire Extinguisher + state Trash Can)
            RectTransform utilityZone = BuildUtilitySafetyAndWasteZone(countertop);

            // Drink Dispenser (Top-Left, 1.png tanks + 2.png previews + 3.png cup)
            RectTransform drinkZone = BuildDrinkDispenserZone(countertop, displayTray);

            // Ingredient Tray (Bottom-Left, Ingridients Tray Lv 1 tiles)
            RectTransform ingredientZone = BuildIngredientTrayZone(countertop);

            // 4-Burner Stove Area (Top-Center, burnerstove.png + Pots & Pans w/ state sprites)
            List<CookingStation> cookingStations = BuildStoveAreaZone(countertop, displayTray, out RectTransform stoveZone, out RectTransform stoveArtRoot);
            WireTrayStations(displayTray, cookingStations);

            // Kitchen Prep Tray (Bottom-Center, expanded board & spacious Chop/Mix hitboxes)
            RectTransform utensilsZone = BuildUtensilsStationZone(
                countertop,
                out RectTransform prepTrayRoot,
                out PrepStation prepStationComp,
                out RectTransform boardRect,
                out RectTransform chopBtnRect,
                out RectTransform mixBtnRect,
                out RectTransform ladleBtnRect,
                out RectTransform spatulaBtnRect);

            // Dish Notification Banner
            RectTransform banner = BuildDishNotificationBanner(layoutRoot);

            // Interactive Day 1 Guided Tutorial (non-blocking banner + pulsing highlight frame)
            RectTransform customerSlot1Rt = customerSlots.Count > 0 ? (RectTransform)customerSlots[0].transform : null;
            RectTransform guidedBannerRect = BuildGuidedTutorialOverlay(
                layoutRoot,
                systemsGo,
                levelManager,
                prepStationComp,
                displayTray,
                ingredientZone,
                boardRect,
                chopBtnRect,
                mixBtnRect,
                ladleBtnRect,
                spatulaBtnRect,
                stoveZone,
                displayZone,
                customerSlot1Rt);

            // Pauseable Help Guide Modal (? button)
            BuildHelpGuideModal(layoutRoot, levelManager, out RectTransform helpCardRect, out GridLayoutGroup helpStepsGrid);

            // Unified Pause Menu Modal (|| button)
            BuildPauseMenuModal(layoutRoot, levelManager);

            // Visualized End-of-Level Summary Modal
            BuildSummaryModal(layoutRoot, levelManager);

            // Wire GameSceneResponsiveLayout
            var respSo = new SerializedObject(responsiveLayout);
            SetRef(respSo, "wallBackdrop", wallBackdrop);
            SetRef(respSo, "topHudBar", topHud);
            SetRef(respSo, "dishNotificationBanner", banner);
            SetRef(respSo, "customerZone", customerRefs.zone);
            SetRef(respSo, "countertopArea", countertop);
            SetRef(respSo, "plasticMatRect", plasticMat);
            SetRef(respSo, "customerSlotsContainer", customerRefs.slotsContainer);
            SetRef(respSo, "customerSlotsLayout", customerRefs.hlg);
            SetRef(respSo, "serviceCounterLedge", customerRefs.ledge);
            SetRef(respSo, "drinkDispenserZone", drinkZone);
            SetRef(respSo, "ingredientTrayZone", ingredientZone);
            SetRef(respSo, "stoveAreaZone", stoveZone);
            SetRef(respSo, "stoveArtRoot", stoveArtRoot);
            SetRef(respSo, "utensilsStationZone", utensilsZone);
            SetRef(respSo, "prepTrayRoot", prepTrayRoot);
            SetRef(respSo, "foodDisplayTrayZone", displayZone);
            SetRef(respSo, "utilitySafetyZone", utilityZone);
            SetRef(respSo, "tutorialCardRect", helpCardRect);
            SetRef(respSo, "tutorialStepsGrid", helpStepsGrid);
            SetRef(respSo, "guidedTutorialBanner", guidedBannerRect);
            respSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static RectTransform BuildTopHudBar(RectTransform parent, LevelGameManager manager)
        {
            RectTransform hud = CreateRect("TopHUD_Bar", parent);
            hud.anchoredPosition = new Vector2(0f, 502f);
            hud.sizeDelta = new Vector2(1880f, 66f);

            // Left Score & Streak Magenta Pill (UI.svg)
            RectTransform scorePill = CreateRect("UI_ScorePill", hud);
            scorePill.anchoredPosition = new Vector2(-710f, 0f);
            scorePill.sizeDelta = new Vector2(400f, 58f);
            AddImage(scorePill, Color.white, scorePillSprite).raycastTarget = false;

            TMP_Text scoreTmp = CreateLabel("ScoreText", scorePill, "Score: 0",
                new Vector2(-95f, 0f), new Vector2(180f, 44f), 22f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 14f);
            TMP_Text comboTmp = CreateLabel("ComboText", scorePill, "Streak x1",
                new Vector2(95f, 0f), new Vector2(180f, 44f), 22f, FontStyles.Bold, TextHintYellow, TextAlignmentOptions.Center, 14f);

            // Center Wooden Timer & Progress Bar (UI.svg)
            RectTransform timerBg = CreateRect("UI_TimerBarBg", hud);
            timerBg.anchoredPosition = new Vector2(-40f, 0f);
            timerBg.sizeDelta = new Vector2(880f, 62f);
            AddImage(timerBg, Color.white, timerBarBgSprite).raycastTarget = false;

            // Day / Mode badge slot right after the clock icon
            TMP_Text dayBadgeTmp = CreatePillLabel("DayModePill", "DayModeText", timerBg, "Day 1/3",
                new Vector2(-276f, 0f), new Vector2(142f, 36f), new Color(0.16f, 0.08f, 0.04f, 0.94f), 17f, TextHintYellow, 11f);

            // Middle progress track with clean capsule pill fill
            RectTransform timerTrack = CreateRect("TimerProgressTrack", timerBg);
            timerTrack.anchoredPosition = new Vector2(-62f, 0f);
            timerTrack.sizeDelta = new Vector2(252f, 34f);
            AddImage(timerTrack, Color.white, timerTrackSprite).raycastTarget = false;

            RectTransform timerFillMask = CreateRect("TimerFillMask", timerTrack);
            Stretch(timerFillMask);
            timerFillMask.offsetMin = new Vector2(8f, 5f);
            timerFillMask.offsetMax = new Vector2(-8f, -5f);
            Image maskImg = AddImage(timerFillMask, Color.white, timerFillPillSprite);
            maskImg.raycastTarget = false;
            Mask mask = timerFillMask.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform timerFillRect = CreateRect("TimerFill", timerFillMask);
            Stretch(timerFillRect);
            Image timerFillImg = AddImage(timerFillRect, Hex("22C55E"), timerFillPillSprite);
            timerFillImg.type = Image.Type.Filled;
            timerFillImg.fillMethod = Image.FillMethod.Horizontal;
            timerFillImg.fillOrigin = 0;
            timerFillImg.fillAmount = 1f;
            timerFillImg.raycastTarget = false;

            // Right dark readout slot inside wooden timer bar
            RectTransform readoutSlot = CreateRect("ReadoutTrackSlot", timerBg);
            readoutSlot.anchoredPosition = new Vector2(248f, 0f);
            readoutSlot.sizeDelta = new Vector2(342f, 36f);
            AddImage(readoutSlot, Color.white, timerTrackSprite).raycastTarget = false;

            TMP_Text timerTmp = CreateLabel("TimerText", readoutSlot, "Time: 03:00",
                new Vector2(-80f, 0f), new Vector2(160f, 32f), 19f, FontStyles.Bold, TextWarmCream, TextAlignmentOptions.Center, 12f);
            TMP_Text ordersTmp = CreateLabel("OrdersText", readoutSlot, "Orders: 0/6",
                new Vector2(82f, 0f), new Vector2(160f, 32f), 19f, FontStyles.Bold, Hex("4ADE80"), TextAlignmentOptions.Center, 12f);

            // Right Unified Help (?) & Pause (||) buttons
            Button helpBtn = CreateActionButton("HelpHudButton", hud, "? Help",
                new Vector2(548f, 0f), new Vector2(160f, 56f), Hex("0284C7"), 20f);
            Button pauseBtn = CreateSpriteButton("PauseHudButton", hud, btnMenuSprite, "|| Pause",
                new Vector2(782f, 0f), new Vector2(260f, 56f), new Vector2(14f, 0f), new Vector2(180f, 38f), 20f, TextWarmCream);

            var so = new SerializedObject(manager);
            SetRef(so, "dayModeBadgeText", dayBadgeTmp);
            SetRef(so, "timerText", timerTmp);
            SetRef(so, "timerFillBar", timerFillImg);
            SetRef(so, "timerThumbRect", null);
            so.FindProperty("timerTrackWidth").floatValue = 236f;
            SetRef(so, "scoreText", scoreTmp);
            SetRef(so, "ordersText", ordersTmp);
            SetRef(so, "comboText", comboTmp);
            SetRef(so, "helpHudButton", helpBtn);
            SetRef(so, "pauseHudButton", pauseBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            return hud;
        }

        private static (RectTransform zone, RectTransform slotsContainer, HorizontalLayoutGroup hlg, RectTransform ledge)
            BuildCustomerZone(RectTransform parent, out List<CustomerSlotUI> slotComponents)
        {
            RectTransform zone = CreateRect("Zone6_CustomerZone", parent);
            zone.anchoredPosition = new Vector2(0f, 294f);
            zone.sizeDelta = new Vector2(1880f, 336f);

            RectTransform slotsContainer = CreateRect("CustomerSlots", zone);
            slotsContainer.anchoredPosition = new Vector2(0f, -2f);
            slotsContainer.sizeDelta = new Vector2(1760f, 276f);

            HorizontalLayoutGroup hlg = slotsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 70f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            slotComponents = new List<CustomerSlotUI>();
            for (int i = 1; i <= 3; i++)
            {
                RectTransform slot = CreateRect($"CustomerSlot_{i}", slotsContainer);
                slot.sizeDelta = new Vector2(510f, 268f);
                Image cardBg = AddImage(slot, new Color(0.18f, 0.13f, 0.10f, 0.28f), roundedSprite, 0.4f);
                cardBg.raycastTarget = true;

                RectTransform highlight = CreateRect("DropHighlightBorder", slot);
                Stretch(highlight);
                Image highlightImg = AddImage(highlight, new Color(0.18f, 0.80f, 0.44f, 0.35f), roundedSprite, 0.4f);
                highlightImg.raycastTarget = false;
                highlightImg.enabled = false;

                // Character PNG holder on the left
                RectTransform spriteHolder = CreateRect("CustomerVisualRoot", slot);
                spriteHolder.anchoredPosition = new Vector2(-146f, 14f);
                spriteHolder.sizeDelta = new Vector2(170f, 206f);

                RectTransform portraitRect = CreateRect("CustomerPortrait", spriteHolder);
                Stretch(portraitRect);
                Image portraitImg = portraitRect.gameObject.AddComponent<Image>();
                portraitImg.preserveAspect = true;
                portraitImg.raycastTarget = false;
                portraitImg.enabled = false;

                // Wooden Nameplate pill below character ("Customer name" style in visualization)
                RectTransform namePill = CreateRect("NameplatePill", slot);
                namePill.anchoredPosition = new Vector2(-146f, -106f);
                namePill.sizeDelta = new Vector2(180f, 34f);
                AddImage(namePill, Hex("5A2D14"), roundedSprite, 0.5f).raycastTarget = false;

                RectTransform nameInner = CreateRect("Inner", namePill);
                Stretch(nameInner);
                nameInner.offsetMin = new Vector2(3f, 3f);
                nameInner.offsetMax = new Vector2(-3f, -3f);
                AddImage(nameInner, Hex("8B4E2B"), roundedSprite, 0.5f).raycastTarget = false;

                TMP_Text slotLabelTmp = CreateLabel("SlotLabelText", nameInner, $"Customer {i}",
                    Vector2.zero, new Vector2(166f, 28f), 17f, FontStyles.Bold, TextWarmCream, TextAlignmentOptions.Center, 12f);

                // Enlarged Customer Order list.png card on the right for maximum readability
                RectTransform bubble = CreateRect("OrderBubble", slot);
                bubble.anchoredPosition = new Vector2(96f, 8f);
                bubble.sizeDelta = new Vector2(248f, 254f);
                Image bubbleImg = AddImage(bubble, Color.white, orderListSprite);
                bubbleImg.preserveAspect = false;
                bubbleImg.raycastTarget = false;

                // 3 exact aligned inner boxes matching Customer Order list.png bounds with higher contrast & crisp typography
                TMP_Text mealOrderTmp = CreateAnchoredLabel("MealOrderText", bubble,
                    new Vector2(0.190f, 0.692f), new Vector2(0.785f, 0.900f),
                    "Sinigang sa Kamatis", 20f, FontStyles.Bold, Hex("0F172A"), 13f);
                TMP_Text mealIngTmp = CreateAnchoredLabel("MealIngredientsText", bubble,
                    new Vector2(0.190f, 0.432f), new Vector2(0.785f, 0.634f),
                    "Tom + Tom | Pot", 18f, FontStyles.Bold, Hex("1E293B"), 12f);
                TMP_Text drinkOrderTmp = CreateAnchoredLabel("DrinkOrderText", bubble,
                    new Vector2(0.190f, 0.155f), new Vector2(0.785f, 0.370f),
                    "+ Berry Juice", 19f, FontStyles.Bold, Hex("9D174D"), 12f);

                // Clean capsule vertical patience bar centered directly on the gray right-hand track
                RectTransform pFillRect = CreateRect("PatienceBarFill", bubble);
                pFillRect.anchorMin = new Vector2(0.878f, 0.162f);
                pFillRect.anchorMax = new Vector2(0.922f, 0.900f);
                pFillRect.offsetMin = Vector2.zero;
                pFillRect.offsetMax = Vector2.zero;
                Image pFillImg = pFillRect.gameObject.AddComponent<Image>();
                pFillImg.sprite = patienceFillPillSprite;
                pFillImg.color = Hex("22C55E");
                pFillImg.type = Image.Type.Filled;
                pFillImg.fillMethod = Image.FillMethod.Vertical;
                pFillImg.fillOrigin = 0;
                pFillImg.fillAmount = 1f;
                pFillImg.raycastTarget = false;

                TMP_Text patienceTmp = CreatePillLabel("PatienceTimerTextPill", "PatienceTimerText", slot, "55s",
                    new Vector2(96f, -122f), new Vector2(246f, 26f), PillDarkAlpha, 16f, TextLight, 12f);
                patienceTmp.transform.parent.name = "PatienceTimerPill";

                TMP_Text feedbackTmp = CreateLabel("FeedbackText", slot, string.Empty,
                    new Vector2(0f, 116f), new Vector2(460f, 28f), 19f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 13f);

                CustomerSlotUI slotUi = slot.gameObject.AddComponent<CustomerSlotUI>();
                var slotSo = new SerializedObject(slotUi);
                slotSo.FindProperty("slotIndex").intValue = i;
                SetRef(slotSo, "cardBackground", cardBg);
                SetRef(slotSo, "dropHighlightBorder", highlightImg);
                SetRef(slotSo, "customerVisualRoot", spriteHolder.gameObject);
                SetRef(slotSo, "customerPortraitImage", portraitImg);
                SetRef(slotSo, "orderBubbleRoot", bubbleImg);
                SetRef(slotSo, "slotLabelText", slotLabelTmp);
                SetRef(slotSo, "mealOrderText", mealOrderTmp);
                SetRef(slotSo, "mealIngredientsText", mealIngTmp);
                SetRef(slotSo, "drinkOrderText", drinkOrderTmp);
                SetRef(slotSo, "feedbackText", feedbackTmp);
                SetRef(slotSo, "patienceBarFill", pFillImg);
                SetRef(slotSo, "patienceTimerText", patienceTmp);
                slotSo.ApplyModifiedPropertiesWithoutUndo();

                slotComponents.Add(slotUi);
            }

            RectTransform ledge = CreateRect("ServiceCounter_Ledge", zone);
            ledge.anchoredPosition = new Vector2(0f, -158f);
            ledge.sizeDelta = new Vector2(2600f, 24f);
            AddImage(ledge, Hex("4A2B16"), roundedSprite, 0.5f).raycastTarget = false;

            return (zone, slotsContainer, hlg, ledge);
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

            SerializedProperty rosterProp = so.FindProperty("characterRoster");
            rosterProp.arraySize = CustomerAssets.Length;
            for (int i = 0; i < CustomerAssets.Length; i++)
            {
                SerializedProperty entry = rosterProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("characterName").stringValue = CustomerAssets[i].name;
                entry.FindPropertyRelative("portraitSprite").objectReferenceValue = LoadLargestSprite(CustomerAssets[i].path);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildDrinkDispenserZone(RectTransform parent, FoodDisplayTray displayTray)
        {
            RectTransform zone = CreateRect("Zone5_DrinkDispenser", parent);
            zone.anchoredPosition = new Vector2(-700f, 134f);
            zone.sizeDelta = new Vector2(416f, 326f);
            AddImage(zone, ZoneGlassBg, roundedSprite, 0.4f).raycastTarget = false;

            DrinkDispenserStation station = zone.gameObject.AddComponent<DrinkDispenserStation>();

            CreatePillLabel("DispenserHeaderPill", "ZoneHeader_DrinkDispenser", zone, "Drink Dispenser",
                new Vector2(0f, 144f), new Vector2(380f, 28f), PillDarkAlpha, 16f, TextLight, 12f);

            // Dispenser Tanks + Tap Hitboxes + Pour Previews
            RectTransform artGroup = CreateRect("DispenserArtGroup", zone);
            artGroup.anchoredPosition = new Vector2(-68f, 12f);
            artGroup.sizeDelta = new Vector2(246f, 238f);

            RectTransform machineRect = CreateRect("DispenserMachine_Art", artGroup);
            machineRect.anchoredPosition = new Vector2(0f, 26f);
            machineRect.sizeDelta = new Vector2(236f, 194f);
            Image machineImg = AddImage(machineRect, Color.white, dispenserTanksSprite);
            machineImg.preserveAspect = true;
            machineImg.raycastTarget = false;

            // Left pink tap hitbox + right green tap hitbox overlaid cleanly on the 2 tanks
            CreateDrinkTapHitbox(machineRect, "DrinkTap_BerryJuice", new Vector2(-58f, 0f), new Vector2(110f, 186f),
                DrinkType.BerryJuice, "Berry", station);
            CreateDrinkTapHitbox(machineRect, "DrinkTap_CalamansiTea", new Vector2(58f, 0f), new Vector2(110f, 186f),
                DrinkType.CalamansiTea, "Calamansi", station);

            // Pouring cup previews right under the dispensers (2.png -> 2_0 & 2_1)
            RectTransform berryCupPrev = CreateRect("BerryPourCupPreview", artGroup);
            berryCupPrev.anchoredPosition = new Vector2(-58f, -66f);
            berryCupPrev.sizeDelta = new Vector2(48f, 76f);
            Image berryPrevImg = AddImage(berryCupPrev, Color.white, drinkBerrySprite);
            berryPrevImg.preserveAspect = true;
            berryPrevImg.raycastTarget = false;
            berryCupPrev.gameObject.SetActive(false);

            RectTransform calCupPrev = CreateRect("CalamansiPourCupPreview", artGroup);
            calCupPrev.anchoredPosition = new Vector2(58f, -66f);
            calCupPrev.sizeDelta = new Vector2(48f, 76f);
            Image calPrevImg = AddImage(calCupPrev, Color.white, drinkCalamansiSprite);
            calPrevImg.preserveAspect = true;
            calPrevImg.raycastTarget = false;
            calCupPrev.gameObject.SetActive(false);

            // Progress bar & status pill cleanly below dispenser tanks
            RectTransform pTrack = CreateRect("PourProgressBar", artGroup);
            pTrack.anchoredPosition = new Vector2(0f, -104f);
            pTrack.sizeDelta = new Vector2(226f, 14f);
            AddImage(pTrack, Hex("141211"), roundedSprite, 0.5f).raycastTarget = false;

            RectTransform pFillRect = CreateRect("Fill", pTrack);
            Stretch(pFillRect);
            Image pFillImg = AddImage(pFillRect, Hex("38BDF8"), timerFillPillSprite);
            pFillImg.type = Image.Type.Filled;
            pFillImg.fillMethod = Image.FillMethod.Horizontal;
            pFillImg.fillOrigin = 0;
            pFillImg.fillAmount = 0f;
            pFillImg.raycastTarget = false;

            // Draggable Cup Dock on right of Dispenser
            RectTransform cupDock = CreateRect("CupSlot_Dock", zone);
            cupDock.anchoredPosition = new Vector2(126f, -14f);
            cupDock.sizeDelta = new Vector2(132f, 240f);
            AddImage(cupDock, new Color(0.08f, 0.06f, 0.05f, 0.42f), roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text statusTmp = CreatePillLabel("DispenserStatusPill", "DispenserStatusText", cupDock, "Drag Cup to Tap",
                new Vector2(0f, -72f), new Vector2(124f, 44f), PillDarkAlpha, 14f, TextHintYellow, 10f);

            Button stagePendingBtn = CreateActionButton("StagePendingDrinkButton", cupDock, "To Tray",
                new Vector2(0f, 88f), new Vector2(118f, 32f), ServeButtonColor, 14f);
            stagePendingBtn.gameObject.SetActive(false);

            RectTransform cupRect = CreateRect("DraggableCup_Item", cupDock);
            cupRect.anchoredPosition = new Vector2(0f, 18f);
            cupRect.sizeDelta = new Vector2(88f, 132f);
            Image cupImg = AddImage(cupRect, Color.white, emptyCupSprite);
            cupImg.preserveAspect = true;
            cupImg.raycastTarget = true;
            cupRect.gameObject.AddComponent<CanvasGroup>();
            DraggableCup draggableCup = cupRect.gameObject.AddComponent<DraggableCup>();

            var stationSo = new SerializedObject(station);
            SetRef(stationSo, "cupItem", draggableCup);
            SetRef(stationSo, "pourProgressFill", pFillImg);
            SetRef(stationSo, "statusText", statusTmp);
            SetRef(stationSo, "stagePendingDrinkButton", stagePendingBtn);
            SetRef(stationSo, "displayTray", displayTray);
            SetRef(stationSo, "berryPourIndicator", berryCupPrev.gameObject);
            SetRef(stationSo, "calamansiPourIndicator", calCupPrev.gameObject);
            stationSo.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        private static void CreateDrinkTapHitbox(
            RectTransform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            DrinkType drinkType,
            string badge,
            DrinkDispenserStation station)
        {
            RectTransform tap = CreateRect(name, parent);
            tap.anchoredPosition = pos;
            tap.sizeDelta = size;
            Image hitImg = AddImage(tap, new Color(1f, 1f, 1f, 0.01f), null);
            hitImg.raycastTarget = true;

            CreatePillLabel($"{name}_Pill", "Label", tap, badge,
                new Vector2(0f, -66f), new Vector2(102f, 26f), PillDarkAlpha, 13f, TextLight, 10f);

            DrinkTapDropZone dropZone = tap.gameObject.AddComponent<DrinkTapDropZone>();
            var so = new SerializedObject(dropZone);
            so.FindProperty("drinkType").enumValueIndex = (int)drinkType;
            SetRef(so, "dispenserStation", station);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildIngredientTrayZone(RectTransform parent)
        {
            RectTransform zone = CreateRect("Zone4_IngredientTray", parent);
            zone.anchoredPosition = new Vector2(-700f, -162f);
            zone.sizeDelta = new Vector2(416f, 286f);
            AddImage(zone, ZoneGlassBg, roundedSprite, 0.4f).raycastTarget = false;

            CreatePillLabel("IngredientHeaderPill", "ZoneHeader_IngredientTray", zone, "Ingredient Tray (Drag to Board)",
                new Vector2(0f, 124f), new Vector2(384f, 26f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            RectTransform grid = CreateRect("IngredientSlotsGrid", zone);
            grid.anchoredPosition = new Vector2(0f, -14f);
            grid.sizeDelta = new Vector2(386f, 238f);

            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(184f, 108f);
            glg.spacing = new Vector2(12f, 10f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.MiddleCenter;

            // Day 1 Pantry (Indices 0..3)
            CreateIngredientPantrySlot(grid, "Slot_Tomato", "Tomato", TomatoPrefabPath, tomatoSprite, false, IngredientType.Tomato, true);
            CreateIngredientPantrySlot(grid, "Slot_Cheese", "Cheese", CheesePrefabPath, cheeseSprite, false, IngredientType.Cheese, true);
            CreateIngredientPantrySlot(grid, "Slot_Meat", "Meat", MeatPrefabPath, meatSprite, false, IngredientType.Meat, true);
            CreateIngredientPantrySlot(grid, "Slot_Mushroom", "Mushroom", MushroomPrefabPath, mushroomSprite, false, IngredientType.Mushroom, true);

            // Day 2 Expansion (Indices 4..7, Ingridients 2)
            CreateIngredientPantrySlot(grid, "Slot_Onion", "Onion", TomatoPrefabPath, onionSprite, true, IngredientType.Onion, false);
            CreateIngredientPantrySlot(grid, "Slot_Garlic", "Garlic", TomatoPrefabPath, garlicSprite, true, IngredientType.Garlic, false);
            CreateIngredientPantrySlot(grid, "Slot_Greens", "Greens", TomatoPrefabPath, greensSprite, true, IngredientType.Herbs, false);
            CreateIngredientPantrySlot(grid, "Slot_Potato", "Potato", TomatoPrefabPath, potatoSprite, true, IngredientType.Potato, false);

            // Day 3 & Endless Expansion (Indices 8..11, Ingridients 3)
            CreateIngredientPantrySlot(grid, "Slot_Cabbage", "Cabbage", TomatoPrefabPath, cabbageSprite, true, IngredientType.Cabbage, false);
            CreateIngredientPantrySlot(grid, "Slot_Carrots", "Carrots", TomatoPrefabPath, carrotsSprite, true, IngredientType.Carrots, false);
            CreateIngredientPantrySlot(grid, "Slot_Pork", "Pork", TomatoPrefabPath, porkSprite, true, IngredientType.Pork, false);
            CreateIngredientPantrySlot(grid, "Slot_BellPepper", "Pepper", TomatoPrefabPath, pepperSprite, true, IngredientType.BellPepper, false);

            return zone;
        }

        private static void CreateIngredientPantrySlot(
            RectTransform parent,
            string slotName,
            string displayName,
            string prefabPath,
            Sprite artSprite,
            bool overrideIdentity,
            IngredientType overrideType,
            bool activeByDefault)
        {
            RectTransform slot = CreateRect(slotName, parent);
            slot.sizeDelta = new Vector2(184f, 108f);
            AddImage(slot, new Color(0.16f, 0.11f, 0.08f, 0.72f), roundedSprite, 0.4f).raycastTarget = false;

            RectTransform socket = CreateRect("SocketRing_Placeholder", slot);
            socket.anchoredPosition = new Vector2(-40f, 0f);
            socket.sizeDelta = new Vector2(82f, 82f);
            AddImage(socket, new Color(1f, 1f, 1f, 0.14f), roundedSprite, 0.4f).raycastTarget = false;

            CreateLabel("TitleText", slot, displayName,
                new Vector2(44f, 18f), new Vector2(88f, 28f), 17f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 10f);

            CreatePillLabel("DragHintPill", "DragHint", slot, "Drag ->",
                new Vector2(44f, -18f), new Vector2(88f, 28f), PillDarkAlpha, 14f, TextHintYellow, 10f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot);
                instance.name = overrideIdentity ? $"Ingredient_{displayName}" : prefab.name;
                RectTransform instRect = instance.GetComponent<RectTransform>();
                if (instRect != null)
                {
                    instRect.anchoredPosition = new Vector2(-40f, 0f);
                    instRect.sizeDelta = new Vector2(82f, 82f);
                    instRect.localScale = Vector3.one;
                }
                Image instImg = instance.GetComponent<Image>();
                if (instImg != null && artSprite != null)
                {
                    instImg.sprite = artSprite;
                    instImg.color = Color.white;
                    instImg.preserveAspect = true;
                }
                DraggableIngredient dragIng = instance.GetComponent<DraggableIngredient>();
                if (dragIng != null && overrideIdentity)
                {
                    dragIng.ConfigureIdentity(overrideType, displayName);
                    var ingSo = new SerializedObject(dragIng);
                    ingSo.FindProperty("ingredientType").enumValueIndex = (int)overrideType;
                    ingSo.FindProperty("ingredientName").stringValue = displayName;
                    ingSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            RectTransform overlay = CreateRect("CooldownOverlay", slot);
            overlay.anchoredPosition = new Vector2(-40f, 0f);
            overlay.sizeDelta = new Vector2(82f, 82f);
            Image overlayImg = AddImage(overlay, new Color(0f, 0f, 0f, 0.72f), knobSprite);
            overlayImg.type = Image.Type.Filled;
            overlayImg.fillMethod = Image.FillMethod.Radial360;
            overlayImg.fillOrigin = 2;
            overlayImg.fillAmount = 0f;
            overlayImg.raycastTarget = false;
            overlay.gameObject.SetActive(false);

            TMP_Text cooldownTmp = CreateLabel("CooldownText", slot, "",
                new Vector2(-40f, 0f), new Vector2(78f, 34f), 18f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 11f);
            cooldownTmp.gameObject.SetActive(false);

            PantrySlot pantrySlot = slot.gameObject.AddComponent<PantrySlot>();
            var so = new SerializedObject(pantrySlot);
            SetRef(so, "ingredientPrefab", prefab);
            so.FindProperty("restockDuration").floatValue = 1.5f;
            so.FindProperty("spawnAnchoredPosition").vector2Value = new Vector2(-40f, 0f);
            SetRef(so, "overrideIconSprite", artSprite);
            so.FindProperty("overrideIconSize").vector2Value = new Vector2(82f, 82f);
            so.FindProperty("useIngredientIdentityOverride").boolValue = overrideIdentity;
            so.FindProperty("overrideIngredientType").enumValueIndex = (int)overrideType;
            so.FindProperty("overrideIngredientName").stringValue = displayName;
            SetRef(so, "cooldownOverlay", overlayImg);
            SetRef(so, "cooldownText", cooldownTmp);
            so.ApplyModifiedPropertiesWithoutUndo();

            slot.gameObject.SetActive(activeByDefault);
        }

        private static List<CookingStation> BuildStoveAreaZone(
            RectTransform parent,
            FoodDisplayTray displayTray,
            out RectTransform zone,
            out RectTransform stoveArtRoot)
        {
            zone = CreateRect("Zone1_StoveArea", parent);
            zone.anchoredPosition = new Vector2(-46f, 118f);
            zone.sizeDelta = new Vector2(780f, 374f);

            // Actual burnerstove.png sized faithfully to its 1238x848 aspect ratio (528x362)
            stoveArtRoot = CreateRect("StoveArtRoot", zone);
            stoveArtRoot.anchoredPosition = new Vector2(0f, -4f);
            stoveArtRoot.sizeDelta = new Vector2(528f, 362f);
            Image stoveImg = AddImage(stoveArtRoot, Color.white, stoveSprite);
            stoveImg.preserveAspect = true;
            stoveImg.raycastTarget = false;

            CreatePillLabel("StoveHeaderPill", "ZoneHeader_StoveArea", zone, "Stove: Drop Prepped Pot/Pan -> Plate in 3s",
                new Vector2(0f, 174f), new Vector2(480f, 26f), PillDarkAlpha, 14f, TextLight, 11f);

            // Exact measured grate centers inside burnerstove.png (528x362):
            // TL (-0.178, +0.262) -> (-94, +95) | TR (+0.201, +0.261) -> (+106, +94)
            // BL (-0.217, -0.078) -> (-115, -28) | BR (+0.241, -0.078) -> (+127, -28)
            return new List<CookingStation>
            {
                CreateBurnerOnGrate(stoveArtRoot, "BurnerSlot_1_Pot", "Burner 1 (Pot)", new Vector2(-94f, 95f), CookwareType.Pot, displayTray),
                CreateBurnerOnGrate(stoveArtRoot, "BurnerSlot_2_Pot", "Burner 2 (Pot)", new Vector2(106f, 94f), CookwareType.Pot, displayTray),
                CreateBurnerOnGrate(stoveArtRoot, "BurnerSlot_3_Pan", "Burner 3 (Pan)", new Vector2(-115f, -28f), CookwareType.Pan, displayTray),
                CreateBurnerOnGrate(stoveArtRoot, "BurnerSlot_4_Pan", "Burner 4 (Pan)", new Vector2(127f, -28f), CookwareType.Pan, displayTray)
            };
        }

        private static CookingStation CreateBurnerOnGrate(
            RectTransform stoveArtRoot,
            string objectName,
            string displayName,
            Vector2 anchoredPos,
            CookwareType cookware,
            FoodDisplayTray displayTray)
        {
            bool isPan = cookware == CookwareType.Pan;
            RectTransform burner = CreateRect(objectName, stoveArtRoot);
            burner.anchoredPosition = anchoredPos;
            burner.sizeDelta = new Vector2(182f, 128f);

            // Transparent raycast drop hitbox over the burner grate
            Image dropAreaImg = AddImage(burner, new Color(1f, 1f, 1f, 0.01f), null);
            dropAreaImg.raycastTarget = true;

            // Smart Cookware Recommendation Glow Halo behind vessel
            RectTransform glowRect = CreateRect("RecommendationGlow", burner);
            glowRect.anchoredPosition = new Vector2(isPan ? 8f : 0f, 12f);
            glowRect.sizeDelta = new Vector2(174f, 124f);
            Image recGlowImg = AddImage(glowRect, new Color(0.98f, 0.78f, 0.22f, 0.7f), knobSprite);
            recGlowImg.raycastTarget = false;
            recGlowImg.enabled = false;

            // Pot or Pan 2D state vessel sitting cleanly on the grate
            RectTransform vesselRect = CreateRect("Vessel_Art", burner);
            vesselRect.anchoredPosition = new Vector2(isPan ? 12f : 0f, 14f);
            vesselRect.sizeDelta = isPan ? new Vector2(168f, 110f) : new Vector2(126f, 90f);
            Image vesselImg = AddImage(vesselRect, Color.white, isPan ? panIdleSprite : potIdleSprite);
            vesselImg.preserveAspect = true;
            vesselImg.raycastTarget = false;

            RectTransform ingredientsContainer = CreateRect("IngredientsContainer", burner);
            ingredientsContainer.anchoredPosition = new Vector2(0f, 14f);
            ingredientsContainer.sizeDelta = new Vector2(120f, 50f);

            // Fire Overlay (on burn Past 3s)
            RectTransform fireOverlay = CreateRect("FireOverlay", burner);
            fireOverlay.anchoredPosition = new Vector2(0f, 16f);
            fireOverlay.sizeDelta = new Vector2(106f, 106f);
            AddImage(fireOverlay, new Color(0.95f, 0.30f, 0.06f, 0.85f), knobSprite).raycastTarget = false;
            RectTransform innerFlame = CreateRect("InnerFlame", fireOverlay);
            innerFlame.anchoredPosition = new Vector2(0f, 8f);
            innerFlame.sizeDelta = new Vector2(64f, 64f);
            AddImage(innerFlame, new Color(0.99f, 0.85f, 0.14f, 0.94f), knobSprite).raycastTarget = false;
            CreateLabel("FireLabel", fireOverlay, "Fire!", Vector2.zero, new Vector2(88f, 34f),
                20f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 15f);
            fireOverlay.gameObject.SetActive(false);

            // Draggable Burnt Food handle (revealed after extinguisher puts out fire)
            RectTransform burntRect = CreateRect("BurntFood_Draggable", burner);
            burntRect.anchoredPosition = new Vector2(0f, 14f);
            burntRect.sizeDelta = new Vector2(114f, 56f);
            Image burntImg = AddImage(burntRect, Hex("1F2937"), roundedSprite, 0.4f);
            burntImg.raycastTarget = true;
            burntRect.gameObject.AddComponent<CanvasGroup>();
            DraggableBurntFood burntHandle = burntRect.gameObject.AddComponent<DraggableBurntFood>();
            CreateLabel("BurntText", burntRect, "Burnt!\nDrag to Trash", Vector2.zero, new Vector2(106f, 48f),
                13f, FontStyles.Bold, Hex("FCA5A5"), TextAlignmentOptions.Center, 10f);
            burntRect.gameObject.SetActive(false);

            TMP_Text statusTmp = CreatePillLabel("StatusPill", "StatusText", burner, $"Drop {cookware} Here",
                new Vector2(0f, -34f), new Vector2(168f, 24f), PillDarkAlpha, 13f, TextHintYellow, 10f);

            RectTransform progressTrack = CreateRect("ProgressBar", burner);
            progressTrack.anchoredPosition = new Vector2(0f, -51f);
            progressTrack.sizeDelta = new Vector2(156f, 8f);
            AddImage(progressTrack, Hex("141211"), timerFillPillSprite).raycastTarget = false;

            RectTransform progressFillRect = CreateRect("Fill", progressTrack);
            Stretch(progressFillRect);
            Image fillImg = AddImage(progressFillRect, Hex("F59E0B"), timerFillPillSprite);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            RectTransform burnTrack = CreateRect("BurnWarningBar", burner);
            burnTrack.anchoredPosition = new Vector2(0f, -61f);
            burnTrack.sizeDelta = new Vector2(156f, 7f);
            AddImage(burnTrack, Hex("291815"), timerFillPillSprite).raycastTarget = false;

            RectTransform burnFillRect = CreateRect("Fill", burnTrack);
            Stretch(burnFillRect);
            Image burnFillImg = AddImage(burnFillRect, Hex("EF4444"), timerFillPillSprite);
            burnFillImg.type = Image.Type.Filled;
            burnFillImg.fillMethod = Image.FillMethod.Horizontal;
            burnFillImg.fillOrigin = 0;
            burnFillImg.fillAmount = 0f;
            burnFillImg.raycastTarget = false;

            Button cookBtn = CreateActionButton("CookButton", burner, "Cook",
                new Vector2(64f, 44f), new Vector2(62f, 24f), Hex("E07A5F"), 12f);
            cookBtn.gameObject.SetActive(false);

            Button serveBtn = CreateActionButton("ServeButton", burner, "Plate!",
                new Vector2(0f, 14f), new Vector2(114f, 36f), ServeButtonColor, 16f);
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
            SetRef(stationSo, "recommendationGlow", recGlowImg);
            stationSo.ApplyModifiedPropertiesWithoutUndo();

            CookingStationUI stationUi = burner.gameObject.AddComponent<CookingStationUI>();
            var uiSo = new SerializedObject(stationUi);
            SetRef(uiSo, "statusText", statusTmp);
            SetRef(uiSo, "cookButton", cookBtn);
            SetRef(uiSo, "serveButton", serveBtn);
            SetRef(uiSo, "burnWarningFill", burnFillImg);
            SetRef(uiSo, "vesselImage", vesselImg);
            SetRef(uiSo, "idleVesselSprite", isPan ? panIdleSprite : potIdleSprite);
            SetRef(uiSo, "cookingVesselSprite", isPan ? panCookedSprite : potCookedSprite);
            SetRef(uiSo, "burnedVesselSprite", isPan ? panBurnedSprite : potBurnedSprite);
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        private static RectTransform BuildUtensilsStationZone(
            RectTransform parent,
            out RectTransform prepTrayRoot,
            out PrepStation prepStationComp,
            out RectTransform boardRectOut,
            out RectTransform chopBtnRectOut,
            out RectTransform mixBtnRectOut,
            out RectTransform ladleBtnRectOut,
            out RectTransform spatulaBtnRectOut)
        {
            RectTransform zone = CreateRect("Zone3_UtensilsStation", parent);
            zone.anchoredPosition = new Vector2(-46f, -186f);
            zone.sizeDelta = new Vector2(852f, 248f);

            prepTrayRoot = CreateRect("PrepTrayRoot", zone);
            prepTrayRoot.anchoredPosition = Vector2.zero;
            prepTrayRoot.sizeDelta = new Vector2(852f, 248f);
            AddImage(prepTrayRoot, Color.white, prepFrameSprite).raycastTarget = false;

            // Left Cutting Board Area with expanded hitboxes & spacious action spacing
            RectTransform boardSlot = CreateRect("Slot_KnifeAndCuttingBoard", prepTrayRoot);
            boardSlot.anchoredPosition = new Vector2(-214f, 0f);
            boardSlot.sizeDelta = new Vector2(416f, 232f);
            Image boardHit = AddImage(boardSlot, new Color(1f, 1f, 1f, 0.01f), null);
            boardHit.raycastTarget = true;
            boardRectOut = boardSlot;

            RectTransform boardGraphic = CreateRect("CuttingBoard_Shape", boardSlot);
            boardGraphic.anchoredPosition = new Vector2(0f, 34f);
            boardGraphic.sizeDelta = new Vector2(392f, 124f);
            AddImage(boardGraphic, Color.white, prepBoardSprite).raycastTarget = false;

            // 3 Visual Ingredient Slots on the wooden Cutting Board (SLOT 1, SLOT 2, SLOT 3)
            var slotIcons = new Image[3];
            var slotLabels = new TextMeshProUGUI[3];
            for (int s = 0; s < 3; s++)
            {
                float sx = -114f + s * 114f;
                RectTransform slotBox = CreateRect($"BoardSlot_{s + 1}", boardGraphic);
                slotBox.anchoredPosition = new Vector2(sx, 10f);
                slotBox.sizeDelta = new Vector2(92f, 76f);
                AddImage(slotBox, new Color(0.47f, 0.21f, 0.06f, 0.22f), roundedSprite, 0.4f).raycastTarget = false;

                RectTransform iconRect = CreateRect("Icon", slotBox);
                Stretch(iconRect);
                iconRect.offsetMin = new Vector2(6f, 6f);
                iconRect.offsetMax = new Vector2(-6f, -6f);
                Image iconImg = iconRect.gameObject.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.enabled = false;
                slotIcons[s] = iconImg;

                TMP_Text lbl = CreateLabel($"SlotLbl_{s + 1}", slotBox, $"Slot {s + 1}",
                    Vector2.zero, new Vector2(82f, 26f), 14f, FontStyles.Bold, new Color(0.47f, 0.21f, 0.06f, 0.78f),
                    TextAlignmentOptions.Center, 11f);
                slotLabels[s] = (TextMeshProUGUI)lbl;
            }

            TMP_Text ingredientsSummaryTmp = CreateLabel("IngredientsSummaryText", boardSlot, "Board: Drop 1-3 Items",
                new Vector2(0f, 82f), new Vector2(376f, 22f), 14f, FontStyles.Bold, Hex("431E0C"), TextAlignmentOptions.Center, 10f);

            TMP_Text stageStatusTmp = CreatePillLabel("StageStatusPill", "StageStatusText", boardSlot, "Drag Ingredients Here",
                new Vector2(0f, -20f), new Vector2(392f, 28f), PillDarkAlpha, 14f, TextHintYellow, 10f);

            // Precision Timing Bar (also clickable as a large trigger hitbox during Chop/Mix!)
            RectTransform precisionRoot = CreateRect("PrecisionBarRoot", boardSlot);
            precisionRoot.anchoredPosition = new Vector2(0f, 32f);
            precisionRoot.sizeDelta = new Vector2(336f, 28f);
            Image barBg = AddImage(precisionRoot, Hex("141211"), timerFillPillSprite);
            barBg.raycastTarget = true;
            Button barBtn = precisionRoot.gameObject.AddComponent<Button>();
            barBtn.targetGraphic = barBg;

            RectTransform sweetSpot = CreateRect("SweetSpotZone", precisionRoot);
            sweetSpot.anchoredPosition = Vector2.zero;
            sweetSpot.sizeDelta = new Vector2(86f, 24f);
            AddImage(sweetSpot, Hex("22C55E"), timerFillPillSprite).raycastTarget = false;

            RectTransform cursorRect = CreateRect("PrecisionCursor", precisionRoot);
            cursorRect.anchoredPosition = Vector2.zero;
            cursorRect.sizeDelta = new Vector2(14f, 32f);
            AddImage(cursorRect, TextLight, roundedSprite, 0.5f).raycastTarget = false;
            precisionRoot.gameObject.SetActive(false);

            // Larger, well-spaced Chop / Mix / Clear buttons (140x56 hitboxes with clear gaps!)
            Button chopBtn = CreateSpriteButton("ChopButton", boardSlot, btnChopSprite, "Chop",
                new Vector2(-132f, -72f), new Vector2(138f, 56f), Vector2.zero, new Vector2(122f, 40f), 19f, TextLight);
            Button mixBtn = CreateSpriteButton("MixButton", boardSlot, btnMixSprite, "Mix",
                new Vector2(8f, -72f), new Vector2(138f, 56f), Vector2.zero, new Vector2(122f, 40f), 19f, TextLight);
            Button resetBtn = CreateSpriteButton("ResetPrepButton", boardSlot, btnClrSprite, "Clear",
                new Vector2(142f, -72f), new Vector2(106f, 54f), Vector2.zero, new Vector2(92f, 38f), 18f, TextLight);
            chopBtnRectOut = (RectTransform)chopBtn.transform;
            mixBtnRectOut = (RectTransform)mixBtn.transform;

            // Middle Ladle -> Pot & Spatula -> Pan Cards with Smart Recommendation Glow
            RectTransform utensilsCol = CreateRect("UtensilsAssignmentColumn", prepTrayRoot);
            utensilsCol.anchoredPosition = new Vector2(106f, 0f);
            utensilsCol.sizeDelta = new Vector2(216f, 218f);

            Button potBtn = CreateSpriteButton("Slot_StirringLadle", utensilsCol, cardLadleSprite, "Ladle\n<color=#4ADE80>-> Pot</color>",
                new Vector2(0f, 54f), new Vector2(208f, 96f), new Vector2(30f, 0f), new Vector2(124f, 68f), 18f, TextLight);
            Image potRecGlow = CreateUtensilRecommendGlow((RectTransform)potBtn.transform, new Color(0.29f, 0.87f, 0.50f, 0.85f));
            CreateUtensilIconBadge("LadleIconBox", potBtn.transform, ladleSprite);

            Button panBtn = CreateSpriteButton("Slot_FryingSpatula", utensilsCol, cardSpatulaSprite, "Spatula\n<color=#FB7185>-> Pan</color>",
                new Vector2(0f, -54f), new Vector2(208f, 96f), new Vector2(30f, 0f), new Vector2(124f, 68f), 18f, TextLight);
            Image panRecGlow = CreateUtensilRecommendGlow((RectTransform)panBtn.transform, new Color(0.98f, 0.78f, 0.22f, 0.85f));
            CreateUtensilIconBadge("SpatulaIconBox", panBtn.transform, spatulaSprite);

            ladleBtnRectOut = (RectTransform)potBtn.transform;
            spatulaBtnRectOut = (RectTransform)panBtn.transform;

            // Right Ready Mix Dock (Prep_ReadyDock.png)
            RectTransform dishDock = CreateRect("PreparedDishDock", prepTrayRoot);
            dishDock.anchoredPosition = new Vector2(320f, 0f);
            dishDock.sizeDelta = new Vector2(202f, 210f);
            AddImage(dishDock, Color.white, readyDockSprite).raycastTarget = false;

            CreateLabel("DockLabel", dishDock, "Ready Mix",
                new Vector2(0f, 78f), new Vector2(160f, 24f), 15f, FontStyles.Bold, TextDark, TextAlignmentOptions.Center, 11f);
            CreateLabel("DockCoasterLabel", dishDock, "Waiting...",
                new Vector2(0f, -14f), new Vector2(114f, 30f), 14f, FontStyles.Bold, TextHintYellow, TextAlignmentOptions.Center, 10f);

            RectTransform tokenRect = CreateRect("PreparedDish_Token", dishDock);
            tokenRect.anchoredPosition = new Vector2(0f, -14f);
            tokenRect.sizeDelta = new Vector2(142f, 110f);
            Image tokenImg = AddImage(tokenRect, Color.white, potCookedSprite);
            tokenImg.preserveAspect = true;
            tokenImg.raycastTarget = true;
            tokenRect.gameObject.AddComponent<CanvasGroup>();
            DraggablePreparedDish dishToken = tokenRect.gameObject.AddComponent<DraggablePreparedDish>();
            TMP_Text tokenLabel = CreatePillLabel("TokenPill", "Label", tokenRect, "Pot\nDrag ->",
                new Vector2(0f, -40f), new Vector2(126f, 28f), PillDarkAlpha, 14f, TextHintYellow, 10f);
            var tokenSo = new SerializedObject(dishToken);
            SetRef(tokenSo, "iconImage", tokenImg);
            SetRef(tokenSo, "labelText", tokenLabel);
            SetRef(tokenSo, "potSprite", potCookedSprite);
            SetRef(tokenSo, "panSprite", panCookedSprite);
            tokenSo.ApplyModifiedPropertiesWithoutUndo();
            tokenRect.gameObject.SetActive(false);

            prepStationComp = boardSlot.gameObject.AddComponent<PrepStation>();
            var prepSo = new SerializedObject(prepStationComp);
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
            SetRef(prepSo, "potRecommendGlow", potRecGlow);
            SetRef(prepSo, "panRecommendGlow", panRecGlow);
            SetRef(prepSo, "precisionBarRoot", precisionRoot.gameObject);
            SetRef(prepSo, "precisionCursor", cursorRect);
            prepSo.FindProperty("barTravelWidth").floatValue = 296f;
            SetRef(prepSo, "preparedDishToken", dishToken);

            SerializedProperty iconsProp = prepSo.FindProperty("ingredientSlotIcons");
            iconsProp.arraySize = 3;
            SerializedProperty lblsProp = prepSo.FindProperty("ingredientSlotLabels");
            lblsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotIcons[i];
                lblsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotLabels[i];
            }
            SetRef(prepSo, "tomatoSprite", tomatoSprite);
            SetRef(prepSo, "cheeseSprite", cheeseSprite);
            SetRef(prepSo, "meatSprite", meatSprite);
            SetRef(prepSo, "mushroomSprite", mushroomSprite);
            SetRef(prepSo, "onionSprite", onionSprite);
            SetRef(prepSo, "herbsSprite", greensSprite);
            SetRef(prepSo, "garlicSprite", garlicSprite);
            SetRef(prepSo, "potatoSprite", potatoSprite);
            SetRef(prepSo, "cabbageSprite", cabbageSprite);
            SetRef(prepSo, "carrotsSprite", carrotsSprite);
            SetRef(prepSo, "porkSprite", porkSprite);
            SetRef(prepSo, "bellPepperSprite", pepperSprite);
            prepSo.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        private static FoodDisplayTray BuildFoodDisplayTrayZone(RectTransform parent, out RectTransform zone)
        {
            zone = CreateRect("Zone2_FoodDisplayTray", parent);
            zone.anchoredPosition = new Vector2(654f, 96f);
            zone.sizeDelta = new Vector2(508f, 414f);
            AddImage(zone, ServingTrayBg, roundedSprite, 0.4f).raycastTarget = false;

            CreatePillLabel("DisplayHeaderPill", "ZoneHeader_FoodDisplayTray", zone, "Display Tray — Drag to Customer",
                new Vector2(0f, 184f), new Vector2(486f, 30f), PillDarkAlpha, 15f, TextHintYellow, 11f);

            RectTransform grid = CreateRect("DisplaySlotsGrid", zone);
            grid.anchoredPosition = new Vector2(0f, -16f);
            grid.sizeDelta = new Vector2(486f, 344f);

            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(154f, 162f);
            glg.spacing = new Vector2(10f, 12f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.MiddleCenter;

            var displaySlots = new List<FoodDisplaySlot>();
            for (int i = 1; i <= 6; i++)
            {
                RectTransform slot = CreateRect($"DisplaySlot_{i}", grid);
                slot.sizeDelta = new Vector2(170f, 166f);
                AddImage(slot, new Color(0.10f, 0.08f, 0.07f, 0.35f), roundedSprite, 0.4f).raycastTarget = false;

                // Serving Tray visual: closed lid when empty, open empty tray when holding a dish/drink
                RectTransform trayFrameRect = CreateRect("TrayFrame_Image", slot);
                trayFrameRect.anchoredPosition = new Vector2(0f, 20f);
                trayFrameRect.sizeDelta = new Vector2(138f, 122f);
                Image trayFrameImg = AddImage(trayFrameRect, Color.white, trayClosedLidSprite);
                trayFrameImg.preserveAspect = true;
                trayFrameImg.raycastTarget = false;

                // Draggable Tray Item (displays with_dish.png or 2_0 / 2_1 filled cup)
                RectTransform itemRect = CreateRect("TrayItem_Draggable", slot);
                itemRect.anchoredPosition = new Vector2(0f, 20f);
                itemRect.sizeDelta = new Vector2(128f, 114f);
                Image itemImg = AddImage(itemRect, Color.white, trayWithDishSprite);
                itemImg.preserveAspect = true;
                itemImg.raycastTarget = true;
                itemRect.gameObject.AddComponent<CanvasGroup>();
                DraggableTrayItem trayItem = itemRect.gameObject.AddComponent<DraggableTrayItem>();
                TMP_Text badgeTmp = CreatePillLabel("BadgePill", "BadgeText", itemRect, "Pot 100%",
                    new Vector2(0f, 44f), new Vector2(104f, 24f), PillDarkAlpha, 12f, TextLight, 9f);
                var itemSo = new SerializedObject(trayItem);
                SetRef(itemSo, "iconImage", itemImg);
                SetRef(itemSo, "badgeText", badgeTmp);
                SetRef(itemSo, "mealTraySprite", trayWithDishSprite);
                SetRef(itemSo, "berryDrinkSprite", drinkBerrySprite);
                SetRef(itemSo, "calamansiDrinkSprite", drinkCalamansiSprite);
                itemSo.ApplyModifiedPropertiesWithoutUndo();

                string defaultLabel = $"Tray #{i} (Closed)";
                TMP_Text slotLabel = CreatePillLabel("SlotLabelPill", "SlotLabel", slot, defaultLabel,
                    new Vector2(0f, -54f), new Vector2(162f, 48f), PillDarkAlpha, 14f, TextLight, 10f);

                FoodDisplaySlot displaySlot = slot.gameObject.AddComponent<FoodDisplaySlot>();
                var slotSo = new SerializedObject(displaySlot);
                SetRef(slotSo, "draggableItem", trayItem);
                SetRef(slotSo, "labelText", slotLabel);
                SetRef(slotSo, "trayFrameImage", trayFrameImg);
                SetRef(slotSo, "closedLidSprite", trayClosedLidSprite);
                SetRef(slotSo, "openTraySprite", trayOpenEmptySprite);
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
            zone.anchoredPosition = new Vector2(654f, -204f);
            zone.sizeDelta = new Vector2(508f, 190f);

            // Fire Extinguisher Dock (Horizontal 352x191 sprite matching visualization!)
            RectTransform extDock = CreateRect("FireExtinguisher_Dock", zone);
            extDock.anchoredPosition = new Vector2(-120f, 0f);
            extDock.sizeDelta = new Vector2(248f, 158f);
            AddImage(extDock, new Color(0.10f, 0.06f, 0.06f, 0.42f), roundedSprite, 0.4f).raycastTarget = false;

            CreatePillLabel("ExtLabelPill", "ExtDockLabel", extDock, "Extinguisher — Drag to Fire",
                new Vector2(0f, -58f), new Vector2(236f, 28f), PillDarkAlpha, 13f, TextHintYellow, 10f);

            RectTransform extToolRect = CreateRect("FireExtinguisher_Tool", extDock);
            extToolRect.anchoredPosition = new Vector2(0f, 12f);
            extToolRect.sizeDelta = new Vector2(214f, 118f);
            Image extImg = AddImage(extToolRect, Color.white, extinguisherSprite);
            extImg.preserveAspect = true;
            extImg.raycastTarget = true;
            extToolRect.gameObject.AddComponent<CanvasGroup>();
            extToolRect.gameObject.AddComponent<FireExtinguisherTool>();

            // Trash Can W State (closed.png -> open.png on hover!)
            RectTransform trashRect = CreateRect("TrashCan_DropZone", zone);
            trashRect.anchoredPosition = new Vector2(130f, 0f);
            trashRect.sizeDelta = new Vector2(224f, 166f);
            Image trashHitBg = AddImage(trashRect, new Color(0.08f, 0.07f, 0.07f, 0.42f), roundedSprite, 0.4f);
            trashHitBg.raycastTarget = true;

            RectTransform trashArt = CreateRect("TrashCan_Art", trashRect);
            trashArt.anchoredPosition = new Vector2(0f, 14f);
            trashArt.sizeDelta = new Vector2(104f, 124f);
            Image trashImg = AddImage(trashArt, Color.white, trashClosedSprite);
            trashImg.preserveAspect = true;
            trashImg.raycastTarget = false;

            CreatePillLabel("TrashLabelPill", "TrashLabel", trashRect, "Trash Can (Drop Burnt)",
                new Vector2(0f, -62f), new Vector2(204f, 28f), PillDarkAlpha, 13f, TextLight, 10f);

            TrashCanDropZone trashDrop = trashRect.gameObject.AddComponent<TrashCanDropZone>();
            var trashSo = new SerializedObject(trashDrop);
            SetRef(trashSo, "trashImage", trashImg);
            SetRef(trashSo, "closedSprite", trashClosedSprite);
            SetRef(trashSo, "openSprite", trashOpenSprite);
            trashSo.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }

        private static RectTransform BuildDishNotificationBanner(RectTransform parent)
        {
            RectTransform banner = CreateRect("DishNotificationBanner", parent);
            banner.anchoredPosition = new Vector2(0f, 444f);
            banner.sizeDelta = new Vector2(760f, 48f);

            AddImage(banner, new Color(0.06f, 0.05f, 0.05f, 0.94f), roundedSprite, 0.4f).raycastTarget = false;

            CanvasGroup cg = banner.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            TMP_Text tmp = CreateLabel("NotificationText", banner, string.Empty,
                Vector2.zero, new Vector2(720f, 40f), 21f, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, 14f);

            DishNotificationUI notification = banner.gameObject.AddComponent<DishNotificationUI>();
            var so = new SerializedObject(notification);
            SetRef(so, "notificationText", tmp);
            so.FindProperty("displayDuration").floatValue = 3.0f;
            so.FindProperty("fadeDuration").floatValue = 0.25f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        private static Image CreateUtensilRecommendGlow(RectTransform buttonRect, Color glowColor)
        {
            RectTransform glow = CreateRect("RecommendGlow", buttonRect);
            Stretch(glow);
            glow.offsetMin = new Vector2(-6f, -6f);
            glow.offsetMax = new Vector2(6f, 6f);
            glow.SetAsFirstSibling();
            Image glowImg = AddImage(glow, glowColor, roundedSprite, 0.35f);
            glowImg.raycastTarget = false;
            glowImg.enabled = false;
            return glowImg;
        }

        private static RectTransform BuildGuidedTutorialOverlay(
            RectTransform layoutRoot,
            GameObject systemsGo,
            LevelGameManager manager,
            PrepStation prepStation,
            FoodDisplayTray displayTray,
            RectTransform ingredientTray,
            RectTransform boardRect,
            RectTransform chopBtnRect,
            RectTransform mixBtnRect,
            RectTransform ladleBtnRect,
            RectTransform spatulaBtnRect,
            RectTransform stoveZone,
            RectTransform displayZone,
            RectTransform customerSlot1Rect)
        {
            // Floating Pulsing Highlight Frame (tracks target zone in world/canvas space)
            RectTransform highlightRect = CreateRect("TutorialHighlightRing", layoutRoot);
            highlightRect.sizeDelta = new Vector2(200f, 120f);
            Image highlightImg = AddImage(highlightRect, new Color(0.99f, 0.85f, 0.22f, 0.26f), roundedSprite, 0.35f);
            highlightImg.raycastTarget = false;
            highlightRect.gameObject.SetActive(false);

            // Non-blocking Guided Tutorial Instruction Banner near Top HUD
            RectTransform banner = CreateRect("GuidedTutorialBanner", layoutRoot);
            banner.anchoredPosition = new Vector2(0f, 434f);
            banner.sizeDelta = new Vector2(1040f, 68f);
            AddImage(banner, Hex("3E1E0E"), roundedSprite, 0.4f).raycastTarget = false;

            TMP_Text stepBadgeTmp = CreatePillLabel("StepBadgePill", "StepBadgeText", banner, "Step 1/6 (Timer Paused)",
                new Vector2(-364f, 0f), new Vector2(268f, 46f), Hex("D97706"), 17f, TextLight, 11f);
            TMP_Text instrTmp = CreateLabel("InstructionText", banner,
                "Drag an ingredient onto the Cutting Board!",
                new Vector2(36f, 0f), new Vector2(512f, 56f), 18f, FontStyles.Bold, TextWarmCream, TextAlignmentOptions.Center, 11f);
            Button skipBtn = CreateActionButton("SkipTutorialButton", banner, "Skip Guide",
                new Vector2(416f, 0f), new Vector2(168f, 46f), Hex("6E231E"), 17f);

            banner.gameObject.SetActive(false);

            GuidedTutorialController ctrl = systemsGo.AddComponent<GuidedTutorialController>();
            var gSo = new SerializedObject(ctrl);
            SetRef(gSo, "tutorialBannerRoot", banner.gameObject);
            SetRef(gSo, "stepBadgeText", stepBadgeTmp);
            SetRef(gSo, "instructionText", instrTmp);
            SetRef(gSo, "skipTutorialButton", skipBtn);
            SetRef(gSo, "highlightRingRect", highlightRect);
            SetRef(gSo, "highlightRingImage", highlightImg);
            SetRef(gSo, "levelManager", manager);
            SetRef(gSo, "prepStation", prepStation);
            SetRef(gSo, "displayTray", displayTray);
            SetRef(gSo, "ingredientTrayRect", ingredientTray);
            SetRef(gSo, "cuttingBoardRect", boardRect);
            SetRef(gSo, "chopButtonRect", chopBtnRect);
            SetRef(gSo, "mixButtonRect", mixBtnRect);
            SetRef(gSo, "ladleButtonRect", ladleBtnRect);
            SetRef(gSo, "spatulaButtonRect", spatulaBtnRect);
            SetRef(gSo, "stoveAreaRect", stoveZone);
            SetRef(gSo, "displayTrayRect", displayZone);
            SetRef(gSo, "customerSlot1Rect", customerSlot1Rect);
            gSo.ApplyModifiedPropertiesWithoutUndo();

            var mgrSo = new SerializedObject(manager);
            SetRef(mgrSo, "guidedTutorial", ctrl);
            mgrSo.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        private static void BuildHelpGuideModal(
            RectTransform parent,
            LevelGameManager manager,
            out RectTransform cardRect,
            out GridLayoutGroup stepsGrid)
        {
            RectTransform overlay = CreateRect("HelpGuideModal", parent);
            Stretch(overlay);
            AddImage(overlay, new Color(0.04f, 0.03f, 0.03f, 0.86f), null).raycastTarget = true;

            cardRect = CreateRect("TutorialCard", overlay);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(1420f, 720f);
            AddImage(cardRect, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = false;

            RectTransform innerCard = CreateRect("InnerPanel", cardRect);
            Stretch(innerCard);
            innerCard.offsetMin = new Vector2(12f, 12f);
            innerCard.offsetMax = new Vector2(-12f, -12f);
            AddImage(innerCard, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            CreateAnchoredLabel("TutorialHeader", innerCard,
                new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.97f),
                "Carinderia Kitchen Guide (Shift Paused)",
                34f, FontStyles.Bold, Hex("FBBF24"), 20f);
            CreateAnchoredLabel("TutorialSubtitle", innerCard,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.865f),
                "Review the drag-and-drop cooking loop anytime — gameplay resumes when you close this guide.",
                20f, FontStyles.Normal, TextWarmCream, 13f);

            RectTransform stepsContainer = CreateRect("TutorialStepsGrid", innerCard);
            stepsContainer.anchoredPosition = new Vector2(0f, -14f);
            stepsContainer.sizeDelta = new Vector2(1340f, 412f);
            stepsGrid = stepsContainer.gameObject.AddComponent<GridLayoutGroup>();
            stepsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            stepsGrid.constraintCount = 4;
            stepsGrid.cellSize = new Vector2(318f, 404f);
            stepsGrid.spacing = new Vector2(18f, 18f);
            stepsGrid.childAlignment = TextAnchor.MiddleCenter;

            Sprite customerDemoSprite = LoadLargestSprite(CustomerAssets[0].path);

            CreateTutorialStepCard(stepsContainer, "Step1_Prep", "1. Prep Ingredients",
                tomatoSprite, prepBoardSprite,
                "Drag 1–3 ingredients onto the Wooden Board, then click Chop & Mix inside the green sweet spot!",
                Hex("E07A5F"));

            CreateTutorialStepCard(stepsContainer, "Step2_Cook", "2. Utensil & Stove",
                ladleSprite ?? potCookedSprite, stoveSprite,
                "Pick Ladle (Pot) or Spatula (Pan), drag Ready Mix onto a glowing burner, and Plate within 3s!",
                Hex("F59E0B"));

            CreateTutorialStepCard(stepsContainer, "Step3_Serve", "3. Drinks & Serve",
                dispenserTanksSprite, customerDemoSprite ?? trayWithDishSprite,
                "Drag an empty cup onto the Drink Dispenser, then drag meals & drinks onto matching customers.",
                Hex("38BDF8"));

            CreateTutorialStepCard(stepsContainer, "Step4_Safety", "4. Fire Safety",
                extinguisherSprite, trashClosedSprite,
                "Burner catches fire? Drag the Fire Extinguisher onto flames, then drag burnt food into the Trash Can.",
                Hex("EF4444"));

            RectTransform btnRow = CreateRect("TutorialFooter", innerCard);
            btnRow.anchorMin = new Vector2(0.25f, 0.03f);
            btnRow.anchorMax = new Vector2(0.75f, 0.14f);
            btnRow.offsetMin = Vector2.zero;
            btnRow.offsetMax = Vector2.zero;

            Button resumeBtn = CreateActionButton("CloseHelpButton", btnRow, "Resume Shift",
                Vector2.zero, new Vector2(300f, 60f), ServeButtonColor, 26f);

            var so = new SerializedObject(manager);
            SetRef(so, "helpModalRoot", overlay.gameObject);
            SetRef(so, "closeHelpButton", resumeBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlay.gameObject.SetActive(false);
        }

        private static void BuildPauseMenuModal(RectTransform parent, LevelGameManager manager)
        {
            RectTransform overlay = CreateRect("PauseMenuModal", parent);
            Stretch(overlay);
            AddImage(overlay, new Color(0.04f, 0.03f, 0.04f, 0.84f), null).raycastTarget = true;

            RectTransform card = CreateRect("PauseCard", overlay);
            card.sizeDelta = new Vector2(600f, 610f);
            AddImage(card, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = false;

            RectTransform inner = CreateRect("InnerSurface", card);
            Stretch(inner);
            inner.offsetMin = new Vector2(10f, 10f);
            inner.offsetMax = new Vector2(-10f, -10f);
            AddImage(inner, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            CreateAnchoredLabel("PauseTitle", inner, new Vector2(0.08f, 0.85f), new Vector2(0.92f, 0.96f),
                "Shift Paused", 36f, FontStyles.Bold, Hex("FBBF24"), 22f);
            TMP_Text subTmp = CreateAnchoredLabel("PauseSubtitle", inner, new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.85f),
                "Day 1/3: Opening Shift", 19f, FontStyles.Normal, TextWarmCream, 12f);

            // In-game Audio Controls (Music & SFX volume sliders)
            RectTransform audioPanel = CreateRect("PauseAudioPanel", inner);
            audioPanel.anchorMin = new Vector2(0.08f, 0.51f);
            audioPanel.anchorMax = new Vector2(0.92f, 0.75f);
            audioPanel.offsetMin = Vector2.zero;
            audioPanel.offsetMax = Vector2.zero;
            AddImage(audioPanel, new Color(0.11f, 0.07f, 0.05f, 0.88f), roundedSprite, 0.4f).raycastTarget = false;

            VerticalLayoutGroup audioVlg = audioPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            audioVlg.padding = new RectOffset(18, 18, 12, 12);
            audioVlg.spacing = 10f;
            audioVlg.childAlignment = TextAnchor.MiddleCenter;
            audioVlg.childControlWidth = true;
            audioVlg.childControlHeight = true;
            audioVlg.childForceExpandWidth = true;
            audioVlg.childForceExpandHeight = true;

            (Slider musicSlider, TMP_Text musicVal) = CreatePauseVolumeRow(audioPanel, "MusicVolumeRow", "Music", "75%");
            (Slider sfxSlider, TMP_Text sfxVal) = CreatePauseVolumeRow(audioPanel, "SfxVolumeRow", "SFX", "90%");

            SettingsPanel pauseAudioSettings = audioPanel.gameObject.AddComponent<SettingsPanel>();
            var audioSo = new SerializedObject(pauseAudioSettings);
            SetRef(audioSo, "musicVolumeSlider", musicSlider);
            SetRef(audioSo, "musicVolumeLabel", musicVal);
            SetRef(audioSo, "sfxVolumeSlider", sfxSlider);
            SetRef(audioSo, "sfxVolumeLabel", sfxVal);
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            RectTransform col = CreateRect("PauseButtonsColumn", inner);
            col.anchorMin = new Vector2(0.14f, 0.06f);
            col.anchorMax = new Vector2(0.86f, 0.47f);
            col.offsetMin = Vector2.zero;
            col.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            Button resumeBtn = CreateActionButton("PauseResumeButton", col, "Resume Shift", Vector2.zero, new Vector2(360f, 62f), ServeButtonColor, 24f);
            Button restartBtn = CreateActionButton("PauseRestartButton", col, "Restart Shift", Vector2.zero, new Vector2(360f, 62f), Hex("D97706"), 24f);
            Button menuBtn = CreateActionButton("PauseMainMenuButton", col, "Exit to Main Menu", Vector2.zero, new Vector2(360f, 62f), Hex("6E231E"), 23f);

            var so = new SerializedObject(manager);
            SetRef(so, "pauseModalRoot", overlay.gameObject);
            SetRef(so, "pauseShiftSubtitleText", subTmp);
            SetRef(so, "resumeFromPauseButton", resumeBtn);
            SetRef(so, "restartFromPauseButton", restartBtn);
            SetRef(so, "mainMenuFromPauseButton", menuBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlay.gameObject.SetActive(false);
        }

        private static (Slider slider, TMP_Text valueLabel) CreatePauseVolumeRow(
            Transform parent,
            string rowName,
            string title,
            string initialPercent)
        {
            RectTransform row = CreateRect(rowName, parent);
            HorizontalLayoutGroup hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            RectTransform lblRect = CreateRect("Label", row);
            LayoutElement lblLe = lblRect.gameObject.AddComponent<LayoutElement>();
            lblLe.preferredWidth = 96f;
            lblLe.preferredHeight = 40f;
            TMP_Text lblTmp = CreateAnchoredLabel("LabelText", lblRect, Vector2.zero, Vector2.one,
                title, 20f, FontStyles.Bold, TextWarmCream, 14f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;

            RectTransform sliderRect = CreateRect("Slider", row);
            LayoutElement sliderLe = sliderRect.gameObject.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1f;
            sliderLe.minWidth = 220f;
            sliderLe.preferredHeight = 36f;

            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.85f;
            slider.direction = Slider.Direction.LeftToRight;

            RectTransform track = CreateRect("Background", sliderRect);
            track.anchorMin = new Vector2(0f, 0.34f);
            track.anchorMax = new Vector2(1f, 0.66f);
            track.offsetMin = Vector2.zero;
            track.offsetMax = Vector2.zero;
            AddImage(track, Hex("18100C"), roundedSprite, 0.5f).raycastTarget = true;

            RectTransform fillArea = CreateRect("Fill Area", sliderRect);
            fillArea.anchorMin = new Vector2(0f, 0.34f);
            fillArea.anchorMax = new Vector2(1f, 0.66f);
            fillArea.offsetMin = new Vector2(4f, 0f);
            fillArea.offsetMax = new Vector2(-4f, 0f);

            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            Image fillImg = AddImage(fill, Hex("F59E0B"), roundedSprite, 0.5f);
            fillImg.raycastTarget = false;

            RectTransform handleArea = CreateRect("Handle Slide Area", sliderRect);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);

            RectTransform handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(28f, 28f);
            Image handleImg = AddImage(handle, Hex("FEF3C7"), knobSprite ?? roundedSprite);
            handleImg.raycastTarget = true;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;

            RectTransform valRect = CreateRect("Value", row);
            LayoutElement valLe = valRect.gameObject.AddComponent<LayoutElement>();
            valLe.preferredWidth = 68f;
            valLe.preferredHeight = 40f;
            TMP_Text valTmp = CreateAnchoredLabel("ValueText", valRect, Vector2.zero, Vector2.one,
                initialPercent, 19f, FontStyles.Bold, TextHintYellow, 13f);
            valTmp.alignment = TextAlignmentOptions.MidlineRight;
            valTmp.raycastTarget = false;

            return (slider, valTmp);
        }

        private static void CreateTutorialStepCard(
            Transform parent,
            string name,
            string title,
            Sprite leftIcon,
            Sprite rightIcon,
            string body,
            Color accentColor)
        {
            RectTransform card = CreateRect(name, parent);
            AddImage(card, new Color(0.11f, 0.07f, 0.05f, 0.96f), roundedSprite, 0.45f).raycastTarget = false;

            RectTransform topBar = CreateRect("TopBar", card);
            topBar.anchorMin = new Vector2(0.05f, 0.85f);
            topBar.anchorMax = new Vector2(0.95f, 0.95f);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;
            AddImage(topBar, accentColor, roundedSprite, 0.5f).raycastTarget = false;

            CreateAnchoredLabel("Title", topBar, Vector2.zero, Vector2.one, title, 18f, FontStyles.Bold, TextLight, 12f);

            RectTransform diagramBox = CreateRect("DiagramBox", card);
            diagramBox.anchorMin = new Vector2(0.06f, 0.44f);
            diagramBox.anchorMax = new Vector2(0.94f, 0.82f);
            diagramBox.offsetMin = Vector2.zero;
            diagramBox.offsetMax = Vector2.zero;
            AddImage(diagramBox, new Color(0.98f, 0.94f, 0.87f, 0.96f), roundedSprite, 0.45f).raycastTarget = false;

            if (leftIcon != null)
            {
                RectTransform leftRect = CreateRect("LeftIcon", diagramBox);
                leftRect.anchorMin = new Vector2(0.04f, 0.12f);
                leftRect.anchorMax = new Vector2(0.42f, 0.88f);
                leftRect.offsetMin = Vector2.zero;
                leftRect.offsetMax = Vector2.zero;
                Image lImg = AddImage(leftRect, Color.white, leftIcon);
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
            }

            CreateAnchoredLabel("Arrow", diagramBox,
                new Vector2(0.41f, 0.20f), new Vector2(0.59f, 0.80f),
                "->", 26f, FontStyles.Bold, Hex("5A2D14"), 16f);

            if (rightIcon != null)
            {
                RectTransform rightRect = CreateRect("RightIcon", diagramBox);
                rightRect.anchorMin = new Vector2(0.58f, 0.12f);
                rightRect.anchorMax = new Vector2(0.96f, 0.88f);
                rightRect.offsetMin = Vector2.zero;
                rightRect.offsetMax = Vector2.zero;
                Image rImg = AddImage(rightRect, Color.white, rightIcon);
                rImg.preserveAspect = true;
                rImg.raycastTarget = false;
            }

            CreateAnchoredLabel("Body", card,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.41f),
                body, 17f, FontStyles.Normal, TextWarmCream, 12f);
        }

        private static void BuildSummaryModal(RectTransform parent, LevelGameManager manager)
        {
            RectTransform overlay = CreateRect("EndOfLevelSummaryModal", parent);
            Stretch(overlay);
            AddImage(overlay, new Color(0.04f, 0.04f, 0.05f, 0.86f), null).raycastTarget = true;

            RectTransform card = CreateRect("SummaryCard", overlay);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(920f, 740f);
            AddImage(card, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = false;

            RectTransform innerCard = CreateRect("InnerSurface", card);
            Stretch(innerCard);
            innerCard.offsetMin = new Vector2(10f, 10f);
            innerCard.offsetMax = new Vector2(-10f, -10f);
            AddImage(innerCard, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            // Hero Header Badge + Title + Concise Subtitle
            RectTransform heroIconRect = CreateRect("HeroBadgeIcon", innerCard);
            heroIconRect.anchoredPosition = new Vector2(-346f, 304f);
            heroIconRect.sizeDelta = new Vector2(72f, 72f);
            Image heroImg = AddImage(heroIconRect, Color.white, trayWithDishSprite);
            heroImg.preserveAspect = true;
            heroImg.raycastTarget = false;

            TMP_Text titleTmp = CreateLabel("SummaryTitleText", innerCard, "Shift Complete!",
                new Vector2(28f, 314f), new Vector2(740f, 48f), 36f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Center, 22f);
            TMP_Text breakdownTmp = CreateLabel("SummaryBreakdownText", innerCard, string.Empty,
                new Vector2(28f, 274f), new Vector2(760f, 30f), 19f, FontStyles.Normal, TextWarmCream, TextAlignmentOptions.Center, 12f);

            // Visual 3-Star Rating Row
            RectTransform starRow = CreateRect("SummaryStarRow", card);
            starRow.anchoredPosition = new Vector2(0f, 206f);
            starRow.sizeDelta = new Vector2(330f, 86f);
            HorizontalLayoutGroup starHlg = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starHlg.spacing = 24f;
            starHlg.childAlignment = TextAnchor.MiddleCenter;
            starHlg.childControlWidth = false;
            starHlg.childControlHeight = false;

            Image[] starImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform st = CreateRect($"Star_{i + 1}", starRow);
                st.sizeDelta = new Vector2(78f, 78f);
                starImgs[i] = AddImage(st, Color.white, starFilledSprite ?? knobSprite);
                starImgs[i].preserveAspect = true;
                starImgs[i].raycastTarget = false;
            }

            // 4 Visual Stat Badge Cards Row
            RectTransform badgesRow = CreateRect("StatBadgesRow", innerCard);
            badgesRow.anchoredPosition = new Vector2(0f, 102f);
            badgesRow.sizeDelta = new Vector2(860f, 104f);
            HorizontalLayoutGroup badgesHlg = badgesRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            badgesHlg.spacing = 14f;
            badgesHlg.childAlignment = TextAnchor.MiddleCenter;
            badgesHlg.childControlWidth = true;
            badgesHlg.childControlHeight = true;
            badgesHlg.childForceExpandWidth = true;
            badgesHlg.childForceExpandHeight = true;

            Sprite customerIcon = LoadLargestSprite(CustomerAssets[0].path);
            TMP_Text[] badgeValues = new TMP_Text[4];
            badgeValues[0] = CreateSummaryStatBadge(badgesRow, "Badge_Customers", "Customers", customerIcon ?? trayWithDishSprite, Hex("38BDF8"));
            badgeValues[1] = CreateSummaryStatBadge(badgesRow, "Badge_Dishes", "Menu Output", potCookedSprite, Hex("E07A5F"));
            badgeValues[2] = CreateSummaryStatBadge(badgesRow, "Badge_Combos", "Combos & Streak", drinkBerrySprite ?? dispenserTanksSprite, Hex("A855F7"));
            badgeValues[3] = CreateSummaryStatBadge(badgesRow, "Badge_Reward", "Shift Reward", panCookedSprite ?? knobSprite, Hex("D97706"));

            // 2 Visual Progress Meters (Dish Quality & Customer Satisfaction)
            RectTransform metersRow = CreateRect("VisualMetersRow", innerCard);
            metersRow.anchoredPosition = new Vector2(0f, 16f);
            metersRow.sizeDelta = new Vector2(840f, 54f);
            CreateSummaryProgressMeter(metersRow, "QualityMeter", new Vector2(-216f, 0f), "Dish Quality", Hex("22C55E"),
                out Image qualityFill, out TMP_Text qualityValTmp);
            CreateSummaryProgressMeter(metersRow, "SatisfactionMeter", new Vector2(216f, 0f), "Customer Satisfaction", Hex("38BDF8"),
                out Image satisfactionFill, out TMP_Text satisfactionValTmp);

            // Final Score Pill
            TMP_Text finalScoreTmp = CreatePillLabel("ScoreSummaryPill", "SummaryFinalScoreText", innerCard, "Final Score: 0",
                new Vector2(0f, -46f), new Vector2(440f, 50f), new Color(0.11f, 0.22f, 0.14f, 0.95f), 28f, Hex("4ADE80"), 18f);

            // Endless Mode 8-Character Name Entry & Save to Browser Leaderboard Section
            RectTransform lbSubmit = CreateRect("LeaderboardSubmitSection", card);
            lbSubmit.anchoredPosition = new Vector2(0f, -150f);
            lbSubmit.sizeDelta = new Vector2(810f, 134f);
            AddImage(lbSubmit, new Color(0.14f, 0.08f, 0.05f, 0.94f), roundedSprite, 0.45f).raycastTarget = false;

            CreateLabel("LeaderboardPromptLabel", lbSubmit, "Enter 8-Char Name for Endless Leaderboard:",
                new Vector2(0f, 45f), new Vector2(760f, 28f), 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);

            RectTransform inputRow = CreateRect("InputRow", lbSubmit);
            inputRow.anchoredPosition = new Vector2(0f, 2f);
            inputRow.sizeDelta = new Vector2(740f, 52f);

            TMP_InputField nameInput = CreateNameInputField("LeaderboardNameInput", inputRow,
                new Vector2(-130f, 0f), new Vector2(280f, 46f));

            Button mobileKeyBtn = CreateActionButton("MobileKeyboardButton", inputRow, "Edit",
                new Vector2(68f, 0f), new Vector2(90f, 46f), Hex("475569"), 16f);
            mobileKeyBtn.gameObject.SetActive(false);

            Button saveScoreBtn = CreateActionButton("SubmitLeaderboardButton", inputRow, "Save Score",
                new Vector2(226f, 0f), new Vector2(204f, 48f), Hex("D97706"), 20f);
            TMP_Text saveScoreBtnLbl = saveScoreBtn.GetComponentInChildren<TMP_Text>();

            TMP_Text submitStatusTmp = CreateLabel("LeaderboardSubmitStatusText", lbSubmit, "Enter up to 8 characters — saves in your browser!",
                new Vector2(0f, -44f), new Vector2(760f, 26f), 16f, FontStyles.Italic, TextWarmCream, TextAlignmentOptions.Center, 11f);

            RectTransform buttonsRow = CreateRect("SummaryButtonsRow", card);
            buttonsRow.anchoredPosition = new Vector2(0f, -278f);
            buttonsRow.sizeDelta = new Vector2(860f, 64f);
            HorizontalLayoutGroup rowHlg = buttonsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 16f;
            rowHlg.childAlignment = TextAnchor.MiddleCenter;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = false;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            Button upgradeBtn = CreateActionButton("UpgradeShopButton", buttonsRow, "Upgrade Shop (+1 SP)",
                Vector2.zero, new Vector2(260f, 58f), Hex("D97706"), 19f);
            TMP_Text upgradeBtnLbl = upgradeBtn.GetComponentInChildren<TMP_Text>();

            Button viewLbBtn = CreateActionButton("ViewLeaderboardButton", buttonsRow, "Leaderboard",
                Vector2.zero, new Vector2(200f, 58f), Hex("0284C7"), 20f);

            Button playAgainBtn = CreateActionButton("PlayAgainButton", buttonsRow, "Play Again",
                Vector2.zero, new Vector2(200f, 58f), ServeButtonColor, 20f);
            TMP_Text playAgainLbl = playAgainBtn.GetComponentInChildren<TMP_Text>();

            Button mainMenuBtn = CreateActionButton("SummaryMainMenuButton", buttonsRow, "Main Menu",
                Vector2.zero, new Vector2(190f, 58f), Hex("6E231E"), 20f);

            // Overlay Top-10 Endless Leaderboard Modal (reused from LeaderboardSceneBuilder)
            LeaderboardPanelController leaderboardOverlay = LeaderboardSceneBuilder.BuildLeaderboardPanel(parent, isOverlay: true, out _);

            var so = new SerializedObject(manager);
            SetRef(so, "summaryModalRoot", overlay.gameObject);
            SetRef(so, "summaryHeroIcon", heroImg);
            SetRef(so, "victoryHeroSprite", trayWithDishSprite);
            SetRef(so, "retryHeroSprite", trashClosedSprite);
            SetRef(so, "summaryTitleText", titleTmp);
            SetRef(so, "summaryBreakdownText", breakdownTmp);
            SetRef(so, "starFilledSprite", starFilledSprite);
            SetRef(so, "starEmptySprite", starEmptySprite);
            SerializedProperty starsProp = so.FindProperty("summaryStarImages");
            starsProp.arraySize = starImgs.Length;
            for (int i = 0; i < starImgs.Length; i++) starsProp.GetArrayElementAtIndex(i).objectReferenceValue = starImgs[i];

            SerializedProperty badgesProp = so.FindProperty("summaryBadgeValues");
            badgesProp.arraySize = badgeValues.Length;
            for (int i = 0; i < badgeValues.Length; i++) badgesProp.GetArrayElementAtIndex(i).objectReferenceValue = badgeValues[i];

            SetRef(so, "summaryQualityMeterFill", qualityFill);
            SetRef(so, "summaryQualityValueText", qualityValTmp);
            SetRef(so, "summarySatisfactionMeterFill", satisfactionFill);
            SetRef(so, "summarySatisfactionValueText", satisfactionValTmp);
            SetRef(so, "summaryFinalScoreText", finalScoreTmp);
            SetRef(so, "upgradeShopButton", upgradeBtn);
            SetRef(so, "upgradeShopButtonLabel", upgradeBtnLbl);
            SetRef(so, "playAgainButton", playAgainBtn);
            SetRef(so, "playAgainButtonLabel", playAgainLbl);
            SetRef(so, "summaryMainMenuButton", mainMenuBtn);
            SetRef(so, "leaderboardSubmitGroup", lbSubmit.gameObject);
            SetRef(so, "leaderboardNameInput", nameInput);
            SetRef(so, "mobileNamePromptButton", mobileKeyBtn);
            SetRef(so, "submitLeaderboardButton", saveScoreBtn);
            SetRef(so, "submitLeaderboardButtonLabel", saveScoreBtnLbl);
            SetRef(so, "leaderboardSubmitStatusText", submitStatusTmp);
            SetRef(so, "viewLeaderboardButton", viewLbBtn);
            SetRef(so, "endlessLeaderboardPanel", leaderboardOverlay);
            so.ApplyModifiedPropertiesWithoutUndo();

            overlay.gameObject.SetActive(false);
        }

        private static TMP_Text CreateSummaryStatBadge(Transform parent, string name, string header, Sprite icon, Color accent)
        {
            RectTransform card = CreateRect(name, parent);
            AddImage(card, new Color(0.14f, 0.08f, 0.05f, 0.96f), roundedSprite, 0.45f).raycastTarget = false;

            RectTransform topStrip = CreateRect("AccentHeader", card);
            topStrip.anchorMin = new Vector2(0.06f, 0.72f);
            topStrip.anchorMax = new Vector2(0.94f, 0.94f);
            topStrip.offsetMin = Vector2.zero;
            topStrip.offsetMax = Vector2.zero;
            AddImage(topStrip, accent, roundedSprite, 0.5f).raycastTarget = false;
            CreateAnchoredLabel("HeaderText", topStrip, Vector2.zero, Vector2.one, header, 14f, FontStyles.Bold, TextLight, 10f);

            if (icon != null)
            {
                RectTransform iconHolder = CreateRect("Icon", card);
                iconHolder.anchorMin = new Vector2(0.06f, 0.12f);
                iconHolder.anchorMax = new Vector2(0.36f, 0.66f);
                iconHolder.offsetMin = Vector2.zero;
                iconHolder.offsetMax = Vector2.zero;
                Image ic = AddImage(iconHolder, Color.white, icon);
                ic.preserveAspect = true;
                ic.raycastTarget = false;
            }

            return CreateAnchoredLabel("ValueText", card, new Vector2(0.38f, 0.08f), new Vector2(0.95f, 0.68f),
                "0", 17f, FontStyles.Bold, TextLight, 11f);
        }

        private static void CreateSummaryProgressMeter(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            string label,
            Color barColor,
            out Image fillImage,
            out TMP_Text valueText)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchoredPosition = anchoredPos;
            root.sizeDelta = new Vector2(396f, 48f);

            CreateLabel("Title", root, label, new Vector2(-60f, 13f), new Vector2(260f, 22f),
                16f, FontStyles.Bold, TextWarmCream, TextAlignmentOptions.Left, 11f);
            valueText = CreateLabel("Pct", root, "0%", new Vector2(146f, 13f), new Vector2(96f, 22f),
                17f, FontStyles.Bold, barColor, TextAlignmentOptions.Right, 11f);

            RectTransform track = CreateRect("Track", root);
            track.anchoredPosition = new Vector2(0f, -12f);
            track.sizeDelta = new Vector2(390f, 18f);
            AddImage(track, Hex("141211"), timerFillPillSprite).raycastTarget = false;

            RectTransform fill = CreateRect("Fill", track);
            Stretch(fill);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
            fillImage = AddImage(fill, barColor, timerFillPillSprite);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
        }

        private static TMP_InputField CreateNameInputField(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchoredPosition = anchoredPos;
            root.sizeDelta = sizeDelta;
            Image bgImg = AddImage(root, Hex("FFF6E5"), roundedSprite, 0.5f);
            bgImg.raycastTarget = true;

            RectTransform textArea = CreateRect("TextArea", root);
            Stretch(textArea);
            textArea.offsetMin = new Vector2(12f, 4f);
            textArea.offsetMax = new Vector2(-12f, -4f);
            textArea.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = CreateAnchoredLabel("Placeholder", textArea, Vector2.zero, Vector2.one,
                "8-Char Name...", 20f, FontStyles.Italic, new Color(0.45f, 0.36f, 0.28f, 0.65f), 12f);
            TMP_Text textComponent = CreateAnchoredLabel("Text", textArea, Vector2.zero, Vector2.one,
                string.Empty, 22f, FontStyles.Bold, TextDark, 14f);
            ((TextMeshProUGUI)textComponent).textWrappingMode = TextWrappingModes.NoWrap;

            TMP_InputField inputField = root.gameObject.AddComponent<TMP_InputField>();
            inputField.targetGraphic = bgImg;
            inputField.textViewport = textArea;
            inputField.textComponent = (TextMeshProUGUI)textComponent;
            inputField.placeholder = placeholder;
            inputField.characterLimit = LeaderboardSystem.MaxNameLength;
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.caretColor = Hex("5A2D14");
            inputField.selectionColor = new Color(0.96f, 0.65f, 0.14f, 0.4f);
            inputField.text = "Chef";
            return inputField;
        }

        private static TMP_Text CreateAnchoredLabel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            float minFontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minFontSize;
            tmp.fontSizeMax = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
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

        private static Button CreateSpriteButton(
            string name,
            Transform parent,
            Sprite sprite,
            string text,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2 labelOffset,
            Vector2 labelSize,
            float fontSize,
            Color textColor)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            Image img = AddImage(rect, Color.white, sprite);
            img.preserveAspect = false;
            img.raycastTarget = true;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            CreateLabel("Text", rect, text, labelOffset, labelSize,
                fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center, Mathf.Max(10f, fontSize - 5f));
            return btn;
        }

        private static void CreateUtensilIconBadge(string boxName, Transform parent, Sprite iconSprite)
        {
            RectTransform box = CreateRect(boxName, parent);
            box.anchoredPosition = new Vector2(-58f, 0f);
            box.sizeDelta = new Vector2(64f, 64f);
            AddImage(box, Hex("111827"), roundedSprite, 0.4f).raycastTarget = false;

            RectTransform iconRect = CreateRect("Icon", box);
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image iconImg = AddImage(iconRect, Color.white, iconSprite);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
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
                fontSize, FontStyles.Bold, TextLight, TextAlignmentOptions.Center, Mathf.Max(10f, fontSize - 5f));
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
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                     ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath)
                     ?? TMP_Settings.defaultFontAsset;

            // Generated sprites (Carinderia environment + UI.svg + Kitchen Tray.svg)
            string gen = CarinderiaAssetGenerator.GeneratedDir;
            wallBackdropSprite = LoadLargestSprite($"{gen}/Carinderia_WallBackdrop.png");
            woodTableSprite = LoadLargestSprite($"{gen}/Carinderia_WoodenTable.png");
            plasticMatSprite = LoadLargestSprite($"{gen}/Carinderia_PlasticMat.png");
            scorePillSprite = LoadLargestSprite($"{gen}/UI_ScorePill.png");
            timerBarBgSprite = LoadLargestSprite($"{gen}/UI_TimerBarBg.png");
            timerTrackSprite = LoadLargestSprite($"{gen}/UI_TimerTrackSlot.png");
            timerFillPillSprite = LoadLargestSprite($"{gen}/UI_TimerFillPill.png");
            patienceFillPillSprite = LoadLargestSprite($"{gen}/UI_PatienceFillPill.png");
            starFilledSprite = LoadLargestSprite($"{gen}/UI_StarFilled.png");
            starEmptySprite = LoadLargestSprite($"{gen}/UI_StarEmpty.png");
            btnRestartSprite = LoadLargestSprite($"{gen}/UI_BtnRestart.png");
            btnMenuSprite = LoadLargestSprite($"{gen}/UI_BtnMenu.png");
            prepFrameSprite = LoadLargestSprite($"{gen}/Prep_FramePanel.png");
            prepBoardSprite = LoadLargestSprite($"{gen}/Prep_WoodBoard.png");
            btnChopSprite = LoadLargestSprite($"{gen}/Prep_BtnChop.png");
            btnMixSprite = LoadLargestSprite($"{gen}/Prep_BtnMix.png");
            btnClrSprite = LoadLargestSprite($"{gen}/Prep_BtnClr.png");
            cardLadleSprite = LoadLargestSprite($"{gen}/Prep_CardLadle.png");
            cardSpatulaSprite = LoadLargestSprite($"{gen}/Prep_CardSpatula.png");
            readyDockSprite = LoadLargestSprite($"{gen}/Prep_ReadyDock.png");

            // 2D Kitchen sprites
            stoveSprite = LoadLargestSprite(StovePath);
            orderListSprite = LoadLargestSprite(OrderListPath);
            extinguisherSprite = LoadLargestSprite(ExtinguisherPath);
            dispenserTanksSprite = LoadLargestSprite(DispenserTanksPath);
            drinkBerrySprite = LoadSubSprite(DispenserFilledCupsPath, "2_0") ?? LoadLargestSprite(DispenserFilledCupsPath);
            drinkCalamansiSprite = LoadSubSprite(DispenserFilledCupsPath, "2_1") ?? LoadLargestSprite(DispenserFilledCupsPath);
            emptyCupSprite = LoadLargestSprite(DispenserEmptyCupPath);

            tomatoSprite = LoadLargestSprite(IngTomatoPath);
            cheeseSprite = LoadLargestSprite(IngCheesePath);
            meatSprite = LoadLargestSprite(IngMeatPath);
            mushroomSprite = LoadLargestSprite(IngMushroomPath);

            onionSprite = LoadLargestSprite(IngOnionPath);
            garlicSprite = LoadLargestSprite(IngGarlicPath);
            greensSprite = LoadLargestSprite(IngGreensPath);
            potatoSprite = LoadLargestSprite(IngPotatoPath);

            cabbageSprite = LoadLargestSprite(IngCabbagePath);
            carrotsSprite = LoadLargestSprite(IngCarrotsPath);
            porkSprite = LoadLargestSprite(IngPorkPath);
            pepperSprite = LoadLargestSprite(IngPepperPath);

            potIdleSprite = LoadLargestSprite(PotIdlePath);
            potCookedSprite = LoadLargestSprite(PotCookedPath);
            potBurnedSprite = LoadLargestSprite(PotBurnedPath);

            panIdleSprite = LoadLargestSprite(PanIdlePath);
            panCookedSprite = LoadLargestSprite(PanCookedPath);
            panBurnedSprite = LoadLargestSprite(PanBurnedPath);

            trayClosedLidSprite = LoadLargestSprite(TrayEmptyLidPath);
            trayOpenEmptySprite = LoadLargestSprite(TrayOpenEmptyPath);
            trayWithDishSprite = LoadLargestSprite(TrayWithDishPath);

            trashClosedSprite = LoadLargestSprite(TrashClosedPath);
            trashOpenSprite = LoadLargestSprite(TrashOpenPath);

            ladleSprite = LoadLargestSprite(SandokLadlePath);
            spatulaSprite = LoadLargestSprite(Spatula2Path);
        }

        /// <summary>
        /// Loads the largest sliced Sprite sub-asset at path (avoids accidental tiny sliver slices like overcooked_0).
        /// </summary>
        private static Sprite LoadLargestSprite(string assetPath)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite best = null;
            float bestArea = -1f;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is Sprite s)
                {
                    float area = s.rect.width * s.rect.height;
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = s;
                    }
                }
            }
            if (best == null)
            {
                Debug.LogWarning($"[GameSceneLayoutBuilder] Could not load Sprite at '{assetPath}'.");
            }
            return best;
        }

        private static Sprite LoadSubSprite(string assetPath, string spriteName)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is Sprite s && s.name == spriteName)
                {
                    return s;
                }
            }
            return null;
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
