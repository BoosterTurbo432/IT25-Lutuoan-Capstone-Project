using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    [RequireComponent(typeof(CookingStation))]
    public class CookingStationUI : MonoBehaviour
    {
        [Header("UI Text Displays")]
        [Tooltip("Displays current station state and ingredient count.")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Displays the station name or recipe title.")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Interactive Controls")]
        [Tooltip("Optional button to start cooking before reaching maxIngredients.")]
        [SerializeField] private Button cookButton;

        [Tooltip("Optional button to clear/serve cooked food and reset station.")]
        [SerializeField] private Button serveButton;

        [Header("Animated Circular Progress Icon")]
        [SerializeField] private GameObject progressIconRoot;
        [SerializeField] private Image cookProgressRing;
        [SerializeField] private Image burnWarningFill;
        [SerializeField] private TMP_Text progressCenterText;

        [Header("Fire & Extinguisher Foam VFX")]
        [SerializeField] private UISpriteSheetAnimator fireAnimator;
        [SerializeField] private UISpriteSheetAnimator foamSprayAnimator;

        [Header("2D Vessel State Sprites")]
        [SerializeField] private Image vesselImage;
        [SerializeField] private Sprite idleVesselSprite;
        [SerializeField] private Sprite cookingVesselSprite;
        [SerializeField] private Sprite burnedVesselSprite;

        private CookingStation station;
        private RectTransform progressIconRect;

        private void Awake()
        {
            station = GetComponent<CookingStation>();
            if (progressIconRoot != null)
            {
                progressIconRect = progressIconRoot.transform as RectTransform;
                progressIconRoot.SetActive(false);
            }

            if (cookButton != null)
            {
                cookButton.onClick.AddListener(OnCookButtonClicked);
                cookButton.gameObject.SetActive(false);
            }

            if (serveButton != null)
            {
                serveButton.onClick.AddListener(OnServeButtonClicked);
                serveButton.gameObject.SetActive(false);
            }

            if (foamSprayAnimator != null)
            {
                foamSprayAnimator.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (titleText != null)
            {
                titleText.text = station.StationName;
            }

            UpdateStatusText();
            SetVesselSprite(idleVesselSprite);
            ResetProgressIconVisuals();
        }

        private void Update()
        {
            if (progressIconRect == null || progressIconRoot == null || !progressIconRoot.activeSelf) return;

            if (station != null && station.CurrentState == CookingStationState.Cooked)
            {
                // Urgent gentle pulse while burn countdown ticks
                float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 10f);
                progressIconRect.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (station != null && station.CurrentState == CookingStationState.Cooking)
            {
                float pulse = 1f + 0.03f * Mathf.Sin(Time.time * 5f);
                progressIconRect.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                progressIconRect.localScale = Vector3.one;
            }
        }

        private void OnEnable()
        {
            if (station == null) station = GetComponent<CookingStation>();

            station.onIngredientAdded.AddListener(OnIngredientAdded);
            station.onCookingStarted.AddListener(OnCookingStarted);
            station.onCookingProgress.AddListener(OnCookingProgress);
            station.onCookingCompleted.AddListener(OnCookingCompleted);
            station.onFoodBurned.AddListener(OnFoodBurned);
            station.onBurnWarningProgress.AddListener(OnBurnWarningProgress);
            station.onStoveCaughtFire.AddListener(OnStoveCaughtFire);
            station.onFireExtinguished.AddListener(OnFireExtinguished);
            station.onStationReset.AddListener(OnStationReset);
        }

        private void OnDisable()
        {
            if (station == null) return;

            station.onIngredientAdded.RemoveListener(OnIngredientAdded);
            station.onCookingStarted.RemoveListener(OnCookingStarted);
            station.onCookingProgress.RemoveListener(OnCookingProgress);
            station.onCookingCompleted.RemoveListener(OnCookingCompleted);
            station.onFoodBurned.RemoveListener(OnFoodBurned);
            station.onBurnWarningProgress.RemoveListener(OnBurnWarningProgress);
            station.onStoveCaughtFire.RemoveListener(OnStoveCaughtFire);
            station.onFireExtinguished.RemoveListener(OnFireExtinguished);
            station.onStationReset.RemoveListener(OnStationReset);
        }

        private void OnIngredientAdded(DraggableIngredient ingredient)
        {
            UpdateStatusText();
            UpdateCookButtonVisibility();
        }

        private void OnCookingStarted()
        {
            if (progressIconRoot != null) progressIconRoot.SetActive(true);
            if (cookProgressRing != null)
            {
                cookProgressRing.enabled = true;
                cookProgressRing.fillAmount = 0f;
            }
            if (burnWarningFill != null)
            {
                burnWarningFill.fillAmount = 0f;
                burnWarningFill.enabled = false;
            }
            if (progressCenterText != null)
            {
                progressCenterText.color = Color.white;
                progressCenterText.text = "0%";
            }

            if (statusText != null)
            {
                statusText.text = "<color=#FFA500>Cooking...</color>";
            }
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (serveButton != null) serveButton.gameObject.SetActive(false);
        }

        private void OnCookingProgress(float progress)
        {
            if (progressIconRoot != null && !progressIconRoot.activeSelf) progressIconRoot.SetActive(true);
            if (cookProgressRing != null)
            {
                cookProgressRing.enabled = true;
                cookProgressRing.fillAmount = Mathf.Clamp01(progress);
            }
            if (progressCenterText != null)
            {
                progressCenterText.color = Color.white;
                progressCenterText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

            if (statusText != null)
            {
                statusText.text = $"<color=#FFA500>Cooking {(progress * 100f):F0}%</color>";
            }
            if (progress >= 0.35f)
            {
                SetVesselSprite(cookingVesselSprite);
            }
        }

        private void OnCookingCompleted()
        {
            SetVesselSprite(cookingVesselSprite);
            if (progressIconRoot != null) progressIconRoot.SetActive(true);
            if (cookProgressRing != null)
            {
                cookProgressRing.enabled = true;
                cookProgressRing.fillAmount = 1f;
            }
            if (burnWarningFill != null)
            {
                burnWarningFill.enabled = true;
                burnWarningFill.fillAmount = 0f;
            }
            if (progressCenterText != null)
            {
                progressCenterText.color = new Color(0.30f, 0.96f, 0.56f, 1f);
                progressCenterText.text = $"{station.EffectiveBurnDuration:0.0}s";
            }

            if (statusText != null)
            {
                string graceLabel = station.EffectiveBurnDuration.ToString("0.#");
                statusText.text = station.RequirePrepStation
                    ? $"<color=#00FF7F>Ready! Plate ({graceLabel}s)</color>"
                    : "<color=#00FF7F>Dish Ready! Serve</color>";
            }
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (serveButton != null) serveButton.gameObject.SetActive(true);
        }

        private void OnBurnWarningProgress(float progress)
        {
            if (burnWarningFill != null)
            {
                burnWarningFill.enabled = true;
                burnWarningFill.fillAmount = Mathf.Clamp01(progress);
            }
            if (station.CurrentState == CookingStationState.Cooked)
            {
                float secondsLeft = Mathf.Max(0f, station.EffectiveBurnDuration * (1f - progress));
                if (progressCenterText != null)
                {
                    progressCenterText.color = progress > 0.6f
                        ? new Color(1f, 0.38f, 0.38f, 1f)
                        : new Color(0.99f, 0.85f, 0.25f, 1f);
                    progressCenterText.text = $"{secondsLeft:F1}s";
                }
                if (statusText != null)
                {
                    statusText.text = $"<color=#F59E0B>Plate Now! ({secondsLeft:F1}s)</color>";
                }
            }
        }

        private void OnStoveCaughtFire()
        {
            SetVesselSprite(burnedVesselSprite);
            if (progressIconRoot != null) progressIconRoot.SetActive(true);
            if (burnWarningFill != null)
            {
                burnWarningFill.enabled = true;
                burnWarningFill.fillAmount = 1f;
            }
            if (progressCenterText != null)
            {
                progressCenterText.color = new Color(1f, 0.35f, 0.35f, 1f);
                progressCenterText.text = "!";
            }
            if (fireAnimator != null)
            {
                fireAnimator.PlayLoop(10f);
            }

            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (serveButton != null) serveButton.gameObject.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "<color=#EF4444>Fire! Drag Extinguisher</color>";
            }
        }

        private void OnFireExtinguished()
        {
            if (fireAnimator != null)
            {
                fireAnimator.StopAndHide();
            }
            if (foamSprayAnimator != null)
            {
                foamSprayAnimator.PlayOneShot(durationSeconds: 0.68f, fadeOutTail: true, hideWhenDone: true, startScale: 0.78f, endScale: 1.16f);
            }

            ResetProgressIconVisuals();
            if (serveButton != null) serveButton.gameObject.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "<color=#FB923C>Burnt! Drag to Trash</color>";
            }
        }

        private void OnFoodBurned()
        {
            SetVesselSprite(burnedVesselSprite);
            if (station.CurrentState == CookingStationState.OnFire)
            {
                OnStoveCaughtFire();
                return;
            }
            if (statusText != null)
            {
                statusText.text = "<color=#FF4500>Burned! Clean Stove</color>";
            }
            if (cookButton != null) cookButton.gameObject.SetActive(false);
            if (serveButton != null) serveButton.gameObject.SetActive(true);
        }

        private void OnStationReset()
        {
            SetVesselSprite(idleVesselSprite);
            if (fireAnimator != null) fireAnimator.StopAndHide();
            ResetProgressIconVisuals();
            UpdateStatusText();
            UpdateCookButtonVisibility();
            if (serveButton != null) serveButton.gameObject.SetActive(false);
        }

        private void ResetProgressIconVisuals()
        {
            if (cookProgressRing != null) cookProgressRing.fillAmount = 0f;
            if (burnWarningFill != null)
            {
                burnWarningFill.fillAmount = 0f;
                burnWarningFill.enabled = false;
            }
            if (progressCenterText != null) progressCenterText.text = string.Empty;
            if (progressIconRect != null) progressIconRect.localScale = Vector3.one;
            if (progressIconRoot != null) progressIconRoot.SetActive(false);
        }

        private void OnCookButtonClicked()
        {
            station.StartCooking();
        }

        private void OnServeButtonClicked()
        {
            string dishName = station.ServeDish();
            if (DishNotificationUI.Instance != null && !string.IsNullOrEmpty(dishName))
            {
                DishNotificationUI.Instance.ShowDishServed(dishName);
            }
        }

        private void UpdateCookButtonVisibility()
        {
            if (cookButton != null)
            {
                cookButton.gameObject.SetActive(station.CurrentState == CookingStationState.Idle && station.IngredientCount > 0);
            }
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                statusText.text = station.RequirePrepStation
                    ? $"Drop {station.Cookware} Here"
                    : $"Ingredients: {station.IngredientCount}";
            }
        }

        private void SetVesselSprite(Sprite sprite)
        {
            if (vesselImage != null && sprite != null)
            {
                vesselImage.sprite = sprite;
                vesselImage.color = Color.white;
                vesselImage.preserveAspect = true;
            }
        }
    }
}
