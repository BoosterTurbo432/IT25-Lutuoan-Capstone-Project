using System.IO;
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
    /// Builds and registers Assets/Scenes/UpgradeShopScene.unity with 3 active upgrades
    /// and 3 locked silhouette preview cards ("Soon in Full Version").
    /// </summary>
    [InitializeOnLoad]
    public static class UpgradeShopSceneBuilder
    {
        public const string UpgradeShopScenePath = "Assets/Scenes/UpgradeShopScene.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/Fonts/Stay With Me SDF.asset";

        private static Sprite roundedSprite;
        private static Sprite wallBackdropSprite;
        private static Sprite woodTableSprite;
        private static Sprite prepBoardSprite;
        private static Sprite stoveSprite;
        private static Sprite drinkCupsSprite;
        private static Sprite potCookedSprite;
        private static Sprite dispenserTanksSprite;
        private static Sprite trayWithDishSprite;
        private static TMP_FontAsset uiFont;

        static UpgradeShopSceneBuilder()
        {
            EditorApplication.delayCall += EnsureSceneCreated;
        }

        private static void EnsureSceneCreated()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(UpgradeShopScenePath) && File.ReadAllText(UpgradeShopScenePath).Contains("SoonPopupModal"))
            {
                BuildSceneListSetup.AddGameScenes();
                return;
            }

            CreateUpgradeShopSceneInternal(openAfterSave: false);
        }

        [MenuItem("Tools/Upgrade Shop/Build Upgrade Shop Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateUpgradeShopSceneInternal(openAfterSave: true);
        }

        public static void BuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool ok = CreateUpgradeShopSceneInternal(openAfterSave: true);
            AssetDatabase.SaveAssets();
            Debug.Log(ok ? "[UpgradeShopSceneBuilder] BUILD OK" : "[UpgradeShopSceneBuilder] BUILD FAILED");
            if (!ok) EditorApplication.Exit(1);
        }

        private static bool CreateUpgradeShopSceneInternal(bool openAfterSave)
        {
            CarinderiaAssetGenerator.EnsureAllGeneratedAssets();
            LoadResources();

            Scene prevScene = SceneManager.GetActiveScene();
            string prevScenePath = prevScene.path;

            Scene shopScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, openAfterSave ? NewSceneMode.Single : NewSceneMode.Additive);

            EnsureCamera(shopScene);
            EnsureEventSystem(shopScene);
            BuildSceneContents(shopScene);

            bool saved = EditorSceneManager.SaveScene(shopScene, UpgradeShopScenePath);
            if (!openAfterSave && !string.IsNullOrEmpty(prevScenePath) && prevScenePath != UpgradeShopScenePath)
            {
                EditorSceneManager.CloseScene(shopScene, true);
            }

            BuildSceneListSetup.AddGameScenes();
            Debug.Log($"[UpgradeShopSceneBuilder] UpgradeShopScene saved cleanly at {UpgradeShopScenePath}");
            return saved;
        }

        private static void BuildSceneContents(Scene scene)
        {
            GameObject canvasGo = new GameObject("Canvas") { layer = LayerMask.NameToLayer("UI") };
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = (RectTransform)canvasGo.transform;

            // Tiled wall + wooden table background matching Carinderia identity
            RectTransform bgWall = CreateRect("BackgroundWall", canvasRect);
            Stretch(bgWall);
            AddImage(bgWall, Hex("221814"), wallBackdropSprite).raycastTarget = false;

            RectTransform woodFloor = CreateRect("WoodCounterBg", canvasRect);
            woodFloor.anchorMin = new Vector2(0f, 0f);
            woodFloor.anchorMax = new Vector2(1f, 0.68f);
            woodFloor.offsetMin = Vector2.zero;
            woodFloor.offsetMax = Vector2.zero;
            AddImage(woodFloor, Color.white, woodTableSprite).raycastTarget = false;

            // Main Panel
            RectTransform mainPanel = CreateRect("UpgradeShopPanel", canvasRect);
            mainPanel.sizeDelta = new Vector2(1760f, 980f);
            AddImage(mainPanel, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = false;

            RectTransform innerPanel = CreateRect("InnerSurface", mainPanel);
            Stretch(innerPanel);
            innerPanel.offsetMin = new Vector2(12f, 12f);
            innerPanel.offsetMax = new Vector2(-12f, -12f);
            AddImage(innerPanel, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            // Header
            RectTransform headerBar = CreateRect("HeaderBar", innerPanel);
            headerBar.anchoredPosition = new Vector2(0f, 394f);
            headerBar.sizeDelta = new Vector2(1660f, 134f);

            CreateAnchoredLabel("ShopTitle", headerBar, new Vector2(0f, 0.56f), new Vector2(0.70f, 1f),
                "Carinderia Kitchen Upgrades", 42f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Left, 22f);
            TMP_Text subtitleTmp = CreateAnchoredLabel("ShopSubtitle", headerBar, new Vector2(0f, 0.22f), new Vector2(0.70f, 0.56f),
                "Spend Skill Points to customize your kitchen before your next shift.",
                20f, FontStyles.Normal, Hex("FFF3D6"), TextAlignmentOptions.Left, 13f);
            TMP_Text feedbackTmp = CreateAnchoredLabel("FeedbackBanner", headerBar, new Vector2(0f, 0f), new Vector2(1f, 0.22f),
                "Select an upgrade below or click locked items to preview Full Version perks.",
                18f, FontStyles.Italic, Hex("4ADE80"), TextAlignmentOptions.Left, 12f);

            RectTransform spPill = CreateRect("SkillPointsPill", headerBar);
            spPill.anchorMin = new Vector2(0.72f, 0.50f);
            spPill.anchorMax = new Vector2(1f, 0.96f);
            spPill.offsetMin = Vector2.zero;
            spPill.offsetMax = Vector2.zero;
            AddImage(spPill, Hex("D97706"), roundedSprite, 0.45f).raycastTarget = false;
            TMP_Text spTmp = CreateAnchoredLabel("SkillPointsText", spPill, Vector2.zero, Vector2.one,
                "Skill Points: 0", 26f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 15f);

            RectTransform summaryPill = CreateRect("SaveSummaryPill", headerBar);
            summaryPill.anchorMin = new Vector2(0.72f, 0.22f);
            summaryPill.anchorMax = new Vector2(1f, 0.46f);
            summaryPill.offsetMin = Vector2.zero;
            summaryPill.offsetMax = Vector2.zero;
            AddImage(summaryPill, new Color(0.10f, 0.06f, 0.05f, 0.90f), roundedSprite, 0.5f).raycastTarget = false;
            TMP_Text summaryTmp = CreateAnchoredLabel("SaveSummaryText", summaryPill, Vector2.zero, Vector2.one,
                "Dishes Served: 0  |  Best Score: 0", 15f, FontStyles.Bold, Hex("FFF3D6"), TextAlignmentOptions.Center, 11f);

            // 6 Upgrade Cards Grid (Top row = 3 Active Upgrades, Bottom row = 3 Locked Silhouette Previews)
            RectTransform cardsRow = CreateRect("UpgradeCardsRow", innerPanel);
            cardsRow.anchoredPosition = new Vector2(0f, -10f);
            cardsRow.sizeDelta = new Vector2(1660f, 650f);
            GridLayoutGroup grid = cardsRow.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(536f, 314f);
            grid.spacing = new Vector2(24f, 20f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            UpgradeCardRefs card1 = BuildUpgradeCard(
                cardsRow,
                "Card_PrepMastery",
                KitchenUpgradeId.PrepMastery,
                "1. Precision Knife & Ladle",
                "Expands Chop & Mix sweet spot and slows cursor speed.",
                prepBoardSprite,
                Hex("E07A5F"));

            UpgradeCardRefs card2 = BuildUpgradeCard(
                cardsRow,
                "Card_FastBurner",
                KitchenUpgradeId.FastBurner,
                "2. Turbo Twin-Jet Burners",
                "Speeds up Pot & Pan cooking and adds fire grace time.",
                stoveSprite,
                Hex("F59E0B"));

            UpgradeCardRefs card3 = BuildUpgradeCard(
                cardsRow,
                "Card_Hospitality",
                KitchenUpgradeId.Hospitality,
                "3. Warm Hospitality",
                "Boosts patience, drink freeze duration, and cash tips.",
                drinkCupsSprite,
                Hex("38BDF8"));

            // Row 2: 3 Locked Full Version Upgrades with Charcoal Silhouette Art
            LockedUpgradeCardRefs locked1 = BuildLockedSilhouetteCard(
                cardsRow,
                "Card_Locked1",
                "4. Auto-Simmer Kaldero",
                "Keeps soups & stews hot automatically with zero burn risk.",
                potCookedSprite);

            LockedUpgradeCardRefs locked2 = BuildLockedSilhouetteCard(
                cardsRow,
                "Card_Locked2",
                "5. Turbo Samalamig Dispenser",
                "Pours two chilled drinks at once with zero pour delay.",
                dispenserTanksSprite);

            LockedUpgradeCardRefs locked3 = BuildLockedSilhouetteCard(
                cardsRow,
                "Card_Locked3",
                "6. VIP Counter Expansion",
                "Adds extra display warmers and higher tip multipliers.",
                trayWithDishSprite);

            // Footer Navigation
            RectTransform footerBar = CreateRect("FooterBar", innerPanel);
            footerBar.anchoredPosition = new Vector2(0f, -412f);
            footerBar.sizeDelta = new Vector2(1660f, 84f);
            HorizontalLayoutGroup footerHlg = footerBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerHlg.spacing = 28f;
            footerHlg.childAlignment = TextAnchor.MiddleCenter;
            footerHlg.childControlWidth = false;
            footerHlg.childControlHeight = false;
            footerHlg.childForceExpandWidth = false;
            footerHlg.childForceExpandHeight = false;

            Button resetBtn = CreateActionButton("ResetUpgradesButton", footerBar, "Reset Build",
                Vector2.zero, new Vector2(220f, 64f), Hex("78350F"), 21f);
            Button menuBtn = CreateActionButton("MainMenuButton", footerBar, "Main Menu",
                Vector2.zero, new Vector2(220f, 64f), Hex("6E231E"), 21f);
            Button nextBtn = CreateActionButton("NextShiftButton", footerBar, "Start Next Day",
                Vector2.zero, new Vector2(380f, 68f), Hex("16A34A"), 24f);
            TMP_Text nextBtnLbl = nextBtn.GetComponentInChildren<TMP_Text>();

            // "Soon in Full Version" Modal Popup
            BuildSoonPopupModal(canvasRect, out GameObject soonRoot, out TMP_Text soonTitle, out TMP_Text soonMessage,
                out Button soonCloseBtn, out Button soonBackdropBtn);

            // Manager Wiring
            UpgradeShopManager manager = canvasGo.AddComponent<UpgradeShopManager>();
            var so = new SerializedObject(manager);
            SetRef(so, "subtitleText", subtitleTmp);
            SetRef(so, "skillPointsText", spTmp);
            SetRef(so, "saveSummaryText", summaryTmp);
            SetRef(so, "feedbackBannerText", feedbackTmp);
            SetRef(so, "soonPopupRoot", soonRoot);
            SetRef(so, "soonPopupTitleText", soonTitle);
            SetRef(so, "soonPopupMessageText", soonMessage);
            SetRef(so, "soonPopupCloseButton", soonCloseBtn);
            SetRef(so, "soonPopupBackdropButton", soonBackdropBtn);
            SetRef(so, "resetUpgradesButton", resetBtn);
            SetRef(so, "mainMenuButton", menuBtn);
            SetRef(so, "nextShiftButton", nextBtn);
            SetRef(so, "nextShiftButtonLabel", nextBtnLbl);
            SetRef(so, "mainPanelRect", mainPanel);
            SetRef(so, "cardsRowRect", cardsRow);
            SetRef(so, "headerBarRect", headerBar);
            SetRef(so, "footerBarRect", footerBar);

            SerializedProperty cardsProp = so.FindProperty("upgradeCards");
            cardsProp.arraySize = 3;
            AssignCardProperty(cardsProp.GetArrayElementAtIndex(0), card1);
            AssignCardProperty(cardsProp.GetArrayElementAtIndex(1), card2);
            AssignCardProperty(cardsProp.GetArrayElementAtIndex(2), card3);

            SerializedProperty lockedProp = so.FindProperty("lockedCards");
            lockedProp.arraySize = 3;
            AssignLockedCardProperty(lockedProp.GetArrayElementAtIndex(0), locked1);
            AssignLockedCardProperty(lockedProp.GetArrayElementAtIndex(1), locked2);
            AssignLockedCardProperty(lockedProp.GetArrayElementAtIndex(2), locked3);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UpgradeCardRefs BuildUpgradeCard(
            RectTransform parent,
            string name,
            KitchenUpgradeId upgradeId,
            string title,
            string description,
            Sprite iconSprite,
            Color accentColor)
        {
            RectTransform card = CreateRect(name, parent);
            AddImage(card, new Color(0.11f, 0.07f, 0.05f, 0.96f), roundedSprite, 0.42f).raycastTarget = false;

            RectTransform banner = CreateRect("TitleBanner", card);
            banner.anchorMin = new Vector2(0.05f, 0.84f);
            banner.anchorMax = new Vector2(0.95f, 0.96f);
            banner.offsetMin = Vector2.zero;
            banner.offsetMax = Vector2.zero;
            AddImage(banner, accentColor, roundedSprite, 0.5f).raycastTarget = false;
            CreateAnchoredLabel("TitleText", banner, Vector2.zero, Vector2.one,
                title, 20f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 13f);

            RectTransform iconBox = CreateRect("IconBox", card);
            iconBox.anchorMin = new Vector2(0.10f, 0.51f);
            iconBox.anchorMax = new Vector2(0.90f, 0.82f);
            iconBox.offsetMin = Vector2.zero;
            iconBox.offsetMax = Vector2.zero;
            AddImage(iconBox, Hex("FFF6E5"), roundedSprite, 0.45f).raycastTarget = false;

            if (iconSprite != null)
            {
                RectTransform imgRect = CreateRect("Icon", iconBox);
                Stretch(imgRect);
                imgRect.offsetMin = new Vector2(14f, 8f);
                imgRect.offsetMax = new Vector2(-14f, -8f);
                Image iconImg = AddImage(imgRect, Color.white, iconSprite);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            CreateAnchoredLabel("DescriptionText", card, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.50f),
                description, 15f, FontStyles.Normal, Hex("FFF3D6"), TextAlignmentOptions.Center, 11f);

            TMP_Text statsTmp = CreateAnchoredLabel("StatsText", card, new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.35f),
                "Current -> Next", 14f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 10f);

            TMP_Text rankTmp = CreateAnchoredLabel("RankText", card, new Vector2(0.07f, 0.135f), new Vector2(0.42f, 0.21f),
                "Rank 0/3", 16f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Left, 11f);

            RectTransform pipsRow = CreateRect("RankPips", card);
            pipsRow.anchorMin = new Vector2(0.46f, 0.145f);
            pipsRow.anchorMax = new Vector2(0.93f, 0.20f);
            pipsRow.offsetMin = Vector2.zero;
            pipsRow.offsetMax = Vector2.zero;
            HorizontalLayoutGroup pipsHlg = pipsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipsHlg.spacing = 10f;
            pipsHlg.childControlWidth = true;
            pipsHlg.childControlHeight = true;
            pipsHlg.childForceExpandWidth = true;
            pipsHlg.childForceExpandHeight = true;

            Image[] pips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform pip = CreateRect($"Pip_{i + 1}", pipsRow);
                pips[i] = AddImage(pip, new Color(0.24f, 0.16f, 0.12f, 0.90f), roundedSprite, 0.5f);
                pips[i].raycastTarget = false;
            }

            RectTransform btnHolder = CreateRect("UpgradeBtnHolder", card);
            btnHolder.anchorMin = new Vector2(0.12f, 0.03f);
            btnHolder.anchorMax = new Vector2(0.88f, 0.13f);
            btnHolder.offsetMin = Vector2.zero;
            btnHolder.offsetMax = Vector2.zero;

            Button upgBtn = CreateActionButton("UpgradeButton", btnHolder, "Upgrade (1 SP)",
                Vector2.zero, new Vector2(360f, 38f), Hex("16A34A"), 18f);
            Stretch((RectTransform)upgBtn.transform);
            TMP_Text upgBtnLbl = upgBtn.GetComponentInChildren<TMP_Text>();

            return new UpgradeCardRefs
            {
                upgradeId = upgradeId,
                rankText = rankTmp,
                statsText = statsTmp,
                upgradeButton = upgBtn,
                upgradeButtonLabel = upgBtnLbl,
                rankPips = pips
            };
        }

        private static LockedUpgradeCardRefs BuildLockedSilhouetteCard(
            RectTransform parent,
            string name,
            string title,
            string description,
            Sprite silhouetteSource)
        {
            RectTransform card = CreateRect(name, parent);
            Image cardBg = AddImage(card, new Color(0.10f, 0.08f, 0.08f, 0.96f), roundedSprite, 0.42f);
            cardBg.raycastTarget = true;
            Button cardBtn = card.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = cardBg;

            RectTransform banner = CreateRect("TitleBanner", card);
            banner.anchorMin = new Vector2(0.05f, 0.84f);
            banner.anchorMax = new Vector2(0.95f, 0.96f);
            banner.offsetMin = Vector2.zero;
            banner.offsetMax = Vector2.zero;
            AddImage(banner, Hex("4B4440"), roundedSprite, 0.5f).raycastTarget = false;
            CreateAnchoredLabel("TitleText", banner, Vector2.zero, Vector2.one,
                title, 20f, FontStyles.Bold, Hex("E5E7EB"), TextAlignmentOptions.Center, 13f);

            // Light muted warm backdrop so the dark charcoal silhouette figure reads clearly (not too dark!)
            RectTransform iconBox = CreateRect("IconBox", card);
            iconBox.anchorMin = new Vector2(0.10f, 0.48f);
            iconBox.anchorMax = new Vector2(0.90f, 0.82f);
            iconBox.offsetMin = Vector2.zero;
            iconBox.offsetMax = Vector2.zero;
            AddImage(iconBox, Hex("CEC2B2"), roundedSprite, 0.45f).raycastTarget = false;

            if (silhouetteSource != null)
            {
                RectTransform imgRect = CreateRect("SilhouetteIcon", iconBox);
                Stretch(imgRect);
                imgRect.offsetMin = new Vector2(14f, 8f);
                imgRect.offsetMax = new Vector2(-14f, -8f);
                // Charcoal silhouette (#242429 at 86% opacity) - visible outline, not pure pitch black
                Image silImg = AddImage(imgRect, new Color(0.14f, 0.14f, 0.16f, 0.86f), silhouetteSource);
                silImg.preserveAspect = true;
                silImg.raycastTarget = false;
            }

            RectTransform lockTag = CreateRect("LockedTag", iconBox);
            lockTag.anchorMin = new Vector2(0.64f, 0.72f);
            lockTag.anchorMax = new Vector2(0.97f, 0.94f);
            lockTag.offsetMin = Vector2.zero;
            lockTag.offsetMax = Vector2.zero;
            AddImage(lockTag, new Color(0.12f, 0.11f, 0.12f, 0.90f), roundedSprite, 0.5f).raycastTarget = false;
            CreateAnchoredLabel("LockTagText", lockTag, Vector2.zero, Vector2.one,
                "LOCKED", 12f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Center, 9f);

            CreateAnchoredLabel("DescriptionText", card, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.46f),
                description, 15f, FontStyles.Normal, Hex("D6CFC7"), TextAlignmentOptions.Center, 11f);

            RectTransform btnHolder = CreateRect("PreviewBtnHolder", card);
            btnHolder.anchorMin = new Vector2(0.12f, 0.04f);
            btnHolder.anchorMax = new Vector2(0.88f, 0.18f);
            btnHolder.offsetMin = Vector2.zero;
            btnHolder.offsetMax = Vector2.zero;

            Button previewBtn = CreateActionButton("SoonPreviewButton", btnHolder, "Soon in Full Version",
                Vector2.zero, new Vector2(360f, 40f), Hex("475569"), 17f);
            Stretch((RectTransform)previewBtn.transform);

            return new LockedUpgradeCardRefs
            {
                upgradeTitle = title,
                cardButton = cardBtn,
                previewButton = previewBtn
            };
        }

        private static void BuildSoonPopupModal(
            RectTransform canvasRect,
            out GameObject root,
            out TMP_Text titleText,
            out TMP_Text messageText,
            out Button closeButton,
            out Button backdropButton)
        {
            RectTransform overlay = CreateRect("SoonPopupModal", canvasRect);
            Stretch(overlay);
            Image bgImg = AddImage(overlay, new Color(0.03f, 0.03f, 0.04f, 0.78f), null);
            bgImg.raycastTarget = true;
            backdropButton = overlay.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = bgImg;

            RectTransform card = CreateRect("SoonCard", overlay);
            card.sizeDelta = new Vector2(720f, 360f);
            AddImage(card, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = true;

            RectTransform inner = CreateRect("Inner", card);
            Stretch(inner);
            inner.offsetMin = new Vector2(10f, 10f);
            inner.offsetMax = new Vector2(-10f, -10f);
            AddImage(inner, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            titleText = CreateAnchoredLabel("SoonTitleText", inner, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.88f),
                "Soon in Full Version", 42f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Center, 24f);

            messageText = CreateAnchoredLabel("SoonMessageText", inner, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.60f),
                "This upgrade is locked in the Showcase Demo.\nComing Soon in Full Version!",
                22f, FontStyles.Normal, Hex("FFF3D6"), TextAlignmentOptions.Center, 14f);

            RectTransform btnRow = CreateRect("CloseBtnHolder", inner);
            btnRow.anchorMin = new Vector2(0.30f, 0.06f);
            btnRow.anchorMax = new Vector2(0.70f, 0.24f);
            btnRow.offsetMin = Vector2.zero;
            btnRow.offsetMax = Vector2.zero;

            closeButton = CreateActionButton("SoonCloseButton", btnRow, "Got It",
                Vector2.zero, new Vector2(240f, 54f), Hex("16A34A"), 22f);
            Stretch((RectTransform)closeButton.transform);

            root = overlay.gameObject;
            root.SetActive(false);
        }

        private static void AssignCardProperty(SerializedProperty prop, UpgradeCardRefs card)
        {
            prop.FindPropertyRelative("upgradeId").enumValueIndex = (int)card.upgradeId;
            prop.FindPropertyRelative("rankText").objectReferenceValue = card.rankText;
            prop.FindPropertyRelative("statsText").objectReferenceValue = card.statsText;
            prop.FindPropertyRelative("upgradeButton").objectReferenceValue = card.upgradeButton;
            prop.FindPropertyRelative("upgradeButtonLabel").objectReferenceValue = card.upgradeButtonLabel;

            SerializedProperty pipsProp = prop.FindPropertyRelative("rankPips");
            pipsProp.arraySize = card.rankPips.Length;
            for (int i = 0; i < card.rankPips.Length; i++)
            {
                pipsProp.GetArrayElementAtIndex(i).objectReferenceValue = card.rankPips[i];
            }
        }

        private static void AssignLockedCardProperty(SerializedProperty prop, LockedUpgradeCardRefs card)
        {
            prop.FindPropertyRelative("upgradeTitle").stringValue = card.upgradeTitle;
            prop.FindPropertyRelative("cardButton").objectReferenceValue = card.cardButton;
            prop.FindPropertyRelative("previewButton").objectReferenceValue = card.previewButton;
        }

        private static void EnsureCamera(Scene scene)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(camGo, scene);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("1E1916");
            cam.depth = -1f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            var esGo = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(esGo, scene);
            esGo.AddComponent<EventSystem>();
            InputSystemUIInputModule module = esGo.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions != null) module.actionsAsset = actions;
        }

        private static void LoadResources()
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                     ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath)
                     ?? TMP_Settings.defaultFontAsset;

            string gen = CarinderiaAssetGenerator.GeneratedDir;
            wallBackdropSprite = LoadLargestSprite($"{gen}/Carinderia_WallBackdrop.png");
            woodTableSprite = LoadLargestSprite($"{gen}/Carinderia_WoodenTable.png");
            prepBoardSprite = LoadLargestSprite($"{gen}/Prep_WoodBoard.png");
            stoveSprite = LoadLargestSprite("Assets/2D Kitchen/burnerstove.png");
            drinkCupsSprite = LoadLargestSprite("Assets/2D Kitchen/Drink Dispenser/2.png");
            potCookedSprite = LoadLargestSprite("Assets/2D Kitchen/Pots w state/2.png");
            dispenserTanksSprite = LoadLargestSprite("Assets/2D Kitchen/Drink Dispenser/1.png");
            trayWithDishSprite = LoadLargestSprite("Assets/2D Kitchen/Serving Tray/with dish.png");
        }

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
            return best;
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

        private static TMP_Text CreateAnchoredLabel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            float minFontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(6f, 4f);
            rect.offsetMax = new Vector2(-6f, -4f);

            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minFontSize;
            tmp.fontSizeMax = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
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
            Image img = AddImage(rect, color, roundedSprite, 0.45f);
            img.raycastTarget = true;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            TMP_Text lbl = CreateAnchoredLabel("Text", rect, Vector2.zero, Vector2.one,
                text, fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Mathf.Max(11f, fontSize - 6f));
            lbl.raycastTarget = false;
            return btn;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
