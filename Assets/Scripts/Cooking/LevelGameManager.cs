using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Controls the 180-second showcase level countdown, dynamic scoring,
    /// combo/quality tracking, SaveSystem persistence, and end-of-level summary modal.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelGameManager : MonoBehaviour
    {
        public static LevelGameManager Instance { get; private set; }

        [Header("Level Configuration")]
        [SerializeField] private float levelDurationSeconds = 180f;
        [SerializeField] private int shiftCustomerQuota = 8;
        [SerializeField] private int pointsPerRemainingSecond = 5;
        [SerializeField] private int comboBonusPoints = 120;

        [Header("HUD References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ordersText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private Button restartHudButton;
        [SerializeField] private Button mainMenuHudButton;

        [Header("End-of-Level Summary Modal")]
        [SerializeField] private GameObject summaryModalRoot;
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryBreakdownText;
        [SerializeField] private TextMeshProUGUI summaryFinalScoreText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button summaryMainMenuButton;

        private float remainingTime;
        private bool isLevelActive;
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
        private readonly List<float> satisfactionSamples = new List<float>();
        private readonly List<float> qualitySamples = new List<float>();

        public bool IsLevelActive => isLevelActive;
        public float RemainingTime => remainingTime;
        public int TotalScore => totalScore;
        public int ShiftCustomerQuota => shiftCustomerQuota;

        public event Action<bool> onLevelEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (summaryModalRoot != null)
            {
                summaryModalRoot.SetActive(false);
            }

            if (restartHudButton != null) restartHudButton.onClick.AddListener(RestartLevel);
            if (mainMenuHudButton != null) mainMenuHudButton.onClick.AddListener(GoToMainMenu);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartLevel);
            if (summaryMainMenuButton != null) summaryMainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void Start()
        {
            remainingTime = levelDurationSeconds;
            isLevelActive = true;
            RefreshHud();
        }

        private void Update()
        {
            if (!isLevelActive) return;

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

        public void RegisterMealDelivery(int basePoints, float combinedQuality, float satisfaction)
        {
            if (!isLevelActive) return;

            dishesServedCount++;
            currentStreak++;

            int qualityBonus = Mathf.RoundToInt(basePoints * Mathf.Clamp01(combinedQuality) * 0.5f);
            int streakBonus = currentStreak > 1 ? Mathf.Min((currentStreak - 1) * 15, 75) : 0;

            baseDishPointsTotal += basePoints;
            qualityBonusTotal += qualityBonus + streakBonus;
            totalScore += basePoints + qualityBonus + streakBonus;

            qualitySamples.Add(combinedQuality);
            satisfactionSamples.Add(satisfaction);
            RefreshHud();
        }

        public void RegisterDrinkDelivery(int drinkPoints, bool isComboWithMeal, float satisfaction)
        {
            if (!isLevelActive) return;

            drinksServedCount++;
            drinkPointsTotal += drinkPoints;
            totalScore += drinkPoints;

            if (isComboWithMeal)
            {
                combosCompletedCount++;
                comboBonusTotal += comboBonusPoints;
                totalScore += comboBonusPoints;
            }

            if (satisfaction >= 0f)
            {
                satisfactionSamples.Add(satisfaction);
            }

            RefreshHud();
        }

        public void RegisterComboCompletion()
        {
            if (!isLevelActive) return;

            combosCompletedCount++;
            comboBonusTotal += comboBonusPoints;
            totalScore += comboBonusPoints;
            RefreshHud();
        }

        public void RegisterCustomerOrderCompleted()
        {
            if (!isLevelActive) return;

            customersCompletedCount++;
            RefreshHud();

            if (shiftCustomerQuota > 0 && customersCompletedCount >= shiftCustomerQuota)
            {
                EndLevel(completedQuota: true);
            }
        }

        public void RegisterCustomerLost()
        {
            if (!isLevelActive) return;

            customersLostCount++;
            currentStreak = 0;
            satisfactionSamples.Add(0f);
            RefreshHud();
        }

        private void EndLevel(bool completedQuota)
        {
            if (!isLevelActive) return;
            isLevelActive = false;

            int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, remainingTime) * pointsPerRemainingSecond);
            totalScore += timeBonus;

            float avgQuality = ComputeAverage(qualitySamples) * 100f;
            float avgSatisfaction = ComputeAverage(satisfactionSamples) * 100f;

            SaveData save = SaveSystem.GetOrCreateCurrent();
            save.dishesServed += dishesServedCount;
            if (totalScore > save.highScore)
            {
                save.highScore = totalScore;
            }
            save.coins += Mathf.Max(0, totalScore / 20);
            SaveSystem.Save();

            if (summaryModalRoot != null)
            {
                summaryModalRoot.SetActive(true);
            }

            if (summaryTitleText != null)
            {
                summaryTitleText.text = completedQuota ? "SHIFT COMPLETE!" : "TIME'S UP!";
            }

            if (summaryBreakdownText != null)
            {
                summaryBreakdownText.text =
                    $"Customers Satisfied: {customersCompletedCount} / {shiftCustomerQuota}   (Lost: {customersLostCount})\n" +
                    $"Dishes Served: {dishesServedCount}   |   Drinks Served: {drinksServedCount}\n" +
                    $"Meal/Drink Combos: {combosCompletedCount}   (+{comboBonusTotal} pts)\n" +
                    $"Avg Dish Quality: {avgQuality:F0}%   |   Avg Customer Satisfaction: {avgSatisfaction:F0}%\n" +
                    $"Base Dish & Drink Points: {baseDishPointsTotal + drinkPointsTotal}\n" +
                    $"Quality & Streak Bonus: +{qualityBonusTotal}\n" +
                    $"Remaining Time Bonus ({FormatTime(remainingTime)}): +{timeBonus}";
            }

            if (summaryFinalScoreText != null)
            {
                summaryFinalScoreText.text = $"FINAL SCORE: {totalScore}";
            }

            RefreshHud();
            onLevelEnded?.Invoke(completedQuota);
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene("mainmenuscene");
        }

        private void RefreshHud()
        {
            if (timerText != null)
            {
                string formatted = FormatTime(remainingTime);
                if (remainingTime <= 30f && isLevelActive)
                {
                    timerText.text = $"<color=#EF4444>TIME {formatted}</color>";
                }
                else
                {
                    timerText.text = $"TIME {formatted}";
                }
            }

            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {totalScore}";
            }

            if (ordersText != null)
            {
                ordersText.text = $"ORDERS: {customersCompletedCount}/{shiftCustomerQuota}";
            }

            if (comboText != null)
            {
                comboText.text = currentStreak > 1 ? $"STREAK x{currentStreak}" : "STREAK x1";
            }
        }

        private static string FormatTime(float seconds)
        {
            int ceil = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int mins = ceil / 60;
            int secs = ceil % 60;
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
