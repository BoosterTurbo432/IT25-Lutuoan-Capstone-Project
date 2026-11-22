using System;
using CookingSystem.Core.Responsive;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem
{
    [Serializable]
    public class UpgradeCardRefs
    {
        public KitchenUpgradeId upgradeId;
        public TMP_Text statusBadgeText;
        public TMP_Text statsText;
        public Button upgradeButton;
        public TMP_Text upgradeButtonLabel;
    }

    [Serializable]
    public class LockedUpgradeCardRefs
    {
        public string upgradeTitle;
        public Button cardButton;
        public Button previewButton;
    }

    /// <summary>
    /// Drives UpgradeShopScene for the 3-Day Demo:
    /// No upgrade ranks or tiers — unlocking any of the 3 active cards immediately grants
    /// its full maxed-out bonus for the rest of the demo run.
    /// </summary>
    public class UpgradeShopManager : MonoBehaviour
    {
        [Header("Header & Resources")]
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text skillPointsText;
        [SerializeField] private TMP_Text saveSummaryText;
        [SerializeField] private TMP_Text feedbackBannerText;

        [Header("3 Active Demo Upgrade Cards (Instant Full Bonus)")]
        [SerializeField] private UpgradeCardRefs[] upgradeCards;

        [Header("3 Locked Preview Upgrade Cards")]
        [SerializeField] private LockedUpgradeCardRefs[] lockedCards;

        [Header("Soon in Full Version Popup")]
        [SerializeField] private GameObject soonPopupRoot;
        [SerializeField] private TMP_Text soonPopupTitleText;
        [SerializeField] private TMP_Text soonPopupMessageText;
        [SerializeField] private Button soonPopupCloseButton;
        [SerializeField] private Button soonPopupBackdropButton;

        [Header("Navigation Buttons")]
        [SerializeField] private Button resetUpgradesButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button nextShiftButton;
        [SerializeField] private TMP_Text nextShiftButtonLabel;

        [Header("Responsive Targets")]
        [SerializeField] private RectTransform mainPanelRect;
        [SerializeField] private RectTransform cardsRowRect;
        [SerializeField] private RectTransform headerBarRect;
        [SerializeField] private RectTransform footerBarRect;

        private SaveData activeSave;
        private ViewportOrientation? appliedOrientation;

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

        private void Awake()
        {
            ProgressionConfig.EndEndlessRun();

            if (soonPopupRoot != null) soonPopupRoot.SetActive(false);
            if (soonPopupCloseButton != null) soonPopupCloseButton.onClick.AddListener(HideSoonPopup);
            if (soonPopupBackdropButton != null) soonPopupBackdropButton.onClick.AddListener(HideSoonPopup);

            if (resetUpgradesButton != null) resetUpgradesButton.onClick.AddListener(HandleResetUpgrades);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenu);
            if (nextShiftButton != null) nextShiftButton.onClick.AddListener(HandleNextShift);

            if (upgradeCards != null)
            {
                for (int i = 0; i < upgradeCards.Length; i++)
                {
                    UpgradeCardRefs card = upgradeCards[i];
                    if (card != null && card.upgradeButton != null)
                    {
                        KitchenUpgradeId capturedId = card.upgradeId;
                        card.upgradeButton.onClick.AddListener(() => HandleUnlockUpgrade(capturedId));
                    }
                }
            }

            if (lockedCards != null)
            {
                for (int i = 0; i < lockedCards.Length; i++)
                {
                    LockedUpgradeCardRefs locked = lockedCards[i];
                    if (locked == null) continue;
                    string capturedTitle = locked.upgradeTitle;
                    if (locked.cardButton != null) locked.cardButton.onClick.AddListener(() => ShowSoonPopup(capturedTitle));
                    if (locked.previewButton != null) locked.previewButton.onClick.AddListener(() => ShowSoonPopup(capturedTitle));
                }
            }
        }

        private void Start()
        {
            if (SaveSystem.Current != null)
            {
                activeSave = SaveSystem.Current;
            }
            else if (SaveSystem.TryLoad(out SaveData loaded))
            {
                activeSave = loaded;
            }
            else
            {
                activeSave = SaveSystem.StartNewGame();
            }

            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = activeSave.skillPoints > 0
                    ? $"You have {activeSave.skillPoints} Skill Point(s)! Pick any card to unlock its FULL maxed bonus!"
                    : "Your unlocked kitchen bonuses stay active for the rest of the 3-Day Demo.";
            }

            RefreshUI();
        }

        private void ShowSoonPopup(string upgradeTitle)
        {
            if (soonPopupTitleText != null)
            {
                soonPopupTitleText.text = "Soon in Full Version";
            }

            if (soonPopupMessageText != null)
            {
                string itemName = string.IsNullOrEmpty(upgradeTitle) ? "This upgrade" : upgradeTitle;
                soonPopupMessageText.text = $"{itemName} is locked in the Showcase Demo.\nComing Soon in Full Version!";
            }

            if (soonPopupRoot != null)
            {
                soonPopupRoot.SetActive(true);
            }

            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = $"{upgradeTitle}: Soon in Full Version!";
            }
        }

        private void HideSoonPopup()
        {
            if (soonPopupRoot != null)
            {
                soonPopupRoot.SetActive(false);
            }
        }

        private void HandleUnlockUpgrade(KitchenUpgradeId id)
        {
            if (activeSave == null) return;

            if (!ProgressionConfig.TryUnlockDemoUpgrade(activeSave, id))
            {
                if (feedbackBannerText != null)
                {
                    feedbackBannerText.text = ProgressionConfig.IsDemoUpgradeUnlocked(activeSave, id)
                        ? $"{ProgressionConfig.GetUpgradeDisplayName(id)} is already unlocked at full strength!"
                        : "No Skill Points left! Clear a day or Reset Build to swap upgrades.";
                }
                return;
            }

            SaveSystem.Save();
            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = $"Unlocked {ProgressionConfig.GetUpgradeDisplayName(id)} — Full Maxed Bonus Active!";
            }
            RefreshUI();
        }

        private void HandleResetUpgrades()
        {
            if (activeSave == null) return;
            int refunded = ProgressionConfig.RefundAllUpgrades(activeSave);
            if (refunded > 0)
            {
                SaveSystem.Save();
                if (feedbackBannerText != null)
                {
                    feedbackBannerText.text = $"Refunded {refunded} Skill Point(s). Pick any full-bonus upgrade!";
                }
            }
            RefreshUI();
        }

        private void HandleNextShift()
        {
            SaveSystem.Save();

            if (IsReadyForEndless())
            {
                GameLaunchOptions.SelectedMode = GameMode.Endless;
                GameLaunchOptions.SelectedDay = 0;
                LoadingBridge.CustomStatusMessage = "Launching Endless Rush (Roguelike Draft)...";
            }
            else
            {
                int nextDay = Mathf.Clamp(activeSave.currentDay, 1, ProgressionConfig.TotalCampaignDays);
                GameLaunchOptions.SelectedMode = GameMode.CampaignDay;
                GameLaunchOptions.SelectedDay = nextDay;
                LoadingBridge.CustomStatusMessage = $"Preparing Day {nextDay} Shift...";
            }

            LoadingBridge.LoadScene("gamescene", loadFromSave: true);
        }

        private void HandleMainMenu()
        {
            SaveSystem.Save();
            SceneManager.LoadScene("mainmenuscene");
        }

        private bool IsReadyForEndless()
        {
            if (activeSave == null) return false;
            return activeSave.endlessModeUnlocked &&
                   (GameLaunchOptions.LastClearedDay >= ProgressionConfig.TotalCampaignDays || activeSave.highestDayCleared >= ProgressionConfig.TotalCampaignDays);
        }

        private void RefreshUI()
        {
            if (activeSave == null) return;

            if (subtitleText != null)
            {
                if (IsReadyForEndless())
                {
                    subtitleText.text = "Day 3 Cleared! Endless Mode Unlocked — Draft Upgrades Live to Kitchen Rank 5!";
                }
                else
                {
                    int nextDay = Mathf.Clamp(activeSave.currentDay, 1, ProgressionConfig.TotalCampaignDays);
                    DayShiftConfig cfg = ProgressionConfig.GetDayConfig(nextDay);
                    subtitleText.text = $"Up Next: Day {nextDay}/3 — {cfg.shiftTitle} (Quota: {cfg.customerQuota} Customers)";
                }
            }

            if (skillPointsText != null)
            {
                skillPointsText.text = $"Skill Points: {activeSave.skillPoints}";
            }

            if (saveSummaryText != null)
            {
                saveSummaryText.text = $"Dishes Served: {activeSave.dishesServed}  |  Best Score: {activeSave.highScore}";
            }

            int totalUnlocked = (activeSave.prepMasteryLevel > 0 ? 1 : 0)
                              + (activeSave.fastBurnerLevel > 0 ? 1 : 0)
                              + (activeSave.hospitalityLevel > 0 ? 1 : 0);
            if (resetUpgradesButton != null)
            {
                resetUpgradesButton.interactable = totalUnlocked > 0;
            }

            if (nextShiftButtonLabel != null)
            {
                if (IsReadyForEndless())
                {
                    nextShiftButtonLabel.text = "Start Endless Mode";
                }
                else
                {
                    int nextDay = Mathf.Clamp(activeSave.currentDay, 1, ProgressionConfig.TotalCampaignDays);
                    nextShiftButtonLabel.text = $"Start Day {nextDay}";
                }
            }

            if (upgradeCards == null) return;
            for (int i = 0; i < upgradeCards.Length; i++)
            {
                UpgradeCardRefs card = upgradeCards[i];
                if (card == null) continue;

                bool isUnlocked = ProgressionConfig.IsDemoUpgradeUnlocked(activeSave, card.upgradeId);

                if (card.statusBadgeText != null)
                {
                    card.statusBadgeText.text = isUnlocked
                        ? "<color=#4ADE80>ACTIVE — FULL MAX BONUS</color>"
                        : (activeSave.skillPoints > 0
                            ? "<color=#FDE047>READY TO UNLOCK (1 SP)</color>"
                            : "<color=#94A3B8>NEEDS 1 SKILL POINT</color>");
                }

                if (card.statsText != null)
                {
                    card.statsText.text = BuildInstantMaxBonusSummary(card.upgradeId, isUnlocked);
                }

                if (card.upgradeButton != null)
                {
                    card.upgradeButton.interactable = !isUnlocked && activeSave.skillPoints > 0;
                }

                if (card.upgradeButtonLabel != null)
                {
                    card.upgradeButtonLabel.text = isUnlocked
                        ? "Unlocked (Active)"
                        : (activeSave.skillPoints > 0 ? "Unlock Full Bonus (1 SP)" : "Need 1 SP");
                }
            }
        }

        private static string BuildInstantMaxBonusSummary(KitchenUpgradeId id, bool unlocked)
        {
            string tag = unlocked ? "<color=#4ADE80>Active Bonus:</color> " : "<color=#FDE047>Instant Max Bonus:</color> ";
            return id switch
            {
                KitchenUpgradeId.PrepMastery =>
                    $"{tag}59% Sweet Spot  |  76% Cursor Speed\nEffortless Chop & Mix precision for the entire demo!",
                KitchenUpgradeId.FastBurner =>
                    $"{tag}2.3s Cook Time  |  4.8s Fire Grace\nBlazing fast pots & pans with extra burn protection!",
                _ =>
                    $"{tag}136% Patience  |  +4.5s Freeze  |  +30% Tips\nHappier diners and bigger cash rewards all shift!"
            };
        }

        private static Vector2 GetReferenceResolutionForOrientation(ViewportOrientation orientation)
        {
            return orientation == ViewportOrientation.Portrait
                ? new Vector2(1080f, 1920f)
                : new Vector2(1920f, 1080f);
        }

        private void ApplyViewportProfile(ViewportProfile profile)
        {
            if (appliedOrientation == profile.orientation) return;
            appliedOrientation = profile.orientation;

            bool portrait = profile.orientation == ViewportOrientation.Portrait;

            if (mainPanelRect != null)
            {
                mainPanelRect.sizeDelta = portrait ? new Vector2(1020f, 1800f) : new Vector2(1760f, 980f);
            }
            if (headerBarRect != null)
            {
                headerBarRect.anchoredPosition = portrait ? new Vector2(0f, 776f) : new Vector2(0f, 394f);
                headerBarRect.sizeDelta = portrait ? new Vector2(940f, 190f) : new Vector2(1660f, 134f);
            }
            if (cardsRowRect != null)
            {
                cardsRowRect.anchoredPosition = portrait ? new Vector2(0f, -16f) : new Vector2(0f, -10f);
                cardsRowRect.sizeDelta = portrait ? new Vector2(940f, 1280f) : new Vector2(1660f, 650f);

                GridLayoutGroup glg = cardsRowRect.GetComponent<GridLayoutGroup>();
                if (glg != null)
                {
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    glg.constraintCount = portrait ? 2 : 3;
                    glg.cellSize = portrait ? new Vector2(456f, 410f) : new Vector2(536f, 314f);
                    glg.spacing = portrait ? new Vector2(22f, 22f) : new Vector2(24f, 20f);
                }
            }
            if (footerBarRect != null)
            {
                footerBarRect.anchoredPosition = portrait ? new Vector2(0f, -792f) : new Vector2(0f, -412f);
                footerBarRect.sizeDelta = portrait ? new Vector2(940f, 130f) : new Vector2(1660f, 84f);
            }
        }
    }
}
