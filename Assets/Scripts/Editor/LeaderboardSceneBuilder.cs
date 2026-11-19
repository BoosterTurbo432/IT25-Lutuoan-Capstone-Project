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
    /// Builds Assets/Scenes/leaderboardscene.unity and provides a shared LeaderboardPanelController
    /// builder reused by GameSceneLayoutBuilder for the in-game Endless Leaderboard overlay.
    /// </summary>
    [InitializeOnLoad]
    public static class LeaderboardSceneBuilder
    {
        public const string LeaderboardScenePath = "Assets/Scenes/leaderboardscene.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string FallbackFontPath = "Assets/Fonts/Stay With Me SDF.asset";

        private static Sprite roundedSprite;
        private static Sprite wallBackdropSprite;
        private static Sprite woodTableSprite;
        private static TMP_FontAsset uiFont;

        static LeaderboardSceneBuilder()
        {
            EditorApplication.delayCall += EnsureSceneCreated;
        }

        private static void EnsureSceneCreated()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(LeaderboardScenePath) && File.ReadAllText(LeaderboardScenePath).Contains("LeaderboardPanel"))
            {
                BuildSceneListSetup.AddGameScenes();
                return;
            }

            CreateLeaderboardSceneInternal(openAfterSave: false);
        }

        [MenuItem("Tools/Leaderboard/Build Leaderboard Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateLeaderboardSceneInternal(openAfterSave: true);
        }

        public static void BuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool ok = CreateLeaderboardSceneInternal(openAfterSave: true);
            AssetDatabase.SaveAssets();
            Debug.Log(ok ? "[LeaderboardSceneBuilder] BUILD OK" : "[LeaderboardSceneBuilder] BUILD FAILED");
            if (!ok) EditorApplication.Exit(1);
        }

        private static bool CreateLeaderboardSceneInternal(bool openAfterSave)
        {
            CarinderiaAssetGenerator.EnsureAllGeneratedAssets();
            LoadResources();

            Scene prevScene = SceneManager.GetActiveScene();
            string prevScenePath = prevScene.path;

            Scene lbScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, openAfterSave ? NewSceneMode.Single : NewSceneMode.Additive);

            EnsureCamera(lbScene);
            EnsureEventSystem(lbScene);
            BuildSceneContents(lbScene);

            bool saved = EditorSceneManager.SaveScene(lbScene, LeaderboardScenePath);
            if (!openAfterSave && !string.IsNullOrEmpty(prevScenePath) && prevScenePath != LeaderboardScenePath)
            {
                EditorSceneManager.CloseScene(lbScene, true);
            }

            BuildSceneListSetup.AddGameScenes();
            Debug.Log($"[LeaderboardSceneBuilder] Leaderboard scene saved cleanly at {LeaderboardScenePath}");
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

            RectTransform bgWall = CreateRect("BackgroundWall", canvasRect);
            Stretch(bgWall);
            AddImage(bgWall, Hex("221814"), wallBackdropSprite).raycastTarget = false;

            RectTransform woodFloor = CreateRect("WoodCounterBg", canvasRect);
            woodFloor.anchorMin = new Vector2(0f, 0f);
            woodFloor.anchorMax = new Vector2(1f, 0.68f);
            woodFloor.offsetMin = Vector2.zero;
            woodFloor.offsetMax = Vector2.zero;
            AddImage(woodFloor, Color.white, woodTableSprite).raycastTarget = false;

            BuildLeaderboardPanel(canvasRect, isOverlay: false, out _);
        }

        /// <summary>
        /// Builds the Top-10 Carinderia Endless Leaderboard UI panel either as a full scene card
        /// or as an overlay modal inside gamescene.unity.
        /// </summary>
        public static LeaderboardPanelController BuildLeaderboardPanel(
            RectTransform parent,
            bool isOverlay,
            out GameObject rootObject)
        {
            LoadResources();

            RectTransform rootRect = CreateRect("LeaderboardPanel", parent);
            Stretch(rootRect);

            if (isOverlay)
            {
                AddImage(rootRect, new Color(0.03f, 0.03f, 0.04f, 0.86f), null).raycastTarget = true;
            }

            RectTransform mainPanel = CreateRect("BoardFrame", rootRect);
            mainPanel.sizeDelta = isOverlay ? new Vector2(1420f, 840f) : new Vector2(1540f, 880f);
            AddImage(mainPanel, Hex("5A2D14"), roundedSprite, 0.35f).raycastTarget = true;

            RectTransform inner = CreateRect("InnerSurface", mainPanel);
            Stretch(inner);
            inner.offsetMin = new Vector2(12f, 12f);
            inner.offsetMax = new Vector2(-12f, -12f);
            AddImage(inner, Hex("2B1A10"), roundedSprite, 0.38f).raycastTarget = false;

            // Header
            CreateAnchoredLabel("TitleText", inner, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f),
                "Endless Rush Leaderboard", 38f, FontStyles.Bold, Hex("FBBF24"), TextAlignmentOptions.Center, 22f);

            // Column Header Bar
            RectTransform colHeader = CreateRect("ColumnsHeader", inner);
            colHeader.anchorMin = new Vector2(0.04f, 0.81f);
            colHeader.anchorMax = new Vector2(0.96f, 0.88f);
            colHeader.offsetMin = Vector2.zero;
            colHeader.offsetMax = Vector2.zero;
            AddImage(colHeader, Hex("452212"), roundedSprite, 0.5f).raycastTarget = false;

            CreateAnchoredLabel("Hdr_Rank", colHeader, new Vector2(0.01f, 0f), new Vector2(0.10f, 1f), "Rank", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);
            CreateAnchoredLabel("Hdr_Name", colHeader, new Vector2(0.11f, 0f), new Vector2(0.35f, 1f), "Name", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);
            CreateAnchoredLabel("Hdr_Score", colHeader, new Vector2(0.36f, 0f), new Vector2(0.55f, 1f), "Score", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);
            CreateAnchoredLabel("Hdr_Time", colHeader, new Vector2(0.56f, 0f), new Vector2(0.71f, 1f), "Time", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);
            CreateAnchoredLabel("Hdr_Dishes", colHeader, new Vector2(0.72f, 0f), new Vector2(0.85f, 1f), "Dishes", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f);
            CreateAnchoredLabel("Hdr_Combo", colHeader, new Vector2(0.86f, 0f), new Vector2(0.99f, 1f), "Best Combo", 18f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 11f);

            // Rows Container (Top 10)
            RectTransform rowsArea = CreateRect("RowsContainer", inner);
            rowsArea.anchorMin = new Vector2(0.04f, 0.16f);
            rowsArea.anchorMax = new Vector2(0.96f, 0.80f);
            rowsArea.offsetMin = Vector2.zero;
            rowsArea.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = rowsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            LeaderboardRowRefs[] rows = new LeaderboardRowRefs[10];
            for (int i = 0; i < 10; i++)
            {
                RectTransform rowRect = CreateRect($"Row_{i + 1}", rowsArea);
                Image rowBg = AddImage(rowRect, new Color(0.14f, 0.09f, 0.07f, 0.92f), roundedSprite, 0.5f);
                rowBg.raycastTarget = false;

                rows[i] = new LeaderboardRowRefs
                {
                    rowRoot = rowRect.gameObject,
                    rowBackground = rowBg,
                    rankText = CreateAnchoredLabel("Rank", rowRect, new Vector2(0.01f, 0f), new Vector2(0.10f, 1f), $"#{i + 1}", 20f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 12f),
                    nameText = CreateAnchoredLabel("Name", rowRect, new Vector2(0.11f, 0f), new Vector2(0.35f, 1f), "Chef", 21f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, 12f),
                    scoreText = CreateAnchoredLabel("Score", rowRect, new Vector2(0.36f, 0f), new Vector2(0.55f, 1f), "0", 21f, FontStyles.Bold, Hex("4ADE80"), TextAlignmentOptions.Center, 12f),
                    timeText = CreateAnchoredLabel("Time", rowRect, new Vector2(0.56f, 0f), new Vector2(0.71f, 1f), "00:00", 19f, FontStyles.Normal, Hex("FFF3D6"), TextAlignmentOptions.Center, 12f),
                    dishesText = CreateAnchoredLabel("Dishes", rowRect, new Vector2(0.72f, 0f), new Vector2(0.85f, 1f), "0", 19f, FontStyles.Normal, Hex("FFF3D6"), TextAlignmentOptions.Center, 12f),
                    comboText = CreateAnchoredLabel("Combo", rowRect, new Vector2(0.86f, 0f), new Vector2(0.99f, 1f), "x1", 19f, FontStyles.Bold, Hex("FDE047"), TextAlignmentOptions.Center, 12f)
                };
            }

            TMP_Text emptyHint = CreateAnchoredLabel("EmptyHintText", inner,
                new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.62f),
                "No Endless Rush scores yet!\nClear Day 3 to unlock Endless Mode and record your first run.",
                26f, FontStyles.Bold, Hex("FFF3D6"), TextAlignmentOptions.Center, 15f);

            // Footer Bar
            TMP_Text footerStatus = CreateAnchoredLabel("FooterStatusText", inner,
                new Vector2(0.04f, 0.09f), new Vector2(0.96f, 0.15f),
                "Saved in Browser Storage", 16f, FontStyles.Italic, Hex("D6CFC7"), TextAlignmentOptions.Center, 11f);

            RectTransform footerRow = CreateRect("FooterButtons", inner);
            footerRow.anchorMin = new Vector2(0.08f, 0.02f);
            footerRow.anchorMax = new Vector2(0.92f, 0.09f);
            footerRow.offsetMin = Vector2.zero;
            footerRow.offsetMax = Vector2.zero;
            HorizontalLayoutGroup hlg = footerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Button clearBtn = CreateActionButton("ClearBoardButton", footerRow, "Clear Board", new Vector2(210f, 52f), Hex("78350F"), 19f);
            Button closeOrBackBtn = CreateActionButton(
                isOverlay ? "CloseBoardButton" : "BackToMenuButton",
                footerRow,
                isOverlay ? "Back to Summary" : "Main Menu",
                new Vector2(260f, 54f),
                Hex("16A34A"),
                21f);

            LeaderboardPanelController controller = rootRect.gameObject.AddComponent<LeaderboardPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("adaptStandaloneLayout").boolValue = !isOverlay;
            so.FindProperty("mainMenuSceneName").stringValue = "mainmenuscene";
            SetRef(so, "emptyStateHint", emptyHint.gameObject);
            SetRef(so, isOverlay ? "closeButton" : "mainMenuButton", closeOrBackBtn);
            SetRef(so, "clearBoardButton", clearBtn);
            SetRef(so, "footerStatusText", footerStatus);
            SetRef(so, "mainPanelRect", mainPanel);

            SerializedProperty rowsProp = so.FindProperty("rowSlots");
            rowsProp.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty elem = rowsProp.GetArrayElementAtIndex(i);
                SetRef(elem, "rowRoot", rows[i].rowRoot);
                SetRef(elem, "rowBackground", rows[i].rowBackground);
                SetRef(elem, "rankText", rows[i].rankText);
                SetRef(elem, "nameText", rows[i].nameText);
                SetRef(elem, "scoreText", rows[i].scoreText);
                SetRef(elem, "timeText", rows[i].timeText);
                SetRef(elem, "dishesText", rows[i].dishesText);
                SetRef(elem, "comboText", rows[i].comboText);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isOverlay)
            {
                rootRect.gameObject.SetActive(false);
            }

            rootObject = rootRect.gameObject;
            return controller;
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
            if (roundedSprite != null && uiFont != null) return;
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                     ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath)
                     ?? TMP_Settings.defaultFontAsset;

            string gen = CarinderiaAssetGenerator.GeneratedDir;
            wallBackdropSprite = LoadLargestSprite($"{gen}/Carinderia_WallBackdrop.png");
            woodTableSprite = LoadLargestSprite($"{gen}/Carinderia_WoodenTable.png");
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
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateActionButton(
            string name,
            Transform parent,
            string text,
            Vector2 sizeDelta,
            Color color,
            float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
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

        private static void SetRef(SerializedProperty prop, string relativeName, Object value)
        {
            SerializedProperty child = prop.FindPropertyRelative(relativeName);
            if (child != null) child.objectReferenceValue = value;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
