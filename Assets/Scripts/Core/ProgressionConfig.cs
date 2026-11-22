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
    /// Shared progression & upgrade configuration split cleanly between:
    /// 1. 3-Day Demo Campaign: Binary instant-max unlocks in UpgradeShopScene (no ranks/tiers).
    /// 2. Endless Mode Roguelike Draft: Per-run customer quota milestones drafting upgrades up to Kitchen Rank 5.
    /// </summary>
    public static class ProgressionConfig
    {
        public const int TotalCampaignDays = 3;
        public const int DemoMaxedBonusEquivalent = 3;
        public const int EndlessMaxLives = 3;
        public const int EndlessMaxKitchenRank = 5;
        public const int EndlessDraftQuotaStep = 3;

        // Per-run Endless Mode Roguelike Draft state (isolated from Campaign SaveData)
        private static bool isEndlessRunActive;
        private static int endlessKitchenRank;
        private static int endlessPrepStacks;
        private static int endlessBurnerStacks;
        private static int endlessHospitalityStacks;

        public static bool IsEndlessRunActive => isEndlessRunActive;
        public static int EndlessKitchenRank => endlessKitchenRank;
        public static bool CanDraftEndlessUpgrade => isEndlessRunActive && endlessKitchenRank < EndlessMaxKitchenRank;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            EndEndlessRun();
        }

        public static void BeginEndlessRun()
        {
            isEndlessRunActive = true;
            endlessKitchenRank = 0;
            endlessPrepStacks = 0;
            endlessBurnerStacks = 0;
            endlessHospitalityStacks = 0;
        }

        public static void EndEndlessRun()
        {
            isEndlessRunActive = false;
            endlessKitchenRank = 0;
            endlessPrepStacks = 0;
            endlessBurnerStacks = 0;
            endlessHospitalityStacks = 0;
        }

        public static int GetEndlessUpgradeStacks(KitchenUpgradeId id)
        {
            return id switch
            {
                KitchenUpgradeId.PrepMastery => endlessPrepStacks,
                KitchenUpgradeId.FastBurner => endlessBurnerStacks,
                KitchenUpgradeId.Hospitality => endlessHospitalityStacks,
                _ => 0
            };
        }

        public static bool ApplyEndlessDraftUpgrade(KitchenUpgradeId id)
        {
            if (!isEndlessRunActive || endlessKitchenRank >= EndlessMaxKitchenRank) return false;

            endlessKitchenRank++;
            switch (id)
            {
                case KitchenUpgradeId.PrepMastery:
                    endlessPrepStacks++;
                    break;
                case KitchenUpgradeId.FastBurner:
                    endlessBurnerStacks++;
                    break;
                case KitchenUpgradeId.Hospitality:
                    endlessHospitalityStacks++;
                    break;
            }
            return true;
        }

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

        // --- Demo Campaign Instant-Max Unlocks (Days 1-3 Upgrade Shop) ---

        public static bool IsDemoUpgradeUnlocked(SaveData save, KitchenUpgradeId id)
        {
            if (save == null) return false;
            return id switch
            {
                KitchenUpgradeId.PrepMastery => save.prepMasteryLevel > 0,
                KitchenUpgradeId.FastBurner => save.fastBurnerLevel > 0,
                KitchenUpgradeId.Hospitality => save.hospitalityLevel > 0,
                _ => false
            };
        }

        public static bool TryUnlockDemoUpgrade(SaveData save, KitchenUpgradeId id)
        {
            if (save == null || save.skillPoints <= 0) return false;
            if (IsDemoUpgradeUnlocked(save, id)) return false;

            save.skillPoints -= 1;
            switch (id)
            {
                case KitchenUpgradeId.PrepMastery:
                    save.prepMasteryLevel = 1;
                    break;
                case KitchenUpgradeId.FastBurner:
                    save.fastBurnerLevel = 1;
                    break;
                case KitchenUpgradeId.Hospitality:
                    save.hospitalityLevel = 1;
                    break;
            }
            return true;
        }

        public static int RefundAllUpgrades(SaveData save)
        {
            if (save == null) return 0;
            int refunded = (save.prepMasteryLevel > 0 ? 1 : 0)
                         + (save.fastBurnerLevel > 0 ? 1 : 0)
                         + (save.hospitalityLevel > 0 ? 1 : 0);
            save.prepMasteryLevel = 0;
            save.fastBurnerLevel = 0;
            save.hospitalityLevel = 0;
            save.skillPoints += refunded;
            return refunded;
        }

        public static string GetUpgradeDisplayName(KitchenUpgradeId id)
        {
            return id switch
            {
                KitchenUpgradeId.PrepMastery => "Precision Knife & Ladle",
                KitchenUpgradeId.FastBurner => "Turbo Twin-Jet Burners",
                KitchenUpgradeId.Hospitality => "Warm Hospitality",
                _ => "Kitchen Upgrade"
            };
        }

        // --- Stat Evaluators (automatically switch between Endless Run Draft stacks & Demo Instant-Max unlocks) ---

        private static int GetEffectiveLevel(SaveData save, KitchenUpgradeId id)
        {
            if (isEndlessRunActive)
            {
                return Mathf.Clamp(GetEndlessUpgradeStacks(id), 0, EndlessMaxKitchenRank);
            }
            return IsDemoUpgradeUnlocked(save, id) ? DemoMaxedBonusEquivalent : 0;
        }

        // Upgrade 1: Precision Knife & Ladle
        public static float GetPrepSweetSpotHalfWidth(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.PrepMastery);
            return isEndlessRunActive
                ? Mathf.Min(0.38f, 0.16f + lvl * 0.042f)
                : 0.16f + lvl * 0.045f; // Demo maxed = 0.295 (59% bar)
        }

        public static float GetPrepYellowHalfWidth(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.PrepMastery);
            return isEndlessRunActive
                ? Mathf.Min(0.46f, 0.32f + lvl * 0.032f)
                : 0.32f + lvl * 0.04f;
        }

        public static float GetPrepCursorSpeedMultiplier(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.PrepMastery);
            return Mathf.Max(0.55f, 1f - lvl * 0.08f); // Demo maxed = 0.76 (76% speed)
        }

        // Upgrade 2: Turbo Twin-Jet Burners
        public static float GetCookDurationMultiplier(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.FastBurner);
            return isEndlessRunActive
                ? Mathf.Max(0.42f, 1f - lvl * 0.11f)
                : 1f - lvl * 0.14f; // Demo maxed = 0.58 (2.3s cook)
        }

        public static float GetBurnGraceBonusSeconds(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.FastBurner);
            return isEndlessRunActive ? lvl * 0.55f : lvl * 0.6f; // Demo maxed = +1.8s
        }

        // Upgrade 3: Warm Carinderia Hospitality
        public static float GetCustomerPatienceMultiplier(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.Hospitality);
            return 1f + lvl * 0.12f; // Demo maxed = 1.36 (136% patience)
        }

        public static float GetDrinkRestoreBonusFraction(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.Hospitality);
            return isEndlessRunActive ? lvl * 0.05f : lvl * 0.06f; // Demo maxed = +18%
        }

        public static float GetDrinkFreezeBonusSeconds(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.Hospitality);
            return isEndlessRunActive ? lvl * 1.25f : lvl * 1.5f; // Demo maxed = +4.5s
        }

        public static float GetScoreMultiplier(SaveData save)
        {
            int lvl = GetEffectiveLevel(save, KitchenUpgradeId.Hospitality);
            return isEndlessRunActive ? 1f + lvl * 0.12f : 1f + lvl * 0.10f; // Demo maxed = +30%
        }

        // Preview strings for Endless Mode Roguelike Draft cards
        public static string BuildEndlessDraftPreview(KitchenUpgradeId id)
        {
            int cur = GetEndlessUpgradeStacks(id);
            int next = cur + 1;
            switch (id)
            {
                case KitchenUpgradeId.PrepMastery:
                {
                    int curSweet = Mathf.RoundToInt(Mathf.Min(0.38f, 0.16f + cur * 0.042f) * 200f);
                    int nextSweet = Mathf.RoundToInt(Mathf.Min(0.38f, 0.16f + next * 0.042f) * 200f);
                    int curSpd = Mathf.RoundToInt(Mathf.Max(0.55f, 1f - cur * 0.08f) * 100f);
                    int nextSpd = Mathf.RoundToInt(Mathf.Max(0.55f, 1f - next * 0.08f) * 100f);
                    return $"Sweet Spot: {curSweet}% -> <color=#4ADE80>{nextSweet}%</color>\nCursor Speed: {curSpd}% -> <color=#4ADE80>{nextSpd}%</color>";
                }
                case KitchenUpgradeId.FastBurner:
                {
                    float curCook = 4.0f * Mathf.Max(0.42f, 1f - cur * 0.11f);
                    float nextCook = 4.0f * Mathf.Max(0.42f, 1f - next * 0.11f);
                    float curGrace = 3.0f + cur * 0.55f;
                    float nextGrace = 3.0f + next * 0.55f;
                    return $"Cook Time: {curCook:F1}s -> <color=#4ADE80>{nextCook:F1}s</color>\nFire Grace: {curGrace:F1}s -> <color=#4ADE80>{nextGrace:F1}s</color>";
                }
                default:
                {
                    int curPat = Mathf.RoundToInt((1f + cur * 0.12f) * 100f);
                    int nextPat = Mathf.RoundToInt((1f + next * 0.12f) * 100f);
                    int curTip = cur * 12;
                    int nextTip = next * 12;
                    return $"Patience: {curPat}% -> <color=#4ADE80>{nextPat}%</color>\nScore Tips: +{curTip}% -> <color=#4ADE80>+{nextTip}%</color>";
                }
            }
        }
    }
}
