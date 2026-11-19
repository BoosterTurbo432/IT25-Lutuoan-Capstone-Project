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
        public TMP_Text rankText;
        public TMP_Text statsText;
        public Button upgradeButton;
        public TMP_Text upgradeButtonLabel;
        public Image[] rankPips;
    }

    [Serializable]
    public class LockedUpgradeCardRefs
    {
        public string upgradeTitle;
        public Button cardButton;
        public Button previewButton;
    }

    /// <summary>
    /// Drives UpgradeShopScene: lets players spend awarded Skill Points on 3 active kitchen upgrades,
    /// preview 3 locked Full-Version silhouette upgrades ("Soon in Full Version"),
    /// respec freely between shifts, and advance to the next Campaign Day or Endless Mode.
    /// </summary>
    public class UpgradeShopManager : MonoBehaviour
    {
        [Header("Header & Resources")]
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text skillPointsText;
        [SerializeField] private TMP_Text saveSummaryText;
        [SerializeField] private TMP_Text feedbackBannerText;

        [Header("3 Active Upgrade Cards")]
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

        private static readonly Color PipFilledColor = new Color(0.98f, 0.75f, 0.14f, 1f);
        private static readonly Color PipEmptyColor = new Color(0.24f, 0.16f, 0.12f, 0.90f);

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
                        card.upgradeButton.onClick.AddListener(() => HandlePurchaseUpgrade(capturedId));
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
                    ? $"You have {activeSave.skillPoints} Skill Point(s) ready to spend!"
                    : "Adjust your active kitchen upgrades or preview locked Full Version items.";
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

        private void HandlePurchaseUpgrade(KitchenUpgradeId id)
        {
            if (activeSave == null) return;

            if (!ProgressionConfig.TryPurchaseUpgrade(activeSave, id))
            {
                if (feedbackBannerText != null)
                {
                    feedbackBannerText.text = activeSave.skillPoints <= 0
                        ? "No Skill Points left! Clear a day or Reset Build to swap upgrades."
                        : "That upgrade is already at Max Rank (3/3)!";
                }
                return;
            }

            SaveSystem.Save();
            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = $"Upgraded {GetUpgradeDisplayName(id)} to Rank {ProgressionConfig.GetUpgradeRank(activeSave, id)}!";
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
                    feedbackBannerText.text = $"Refunded {refunded} Skill Point(s). Customize your build!";
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
                LoadingBridge.CustomStatusMessage = "Launching Endless Rush...";
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
                    subtitleText.text = "Day 3 Cleared! Endless Mode Unlocked — Spend Skill Points & Enter Endless Rush";
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

            int totalSpent = activeSave.prepMasteryLevel + activeSave.fastBurnerLevel + activeSave.hospitalityLevel;
            if (resetUpgradesButton != null)
            {
                resetUpgradesButton.interactable = totalSpent > 0;
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

                int rank = ProgressionConfig.GetUpgradeRank(activeSave, card.upgradeId);
                bool isMax = rank >= ProgressionConfig.MaxUpgradeRank;

                if (card.rankText != null)
                {
                    card.rankText.text = $"Rank {rank}/{ProgressionConfig.MaxUpgradeRank}";
                }

                if (card.statsText != null)
                {
                    card.statsText.text = BuildUpgradeStatSummary(card.upgradeId, rank);
                }

                if (card.rankPips != null)
                {
                    for (int p = 0; p < card.rankPips.Length; p++)
                    {
                        if (card.rankPips[p] != null)
                        {
                            card.rankPips[p].color = p < rank ? PipFilledColor : PipEmptyColor;
                        }
                    }
                }

                if (card.upgradeButton != null)
                {
                    card.upgradeButton.interactable = !isMax && activeSave.skillPoints > 0;
                }

                if (card.upgradeButtonLabel != null)
                {
                    card.upgradeButtonLabel.text = isMax
                        ? "Maxed Out"
                        : (activeSave.skillPoints > 0 ? "Upgrade (1 SP)" : "Need 1 SP");
                }
            }
        }

        private static string GetUpgradeDisplayName(KitchenUpgradeId id)
        {
            return id switch
            {
                KitchenUpgradeId.PrepMastery => "Precision Knife & Ladle",
                KitchenUpgradeId.FastBurner => "Turbo Twin-Jet Burners",
                KitchenUpgradeId.Hospitality => "Warm Hospitality",
                _ => "Upgrade"
            };
        }

        private string BuildUpgradeStatSummary(KitchenUpgradeId id, int rank)
        {
            switch (id)
            {
                case KitchenUpgradeId.PrepMastery:
                {
                    int sweetPct = Mathf.RoundToInt(ProgressionConfig.GetPrepSweetSpotHalfWidth(activeSave) * 200f);
                    int speedPct = Mathf.RoundToInt(ProgressionConfig.GetPrepCursorSpeedMultiplier(activeSave) * 100f);
                    string curr = $"Sweet Spot: {sweetPct}% Bar  |  Speed: {speedPct}%";
                    if (rank >= ProgressionConfig.MaxUpgradeRank) return $"{curr}\n<color=#4ADE80>Max Precision Mastery Reached!</color>";
                    int nextSweet = Mathf.RoundToInt((0.16f + (rank + 1) * 0.045f) * 200f);
                    int nextSpeed = Mathf.RoundToInt((1f - (rank + 1) * 0.08f) * 100f);
                    return $"{curr}\n<color=#FDE047>Next: {nextSweet}% Sweet Spot, {nextSpeed}% Speed</color>";
                }
                case KitchenUpgradeId.FastBurner:
                {
                    float cookSecs = 4.0f * ProgressionConfig.GetCookDurationMultiplier(activeSave);
                    float graceSecs = 3.0f + ProgressionConfig.GetBurnGraceBonusSeconds(activeSave);
                    string curr = $"Cook: {cookSecs:F1}s  |  Fire Grace: {graceSecs:F1}s";
                    if (rank >= ProgressionConfig.MaxUpgradeRank) return $"{curr}\n<color=#4ADE80>Max Burner Speed & Safety Reached!</color>";
                    float nextCook = 4.0f * (1f - (rank + 1) * 0.14f);
                    float nextGrace = 3.0f + (rank + 1) * 0.6f;
                    return $"{curr}\n<color=#FDE047>Next: {nextCook:F1}s Cook, {nextGrace:F1}s Grace</color>";
                }
                default:
                {
                    int patPct = Mathf.RoundToInt(ProgressionConfig.GetCustomerPatienceMultiplier(activeSave) * 100f);
                    int tipPct = Mathf.RoundToInt((ProgressionConfig.GetScoreMultiplier(activeSave) - 1f) * 100f);
                    float freezeBonus = ProgressionConfig.GetDrinkFreezeBonusSeconds(activeSave);
                    string curr = $"Patience: {patPct}%  |  Freeze: +{freezeBonus:F1}s  |  Tips: +{tipPct}%";
                    if (rank >= ProgressionConfig.MaxUpgradeRank) return $"{curr}\n<color=#4ADE80>Max Hospitality Reached!</color>";
                    int nextPat = Mathf.RoundToInt((1f + (rank + 1) * 0.12f) * 100f);
                    int nextTip = (rank + 1) * 10;
                    return $"{curr}\n<color=#FDE047>Next: {nextPat}% Patience, +{(rank + 1) * 1.5f:F1}s Freeze, +{nextTip}% Tips</color>";
                }
            }
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
