using UnityEngine;

namespace CookingSystem
{
    public enum KitchenUpgradeId
    {
        PrepMastery = 0,
        FastBurner = 1,
        Hospitality = 2
    }

    public readonly struct DayShiftConfig
    {
        public readonly int dayNumber;
        public readonly string shiftTitle;
        public readonly float durationSeconds;
        public readonly int customerQuota;
        public readonly float initialSpawnDelay;
        public readonly float spawnInterval;
        public readonly float basePatienceSeconds;
        public readonly float mealOnlyWeight;
        public readonly float drinkOnlyWeight;
        public readonly bool showTutorialOnStart;

        public DayShiftConfig(
            int dayNumber,
            string shiftTitle,
            float durationSeconds,
            int customerQuota,
            float initialSpawnDelay,
            float spawnInterval,
            float basePatienceSeconds,
            float mealOnlyWeight,
            float drinkOnlyWeight,
            bool showTutorialOnStart)
        {
            this.dayNumber = dayNumber;
            this.shiftTitle = shiftTitle;
            this.durationSeconds = durationSeconds;
            this.customerQuota = customerQuota;
            this.initialSpawnDelay = initialSpawnDelay;
            this.spawnInterval = spawnInterval;
            this.basePatienceSeconds = basePatienceSeconds;
            this.mealOnlyWeight = mealOnlyWeight;
            this.drinkOnlyWeight = drinkOnlyWeight;
            this.showTutorialOnStart = showTutorialOnStart;
        }
    }

    /// <summary>
    /// Shared configuration for Days 1-3, Endless Mode difficulty scaling,
    /// and the 3 Carinderia Kitchen Upgrades.
    /// </summary>
    public static class ProgressionConfig
    {
        public const int TotalCampaignDays = 3;
        public const int MaxUpgradeRank = 3;
        public const int EndlessMaxLives = 3;

        public static DayShiftConfig GetDayConfig(int day)
        {
            return Mathf.Clamp(day, 1, TotalCampaignDays) switch
            {
                1 => new DayShiftConfig(
                    dayNumber: 1,
                    shiftTitle: "Opening Shift",
                    durationSeconds: 180f,
                    customerQuota: 6,
                    initialSpawnDelay: 1.0f,
                    spawnInterval: 7.0f,
                    basePatienceSeconds: 62f,
                    mealOnlyWeight: 0.55f,
                    drinkOnlyWeight: 0.25f,
                    showTutorialOnStart: true),
                2 => new DayShiftConfig(
                    dayNumber: 2,
                    shiftTitle: "Lunch Rush",
                    durationSeconds: 180f,
                    customerQuota: 8,
                    initialSpawnDelay: 0.8f,
                    spawnInterval: 5.5f,
                    basePatienceSeconds: 52f,
                    mealOnlyWeight: 0.40f,
                    drinkOnlyWeight: 0.20f,
                    showTutorialOnStart: false),
                _ => new DayShiftConfig(
                    dayNumber: 3,
                    shiftTitle: "Fiesta Peak",
                    durationSeconds: 180f,
                    customerQuota: 10,
                    initialSpawnDelay: 0.6f,
                    spawnInterval: 4.5f,
                    basePatienceSeconds: 44f,
                    mealOnlyWeight: 0.30f,
                    drinkOnlyWeight: 0.15f,
                    showTutorialOnStart: false)
            };
        }

        // --- Endless Mode Dynamic Difficulty Curve (steps every 2-3 customers served) ---

        public static int GetEndlessRushTier(int customersServed)
        {
            if (customersServed < 2) return 0;
            // Thresholds: 2, 5, 7, 10, 12, 15... (alternating every 2 and 3 customers served)
            return (customersServed * 2 + 1) / 5;
        }

        public static float GetEndlessSpawnInterval(int rushTier)
        {
            return Mathf.Max(2.0f, 6.2f - rushTier * 0.65f);
        }

        public static float GetEndlessBasePatience(int rushTier)
        {
            return Mathf.Max(22f, 54f - rushTier * 4.5f);
        }

        public static void GetEndlessOrderWeights(int rushTier, out float mealOnlyWeight, out float drinkOnlyWeight)
        {
            float comboWeight = Mathf.Clamp(0.25f + rushTier * 0.06f, 0.25f, 0.65f);
            float remaining = 1f - comboWeight;
            mealOnlyWeight = remaining * 0.68f;
            drinkOnlyWeight = remaining * 0.32f;
        }

        // --- Upgrade 1: Precision Knife & Ladle ---

        public static float GetPrepSweetSpotHalfWidth(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.prepMasteryLevel, 0, MaxUpgradeRank) : 0;
            return 0.16f + rank * 0.045f;
        }

        public static float GetPrepYellowHalfWidth(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.prepMasteryLevel, 0, MaxUpgradeRank) : 0;
            return 0.32f + rank * 0.04f;
        }

        public static float GetPrepCursorSpeedMultiplier(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.prepMasteryLevel, 0, MaxUpgradeRank) : 0;
            return 1f - rank * 0.08f;
        }

        // --- Upgrade 2: Turbo Twin-Jet Burners ---

        public static float GetCookDurationMultiplier(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.fastBurnerLevel, 0, MaxUpgradeRank) : 0;
            return 1f - rank * 0.14f;
        }

        public static float GetBurnGraceBonusSeconds(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.fastBurnerLevel, 0, MaxUpgradeRank) : 0;
            return rank * 0.6f;
        }

        // --- Upgrade 3: Warm Carinderia Hospitality ---

        public static float GetCustomerPatienceMultiplier(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.hospitalityLevel, 0, MaxUpgradeRank) : 0;
            return 1f + rank * 0.12f;
        }

        public static float GetDrinkRestoreBonusFraction(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.hospitalityLevel, 0, MaxUpgradeRank) : 0;
            return rank * 0.06f;
        }

        public static float GetDrinkFreezeBonusSeconds(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.hospitalityLevel, 0, MaxUpgradeRank) : 0;
            return rank * 1.5f;
        }

        public static float GetScoreMultiplier(SaveData save)
        {
            int rank = save != null ? Mathf.Clamp(save.hospitalityLevel, 0, MaxUpgradeRank) : 0;
            return 1f + rank * 0.10f;
        }

        public static int GetUpgradeRank(SaveData save, KitchenUpgradeId id)
        {
            if (save == null) return 0;
            return id switch
            {
                KitchenUpgradeId.PrepMastery => Mathf.Clamp(save.prepMasteryLevel, 0, MaxUpgradeRank),
                KitchenUpgradeId.FastBurner => Mathf.Clamp(save.fastBurnerLevel, 0, MaxUpgradeRank),
                KitchenUpgradeId.Hospitality => Mathf.Clamp(save.hospitalityLevel, 0, MaxUpgradeRank),
                _ => 0
            };
        }

        public static bool TryPurchaseUpgrade(SaveData save, KitchenUpgradeId id)
        {
            if (save == null || save.skillPoints <= 0) return false;

            int rank = GetUpgradeRank(save, id);
            if (rank >= MaxUpgradeRank) return false;

            save.skillPoints -= 1;
            switch (id)
            {
                case KitchenUpgradeId.PrepMastery:
                    save.prepMasteryLevel = rank + 1;
                    break;
                case KitchenUpgradeId.FastBurner:
                    save.fastBurnerLevel = rank + 1;
                    break;
                case KitchenUpgradeId.Hospitality:
                    save.hospitalityLevel = rank + 1;
                    break;
            }
            return true;
        }

        public static int RefundAllUpgrades(SaveData save)
        {
            if (save == null) return 0;
            int refunded = Mathf.Max(0, save.prepMasteryLevel)
                         + Mathf.Max(0, save.fastBurnerLevel)
                         + Mathf.Max(0, save.hospitalityLevel);
            save.prepMasteryLevel = 0;
            save.fastBurnerLevel = 0;
            save.hospitalityLevel = 0;
            save.skillPoints += refunded;
            return refunded;
        }
    }
}
