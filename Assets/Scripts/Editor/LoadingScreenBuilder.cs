using System.IO;
using System.Linq;
using CookingSystem.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.EditorTools
{
    /// <summary>
    /// Automated builder for the dedicated Loading Screen scene (Tools > Loading Screen > Build Loading Screen UI).
    /// Constructs a cozy, lightweight, WebGL-optimized loading screen hierarchy with
    /// progress bar, rotating educational cooking tips, dynamic culinary statuses, and camera setup.
    /// Safe to execute repeatedly.
    /// </summary>
    public static class LoadingScreenBuilder
    {
        public const string LoadingScenePath = "Assets/Scenes/loadingscreen.unity";
        private const string FontPath = "Assets/Fonts/Stay With Me SDF.asset";
        private const string IconPath = "Assets/CozyUI/Icons/IconsFlat_16px/IconFlat1.png";
        private const string ContainerPath = "Assets/CozyUI/Containers/DoubleStripeContainer/DoubleStripeContainer1_cream.png";
        private const string ProgressFillPath = "Assets/CozyUI/ProgressBars/ProgressBar/ProgressBarFill1_wood.png";

        // Warm culinary palette
        private static readonly Color DeepBlueBg = Hex("273B5B");
        private static readonly Color CardColor = Hex("FFF8EE");
        private static readonly Color IconFrameColor = Hex("EADCC9");
        private static readonly Color TitleColor = Hex("4A2E1B");
        private static readonly Color SubtitleColor = Hex("8C7058");
        private static readonly Color StatusColor = Hex("5C3E28");
        private static readonly Color PrimaryAmber = Hex("F4A259");
        private static readonly Color TrackColor = Hex("E5D5C0");
        private static readonly Color TipCardColor = Hex("F5ECE0");
        private static readonly Color TipTextColor = Hex("3B2A1A");

        private static Sprite roundedSprite;
        private static Sprite backgroundSprite;
        private static Sprite iconSprite;
        private static Sprite containerSprite;
        private static Sprite progressFillSprite;
        private static TMP_FontAsset font;

        [MenuItem("Tools/Loading Screen/Build Loading Screen UI")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = OpenLoadingScene();
            if (!Build(scene)) return;

            EditorSceneManager.SaveScene(scene, LoadingScenePath);
            BuildSceneListSetup.AddGameScenes();
            Debug.Log("[LoadingScreenBuilder] Dedicated Loading Screen built, optimized, and saved.");
        }

        [MenuItem("Tools/Loading Screen/Open Loading Screen Scene")]
        public static void OpenLoadingSceneFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
        }

        public static Scene OpenLoadingScene()
        {
            if (File.Exists(LoadingScenePath))
            {
                return EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, LoadingScenePath);
            return newScene;
        }

        public static bool Build(Scene scene)
        {
            LoadResources();

            // Clear existing root objects for an idempotent, clean rebuild
            foreach (GameObject rootGo in scene.GetRootGameObjects().ToList())
            {
                Object.DestroyImmediate(rootGo);
            }

            // 1. Camera Setup
            var cameraGo = new GameObject("Main Camera");
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = DeepBlueBg;
            cam.depth = -1f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            // 2. EventSystem Setup
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // 3. Canvas Setup
            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = (RectTransform)canvas.transform;

            // 4. Root Container with CanvasGroup for master fade transitions
            RectTransform rootContainer = CreateRect("RootContainer", canvasRect);
            Stretch(rootContainer);
            CanvasGroup rootCg = rootContainer.gameObject.AddComponent<CanvasGroup>();
            rootCg.alpha = 1f;

            // 5. Background Overlay
            RectTransform bgRect = CreateRect("BackgroundOverlay", rootContainer);
            Stretch(bgRect);
            Image bgImg = AddImage(bgRect, new Color(0.12f, 0.18f, 0.28f, 0.95f), backgroundSprite);
            bgImg.raycastTarget = false;

            // 6. Central Card Window
            RectTransform cardRect = CreateRect("LoadingCard", rootContainer);
            cardRect.sizeDelta = new Vector2(1040f, 680f);
            Image cardImg = AddImage(cardRect, CardColor, containerSprite ?? roundedSprite, 0.35f);
            cardImg.raycastTarget = false;
            AddShadow(cardRect.gameObject, new Vector2(0f, -10f), 0.35f);

            // 7. Cooking Icon with Bobbing Motion
            RectTransform iconFrame = CreateRect("CookingIconFrame", cardRect);
            iconFrame.anchoredPosition = new Vector2(0f, 195f);
            iconFrame.sizeDelta = new Vector2(100f, 100f);
            Image frameImg = AddImage(iconFrame, IconFrameColor, roundedSprite, 0.5f);
            frameImg.raycastTarget = false;

            RectTransform iconRect = CreateRect("CookingIcon", iconFrame);
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(14f, 14f);
            iconRect.offsetMax = new Vector2(-14f, -14f);
            Image iconImg = AddImage(iconRect, PrimaryAmber, iconSprite);
            iconImg.raycastTarget = false;

            // 8. Title & Subtitle Header
            RectTransform titleRect = CreateRect("TitleText", cardRect);
            titleRect.anchoredPosition = new Vector2(0f, 105f);
            titleRect.sizeDelta = new Vector2(800f, 65f);
            TextMeshProUGUI titleTmp = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) titleTmp.font = font;
            titleTmp.text = "Lutuoan";
            titleTmp.fontSize = 58f;
            titleTmp.color = TitleColor;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            RectTransform subtitleRect = CreateRect("SubtitleText", cardRect);
            subtitleRect.anchoredPosition = new Vector2(0f, 60f);
            subtitleRect.sizeDelta = new Vector2(800f, 35f);
            TextMeshProUGUI subtitleTmp = subtitleRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) subtitleTmp.font = font;
            subtitleTmp.text = "Educational Cooking Game";
            subtitleTmp.fontSize = 24f;
            subtitleTmp.color = SubtitleColor;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.raycastTarget = false;

            // 9. Status Message Text
            RectTransform statusRect = CreateRect("StatusText", cardRect);
            statusRect.anchoredPosition = new Vector2(0f, -5f);
            statusRect.sizeDelta = new Vector2(800f, 38f);
            TextMeshProUGUI statusTmp = statusRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) statusTmp.font = font;
            statusTmp.text = "Preheating kitchen stoves...";
            statusTmp.fontSize = 28f;
            statusTmp.color = StatusColor;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.raycastTarget = false;

            // 10. Progress Bar Container
            RectTransform barContainer = CreateRect("ProgressBar", cardRect);
            barContainer.anchoredPosition = new Vector2(0f, -60f);
            barContainer.sizeDelta = new Vector2(780f, 32f);

            // Track Background
            Image trackImg = AddImage(barContainer, TrackColor, roundedSprite, 0.5f);
            trackImg.raycastTarget = false;

            // Fill Area & Fill Image
            RectTransform fillArea = CreateRect("Fill Area", barContainer);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);

            RectTransform fillRect = CreateRect("Fill", fillArea);
            Stretch(fillRect);
            Image fillImg = AddImage(fillRect, PrimaryAmber, progressFillSprite ?? roundedSprite, 0.5f);
            fillImg.type = Image.Type.Sliced;
            fillImg.raycastTarget = false;

            Slider slider = barContainer.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Percentage Text
            RectTransform percentRect = CreateRect("PercentText", cardRect);
            percentRect.anchoredPosition = new Vector2(0f, -100f);
            percentRect.sizeDelta = new Vector2(200f, 32f);
            TextMeshProUGUI percentTmp = percentRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) percentTmp.font = font;
            percentTmp.text = "0%";
            percentTmp.fontSize = 26f;
            percentTmp.color = TitleColor;
            percentTmp.alignment = TextAlignmentOptions.Center;
            percentTmp.raycastTarget = false;

            // 11. Educational Culinary Tip Card
            RectTransform tipCard = CreateRect("TipCard", cardRect);
            tipCard.anchoredPosition = new Vector2(0f, -200f);
            tipCard.sizeDelta = new Vector2(880f, 130f);
            Image tipCardImg = AddImage(tipCard, TipCardColor, roundedSprite, 0.3f);
            tipCardImg.raycastTarget = false;

            // Badge "CHEF'S TIP"
            RectTransform badgeRect = CreateRect("TipBadge", tipCard);
            badgeRect.anchoredPosition = new Vector2(0f, 54f);
            badgeRect.sizeDelta = new Vector2(170f, 30f);
            AddImage(badgeRect, PrimaryAmber, roundedSprite, 0.5f);

            RectTransform badgeTextRect = CreateRect("TipBadgeText", badgeRect);
            Stretch(badgeTextRect);
            TextMeshProUGUI badgeTmp = badgeTextRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) badgeTmp.font = font;
            badgeTmp.text = "CHEF'S TIP";
            badgeTmp.fontSize = 18f;
            badgeTmp.color = Color.white;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.raycastTarget = false;

            // Tip Content Text with CanvasGroup
            RectTransform tipContent = CreateRect("TipContent", tipCard);
            tipContent.anchoredPosition = new Vector2(0f, -10f);
            tipContent.sizeDelta = new Vector2(820f, 85f);
            CanvasGroup tipCg = tipContent.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI tipTmp = tipContent.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tipTmp.font = font;
            tipTmp.text = "Mise en place: Preparing ingredients before cooking ensures an even, stress-free meal!";
            tipTmp.fontSize = 22f;
            tipTmp.color = TipTextColor;
            tipTmp.alignment = TextAlignmentOptions.Center;
            tipTmp.wordWrappingRatios = 0.4f;
            tipTmp.textWrappingMode = TextWrappingModes.Normal;
            tipTmp.raycastTarget = false;

            // 12. Attach & Wire LoadingScreenController
            LoadingScreenController controller = canvasGo.AddComponent<LoadingScreenController>();
            var so = new SerializedObject(controller);
            SetRef(so, "rootCanvasGroup", rootCg);
            SetRef(so, "progressBar", slider);
            SetRef(so, "progressFillImage", fillImg);
            SetRef(so, "percentText", percentTmp);
            SetRef(so, "statusText", statusTmp);
            SetRef(so, "tipText", tipTmp);
            SetRef(so, "tipCanvasGroup", tipCg);
            SetRef(so, "cookingIcon", iconFrame);
            so.FindProperty("progressSmoothingSpeed").floatValue = 2.4f;
            so.FindProperty("minBootDisplayTime").floatValue = 0.9f;
            so.FindProperty("minTransitionDisplayTime").floatValue = 0.65f;
            so.FindProperty("cleanMemoryBeforeActivation").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static void LoadResources()
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ContainerPath);
            progressFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProgressFillPath);

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
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

        private static void AddShadow(GameObject go, Vector2 offset, float alpha)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = offset;
            shadow.useGraphicAlpha = true;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[LoadingScreenBuilder] Property '{propertyName}' not found on {so.targetObject.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
