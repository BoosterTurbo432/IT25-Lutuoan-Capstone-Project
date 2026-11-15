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
    /// Constructs a dark chalkboard-style loading screen matching the visual reference with
    /// starry LOADING backdrop, flanking food illustrations, hanging kitchen utensils,
    /// yellow-on-green pill progress bar, and rotating culinary tips.
    /// Safe to execute repeatedly.
    /// </summary>
    public static class LoadingScreenBuilder
    {
        public const string LoadingScenePath = "Assets/Scenes/loadingscreen.unity";

        private const string BackgroundPath = "Assets/Images/loadingscreen.png";
        private const string FoodDecorPath = "Assets/Images/Food.png";
        private const string UtensilsPath = "Assets/Images/utensils.png";
        private const string PillSpritePath = "Assets/Images/ProgressBarPill.png";
        private const string TipFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        // Palette matched to the loading screen visual reference
        private static readonly Color CameraBgColor = Hex("0A0C10");
        private static readonly Color TrackGreen = Hex("568C34");
        private static readonly Color FillYellow = Hex("ECB71B");

        private static Sprite backgroundSprite;
        private static Sprite foodDecorSprite;
        private static Sprite utensilsSprite;
        private static Sprite pillSprite;
        private static Sprite fallbackRoundedSprite;
        private static TMP_FontAsset tipFont;

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
            cam.backgroundColor = CameraBgColor;
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

            // 5. Starry Chalkboard "LOADING" Background
            RectTransform bgRect = CreateRect("Background", rootContainer);
            Stretch(bgRect);
            Image bgImg = AddImage(bgRect, Color.white, backgroundSprite);
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // 6. Flanking Food Illustrations (egg, bok choy, shrimp, dish)
            RectTransform foodRect = CreateRect("FoodDecor", rootContainer);
            Stretch(foodRect);
            Image foodImg = AddImage(foodRect, Color.white, foodDecorSprite);
            foodImg.preserveAspect = false;
            foodImg.raycastTarget = false;

            // 7. Hanging Kitchen Utensils Rack (Top Center)
            RectTransform utensilsRect = CreateRect("Utensils", rootContainer);
            utensilsRect.anchoredPosition = new Vector2(0f, 348f);
            utensilsRect.sizeDelta = new Vector2(780f, 440f);
            Image utensilsImg = AddImage(utensilsRect, Color.white, utensilsSprite);
            utensilsImg.preserveAspect = true;
            utensilsImg.raycastTarget = false;

            // 8. Pill Progress Bar (Yellow fill over Green track)
            Sprite barSprite = pillSprite != null ? pillSprite : fallbackRoundedSprite;
            float barPpu = pillSprite != null ? 2.625f : 0.22f;

            RectTransform barContainer = CreateRect("ProgressBar", rootContainer);
            barContainer.anchoredPosition = new Vector2(0f, -180f);
            barContainer.sizeDelta = new Vector2(1210f, 48f);

            Image trackImg = AddImage(barContainer, TrackGreen, barSprite, barPpu);
            trackImg.type = Image.Type.Sliced;
            trackImg.raycastTarget = false;

            RectTransform fillArea = CreateRect("Fill Area", barContainer);
            Stretch(fillArea);

            RectTransform fillRect = CreateRect("Fill", fillArea);
            Stretch(fillRect);
            Image fillImg = AddImage(fillRect, FillYellow, barSprite, barPpu);
            fillImg.type = Image.Type.Sliced;
            fillImg.raycastTarget = false;

            Slider slider = barContainer.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.41f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // 9. "Tip:" Header
            RectTransform tipHeaderRect = CreateRect("TipHeader", rootContainer);
            tipHeaderRect.anchoredPosition = new Vector2(0f, -255f);
            tipHeaderRect.sizeDelta = new Vector2(400f, 64f);
            TextMeshProUGUI tipHeaderTmp = tipHeaderRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (tipFont != null) tipHeaderTmp.font = tipFont;
            tipHeaderTmp.text = "Tip:";
            tipHeaderTmp.fontSize = 52f;
            tipHeaderTmp.fontStyle = FontStyles.Bold;
            tipHeaderTmp.color = Color.white;
            tipHeaderTmp.alignment = TextAlignmentOptions.Center;
            tipHeaderTmp.raycastTarget = false;

            // 10. Rotating Culinary Tip Content
            RectTransform tipContent = CreateRect("TipContent", rootContainer);
            tipContent.anchoredPosition = new Vector2(0f, -395f);
            tipContent.sizeDelta = new Vector2(1120f, 150f);
            CanvasGroup tipCg = tipContent.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI tipTmp = tipContent.gameObject.AddComponent<TextMeshProUGUI>();
            if (tipFont != null) tipTmp.font = tipFont;
            tipTmp.text = "Carinderia Fun Fact — Carinderias are nicknamed 'turo-turo' ('point-point') because diners simply point at the glass-covered trays of cooked ulam!";
            tipTmp.fontSize = 24f;
            tipTmp.color = Color.white;
            tipTmp.alignment = TextAlignmentOptions.Top;
            tipTmp.lineSpacing = 6f;
            tipTmp.wordWrappingRatios = 0.4f;
            tipTmp.textWrappingMode = TextWrappingModes.Normal;
            tipTmp.raycastTarget = false;

            // 11. Attach & Wire LoadingScreenController
            LoadingScreenController controller = canvasGo.AddComponent<LoadingScreenController>();
            var so = new SerializedObject(controller);
            SetRef(so, "rootCanvasGroup", rootCg);
            SetRef(so, "progressBar", slider);
            SetRef(so, "progressFillImage", fillImg);
            SetRef(so, "percentText", null);
            SetRef(so, "statusText", null);
            SetRef(so, "tipText", tipTmp);
            SetRef(so, "tipCanvasGroup", tipCg);
            SetRef(so, "cookingIcon", utensilsRect);
            so.FindProperty("iconBobAmplitude").floatValue = 5f;
            so.FindProperty("iconBobSpeed").floatValue = 2.5f;
            so.FindProperty("fadeOutDuration").floatValue = 0f;
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
            fallbackRoundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            backgroundSprite = LoadFirstSprite(BackgroundPath);
            foodDecorSprite = LoadFirstSprite(FoodDecorPath);
            utensilsSprite = LoadFirstSprite(UtensilsPath);
            pillSprite = LoadFirstSprite(PillSpritePath);

            tipFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TipFontPath);
            if (tipFont == null)
            {
                tipFont = TMP_Settings.defaultFontAsset;
            }
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (direct != null) return direct;

            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
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
