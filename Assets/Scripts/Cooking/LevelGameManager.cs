using System;
using System.Collections.Generic;
using CookingSystem.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Controls the 3-Day Carinderia progression loop, Day 1 Tutorial Pause,
    /// Endless Mode (no timer, 3-Lives/Strikes, dynamic Rush Tiers),
    /// Upgrade multipliers, SaveSystem persistence, and end-of-shift modals.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelGameManager : MonoBehaviour
    {
        public static LevelGameManager Instance { get; private set; }

        public const string UpgradeShopSceneName = "UpgradeShopScene";

        [Header("Level Configuration")]
        [SerializeField] private float levelDurationSeconds = 180f;
        [SerializeField] private int shiftCustomerQuota = 6;
        [SerializeField] private int pointsPerRemainingSecond = 5;
        [SerializeField] private int comboBonusPoints = 120;

        [Header("HUD References")]
        [SerializeField] private TMP_Text dayModeBadgeText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillBar;
        [SerializeField] private RectTransform timerThumbRect;
        [SerializeField] private float timerTrackWidth = 160f;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ordersText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private Button restartHudButton;
        [SerializeField] private Button mainMenuHudButton;

        [Header("Day 1 Visual Tutorial Modal")]
        [SerializeField] private GameObject tutorialModalRoot;
        [SerializeField] private Button startShiftButton;

        [Header("End-of-Level Summary Modal")]
        [SerializeField] private GameObject summaryModalRoot;
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryBreakdownText;
        [SerializeField] private TextMeshProUGUI summaryFinalScoreText;
        [SerializeField] private Button upgradeShopButton;
        [SerializeField] private TMP_Text upgradeShopButtonLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private TMP_Text playAgainButtonLabel;
        [SerializeField] private Button summaryMainMenuButton;

        [Header("Endless Leaderboard Submission")]
        [SerializeField] private GameObject leaderboardSubmitGroup;
        [SerializeField] private TMP_InputField leaderboardNameInput;
        [SerializeField] private Button mobileNamePromptButton;
        [SerializeField] private Button submitLeaderboardButton;
        [SerializeField] private TMP_Text submitLeaderboardButtonLabel;
        [SerializeField] private TMP_Text leaderboardSubmitStatusText;
        [SerializeField] private Button viewLeaderboardButton;
        [SerializeField] private LeaderboardPanelController endlessLeaderboardPanel;

        private GameMode activeMode = GameMode.CampaignDay;
        private int activeDay = 1;
        private DayShiftConfig activeDayConfig;

        private float remainingTime;
        private float elapsedEndlessSeconds;
        private bool isLevelActive;
        private bool isTutorialOpen;
        private bool hasShiftEnded;
        private bool hasSubmittedLeaderboardScore;

        private int livesRemaining = ProgressionConfig.EndlessMaxLives;
        private int currentEndlessTier;

        private int totalScore;
        private int baseDishPointsTotal;
        private int qualityBonusTotal;
        private int comboBonusTotal;
        private int drinkPointsTotal;
        private int dishesServedCount;
        private int drinksServedCount;
        private int combosCompletedCount;
        private int customersCompletedCount;
        private int customersLostCount;
        private int currentStreak;
        private int highestComboStreak = 1;
        private float scoreMultiplier = 1f;

        private readonly List<float> satisfactionSamples = new List<float>();
        private readonly List<float> qualitySamples = new List<float>();

        public GameMode ActiveMode => activeMode;
        public int ActiveDay => activeDay;
        public bool IsEndlessMode => activeMode == GameMode.Endless;
        public bool IsTutorialOpen => isTutorialOpen;
        public bool IsLevelActive => isLevelActive && !isTutorialOpen && !hasShiftEnded;
        public float RemainingTime => remainingTime;
        public int TotalScore => totalScore;
        public int ShiftCustomerQuota => shiftCustomerQuota;
        public int LivesRemaining => livesRemaining;
        public int CurrentEndlessTier => currentEndlessTier;

        public event Action<bool> onLevelEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (summaryModalRoot != null) summaryModalRoot.SetActive(false);
            if (tutorialModalRoot != null) tutorialModalRoot.SetActive(false);
            if (leaderboardSubmitGroup != null) leaderboardSubmitGroup.SetActive(false);
            if (endlessLeaderboardPanel != null) endlessLeaderboardPanel.CloseOverlay();

            if (startShiftButton != null) startShiftButton.onClick.AddListener(DismissTutorial);
            if (restartHudButton != null) restartHudButton.onClick.AddListener(RestartLevel);
            if (mainMenuHudButton != null) mainMenuHudButton.onClick.AddListener(GoToMainMenu);
            if (upgradeShopButton != null) upgradeShopButton.onClick.AddListener(GoToUpgradeShop);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartLevel);
            if (summaryMainMenuButton != null) summaryMainMenuButton.onClick.AddListener(GoToMainMenu);
            if (submitLeaderboardButton != null) submitLeaderboardButton.onClick.AddListener(SubmitEndlessScore);
            if (mobileNamePromptButton != null) mobileNamePromptButton.onClick.AddListener(PromptMobileNameInput);
            if (viewLeaderboardButton != null) viewLeaderboardButton.onClick.AddListener(OpenEndlessLeaderboard);

            if (leaderboardNameInput != null)
            {
                leaderboardNameInput.characterLimit = LeaderboardSystem.MaxNameLength;
            }

            InitializeSessionConfig();
        }

        private void InitializeSessionConfig()
        {
            SaveData save = SaveSystem.GetOrCreateCurrent();
            scoreMultiplier = ProgressionConfig.GetScoreMultiplier(save);
            activeMode = GameLaunchOptions.SelectedMode;

            if (activeMode == GameMode.Endless)
            {
                activeDay = ProgressionConfig.TotalCampaignDays;
                livesRemaining = ProgressionConfig.EndlessMaxLives;
                elapsedEndlessSeconds = 0f;
                currentEndlessTier = 0;
                shiftCustomerQuota = 0;
            }
            else
            {
                int resolvedDay = GameLaunchOptions.SelectedDay > 0
                    ? GameLaunchOptions.SelectedDay
                    : save.currentDay;
                activeDay = Mathf.Clamp(resolvedDay, 1, ProgressionConfig.TotalCampaignDays);
                activeDayConfig = ProgressionConfig.GetDayConfig(activeDay);
                levelDurationSeconds = activeDayConfig.durationSeconds;
                shiftCustomerQuota = activeDayConfig.customerQuota;
                remainingTime = levelDurationSeconds;
            }
        }

        private void Start()
        {
            SaveData save = SaveSystem.GetOrCreateCurrent();
            CustomerManager.Instance?.ConfigureForSession(activeMode, activeDay, save);

            int unlockTier = activeMode == GameMode.Endless ? 3 : activeDay;
            GameSceneResponsiveLayout layout = FindAnyObjectByType<GameSceneResponsiveLayout>();
            if (layout != null)
            {
                layout.ConfigureIngredientTier(unlockTier);
            }

            if (activeMode == GameMode.CampaignDay && activeDay == 1 && tutorialModalRoot != null)
            {
                tutorialModalRoot.SetActive(true);
                isTutorialOpen = true;
                isLevelActive = false;
            }
            else
            {
                isTutorialOpen = false;
                isLevelActive = true;
                ShowShiftStartBanner();
            }

            RefreshHud();
        }

        private void Update()
        {
            if (!IsLevelActive) return;

            if (activeMode == GameMode.Endless)
            {
                elapsedEndlessSeconds += Time.deltaTime;
                RefreshHud();
                return;
            }

            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                RefreshHud();
                EndLevel(completedQuota: false);
                return;
            }

            RefreshHud();
        }

        public void DismissTutorial()
        {
            if (!isTutorialOpen) return;

            isTutorialOpen = false;
            if (tutorialModalRoot != null)
            {
                tutorialModalRoot.SetActive(false);
            }

            isLevelActive = true;
            RefreshHud();
            ShowShiftStartBanner();
        }

        private void ShowShiftStartBanner()
        {
            if (activeMode == GameMode.Endless)
            {
                DishNotificationUI.Instance?.ShowNotification("Endless Rush! 3 Lives & Full 12-Ingredient Pantry!");
            }
            else if (activeDay == 2)
            {
                DishNotificationUI.Instance?.ShowNotification(
                    $"Day 2: {activeDayConfig.shiftTitle} — New Pantry: Onion, Garlic, Greens & Potato!");
            }
            else if (activeDay >= 3)
            {
                DishNotificationUI.Instance?.ShowNotification(
                    $"Day 3: {activeDayConfig.shiftTitle} — New Pantry: Cabbage, Carrots, Pork & Pepper!");
            }
            else
            {
                DishNotificationUI.Instance?.ShowNotification(
                    $"Day {activeDay}: {activeDayConfig.shiftTitle} — Serve {shiftCustomerQuota} Customers!");
            }
        }

        public void RegisterMealDelivery(int basePoints, float combinedQuality, float satisfaction)
        {
            if (!IsLevelActive) return;

            dishesServedCount++;
            currentStreak++;
            if (currentStreak > highestComboStreak)
            {
                highestComboStreak = currentStreak;
            }

            int qualityBonus = Mathf.RoundToInt(basePoints * Mathf.Clamp01(combinedQuality) * 0.5f);
            int streakBonus = currentStreak > 1 ? Mathf.Min((currentStreak - 1) * 15, 75) : 0;

            int scaledBase = Mathf.RoundToInt(basePoints * scoreMultiplier);
            int scaledBonus = Mathf.RoundToInt((qualityBonus + streakBonus) * scoreMultiplier);

            baseDishPointsTotal += scaledBase;
            qualityBonusTotal += scaledBonus;
            totalScore += scaledBase + scaledBonus;

            qualitySamples.Add(combinedQuality);
            satisfactionSamples.Add(satisfaction);
            RefreshHud();
        }

        public void RegisterDrinkDelivery(int drinkPoints, bool isComboWithMeal, float satisfaction)
        {
            if (!IsLevelActive) return;

            drinksServedCount++;
            int scaledDrink = Mathf.RoundToInt(drinkPoints * scoreMultiplier);
            drinkPointsTotal += scaledDrink;
            totalScore += scaledDrink;

            if (isComboWithMeal)
            {
                int scaledCombo = Mathf.RoundToInt(comboBonusPoints * scoreMultiplier);
                combosCompletedCount++;
                comboBonusTotal += scaledCombo;
                totalScore += scaledCombo;
            }

            if (satisfaction >= 0f)
            {
                satisfactionSamples.Add(satisfaction);
            }

            RefreshHud();
        }

        public void RegisterComboCompletion()
        {
            if (!IsLevelActive) return;

            int scaledCombo = Mathf.RoundToInt(comboBonusPoints * scoreMultiplier);
            combosCompletedCount++;
            comboBonusTotal += scaledCombo;
            totalScore += scaledCombo;
            RefreshHud();
        }

        public void RegisterCustomerOrderCompleted()
        {
            if (!IsLevelActive) return;

            customersCompletedCount++;

            if (activeMode == GameMode.Endless)
            {
                int nextTier = ProgressionConfig.GetEndlessRushTier(customersCompletedCount);
                if (nextTier > currentEndlessTier)
                {
                    currentEndlessTier = nextTier;
                    CustomerManager.Instance?.UpdateEndlessDifficulty(currentEndlessTier);
                    DishNotificationUI.Instance?.ShowNotification(
                        $"Rush Level {currentEndlessTier + 1}! Customers Arrive Faster & Less Patience!");
                }
                RefreshHud();
                return;
            }

            RefreshHud();

            if (shiftCustomerQuota > 0 && customersCompletedCount >= shiftCustomerQuota)
            {
                EndLevel(completedQuota: true);
            }
        }

        public void RegisterCustomerLost()
        {
            if (!IsLevelActive) return;

            customersLostCount++;
            currentStreak = 0;
            satisfactionSamples.Add(0f);

            if (activeMode == GameMode.Endless)
            {
                livesRemaining = Mathf.Max(0, livesRemaining - 1);
                int strikes = ProgressionConfig.EndlessMaxLives - livesRemaining;
                if (livesRemaining > 0)
                {
                    string lifeWord = livesRemaining == 1 ? "Life" : "Lives";
                    DishNotificationUI.Instance?.ShowNotification(
                        $"Strike {strikes}/{ProgressionConfig.EndlessMaxLives}! {livesRemaining} {lifeWord} Remaining!");
                }

                RefreshHud();
                if (livesRemaining <= 0)
                {
                    EndLevel(completedQuota: false);
                }
                return;
            }

            RefreshHud();
        }

        private void EndLevel(bool completedQuota)
        {
            if (hasShiftEnded) return;
            hasShiftEnded = true;
            isLevelActive = false;

            int timeBonus = activeMode == GameMode.CampaignDay
                ? Mathf.RoundToInt(Mathf.Max(0f, remainingTime) * pointsPerRemainingSecond)
                : 0;
            totalScore += timeBonus;

            float avgQuality = ComputeAverage(qualitySamples) * 100f;
            float avgSatisfaction = ComputeAverage(satisfactionSamples) * 100f;

            SaveData save = SaveSystem.GetOrCreateCurrent();
            save.dishesServed += dishesServedCount;
            save.coins += Mathf.Max(0, totalScore / 20);

            bool newlyUnlockedEndless = false;
            int awardedSkillPoints = 0;

            if (activeMode == GameMode.Endless)
            {
                if (totalScore > save.endlessHighScore) save.endlessHighScore = totalScore;
                if (customersCompletedCount > save.endlessBestServed) save.endlessBestServed = customersCompletedCount;
            }
            else
            {
                if (totalScore > save.highScore) save.highScore = totalScore;

                if (completedQuota)
                {
                    awardedSkillPoints = 1;
                    save.skillPoints += awardedSkillPoints;
                    save.highestDayCleared = Mathf.Max(save.highestDayCleared, activeDay);
                    GameLaunchOptions.LastClearedDay = activeDay;

                    if (activeDay >= ProgressionConfig.TotalCampaignDays)
                    {
                        if (!save.endlessModeUnlocked) newlyUnlockedEndless = true;
                        save.endlessModeUnlocked = true;
                        save.currentDay = ProgressionConfig.TotalCampaignDays;
                    }
                    else
                    {
                        save.currentDay = activeDay + 1;
                    }
                }
            }

            SaveSystem.Save();
            PopulateSummaryModal(completedQuota, newlyUnlockedEndless, awardedSkillPoints, timeBonus, avgQuality, avgSatisfaction, save);
            RefreshHud();
            onLevelEnded?.Invoke(completedQuota);
        }

        private void PopulateSummaryModal(
            bool completedQuota,
            bool newlyUnlockedEndless,
            int awardedSkillPoints,
            int timeBonus,
            float avgQuality,
            float avgSatisfaction,
            SaveData save)
        {
            if (summaryModalRoot != null)
            {
                summaryModalRoot.SetActive(true);
            }

            if (summaryTitleText != null)
            {
                if (activeMode == GameMode.Endless)
                {
                    summaryTitleText.text = "Endless Shift Over — 3 Strikes!";
                }
                else if (completedQuota && activeDay >= ProgressionConfig.TotalCampaignDays)
                {
                    summaryTitleText.text = newlyUnlockedEndless
                        ? "Day 3 Complete — Endless Unlocked!"
                        : "Day 3 Shift Complete!";
                }
                else if (completedQuota)
                {
                    summaryTitleText.text = $"Day {activeDay} Shift Complete!";
                }
                else
                {
                    summaryTitleText.text = $"Day {activeDay} — Time's Up!";
                }
            }

            if (summaryBreakdownText != null)
            {
                if (activeMode == GameMode.Endless)
                {
                    summaryBreakdownText.text =
                        $"Survival Time: {FormatTime(elapsedEndlessSeconds)}   |   Rush Tier Reached: Lv {currentEndlessTier + 1}\n" +
                        $"Dishes Served: {dishesServedCount}   |   Drinks: {drinksServedCount}   |   Highest Combo: x{highestComboStreak}\n" +
                        $"Customers Served: {customersCompletedCount} (Best: {save.endlessBestServed})   |   Endless Best: {save.endlessHighScore}\n" +
                        $"Avg Dish Quality: {avgQuality:F0}%   |   Avg Satisfaction: {avgSatisfaction:F0}%";
                }
                else
                {
                    string rewardLine = completedQuota
                        ? $"\n<color=#FBBF24>+{awardedSkillPoints} Skill Point Awarded! (Total SP: {save.skillPoints})</color>"
                        : "\n<color=#FCA5A5>Reach the customer quota to unlock the next day & earn +1 SP!</color>";

                    summaryBreakdownText.text =
                        $"Day {activeDay} ({activeDayConfig.shiftTitle})   |   Satisfied: {customersCompletedCount}/{shiftCustomerQuota}   (Lost: {customersLostCount})\n" +
                        $"Dishes Served: {dishesServedCount}   |   Drinks Served: {drinksServedCount}   |   Combos: {combosCompletedCount}\n" +
                        $"Avg Dish Quality: {avgQuality:F0}%   |   Avg Customer Satisfaction: {avgSatisfaction:F0}%\n" +
                        $"Dish & Drink Points: {baseDishPointsTotal + drinkPointsTotal}   |   Quality/Combo Bonus: +{qualityBonusTotal + comboBonusTotal}\n" +
                        $"Remaining Time Bonus ({FormatTime(remainingTime)}): +{timeBonus}" +
                        rewardLine;
                }
            }

            if (summaryFinalScoreText != null)
            {
                summaryFinalScoreText.text = $"Final Score: {totalScore}";
            }

            bool showUpgradeShop = activeMode == GameMode.CampaignDay && completedQuota && upgradeShopButton != null;
            if (upgradeShopButton != null)
            {
                upgradeShopButton.gameObject.SetActive(showUpgradeShop);
            }
            if (upgradeShopButtonLabel != null && showUpgradeShop)
            {
                upgradeShopButtonLabel.text = activeDay >= ProgressionConfig.TotalCampaignDays
                    ? "Upgrade Shop (Endless Next)"
                    : "Upgrade Shop (+1 SP)";
            }

            if (playAgainButtonLabel != null)
            {
                playAgainButtonLabel.text = activeMode == GameMode.Endless
                    ? "Play Again"
                    : (completedQuota ? $"Replay Day {activeDay}" : $"Retry Day {activeDay}");
            }

            bool showEndlessLeaderboard = activeMode == GameMode.Endless;
            if (leaderboardSubmitGroup != null)
            {
                leaderboardSubmitGroup.SetActive(showEndlessLeaderboard);
            }
            if (viewLeaderboardButton != null)
            {
                viewLeaderboardButton.gameObject.SetActive(showEndlessLeaderboard);
            }
            if (showEndlessLeaderboard)
            {
                hasSubmittedLeaderboardScore = false;
                if (leaderboardNameInput != null)
                {
                    leaderboardNameInput.characterLimit = LeaderboardSystem.MaxNameLength;
                    leaderboardNameInput.text = LeaderboardSystem.GetLastPlayerName();
                    leaderboardNameInput.interactable = true;
                }
                if (mobileNamePromptButton != null)
                {
                    mobileNamePromptButton.gameObject.SetActive(BrowserStorageBridge.IsMobileBrowser());
                }
                if (submitLeaderboardButton != null)
                {
                    submitLeaderboardButton.interactable = true;
                }
                if (submitLeaderboardButtonLabel != null)
                {
                    submitLeaderboardButtonLabel.text = "Save Score";
                }
                if (leaderboardSubmitStatusText != null)
                {
                    leaderboardSubmitStatusText.text = "Enter up to 8 characters — saves in your browser!";
                }
            }
        }

        public void PromptMobileNameInput()
        {
            if (hasSubmittedLeaderboardScore || leaderboardNameInput == null) return;
            if (BrowserStorageBridge.TryPromptMobileInput("Enter Player Name (Max 8 Characters):", leaderboardNameInput.text, out string entered))
            {
                leaderboardNameInput.text = LeaderboardSystem.SanitizeName(entered);
            }
        }

        public void SubmitEndlessScore()
        {
            if (activeMode != GameMode.Endless || hasSubmittedLeaderboardScore) return;

            string rawName = leaderboardNameInput != null ? leaderboardNameInput.text : string.Empty;
            string cleanName = LeaderboardSystem.SanitizeName(rawName);
            if (leaderboardNameInput != null)
            {
                leaderboardNameInput.text = cleanName;
                leaderboardNameInput.interactable = false;
            }

            int rank = LeaderboardSystem.AddEntry(
                cleanName,
                totalScore,
                elapsedEndlessSeconds,
                dishesServedCount,
                highestComboStreak);

            hasSubmittedLeaderboardScore = true;
            if (submitLeaderboardButton != null) submitLeaderboardButton.interactable = false;
            if (submitLeaderboardButtonLabel != null) submitLeaderboardButtonLabel.text = "Saved!";
            if (leaderboardSubmitStatusText != null)
            {
                leaderboardSubmitStatusText.text = rank > 0
                    ? $"<color=#4ADE80>Saved '{cleanName}' to Browser Leaderboard (#{rank})!</color>"
                    : $"<color=#FDE047>Saved '{cleanName}'!</color>";
            }

            OpenEndlessLeaderboard();
        }

        public void OpenEndlessLeaderboard()
        {
            if (endlessLeaderboardPanel != null)
            {
                endlessLeaderboardPanel.OpenOverlay();
            }
        }

        public void GoToUpgradeShop()
        {
            GameLaunchOptions.LoadFromSave = true;
            LoadingBridge.LoadScene(UpgradeShopSceneName, loadFromSave: true);
        }

        public void RestartLevel()
        {
            GameLaunchOptions.SelectedMode = activeMode;
            GameLaunchOptions.SelectedDay = activeMode == GameMode.CampaignDay ? activeDay : 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene("mainmenuscene");
        }

        private void RefreshHud()
        {
            if (dayModeBadgeText != null)
            {
                dayModeBadgeText.text = activeMode == GameMode.Endless
                    ? $"Endless Lv {currentEndlessTier + 1}"
                    : $"Day {activeDay}/{ProgressionConfig.TotalCampaignDays}";
            }

            if (timerText != null)
            {
                if (activeMode == GameMode.Endless)
                {
                    timerText.text = $"Shift: {FormatTime(elapsedEndlessSeconds)}";
                }
                else
                {
                    string formatted = FormatTime(remainingTime);
                    timerText.text = remainingTime <= 30f && IsLevelActive
                        ? $"<color=#EF4444>Time: {formatted}</color>"
                        : $"Time: {formatted}";
                }
            }

            if (timerFillBar != null)
            {
                float ratio = activeMode == GameMode.Endless
                    ? Mathf.Clamp01(livesRemaining / (float)ProgressionConfig.EndlessMaxLives)
                    : (levelDurationSeconds > 0f ? Mathf.Clamp01(remainingTime / levelDurationSeconds) : 0f);

                timerFillBar.fillAmount = ratio;
                if (ratio > 0.5f)
                {
                    timerFillBar.color = new Color(0.20f, 0.80f, 0.40f, 1f);
                }
                else if (ratio > 0.25f)
                {
                    timerFillBar.color = new Color(0.96f, 0.72f, 0.14f, 1f);
                }
                else
                {
                    timerFillBar.color = new Color(0.94f, 0.27f, 0.27f, 1f);
                }

                if (timerThumbRect != null)
                {
                    float x = Mathf.Lerp(-timerTrackWidth * 0.5f, timerTrackWidth * 0.5f, ratio);
                    timerThumbRect.anchoredPosition = new Vector2(x, timerThumbRect.anchoredPosition.y);
                }
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {totalScore}";
            }

            if (ordersText != null)
            {
                if (activeMode == GameMode.Endless)
                {
                    string livesColor = livesRemaining >= 3 ? "#4ADE80" : (livesRemaining == 2 ? "#FBBF24" : "#EF4444");
                    ordersText.text = $"<color={livesColor}>Lives: {livesRemaining}/{ProgressionConfig.EndlessMaxLives}</color>";
                }
                else
                {
                    ordersText.text = $"Orders: {customersCompletedCount}/{shiftCustomerQuota}";
                }
            }

            if (comboText != null)
            {
                if (activeMode == GameMode.Endless)
                {
                    comboText.text = $"Served: {customersCompletedCount} | x{Mathf.Max(1, currentStreak)}";
                }
                else
                {
                    comboText.text = currentStreak > 1 ? $"Streak x{currentStreak}" : "Streak x1";
                }
            }
        }

        private static string FormatTime(float seconds)
        {
            int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int mins = displaySeconds / 60;
            int secs = displaySeconds % 60;
            return $"{mins:00}:{secs:00}";
        }

        private static float ComputeAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < values.Count; i++) sum += values[i];
            return sum / values.Count;
        }
    }
}
