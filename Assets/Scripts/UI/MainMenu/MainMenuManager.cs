using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CookingSystem.UI
{
    /// <summary>
    /// Drives the main menu: opens/closes popups (Play Configuration, Settings, Quit Confirmation, Credits),
    /// keeps a popup stack, blocks the background menu while any popup is open, handles Escape / gamepad B,
    /// and restores keyboard/gamepad focus when popups close.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Main Menu (Background)")]
        [Tooltip("CanvasGroup wrapping all main menu buttons. Made non-interactable while any popup is open.")]
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private Button startCookingButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [Tooltip("Optional.")]
        [SerializeField] private Button creditsButton;

        [Header("Popups")]
        [SerializeField] private UIPopupPanel playConfigPopup;
        [SerializeField] private UIPopupPanel settingsPopup;
        [SerializeField] private UIPopupPanel quitConfirmPopup;
        [Tooltip("Optional.")]
        [SerializeField] private UIPopupPanel creditsPopup;

        [Header("Play Configuration")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadSaveButton;
        [Tooltip("Optional object shown when no save exists (e.g. a 'No save found' label).")]
        [SerializeField] private GameObject noSaveFoundHint;
        [Tooltip("Optional label describing the existing save (day + last saved time).")]
        [SerializeField] private TMP_Text saveInfoLabel;

        [Header("Overwrite Confirmation (Optional)")]
        [Tooltip("Shown when 'New Game' would overwrite an existing save. Leave empty to skip the warning.")]
        [SerializeField] private UIPopupPanel overwriteConfirmPopup;
        [SerializeField] private Button confirmOverwriteButton;
        [SerializeField] private Button cancelOverwriteButton;

        [Header("Quit Confirmation")]
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;

        [Header("Scene Loading")]
        [Tooltip("Scene loaded by New Game / Load Save. Must be added to the Build Profile scene list.")]
        [SerializeField] private string gameplaySceneName = "gamescene";

        [Header("Intro Animation (Optional)")]
        [Tooltip("Smooth Pan Right entrance animation on boot.")]
        [SerializeField] private UIPanRightIntro panRightIntro;

        [Header("Input")]
        [Tooltip("Escape / gamepad B closes the top popup, or opens the quit prompt when no popup is open.")]
        [SerializeField] private bool handleBackInput = true;

        private struct PopupEntry
        {
            public UIPopupPanel Popup;
            public GameObject ReturnSelection; // What to focus again when this popup closes.
        }

        private readonly List<PopupEntry> popupStack = new List<PopupEntry>();
        private readonly List<UIPopupPanel> registeredPopups = new List<UIPopupPanel>();
        private bool isLoadingScene;
        private bool isUsingPointer = true;

        public bool IsAnyPopupOpen => popupStack.Count > 0;

        #region Unity Lifecycle

        private void Awake()
        {
            Bind(startCookingButton, OpenPlayConfiguration);
            Bind(settingsButton, OpenSettings);
            Bind(exitButton, OpenQuitConfirmation);
            Bind(creditsButton, OpenCredits);

            Bind(newGameButton, StartNewGame);
            Bind(loadSaveButton, LoadSavedGame);

            Bind(confirmOverwriteButton, ConfirmOverwriteAndStartNewGame);
            Bind(cancelOverwriteButton, () => ClosePopup(overwriteConfirmPopup));

            Bind(confirmQuitButton, QuitGame);
            Bind(cancelQuitButton, () => ClosePopup(quitConfirmPopup));

            RegisterPopup(playConfigPopup);
            RegisterPopup(settingsPopup);
            RegisterPopup(quitConfirmPopup);
            RegisterPopup(creditsPopup);
            RegisterPopup(overwriteConfirmPopup);

            if (panRightIntro != null)
            {
                panRightIntro.onIntroComplete.AddListener(HandleIntroComplete);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Browsers can't be closed by Application.Quit, so hide the Exit button entirely.
            if (exitButton != null) exitButton.gameObject.SetActive(false);
#endif
        }

        private void Start()
        {
            // Guarantee a clean state even if a popup was left active in the editor.
            foreach (UIPopupPanel popup in registeredPopups)
            {
                popup.HideInstant();
            }

            RefreshMainMenuInteractivity();
        }

        private void OnDestroy()
        {
            foreach (UIPopupPanel popup in registeredPopups)
            {
                if (popup != null) popup.CloseRequested -= HandlePopupCloseRequested;
            }
        }

        private void Update()
        {
            TrackInputMode();

            if (handleBackInput && !isLoadingScene && WasBackPressedThisFrame())
            {
                if (IsAnyPopupOpen) CloseTopPopup();
                else OpenQuitConfirmation();
            }
        }

        #endregion

        #region Public Actions (also usable from Inspector OnClick)

        public void OpenPlayConfiguration()
        {
            RefreshSaveAvailability();
            OpenPopup(playConfigPopup);
        }

        public void OpenSettings() => OpenPopup(settingsPopup);

        public void OpenQuitConfirmation() => OpenPopup(quitConfirmPopup);

        public void OpenCredits() => OpenPopup(creditsPopup);

        /// <summary>"New Game": warns first if it would overwrite an existing save (when the popup is assigned).</summary>
        public void StartNewGame()
        {
            if (overwriteConfirmPopup != null && HasSaveData())
            {
                OpenPopup(overwriteConfirmPopup);
                return;
            }

            BeginNewGame();
        }

        public void ConfirmOverwriteAndStartNewGame() => BeginNewGame();

        public void LoadSavedGame()
        {
            if (!SaveSystem.TryLoad(out _))
            {
                Debug.LogWarning("[MainMenuManager] No readable save found.", this);
                RefreshSaveAvailability();
                return;
            }

            LoadGameplayScene(true);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CloseTopPopup()
        {
            if (IsAnyPopupOpen) ClosePopup(popupStack[popupStack.Count - 1].Popup);
        }

        public bool HasSaveData() => SaveSystem.HasSave();

        #endregion

        #region Popup Stack

        public void OpenPopup(UIPopupPanel popup)
        {
            if (popup == null || popup.IsOpen || isLoadingScene) return;

            EventSystem eventSystem = EventSystem.current;

            // Freeze whatever popup is currently on top; the new one stacks above it.
            if (IsAnyPopupOpen) popupStack[popupStack.Count - 1].Popup.SetSuspended(true);

            popupStack.Add(new PopupEntry
            {
                Popup = popup,
                ReturnSelection = eventSystem != null ? eventSystem.currentSelectedGameObject : null
            });

            // Drop focus from the background so Submit can't re-trigger it during the transition.
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);

            RefreshMainMenuInteractivity();
            popup.Show(selectFirst: !isUsingPointer);
        }

        public void ClosePopup(UIPopupPanel popup)
        {
            if (popup == null) return;

            int index = popupStack.FindIndex(entry => entry.Popup == popup);
            if (index < 0) return;

            PopupEntry closing = popupStack[index];
            bool wasTop = index == popupStack.Count - 1;
            popupStack.RemoveAt(index);
            popup.Hide();

            if (!wasTop)
            {
                // Closed from the middle of the stack: hand its return-focus to the popup that was above it.
                PopupEntry above = popupStack[index];
                above.ReturnSelection = closing.ReturnSelection;
                popupStack[index] = above;
                return;
            }

            if (IsAnyPopupOpen) popupStack[popupStack.Count - 1].Popup.SetSuspended(false);

            RefreshMainMenuInteractivity();
            RestoreSelection(closing.ReturnSelection);
        }

        private void RegisterPopup(UIPopupPanel popup)
        {
            if (popup == null || registeredPopups.Contains(popup)) return;

            registeredPopups.Add(popup);
            popup.CloseRequested += HandlePopupCloseRequested;
        }

        private void HandlePopupCloseRequested(UIPopupPanel popup) => ClosePopup(popup);

        private void HandleIntroComplete()
        {
            RefreshMainMenuInteractivity();

            if (!isUsingPointer && startCookingButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(startCookingButton.gameObject);
            }
        }

        private void RefreshMainMenuInteractivity()
        {
            if (mainMenuGroup == null) return;

            bool isIntroBlocking = panRightIntro != null && panRightIntro.IsIntroPlaying;
            bool isInteractive = !IsAnyPopupOpen && !isLoadingScene && !isIntroBlocking;
            mainMenuGroup.interactable = isInteractive;   // Blocks keyboard/gamepad navigation & submit.
            mainMenuGroup.blocksRaycasts = isInteractive; // Blocks mouse/touch hover & clicks.
        }

        private void RestoreSelection(GameObject target)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            // Mouse users don't need a focus highlight; keyboard/gamepad users get focus back where they left it.
            bool canRestore = !isUsingPointer && target != null && target.activeInHierarchy;
            eventSystem.SetSelectedGameObject(canRestore ? target : null);
        }

        #endregion

        #region Play Configuration

        private void RefreshSaveAvailability()
        {
            bool hasSave = SaveSystem.TryPeek(out SaveData save);
            if (loadSaveButton != null) loadSaveButton.interactable = hasSave;
            if (noSaveFoundHint != null) noSaveFoundHint.SetActive(!hasSave);

            if (saveInfoLabel != null)
            {
                saveInfoLabel.gameObject.SetActive(hasSave);
                if (hasSave) saveInfoLabel.text = DescribeSave(save);
            }
        }

        private static string DescribeSave(SaveData save)
        {
            string description = $"Day {save.currentDay}";
            if (save.TryGetLastSavedLocalTime(out DateTime savedAt))
            {
                description += $"  |  Saved {savedAt:MMM d, h:mm tt}";
            }

            return description;
        }

        private void BeginNewGame()
        {
            if (isLoadingScene || !CanLoadGameplayScene()) return;

            // Create the save slot right away so "Load Save" is available next time.
            SaveSystem.StartNewGame();
            SaveSystem.Save();

            LoadGameplayScene(false);
        }

        private bool CanLoadGameplayScene()
        {
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName)) return true;

            Debug.LogError($"[MainMenuManager] Scene '{gameplaySceneName}' can't be loaded. " +
                           "Add it to File > Build Profiles > Scene List.", this);
            return false;
        }

        private void LoadGameplayScene(bool loadFromSave)
        {
            if (isLoadingScene || !CanLoadGameplayScene()) return;

            GameLaunchOptions.LoadFromSave = loadFromSave;
            isLoadingScene = true;

            // Lock every piece of UI so nothing can be double-clicked while the scene loads.
            if (IsAnyPopupOpen) popupStack[popupStack.Count - 1].Popup.SetSuspended(true);
            RefreshMainMenuInteractivity();

            SceneManager.LoadSceneAsync(gameplaySceneName);
        }

        #endregion

        #region Input

        private void TrackInputMode()
        {
            bool wasUsingPointer = isUsingPointer;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 4f))
                isUsingPointer = true;

            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                isUsingPointer = true;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                isUsingPointer = false;

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame
                                    || gamepad.dpad.ReadValue().sqrMagnitude > 0.25f
                                    || gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f))
                isUsingPointer = false;
#else
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) isUsingPointer = true;
            else if (Input.anyKeyDown) isUsingPointer = false;
#endif

            // Switched to keyboard/gamepad with nothing focused: focus a sensible default so navigation works.
            if (wasUsingPointer && !isUsingPointer)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.currentSelectedGameObject == null)
                {
                    Selectable fallback = IsAnyPopupOpen
                        ? popupStack[popupStack.Count - 1].Popup.FirstSelected
                        : startCookingButton;

                    if (fallback != null && fallback.IsInteractable())
                        eventSystem.SetSelectedGameObject(fallback.gameObject);
                }
            }
        }

        private static bool WasBackPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        #endregion

        private static void Bind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
