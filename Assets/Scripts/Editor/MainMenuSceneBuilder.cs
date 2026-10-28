using System.Linq;
using CookingSystem.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.EditorTools
{
    /// <summary>
    /// One-click builder for the main menu (Tools > Main Menu > Build Main Menu UI).
    /// - Re-parents the existing menu buttons into a CanvasGroup and adds hover effects.
    /// - Regenerates every popup (Play, Settings, Overwrite, Quit) under "PopupLayer".
    /// - Wires all MainMenuManager / UIPopupPanel / SettingsPanel references.
    /// Safe to run repeatedly. Note: "PopupLayer" is rebuilt from scratch on each run,
    /// so hand-made edits inside it are replaced.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string FontPath = "Assets/Fonts/Stay With Me SDF.asset";
        private const string GifPath = "Assets/CozyUI/Lutuoanbackground.gif";
        private const float ContentWidth = 560f;

        private enum ButtonStyle { Light, Primary, Danger }

        // Warm "kitchen" palette.
        private static readonly Color WindowColor = Hex("FFF6E5");
        private static readonly Color CardColor = Hex("FFFFFF");
        private static readonly Color IconFrameColor = Hex("EADCC9");
        private static readonly Color IconPlaceholderColor = new Color(0.85f, 0.8f, 0.75f, 0.5f);
        private static readonly Color TitleColor = Hex("5A3A22");
        private static readonly Color TextColor = Hex("3B2A1A");
        private static readonly Color MutedTextColor = Hex("8C7A6B");
        private static readonly Color PrimaryColor = Hex("F4A259");
        private static readonly Color DangerColor = Hex("E76F51");
        private static readonly Color LightHoverColor = Hex("FFEDBF");
        private static readonly Color LightPressedColor = Hex("E6C78C");
        private static readonly Color SliderTrackColor = Hex("E8D9C4");
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.6f);

        private static Sprite roundedSprite;
        private static Sprite backgroundSprite;
        private static Sprite lutuoanBackgroundSprite;
        private static Sprite knobSprite;
        private static Sprite checkmarkSprite;
        private static TMP_FontAsset font;

        #region Entry Points

        [MenuItem("Tools/Main Menu/Build Main Menu UI")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = OpenMainMenuScene();
            if (!Build(scene)) return;

            EditorSceneManager.SaveScene(scene);
            BuildSceneListSetup.AddGameScenes();
            Debug.Log("[MainMenuSceneBuilder] Main menu built and saved.");
        }

        [MenuItem("Tools/Main Menu/Create Standalone Credits Scene")]
        public static void CreateCreditsSceneFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildCreditsScene();
        }

        /// <summary>
        /// Batch mode: Unity.exe -batchmode -projectPath &lt;path&gt; -quit
        /// -executeMethod CookingSystem.EditorTools.MainMenuSceneBuilder.BuildFromCommandLine
        /// </summary>
        public static void BuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = OpenMainMenuScene();
            bool success = Build(scene) && EditorSceneManager.SaveScene(scene);

            BuildSceneListSetup.AddGameScenes();
            AssetDatabase.SaveAssets();

            Debug.Log(success ? "[MainMenuSceneBuilder] BUILD OK" : "[MainMenuSceneBuilder] BUILD FAILED");
            if (!success) EditorApplication.Exit(1);
        }

        #endregion

        #region Build

        private static Scene OpenMainMenuScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == BuildSceneListSetup.MainMenuScenePath) return active;
            return EditorSceneManager.OpenScene(BuildSceneListSetup.MainMenuScenePath, OpenSceneMode.Single);
        }

        private static bool Build(Scene scene)
        {
            LoadResources();

            Canvas canvas = scene.GetRootGameObjects()
                .Select(root => root.GetComponent<Canvas>())
                .Where(c => c != null)
                .OrderByDescending(c => c.name == "Canvas")
                .FirstOrDefault();

            if (canvas == null)
            {
                Debug.LogError("[MainMenuSceneBuilder] No root Canvas found in the main menu scene.");
                return false;
            }

            RectTransform canvasRect = (RectTransform)canvas.transform;
            ConfigureCanvasScaler(canvas);

            // --- Background & main menu group ---
            Transform bgChild = RenameChild(canvasRect, "BackgroundTint", "Background") ??
                                RenameChild(canvasRect, "Panel", "Background");
            if (bgChild == null) bgChild = canvasRect.Find("Background");

            RectTransform backgroundRect = bgChild != null ? (RectTransform)bgChild : CreateRect("Background", canvasRect);
            backgroundRect.name = "Background";
            Stretch(backgroundRect);
            // Slight margin padding so ambient drift pan (+-30px) does not expose borders
            backgroundRect.offsetMin = new Vector2(-40f, -20f);
            backgroundRect.offsetMax = new Vector2(40f, 20f);

            Image bgImage = GetOrAdd<Image>(backgroundRect.gameObject);
            bgImage.sprite = lutuoanBackgroundSprite ?? backgroundSprite;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;

            CanvasGroup bgCanvasGroup = GetOrAdd<CanvasGroup>(backgroundRect.gameObject);
            bgCanvasGroup.alpha = 1f;

            // Remove any old/broken or duplicate animator components
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(backgroundRect.gameObject);
            foreach (var oldAnim in backgroundRect.GetComponents<UIAnimatedBackground>())
            {
                Object.DestroyImmediate(oldAnim);
            }

            UIAnimatedBackground animBg = backgroundRect.gameObject.AddComponent<UIAnimatedBackground>();
            Sprite[] frames = LoadBackgroundFrames();
            if (frames != null && frames.Length > 0)
            {
                animBg.Frames = frames;
                animBg.FramesPerSecond = 8f;
            }

            Transform logo = RenameChild(canvasRect, "Image", "Logo");
            if (logo != null)
            {
                GetOrAdd<CanvasGroup>(logo.gameObject);
            }

            RectTransform menuGroup = FindOrCreateChild(canvasRect, "MainMenuGroup");
            Stretch(menuGroup);
            CanvasGroup menuCanvasGroup = GetOrAdd<CanvasGroup>(menuGroup.gameObject);

            Button startButton = AdoptMenuButton(canvasRect, menuGroup, "start", "StartCookingButton");
            Button settingsButton = AdoptMenuButton(canvasRect, menuGroup, "settings", "SettingsButton");
            Button creditsButton = AdoptMenuButton(canvasRect, menuGroup, "credits", "CreditsButton");
            Button exitButton = AdoptMenuButton(canvasRect, menuGroup, "exit", "ExitButton");

            if (startButton == null || settingsButton == null || exitButton == null)
            {
                Debug.LogError("[MainMenuSceneBuilder] Could not find the Start / Settings / Exit buttons.");
                return false;
            }

            // CanvasGroups on each button for individual alpha fading during the staggered pan
            GetOrAdd<CanvasGroup>(startButton.gameObject);
            GetOrAdd<CanvasGroup>(settingsButton.gameObject);
            GetOrAdd<CanvasGroup>(creditsButton.gameObject);
            GetOrAdd<CanvasGroup>(exitButton.gameObject);

            // Top-to-bottom order so automatic keyboard/gamepad navigation feels natural.
            foreach (Button button in new[] { startButton, settingsButton, creditsButton, exitButton })
            {
                if (button != null) button.transform.SetAsLastSibling();
            }

            // --- Pan Right Intro Animation ---
            UIPanRightIntro panIntro = GetOrAdd<UIPanRightIntro>(canvas.gameObject);
            var panSo = new SerializedObject(panIntro);
            SetRef(panSo, "logoRect", logo != null ? (RectTransform)logo : null);
            SetRef(panSo, "logoCanvasGroup", logo != null ? logo.GetComponent<CanvasGroup>() : null);

            var buttonsProp = panSo.FindProperty("menuButtons");
            if (buttonsProp != null)
            {
                buttonsProp.ClearArray();
                Button[] orderedButtons = new[] { startButton, settingsButton, creditsButton, exitButton };
                for (int i = 0; i < orderedButtons.Length; i++)
                {
                    if (orderedButtons[i] != null)
                    {
                        buttonsProp.InsertArrayElementAtIndex(buttonsProp.arraySize);
                        buttonsProp.GetArrayElementAtIndex(buttonsProp.arraySize - 1).objectReferenceValue = (RectTransform)orderedButtons[i].transform;
                    }
                }
            }
            SetRef(panSo, "mainMenuGroup", menuCanvasGroup);
            SetRef(panSo, "backgroundCanvasGroup", bgCanvasGroup);
            panSo.FindProperty("panDistance").floatValue = 140f;
            panSo.FindProperty("logoDuration").floatValue = 0.75f;
            panSo.FindProperty("buttonDuration").floatValue = 0.58f;
            panSo.FindProperty("buttonStaggerDelay").floatValue = 0.09f;
            panSo.ApplyModifiedPropertiesWithoutUndo();

            panIntro.RecordRestingPositions();

            // --- Popups (rebuilt every run) ---
            Transform oldLayer = canvasRect.Find("PopupLayer");
            if (oldLayer != null) Object.DestroyImmediate(oldLayer.gameObject);

            RectTransform popupLayer = CreateRect("PopupLayer", canvasRect);
            Stretch(popupLayer);

            PlayPopupRefs play = BuildPlayConfigPopup(popupLayer);
            SettingsPopupRefs settings = BuildSettingsPopup(popupLayer);
            CreditsPopupRefs credits = BuildCreditsPopup(popupLayer);
            ConfirmPopupRefs overwrite = BuildConfirmPopup(popupLayer, "OverwriteConfirmPopup",
                "Start Over?", "Starting a new game will replace your saved progress.",
                "Go Back", "Start New", new Vector2(860f, 470f));
            ConfirmPopupRefs quit = BuildConfirmPopup(popupLayer, "QuitConfirmPopup",
                null, "Are you sure you want to quit?",
                "Keep Cooking!", "Yes, Quit", new Vector2(820f, 400f));

            // --- Draw order: background, logo, menu, popups on top ---
            if (backgroundRect != null) backgroundRect.SetSiblingIndex(0);
            if (logo != null) logo.SetSiblingIndex(1);
            menuGroup.SetSiblingIndex(canvasRect.childCount - 2);
            popupLayer.SetAsLastSibling();

            // --- Manager wiring ---
            MainMenuManager manager = GetOrAdd<MainMenuManager>(canvas.gameObject);
            var so = new SerializedObject(manager);
            SetRef(so, "mainMenuGroup", menuCanvasGroup);
            SetRef(so, "startCookingButton", startButton);
            SetRef(so, "settingsButton", settingsButton);
            SetRef(so, "exitButton", exitButton);
            SetRef(so, "creditsButton", creditsButton);
            SetRef(so, "panRightIntro", panIntro);
            SetRef(so, "playConfigPopup", play.Popup);
            SetRef(so, "settingsPopup", settings.Popup);
            SetRef(so, "creditsPopup", credits.Popup);
            SetRef(so, "quitConfirmPopup", quit.Popup);
            SetRef(so, "newGameButton", play.NewGame);
            SetRef(so, "loadSaveButton", play.LoadSave);
            SetRef(so, "noSaveFoundHint", play.NoSaveHint);
            SetRef(so, "saveInfoLabel", play.SaveInfo);
            SetRef(so, "overwriteConfirmPopup", overwrite.Popup);
            SetRef(so, "confirmOverwriteButton", overwrite.Confirm);
            SetRef(so, "cancelOverwriteButton", overwrite.Cancel);
            SetRef(so, "confirmQuitButton", quit.Confirm);
            SetRef(so, "cancelQuitButton", quit.Cancel);
            so.FindProperty("gameplaySceneName").stringValue = "gamescene";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Start hidden in the editor (MainMenuManager also hides them on Start).
            play.Popup.gameObject.SetActive(false);
            settings.Popup.gameObject.SetActive(false);
            credits.Popup.gameObject.SetActive(false);
            overwrite.Popup.gameObject.SetActive(false);
            quit.Popup.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static Button AdoptMenuButton(Transform canvas, RectTransform menuGroup, string oldName, string newName)
        {
            Transform target = menuGroup.Find(newName);
            if (target == null)
            {
                target = canvas.Find(oldName);
                if (target == null) target = canvas.Find(newName);
                if (target == null) return null;

                // The group is stretch-filled over the canvas, so center-anchored positions stay identical.
                target.SetParent(menuGroup, false);
                target.name = newName;
            }

            Button button = target.GetComponent<Button>();
            if (button == null) return null;

            button.transition = Selectable.Transition.None;
            foreach (TMP_Text label in target.GetComponentsInChildren<TMP_Text>(true)) label.raycastTarget = false;

            ConfigureHover(button, Color.white, LightHoverColor, LightPressedColor);
            return button;
        }

        #endregion

        #region Popups

        private struct PlayPopupRefs
        {
            public UIPopupPanel Popup;
            public Button NewGame, LoadSave;
            public GameObject NoSaveHint;
            public TMP_Text SaveInfo;
        }

        private struct SettingsPopupRefs
        {
            public UIPopupPanel Popup;
        }

        private struct CreditsPopupRefs
        {
            public UIPopupPanel Popup;
        }

        private struct ConfirmPopupRefs
        {
            public UIPopupPanel Popup;
            public Button Confirm, Cancel;
        }

        private static PlayPopupRefs BuildPlayConfigPopup(Transform layer)
        {
            UIPopupPanel popup = CreatePopupRoot(layer, "PlayConfigPopup", out Button backdrop);
            RectTransform window = CreateWindow(popup.transform, new Vector2(720f, 640f));

            CreateTitle(window, "Ready to Cook?");
            Button newGame = CreateButton(window, "NewGameButton", "New Game", ButtonStyle.Primary);
            Button loadSave = CreateButton(window, "LoadSaveButton", "Load Save", ButtonStyle.Light);
            TMP_Text saveInfo = CreateLayoutText(window, "SaveInfoLabel", "Day 1", 30f, MutedTextColor, 40f);
            TMP_Text noSave = CreateLayoutText(window, "NoSaveHint", "No saved game yet", 30f, MutedTextColor, 40f);
            Button back = CreateButton(window, "BackButton", "Back", ButtonStyle.Light);

            ConfigurePopup(popup, window, newGame, backdrop, back);

            return new PlayPopupRefs
            {
                Popup = popup,
                NewGame = newGame,
                LoadSave = loadSave,
                NoSaveHint = noSave.gameObject,
                SaveInfo = saveInfo
            };
        }

        private static SettingsPopupRefs BuildSettingsPopup(Transform layer)
        {
            UIPopupPanel popup = CreatePopupRoot(layer, "SettingsPopup", out Button backdrop);
            RectTransform window = CreateWindow(popup.transform, new Vector2(760f, 540f));

            CreateTitle(window, "Settings");

            // Volume row: label | slider | value
            RectTransform volumeRow = CreateRow(window, "VolumeRow", 70f, 20f);
            CreateRowLabel(volumeRow, "VolumeLabel", "Volume", 190f);
            Slider slider = CreateSlider(volumeRow, "VolumeSlider", 260f);
            TMP_Text volumeValue = CreateRowLabel(volumeRow, "VolumeValue", "100%", 90f);
            volumeValue.alignment = TextAlignmentOptions.Right;

            // Fullscreen row: label | toggle
            RectTransform fullscreenRow = CreateRow(window, "FullscreenRow", 70f, 20f);
            CreateRowLabel(fullscreenRow, "FullscreenLabel", "Fullscreen", 394f);
            Toggle fullscreenToggle = CreateToggle(fullscreenRow, "FullscreenToggle");
            AddLayoutSpacer(fullscreenRow, 90f); // Lines the toggle up with the slider's right end.

            Button done = CreateButton(window, "DoneButton", "Done", ButtonStyle.Primary);

            ConfigurePopup(popup, window, slider, backdrop, done);

            SettingsPanel settingsPanel = popup.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(settingsPanel);
            SetRef(so, "masterVolumeSlider", slider);
            SetRef(so, "masterVolumeLabel", volumeValue);
            SetRef(so, "fullscreenToggle", fullscreenToggle);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new SettingsPopupRefs { Popup = popup };
        }

        private static CreditsPopupRefs BuildCreditsPopup(Transform layer)
        {
            UIPopupPanel popup = CreatePopupRoot(layer, "CreditsPopup", out Button backdrop);
            RectTransform window = CreateWindow(popup.transform, new Vector2(1180f, 740f));

            CreateTitle(window, "Credits");
            CreateLayoutText(window, "Subtitle", "Development Team", 30f, MutedTextColor, 36f, 1040f);

            // Container row for 5 member cards
            RectTransform membersRow = CreateRect("MembersRow", window);
            HorizontalLayoutGroup hlg = membersRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            LayoutElement rowLayout = membersRow.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = 1040f;
            rowLayout.preferredHeight = 360f;

            for (int i = 0; i < 5; i++)
            {
                CreateMemberCard(membersRow, i + 1);
            }

            Button back = CreateButton(window, "BackButton", "Back", ButtonStyle.Light, 320f, 76f);
            ConfigurePopup(popup, window, back, backdrop, back);

            return new CreditsPopupRefs { Popup = popup };
        }

        private static void CreateMemberCard(Transform parent, int index)
        {
            RectTransform card = CreateRect($"MemberCard_{index}", parent);
            AddImage(card, CardColor, roundedSprite, 0.35f);
            AddShadow(card.gameObject, new Vector2(0f, -4f), 0.2f);

            LayoutElement cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = 184f;
            cardLayout.preferredHeight = 350f;

            VerticalLayoutGroup vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 20, 16);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Avatar / Icon Frame
            RectTransform iconFrame = CreateRect("IconFrame", card);
            AddImage(iconFrame, IconFrameColor, roundedSprite, 0.5f);
            LayoutElement frameLayout = iconFrame.gameObject.AddComponent<LayoutElement>();
            frameLayout.preferredWidth = 120f;
            frameLayout.preferredHeight = 120f;

            // Blank image slot ready to be setup later
            RectTransform iconRect = CreateRect("ImageIcon", iconFrame);
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = null;
            iconImage.color = IconPlaceholderColor;
            iconImage.raycastTarget = false;

            // Member Name
            RectTransform nameRect = CreateRect("MemberName", card);
            TextMeshProUGUI nameText = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) nameText.font = font;
            nameText.text = $"Member {index}";
            nameText.fontSize = 34f;
            nameText.color = TextColor;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.raycastTarget = false;
            LayoutElement nameLayout = nameRect.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 160f;
            nameLayout.preferredHeight = 44f;

            // Member Role
            RectTransform roleRect = CreateRect("MemberRole", card);
            TextMeshProUGUI roleText = roleRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) roleText.font = font;
            roleText.text = "Role / Title";
            roleText.fontSize = 24f;
            roleText.color = MutedTextColor;
            roleText.alignment = TextAlignmentOptions.Center;
            roleText.raycastTarget = false;
            LayoutElement roleLayout = roleRect.gameObject.AddComponent<LayoutElement>();
            roleLayout.preferredWidth = 160f;
            roleLayout.preferredHeight = 32f;
        }

        private static ConfirmPopupRefs BuildConfirmPopup(Transform layer, string name, string title, string message,
            string cancelLabel, string confirmLabel, Vector2 windowSize)
        {
            UIPopupPanel popup = CreatePopupRoot(layer, name, out Button backdrop);
            RectTransform window = CreateWindow(popup.transform, windowSize);

            if (!string.IsNullOrEmpty(title)) CreateTitle(window, title);

            bool isMainMessage = string.IsNullOrEmpty(title);
            CreateLayoutText(window, "Message", message,
                isMainMessage ? 56f : 40f, isMainMessage ? TitleColor : TextColor,
                isMainMessage ? 150f : 110f, ContentWidth + 80f);

            RectTransform row = CreateRow(window, "ButtonRow", 96f, 30f);
            // Cancel first + focused by default: the safe choice for keyboard/gamepad users.
            Button cancel = CreateButton(row, "CancelButton", cancelLabel, ButtonStyle.Light, 320f);
            Button confirm = CreateButton(row, "ConfirmButton", confirmLabel, ButtonStyle.Danger, 320f);

            // Clicking outside = cancel (always safe). Cancel is wired by MainMenuManager.
            ConfigurePopup(popup, window, cancel, backdrop);

            return new ConfirmPopupRefs { Popup = popup, Confirm = confirm, Cancel = cancel };
        }

        private static UIPopupPanel CreatePopupRoot(Transform layer, string name, out Button backdropButton)
        {
            RectTransform root = CreateRect(name, layer);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            UIPopupPanel popup = root.gameObject.AddComponent<UIPopupPanel>();

            RectTransform backdrop = CreateRect("Backdrop", root);
            Stretch(backdrop);
            Image backdropImage = AddImage(backdrop, BackdropColor, null);
            backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdropImage;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.navigation = new Navigation { mode = Navigation.Mode.None }; // Never keyboard-focusable.

            return popup;
        }

        private static RectTransform CreateWindow(Transform popupRoot, Vector2 size)
        {
            RectTransform window = CreateRect("Window", popupRoot);
            window.sizeDelta = size;
            AddImage(window, WindowColor, roundedSprite, 0.25f);
            AddShadow(window.gameObject, new Vector2(0f, -10f), 0.35f);

            VerticalLayoutGroup layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 50, 50);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return window;
        }

        private static void ConfigurePopup(UIPopupPanel popup, RectTransform window, Selectable firstSelected,
            Button backdrop, params Button[] closeButtons)
        {
            var so = new SerializedObject(popup);
            SetRef(so, "window", window);
            SetRef(so, "firstSelected", firstSelected);
            SetRef(so, "backdropButton", backdrop);

            SerializedProperty closeArray = so.FindProperty("closeButtons");
            closeArray.arraySize = closeButtons.Length;
            for (int i = 0; i < closeButtons.Length; i++)
            {
                closeArray.GetArrayElementAtIndex(i).objectReferenceValue = closeButtons[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Widgets

        private static TMP_Text CreateTitle(Transform parent, string text)
        {
            return CreateLayoutText(parent, "Title", text, 72f, TitleColor, 90f);
        }

        private static TMP_Text CreateLayoutText(Transform parent, string name, string text, float fontSize,
            Color color, float height, float width = ContentWidth)
        {
            TextMeshProUGUI tmp = CreateText(parent, name, text, fontSize, color);
            LayoutElement layout = tmp.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return tmp;
        }

        private static TMP_Text CreateRowLabel(Transform row, string name, string text, float width)
        {
            TextMeshProUGUI tmp = CreateText(row, name, text, 44f, TextColor);
            tmp.alignment = TextAlignmentOptions.Left;
            LayoutElement layout = tmp.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 60f;
            return tmp;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, ButtonStyle style,
            float width = 460f, float height = 96f)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(width, height);

            Color baseColor = style switch
            {
                ButtonStyle.Primary => PrimaryColor,
                ButtonStyle.Danger => DangerColor,
                _ => Color.white
            };

            Image image = AddImage(rect, baseColor, roundedSprite, 0.5f);
            AddShadow(rect.gameObject, new Vector2(0f, -4f), 0.25f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None; // UIButtonHoverEffect handles visuals.

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            TextMeshProUGUI text = CreateText(rect, "Label", label, 52f,
                style == ButtonStyle.Danger ? Color.white : TextColor);
            Stretch(text.rectTransform);

            if (style == ButtonStyle.Light) ConfigureHover(button, baseColor, LightHoverColor, LightPressedColor);
            else ConfigureHover(button, baseColor, Color.Lerp(baseColor, Color.white, 0.25f), Darken(baseColor, 0.85f));

            return button;
        }

        private static RectTransform CreateRow(Transform parent, string name, float height, float spacing)
        {
            RectTransform row = CreateRect(name, parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ContentWidth + 80f;
            element.preferredHeight = height;
            return row;
        }

        private static void AddLayoutSpacer(Transform row, float width)
        {
            RectTransform spacer = CreateRect("Spacer", row);
            LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }

        private static Slider CreateSlider(Transform parent, string name, float width)
        {
            var resources = new DefaultControls.Resources
            {
                standard = roundedSprite,
                background = backgroundSprite,
                knob = knobSprite
            };

            GameObject go = DefaultControls.CreateSlider(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            SetLayerRecursively(go, LayerMask.NameToLayer("UI"));

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 40f;

            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transition = Selectable.Transition.None;

            Image track = go.transform.Find("Background").GetComponent<Image>();
            track.color = SliderTrackColor;
            Image fill = go.transform.Find("Fill Area/Fill").GetComponent<Image>();
            fill.color = PrimaryColor;

            // Hover feedback on the handle only, so the track doesn't jump around.
            RectTransform handle = slider.handleRect;
            handle.sizeDelta = new Vector2(36f, 0f);
            ConfigureHover(slider, Color.white, LightHoverColor, LightPressedColor, handle, 1.25f);

            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name)
        {
            RectTransform rect = CreateRect(name, parent);
            Image background = AddImage(rect, Color.white, roundedSprite, 0.5f);
            AddShadow(rect.gameObject, new Vector2(0f, -3f), 0.25f);

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 56f;
            layout.preferredHeight = 56f;

            RectTransform check = CreateRect("Checkmark", rect);
            Stretch(check);
            check.offsetMin = new Vector2(6f, 6f);
            check.offsetMax = new Vector2(-6f, -6f);
            Image checkImage = AddImage(check, TextColor, checkmarkSprite);
            checkImage.type = Image.Type.Simple;
            checkImage.preserveAspect = true;
            checkImage.raycastTarget = false;

            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.None;
            toggle.isOn = true;

            ConfigureHover(toggle, Color.white, LightHoverColor, LightPressedColor);
            return toggle;
        }

        private static void ConfigureHover(Selectable selectable, Color normal, Color hover, Color pressed,
            RectTransform scaleTarget = null, float hoverScale = 1.08f)
        {
            UIButtonHoverEffect effect = selectable.GetComponent<UIButtonHoverEffect>();
            if (effect == null) effect = selectable.gameObject.AddComponent<UIButtonHoverEffect>();

            var so = new SerializedObject(effect);
            so.FindProperty("normalColor").colorValue = normal;
            so.FindProperty("hoverColor").colorValue = hover;
            so.FindProperty("pressedColor").colorValue = pressed;
            so.FindProperty("hoverScale").floatValue = hoverScale;
            if (scaleTarget != null) SetRef(so, "scaleTarget", scaleTarget);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (selectable.targetGraphic != null) selectable.targetGraphic.color = normal;
        }

        #endregion

        #region Helpers

        private static string ResolveBackgroundGifPath()
        {
            string[] preferred = new[]
            {
                "Assets/CozyUI/Lutuoanbackground 1.gif",
                "Assets/CozyUI/Lutuoanbackground.gif"
            };

            foreach (string p in preferred)
            {
                if (System.IO.File.Exists(p)) return p;
            }

            string dir = "Assets/CozyUI";
            if (System.IO.Directory.Exists(dir))
            {
                string[] files = System.IO.Directory.GetFiles(dir, "*background*.gif");
                if (files.Length > 0) return files[0].Replace('\\', '/');

                string[] allGifs = System.IO.Directory.GetFiles(dir, "*.gif");
                if (allGifs.Length > 0) return allGifs[0].Replace('\\', '/');
            }

            return "Assets/CozyUI/Lutuoanbackground 1.gif";
        }

        private static void LoadResources()
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            checkmarkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

            string gifPath = ResolveBackgroundGifPath();
            lutuoanBackgroundSprite = AssetDatabase.LoadAllAssetsAtPath(gifPath).OfType<Sprite>().FirstOrDefault();
            if (lutuoanBackgroundSprite == null)
            {
                Sprite[] frames = LoadBackgroundFrames();
                if (frames != null && frames.Length > 0) lutuoanBackgroundSprite = frames[0];
            }

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogWarning($"[MainMenuSceneBuilder] Font not found at {FontPath}; using the TMP default font.");
                font = TMP_Settings.defaultFontAsset;
            }
        }

        private static Sprite[] LoadBackgroundFrames()
        {
            string framesDir = "Assets/CozyUI/BackgroundFrames";
            if (!System.IO.Directory.Exists(framesDir)) return null;

            string[] frameFiles = System.IO.Directory.GetFiles(framesDir, "bg_frame_*.png");
            if (frameFiles == null || frameFiles.Length == 0) return null;

            System.Array.Sort(frameFiles, string.CompareOrdinal);

            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (string file in frameFiles)
            {
                string unityPath = file.Replace('\\', '/');
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
                if (s != null) sprites.Add(s);
            }

            return sprites.Count > 0 ? sprites.ToArray() : null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? (RectTransform)existing : CreateRect(name, parent);
        }

        private static Transform RenameChild(Transform parent, string oldName, string newName)
        {
            Transform child = parent.Find(newName);
            if (child != null) return child;

            child = parent.Find(oldName);
            if (child != null) child.name = newName;
            return child;
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
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier; // < 1 = rounder corners on the 9-sliced sprite.
            return image;
        }

        private static void AddShadow(GameObject go, Vector2 distance, float alpha)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectDistance = distance;
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[MainMenuSceneBuilder] Property '{propertyName}' not found on {so.targetObject.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static Color Darken(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }

        public static void BuildCreditsScene()
        {
            LoadResources();
            string scenePath = "Assets/Scenes/creditsscene.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var cameraGo = new GameObject("Main Camera");
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("314D79"); // Matches main menu camera
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            // EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            var canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();
            ConfigureCanvasScaler(canvas);

            RectTransform canvasRect = (RectTransform)canvas.transform;

            // Background
            RectTransform bg = CreateRect("Background", canvasRect);
            Stretch(bg);
            Image creditsBg = AddImage(bg, Color.white, lutuoanBackgroundSprite ?? backgroundSprite);
            creditsBg.raycastTarget = false;

            // Credits Window
            RectTransform window = CreateRect("CreditsWindow", canvasRect);
            window.sizeDelta = new Vector2(1180f, 740f);
            AddImage(window, WindowColor, roundedSprite, 0.25f);
            AddShadow(window.gameObject, new Vector2(0f, -10f), 0.35f);

            VerticalLayoutGroup layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 45, 45);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTitle(window, "Credits");
            CreateLayoutText(window, "Subtitle", "Development Team", 30f, MutedTextColor, 36f, 1040f);

            // Members row
            RectTransform membersRow = CreateRect("MembersRow", window);
            HorizontalLayoutGroup hlg = membersRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            LayoutElement rowLayout = membersRow.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredWidth = 1040f;
            rowLayout.preferredHeight = 360f;

            for (int i = 0; i < 5; i++)
            {
                CreateMemberCard(membersRow, i + 1);
            }

            Button backButton = CreateButton(window, "BackToMainMenuButton", "Main Menu", ButtonStyle.Light, 340f, 76f);
            SceneLoader loader = canvasGo.AddComponent<SceneLoader>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(backButton.onClick, loader.LoadMainMenu);

            EditorSceneManager.SaveScene(newScene, scenePath);
            BuildSceneListSetup.AddGameScenes();
            Debug.Log($"[MainMenuSceneBuilder] Standalone Credits Scene created at {scenePath}");
        }

        #endregion
    }

    /// <summary>
    /// Keeps the Build Profile scene list usable: if it is empty (scene loading would fail),
    /// the game's scenes are added automatically with the main menu first.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildSceneListSetup
    {
        public const string MainMenuScenePath = "Assets/Scenes/mainmenuscene.unity";

        private static readonly string[] DefaultScenePaths =
        {
            MainMenuScenePath,
            "Assets/Scenes/loadingscreen.unity",
            "Assets/Scenes/gamescene.unity",
            "Assets/Scenes/leaderboardscene.unity",
            "Assets/Scenes/creditsscene.unity",
        };

        static BuildSceneListSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorBuildSettings.scenes.Length == 0) AddGameScenes();
            };
        }

        [MenuItem("Tools/Main Menu/Add Game Scenes to Build Profile")]
        public static void AddGameScenes()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            bool changed = false;

            foreach (string path in DefaultScenePaths)
            {
                if (!System.IO.File.Exists(path) || scenes.Any(scene => scene.path == path)) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }

            // The first enabled scene is the one the built game boots into.
            int mainMenuIndex = scenes.FindIndex(scene => scene.path == MainMenuScenePath);
            if (mainMenuIndex > 0 || (mainMenuIndex == 0 && !scenes[0].enabled))
            {
                EditorBuildSettingsScene mainMenu = scenes[mainMenuIndex];
                scenes.RemoveAt(mainMenuIndex);
                scenes.Insert(0, new EditorBuildSettingsScene(mainMenu.path, true));
                changed = true;
            }

            if (!changed) return;

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[BuildSceneListSetup] Build scene list updated: " +
                      string.Join(", ", scenes.Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path))));
        }
    }

    /// <summary>Developer shortcuts for testing the save system (Tools > Save System).</summary>
    public static class SaveSystemDevTools
    {
        [MenuItem("Tools/Save System/Open Save Folder")]
        private static void OpenSaveFolder()
        {
            System.IO.Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(SaveSystem.SaveFilePath);
        }

        [MenuItem("Tools/Save System/Create Test Save")]
        private static void CreateTestSave()
        {
            SaveData data = SaveSystem.StartNewGame();
            data.currentDay = 3;
            data.coins = 150;
            data.dishesServed = 12;

            if (SaveSystem.Save()) Debug.Log($"[SaveSystem] Test save written to {SaveSystem.SaveFilePath}");
        }

        [MenuItem("Tools/Save System/Delete Save")]
        private static void DeleteSave()
        {
            if (!SaveSystem.HasSave())
            {
                Debug.Log("[SaveSystem] No save to delete.");
                return;
            }

            if (EditorUtility.DisplayDialog("Delete Save", "Delete the save file? This can't be undone.", "Delete", "Cancel"))
            {
                SaveSystem.Delete();
                Debug.Log("[SaveSystem] Save deleted.");
            }
        }
    }
}
