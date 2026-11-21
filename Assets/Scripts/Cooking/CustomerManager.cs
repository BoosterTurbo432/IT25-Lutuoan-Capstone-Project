using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookingSystem
{
    [Serializable]
    public class CustomerCharacterEntry
    {
        public string characterName;
        public Sprite portraitSprite;
    }

    /// <summary>
    /// Manages spawning customers into the 3 CustomerSlotUIs with randomized orders
    /// (MealOnly, DrinkOnly, MealAndDrink), applies Hospitality upgrade bonuses,
    /// and scales spawn/patience pressure dynamically across Days 1-3 and Endless Mode.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("Customer Slots")]
        [SerializeField] private List<CustomerSlotUI> customerSlots = new List<CustomerSlotUI>();

        [Header("2D Customer Characters")]
        [SerializeField] private List<CustomerCharacterEntry> characterRoster = new List<CustomerCharacterEntry>();

        [Header("Spawning")]
        [SerializeField] private float initialSpawnDelay = 1.0f;
        [SerializeField] private float spawnInterval = 6.5f;
        [SerializeField] private float basePatienceSeconds = 58f;

        private static readonly Color[] CustomerPalette =
        {
            new Color(0.84f, 0.62f, 0.51f, 1f),
            new Color(0.56f, 0.72f, 0.84f, 1f),
            new Color(0.68f, 0.80f, 0.58f, 1f),
            new Color(0.85f, 0.76f, 0.52f, 1f),
            new Color(0.76f, 0.60f, 0.78f, 1f)
        };

        private float spawnTimer;
        private int totalSpawned;
        private bool spawningEnabled = true;
        private bool slotsInitialized;
        private float mealOnlyChance = 0.45f;
        private float drinkOnlyChance = 0.25f;
        private float hospitalityPatienceMult = 1f;
        private int activeRecipeUnlockDay = 1;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (LevelGameManager.Instance != null)
            {
                LevelGameManager.Instance.onLevelEnded += HandleLevelEnded;
            }
        }

        private void OnDisable()
        {
            if (LevelGameManager.Instance != null)
            {
                LevelGameManager.Instance.onLevelEnded -= HandleLevelEnded;
            }
        }

        private void Start()
        {
            InitializeSlotsIfNeeded();
            spawnTimer = initialSpawnDelay;
        }

        private void InitializeSlotsIfNeeded()
        {
            if (slotsInitialized) return;
            slotsInitialized = true;
            for (int i = 0; i < customerSlots.Count; i++)
            {
                if (customerSlots[i] != null) customerSlots[i].InitializeEmpty();
            }
        }

        public void ResumeAfterTutorial()
        {
            spawnTimer = 1.0f;
        }

        public void ConfigureForSession(GameMode mode, int day, SaveData save)
        {
            hospitalityPatienceMult = ProgressionConfig.GetCustomerPatienceMultiplier(save);

            if (mode == GameMode.Endless)
            {
                activeRecipeUnlockDay = 3;
                initialSpawnDelay = 0.8f;
                UpdateEndlessDifficulty(0);
            }
            else
            {
                activeRecipeUnlockDay = Mathf.Clamp(day, 1, 3);
                DayShiftConfig cfg = ProgressionConfig.GetDayConfig(day);
                initialSpawnDelay = cfg.initialSpawnDelay;
                spawnInterval = cfg.spawnInterval;
                basePatienceSeconds = cfg.basePatienceSeconds * hospitalityPatienceMult;
                mealOnlyChance = cfg.mealOnlyWeight;
                drinkOnlyChance = cfg.drinkOnlyWeight;
            }

            spawnTimer = initialSpawnDelay;
        }

        public void UpdateEndlessDifficulty(int rushTier)
        {
            spawnInterval = ProgressionConfig.GetEndlessSpawnInterval(rushTier);
            float baseSeconds = ProgressionConfig.GetEndlessBasePatience(rushTier);
            basePatienceSeconds = baseSeconds * hospitalityPatienceMult;
            ProgressionConfig.GetEndlessOrderWeights(rushTier, out mealOnlyChance, out drinkOnlyChance);

            spawnTimer = Mathf.Min(spawnTimer, spawnInterval * 0.6f);
        }

        private void Update()
        {
            if (!spawningEnabled) return;
            if (LevelGameManager.Instance != null &&
                (!LevelGameManager.Instance.IsLevelActive || LevelGameManager.Instance.IsTimerPausedForTutorial))
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                bool spawned = TrySpawnCustomer();
                float quickFillDelay = Mathf.Min(2.2f, spawnInterval * 0.45f);
                spawnTimer = spawned && GetOccupiedCount() < 2 ? quickFillDelay : spawnInterval;
            }
        }

        public bool TrySpawnCustomer(bool forceTutorialMeal = false)
        {
            InitializeSlotsIfNeeded();

            var freeSlots = new List<CustomerSlotUI>();
            for (int i = 0; i < customerSlots.Count; i++)
            {
                if (customerSlots[i] != null && !customerSlots[i].IsOccupied)
                {
                    freeSlots.Add(customerSlots[i]);
                }
            }

            if (freeSlots.Count == 0) return false;

            CustomerSlotUI target = forceTutorialMeal ? freeSlots[0] : freeSlots[UnityEngine.Random.Range(0, freeSlots.Count)];
            OrderType orderType = forceTutorialMeal ? OrderType.MealOnly : RollOrderType();
            RecipeDefinition recipe = orderType == OrderType.DrinkOnly
                ? null
                : (forceTutorialMeal
                    ? (RecipeDatabase.GetById("sinigang_kamatis") ?? RecipeDatabase.GetRandomRecipeForDay(1))
                    : RecipeDatabase.GetRandomRecipeForDay(activeRecipeUnlockDay));
            DrinkType drink = orderType == OrderType.MealOnly
                ? DrinkType.None
                : (UnityEngine.Random.value < 0.5f ? DrinkType.BerryJuice : DrinkType.CalamansiTea);

            Color paletteColor = CustomerPalette[totalSpawned % CustomerPalette.Length];
            string charName = null;
            Sprite charSprite = null;
            if (characterRoster != null && characterRoster.Count > 0)
            {
                CustomerCharacterEntry entry = characterRoster[totalSpawned % characterRoster.Count];
                if (entry != null)
                {
                    charName = entry.characterName;
                    charSprite = entry.portraitSprite;
                }
            }
            totalSpawned++;

            target.AssignOrder(orderType, recipe, drink, paletteColor, basePatienceSeconds, charName, charSprite);
            return true;
        }

        public void ApplyFirePenalty(float patienceFraction = 0.15f)
        {
            for (int i = 0; i < customerSlots.Count; i++)
            {
                if (customerSlots[i] != null && customerSlots[i].IsOccupied)
                {
                    customerSlots[i].ApplyPatiencePenalty(patienceFraction, "Stove Fire!");
                }
            }
        }

        private void HandleLevelEnded(bool _)
        {
            spawningEnabled = false;
        }

        private int GetOccupiedCount()
        {
            int count = 0;
            for (int i = 0; i < customerSlots.Count; i++)
            {
                if (customerSlots[i] != null && customerSlots[i].IsOccupied) count++;
            }
            return count;
        }

        private OrderType RollOrderType()
        {
            float r = UnityEngine.Random.value;
            if (r < mealOnlyChance) return OrderType.MealOnly;
            if (r < mealOnlyChance + drinkOnlyChance) return OrderType.DrinkOnly;
            return OrderType.MealAndDrink;
        }
    }
}
