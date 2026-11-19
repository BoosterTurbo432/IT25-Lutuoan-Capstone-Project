using System;
using System.Collections.Generic;
using CookingSystem.Core.Responsive;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    [Serializable]
    public class LeaderboardRowRefs
    {
        public GameObject rowRoot;
        public Image rowBackground;
        public TMP_Text rankText;
        public TMP_Text nameText;
        public TMP_Text scoreText;
        public TMP_Text timeText;
        public TMP_Text dishesText;
        public TMP_Text comboText;
    }

    /// <summary>
    /// Displays the Top-10 Endless Mode leaderboard (Rank, 8-Char Name, Score, Time Spent,
    /// Dishes Served, Highest Combo). Works both as an in-game overlay and in standalone leaderboardscene.
    /// </summary>
    public class LeaderboardPanelController : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private bool adaptStandaloneLayout;
        [SerializeField] private string mainMenuSceneName = "mainmenuscene";

        [Header("UI References")]
        [SerializeField] private LeaderboardRowRefs[] rowSlots;
        [SerializeField] private GameObject emptyStateHint;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button clearBoardButton;
        [SerializeField] private TMP_Text footerStatusText;

        [Header("Standalone Responsive Targets")]
        [SerializeField] private RectTransform mainPanelRect;

        private static readonly Color EvenRowColor = new Color(0.14f, 0.09f, 0.07f, 0.92f);
        private static readonly Color OddRowColor = new Color(0.19f, 0.12f, 0.09f, 0.92f);
        private static readonly Color HighlightRowColor = new Color(0.45f, 0.28f, 0.08f, 0.98f);
        private static readonly Color GoldColor = new Color(0.98f, 0.75f, 0.14f, 1f);
        private static readonly Color CreamColor = new Color(1f, 0.95f, 0.84f, 1f);

        private ViewportOrientation? appliedOrientation;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseOverlay);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
            if (clearBoardButton != null) clearBoardButton.onClick.AddListener(HandleClearLeaderboard);
        }

        private void OnEnable()
        {
            RefreshTable();

            ResponsiveViewportManager mgr = ResponsiveViewportManager.GetOrCreate();
            if (adaptStandaloneLayout)
            {
                mgr.RegisterAdaptiveSceneHandler(GetReferenceResolutionForOrientation, ApplyViewportProfile);
            }
            else
            {
                mgr.OnViewportChanged += ApplyViewportProfile;
                ApplyViewportProfile(mgr.CurrentProfile);
            }
        }

        private void OnDisable()
        {
            if (ResponsiveViewportManager.Instance != null)
            {
                if (adaptStandaloneLayout)
                {
                    ResponsiveViewportManager.Instance.UnregisterAdaptiveSceneHandler(ApplyViewportProfile);
                }
                else
                {
                    ResponsiveViewportManager.Instance.OnViewportChanged -= ApplyViewportProfile;
                }
            }
        }

        public void OpenOverlay()
        {
            gameObject.SetActive(true);
            RefreshTable();
        }

        public void CloseOverlay()
        {
            gameObject.SetActive(false);
        }

        public void RefreshTable()
        {
            IReadOnlyList<LeaderboardEntry> entries = LeaderboardSystem.GetEntries();
            bool hasEntries = entries != null && entries.Count > 0;

            if (emptyStateHint != null)
            {
                emptyStateHint.SetActive(!hasEntries);
            }

            if (rowSlots != null)
            {
                string highlightTimestamp = LeaderboardSystem.LastSubmittedEntryTimestamp;

                for (int i = 0; i < rowSlots.Length; i++)
                {
                    LeaderboardRowRefs slot = rowSlots[i];
                    if (slot == null || slot.rowRoot == null) continue;

                    if (!hasEntries || i >= entries.Count)
                    {
                        slot.rowRoot.SetActive(false);
                        continue;
                    }

                    slot.rowRoot.SetActive(true);
                    LeaderboardEntry entry = entries[i];
                    bool isRecentHighlight = !string.IsNullOrEmpty(highlightTimestamp) &&
                                             entry.recordedAtUtc == highlightTimestamp;

                    if (slot.rowBackground != null)
                    {
                        slot.rowBackground.color = isRecentHighlight
                            ? HighlightRowColor
                            : (i % 2 == 0 ? EvenRowColor : OddRowColor);
                    }

                    Color primaryColor = (i == 0 || isRecentHighlight) ? GoldColor : CreamColor;

                    if (slot.rankText != null)
                    {
                        slot.rankText.text = $"#{i + 1}";
                        slot.rankText.color = primaryColor;
                    }
                    if (slot.nameText != null)
                    {
                        slot.nameText.text = string.IsNullOrEmpty(entry.playerName) ? "Chef" : entry.playerName;
                        slot.nameText.color = primaryColor;
                    }
                    if (slot.scoreText != null)
                    {
                        slot.scoreText.text = entry.score.ToString("N0");
                        slot.scoreText.color = isRecentHighlight ? GoldColor : new Color(0.29f, 0.87f, 0.50f, 1f);
                    }
                    if (slot.timeText != null)
                    {
                        slot.timeText.text = LeaderboardSystem.FormatDuration(entry.timeSpentSeconds);
                        slot.timeText.color = CreamColor;
                    }
                    if (slot.dishesText != null)
                    {
                        slot.dishesText.text = entry.dishesServed.ToString();
                        slot.dishesText.color = CreamColor;
                    }
                    if (slot.comboText != null)
                    {
                        slot.comboText.text = $"x{Mathf.Max(1, entry.highestCombo)}";
                        slot.comboText.color = new Color(0.99f, 0.88f, 0.28f, 1f);
                    }
                }
            }

            if (footerStatusText != null)
            {
                footerStatusText.text = hasEntries
                    ? $"Showing Top {Mathf.Min(entries.Count, rowSlots != null ? rowSlots.Length : entries.Count)} Endless Runs (Saved in Browser)"
                    : "No Endless Mode scores recorded yet. Clear Day 3 to unlock Endless Rush!";
            }

            if (clearBoardButton != null)
            {
                clearBoardButton.interactable = hasEntries;
            }
        }

        private void HandleClearLeaderboard()
        {
            LeaderboardSystem.ClearAll();
            RefreshTable();
        }

        private void GoToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
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
                if (adaptStandaloneLayout)
                {
                    mainPanelRect.sizeDelta = portrait ? new Vector2(1010f, 1520f) : new Vector2(1540f, 880f);
                }
                else
                {
                    mainPanelRect.sizeDelta = portrait ? new Vector2(1000f, 1460f) : new Vector2(1420f, 840f);
                }
            }
        }
    }
}
