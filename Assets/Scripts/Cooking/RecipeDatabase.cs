using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookingSystem
{
    public enum CookwareType
    {
        Pot,
        Pan
    }

    public enum DrinkType
    {
        None,
        BerryJuice,
        CalamansiTea
    }

    public enum OrderType
    {
        MealOnly,
        DrinkOnly,
        MealAndDrink
    }

    [Serializable]
    public class RecipeDefinition
    {
        public string recipeId;
        public string displayName;
        public CookwareType requiredCookware;
        public List<IngredientType> requiredIngredients;
        public int basePoints;
        public int minUnlockDay = 1;
        public Color placeholderColor;

        public string GetIngredientSummary()
        {
            if (requiredIngredients == null || requiredIngredients.Count == 0) return string.Empty;
            var shortNames = new List<string>(requiredIngredients.Count);
            foreach (IngredientType ing in requiredIngredients)
            {
                shortNames.Add(ing switch
                {
                    IngredientType.Tomato => "Tom",
                    IngredientType.Cheese => "Chs",
                    IngredientType.Meat => "Meat",
                    IngredientType.Mushroom => "Mush",
                    IngredientType.Onion => "Onn",
                    IngredientType.Herbs => "Grn",
                    IngredientType.Garlic => "Grl",
                    IngredientType.Potato => "Pat",
                    IngredientType.Cabbage => "Cab",
                    IngredientType.Carrots => "Car",
                    IngredientType.Pork => "Pork",
                    IngredientType.BellPepper => "Pep",
                    _ => ing.ToString()
                });
            }
            return string.Join(" + ", shortNames);
        }
    }

    [Serializable]
    public class DishPayload
    {
        public string dishName;
        public string recipeId;
        public CookwareType cookwareUsed;
        public List<IngredientType> ingredients = new List<IngredientType>();
        public float prepQuality;   // 0..1
        public float cookQuality;   // 0..1
        public Color placeholderColor;

        public float CombinedQuality => Mathf.Clamp01((prepQuality * 0.6f) + (cookQuality * 0.4f));
    }

    public static class RecipeDatabase
    {
        private static readonly List<RecipeDefinition> AllRecipes = new List<RecipeDefinition>
        {
            // --- Day 1 Recipes (Ingridients Tray Lv 1) ---
            new RecipeDefinition
            {
                recipeId = "sinigang_kamatis",
                displayName = "Sinigang sa Kamatis",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Tomato, IngredientType.Tomato },
                basePoints = 150,
                minUnlockDay = 1,
                placeholderColor = new Color(0.91f, 0.36f, 0.29f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "kabute_keso_pot",
                displayName = "Creamy Kabute & Keso",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Mushroom, IngredientType.Cheese },
                basePoints = 180,
                minUnlockDay = 1,
                placeholderColor = new Color(0.83f, 0.75f, 0.60f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "beef_mechado",
                displayName = "Mechado w/ Kabute",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Tomato, IngredientType.Mushroom },
                basePoints = 250,
                minUnlockDay = 1,
                placeholderColor = new Color(0.65f, 0.28f, 0.23f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "cheesy_ginisang_kamatis",
                displayName = "Cheesy Ginisang Kamatis",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Tomato, IngredientType.Cheese },
                basePoints = 180,
                minUnlockDay = 1,
                placeholderColor = new Color(0.96f, 0.65f, 0.30f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "pinoy_mushroom_steak",
                displayName = "Pinoy Mushroom Steak",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Mushroom },
                basePoints = 210,
                minUnlockDay = 1,
                placeholderColor = new Color(0.55f, 0.24f, 0.18f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "cheesy_kaldereta",
                displayName = "Cheesy Kaldereta",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Cheese, IngredientType.Tomato },
                basePoints = 260,
                minUnlockDay = 1,
                placeholderColor = new Color(0.85f, 0.42f, 0.26f, 1f)
            },

            // --- Day 2 Expanded Recipes (Ingridients 2: Onion, Garlic, Greens, Potato) ---
            new RecipeDefinition
            {
                recipeId = "bistek_tagalog",
                displayName = "Bistek Tagalog",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Onion, IngredientType.Garlic },
                basePoints = 240,
                minUnlockDay = 2,
                placeholderColor = new Color(0.58f, 0.32f, 0.22f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "nilagang_baka",
                displayName = "Nilagang Baka",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Potato, IngredientType.Herbs },
                basePoints = 250,
                minUnlockDay = 2,
                placeholderColor = new Color(0.76f, 0.68f, 0.42f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "ginisang_pechay",
                displayName = "Ginisang Pechay",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Garlic, IngredientType.Onion, IngredientType.Herbs },
                basePoints = 195,
                minUnlockDay = 2,
                placeholderColor = new Color(0.32f, 0.72f, 0.44f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "tortang_patatas",
                displayName = "Tortang Patatas",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Potato, IngredientType.Onion, IngredientType.Cheese },
                basePoints = 215,
                minUnlockDay = 2,
                placeholderColor = new Color(0.92f, 0.76f, 0.34f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "picadillo_sabaw",
                displayName = "Picadillo Sabaw",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Tomato, IngredientType.Potato },
                basePoints = 245,
                minUnlockDay = 2,
                placeholderColor = new Color(0.82f, 0.42f, 0.28f, 1f)
            },

            // --- Day 3 & Endless Expanded Recipes (Ingridients 3: Cabbage, Carrots, Pork, Pepper) ---
            new RecipeDefinition
            {
                recipeId = "pork_menudo",
                displayName = "Pork Menudo",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Pork, IngredientType.Potato, IngredientType.Carrots },
                basePoints = 275,
                minUnlockDay = 3,
                placeholderColor = new Color(0.88f, 0.44f, 0.25f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "afritadang_baboy",
                displayName = "Afritadang Baboy",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Pork, IngredientType.BellPepper, IngredientType.Tomato },
                basePoints = 280,
                minUnlockDay = 3,
                placeholderColor = new Color(0.84f, 0.28f, 0.26f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "pork_pochero",
                displayName = "Pork Pochero",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Pork, IngredientType.Cabbage, IngredientType.Tomato },
                basePoints = 270,
                minUnlockDay = 3,
                placeholderColor = new Color(0.75f, 0.48f, 0.35f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "carinderia_chop_suey",
                displayName = "Pinoy Chop Suey",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Cabbage, IngredientType.Carrots, IngredientType.BellPepper },
                basePoints = 240,
                minUnlockDay = 3,
                placeholderColor = new Color(0.38f, 0.76f, 0.46f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "spicy_pork_sisig",
                displayName = "Sizzling Pork Sisig",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Pork, IngredientType.Onion, IngredientType.BellPepper },
                basePoints = 285,
                minUnlockDay = 3,
                placeholderColor = new Color(0.86f, 0.56f, 0.28f, 1f)
            }
        };

        public static IReadOnlyList<RecipeDefinition> Recipes => AllRecipes;

        public static RecipeDefinition GetRandomRecipe()
        {
            return AllRecipes[UnityEngine.Random.Range(0, AllRecipes.Count)];
        }

        public static RecipeDefinition GetRandomRecipeForDay(int maxUnlockDay)
        {
            int clampedDay = Mathf.Clamp(maxUnlockDay, 1, 3);
            var unlocked = new List<RecipeDefinition>();
            var newlyAdded = new List<RecipeDefinition>();

            for (int i = 0; i < AllRecipes.Count; i++)
            {
                if (AllRecipes[i].minUnlockDay <= clampedDay)
                {
                    unlocked.Add(AllRecipes[i]);
                    if (AllRecipes[i].minUnlockDay == clampedDay)
                    {
                        newlyAdded.Add(AllRecipes[i]);
                    }
                }
            }

            if (unlocked.Count == 0) return AllRecipes[0];

            // Favor newly unlocked Day 2/3 dishes so players experience fresh ingredients immediately
            if (clampedDay > 1 && newlyAdded.Count > 0 && UnityEngine.Random.value < 0.62f)
            {
                return newlyAdded[UnityEngine.Random.Range(0, newlyAdded.Count)];
            }

            return unlocked[UnityEngine.Random.Range(0, unlocked.Count)];
        }

        public static RecipeDefinition GetById(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            for (int i = 0; i < AllRecipes.Count; i++)
            {
                if (AllRecipes[i].recipeId == recipeId) return AllRecipes[i];
            }
            return null;
        }

        /// <summary>
        /// Finds the best matching recipe for a given set of ingredients and cookware.
        /// Exact multiset + cookware match returns 1f; ingredient match with wrong cookware returns 0.5f.
        /// </summary>
        public static RecipeDefinition FindBestMatch(
            IReadOnlyList<IngredientType> ingredients,
            CookwareType cookware,
            out float cookwareAccuracy)
        {
            cookwareAccuracy = 0.6f;
            if (ingredients == null || ingredients.Count == 0) return null;

            RecipeDefinition bestPartial = null;
            float bestScore = -1f;

            foreach (RecipeDefinition recipe in AllRecipes)
            {
                bool exactIngredients = AreSameMultiset(ingredients, recipe.requiredIngredients);
                if (exactIngredients && recipe.requiredCookware == cookware)
                {
                    cookwareAccuracy = 1f;
                    return recipe;
                }

                if (exactIngredients)
                {
                    // Right ingredients, wrong Pot/Pan choice
                    cookwareAccuracy = 0.5f;
                    bestPartial = recipe;
                    bestScore = 100f;
                    continue;
                }

                float overlap = ComputeOverlapRatio(ingredients, recipe.requiredIngredients);
                if (recipe.requiredCookware == cookware) overlap += 0.2f;
                if (overlap > bestScore)
                {
                    bestScore = overlap;
                    bestPartial = recipe;
                }
            }

            if (bestScore >= 100f)
            {
                cookwareAccuracy = 0.5f;
                return bestPartial;
            }

            // Ad-hoc dish fallback
            cookwareAccuracy = bestPartial != null && bestPartial.requiredCookware == cookware ? 0.75f : 0.55f;
            return null;
        }

        public static string GetFallbackDishName(IReadOnlyList<IngredientType> ingredients, CookwareType cookware)
        {
            if (ingredients == null || ingredients.Count == 0) return "Carinderia Special Ulam";

            bool hasTomato = false;
            bool hasCheese = false;
            bool hasMeat = false;
            bool hasMushroom = false;
            bool hasOnion = false;
            bool hasGreens = false;
            bool hasGarlic = false;
            bool hasPotato = false;
            bool hasCabbage = false;
            bool hasCarrots = false;
            bool hasPork = false;
            bool hasPepper = false;

            for (int i = 0; i < ingredients.Count; i++)
            {
                switch (ingredients[i])
                {
                    case IngredientType.Tomato: hasTomato = true; break;
                    case IngredientType.Cheese: hasCheese = true; break;
                    case IngredientType.Meat: hasMeat = true; break;
                    case IngredientType.Mushroom: hasMushroom = true; break;
                    case IngredientType.Onion: hasOnion = true; break;
                    case IngredientType.Herbs: hasGreens = true; break;
                    case IngredientType.Garlic: hasGarlic = true; break;
                    case IngredientType.Potato: hasPotato = true; break;
                    case IngredientType.Cabbage: hasCabbage = true; break;
                    case IngredientType.Carrots: hasCarrots = true; break;
                    case IngredientType.Pork: hasPork = true; break;
                    case IngredientType.BellPepper: hasPepper = true; break;
                }
            }

            if (hasPork && hasPepper && hasTomato) return "Afritadang Baboy";
            if (hasPork && hasPotato && hasCarrots) return "Pork Menudo";
            if (hasPork && hasCabbage) return "Pork Pochero";
            if (hasPork && (hasOnion || hasPepper)) return "Sizzling Pork Sisig";
            if (hasCabbage && hasCarrots && hasPepper) return "Pinoy Chop Suey";
            if (hasMeat && hasOnion && hasGarlic) return "Bistek Tagalog";
            if (hasMeat && hasPotato && hasGreens) return "Nilagang Baka";
            if (hasMeat && hasTomato && hasCheese) return "Cheesy Kaldereta";
            if (hasMeat && hasTomato && hasMushroom) return "Mechado w/ Kabute";
            if (hasMeat && hasMushroom && hasCheese) return "Creamy Mushroom Bistek";
            if (hasTomato && hasCheese && hasMushroom) return "Cheesy Kamatis-Kabute Gisa";
            if (hasGarlic && hasOnion && hasGreens) return "Ginisang Pechay";
            if (hasPotato && hasOnion && hasCheese) return "Tortang Patatas";
            if (hasMeat && hasMushroom) return "Pinoy Mushroom Steak";
            if (hasTomato && hasCheese) return "Cheesy Ginisang Kamatis";
            if (hasTomato && hasMushroom) return "Ginisang Kamatis & Kabute";
            if (hasMeat && hasTomato) return "Pork Menudillo";
            if (hasCheese && hasMushroom) return "Creamy Kabute & Keso";
            if (hasMeat && hasCheese) return "Cheesy Sisig";
            if (hasPork) return cookware == CookwareType.Pot ? "Sinigang na Baboy" : "Lechon Kawali";
            if (hasCabbage || hasCarrots || hasGreens) return cookware == CookwareType.Pot ? "Bulanglang Gulay" : "Ginisang Gulay";
            if (hasPotato) return "Adobong Patatas";
            if (hasPepper) return "Bicol Sili Stir-Fry";
            if (hasGarlic || hasOnion) return "Sangkutsa Special";
            if (hasMeat) return cookware == CookwareType.Pot ? "Nilagang Baka" : "Bistek Tagalog";
            if (hasTomato) return cookware == CookwareType.Pot ? "Sinigang sa Kamatis" : "Sarsyadong Kamatis";
            if (hasCheese) return cookware == CookwareType.Pot ? "Mainit na Keso Soup" : "Pinoy Kesong Melt";
            if (hasMushroom) return cookware == CookwareType.Pot ? "Sabaw ng Kabute" : "Ginisang Kabute";

            return "Lutong Bahay Special";
        }

        public static Color GetFallbackColor(IReadOnlyList<IngredientType> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return new Color(0.88f, 0.48f, 0.37f, 1f);
            return ingredients[0] switch
            {
                IngredientType.Tomato => new Color(0.91f, 0.36f, 0.29f, 1f),
                IngredientType.Cheese => new Color(0.96f, 0.79f, 0.36f, 1f),
                IngredientType.Meat => new Color(0.72f, 0.31f, 0.26f, 1f),
                IngredientType.Mushroom => new Color(0.78f, 0.68f, 0.55f, 1f),
                IngredientType.Onion => new Color(0.68f, 0.42f, 0.80f, 1f),
                IngredientType.Herbs => new Color(0.32f, 0.76f, 0.45f, 1f),
                IngredientType.Garlic => new Color(0.92f, 0.84f, 0.68f, 1f),
                IngredientType.Potato => new Color(0.88f, 0.70f, 0.32f, 1f),
                IngredientType.Cabbage => new Color(0.45f, 0.82f, 0.52f, 1f),
                IngredientType.Carrots => new Color(0.96f, 0.52f, 0.22f, 1f),
                IngredientType.Pork => new Color(0.88f, 0.34f, 0.38f, 1f),
                IngredientType.BellPepper => new Color(0.94f, 0.68f, 0.24f, 1f),
                _ => new Color(0.88f, 0.48f, 0.37f, 1f)
            };
        }

        public static string GetDrinkDisplayName(DrinkType drink)
        {
            return drink switch
            {
                DrinkType.BerryJuice => "Berry Juice",
                DrinkType.CalamansiTea => "Calamansi Tea",
                _ => "None"
            };
        }

        public static Color GetDrinkColor(DrinkType drink)
        {
            return drink switch
            {
                DrinkType.BerryJuice => new Color(0.88f, 0.48f, 0.37f, 1f),
                DrinkType.CalamansiTea => new Color(0.32f, 0.72f, 0.53f, 1f),
                _ => Color.white
            };
        }

        public static float ComputeOrderAccuracy(DishPayload delivered, RecipeDefinition ordered)
        {
            if (delivered == null || ordered == null) return 0f;
            if (delivered.recipeId == ordered.recipeId && delivered.cookwareUsed == ordered.requiredCookware)
            {
                return 1f;
            }

            float ingredientOverlap = ComputeOverlapRatio(delivered.ingredients, ordered.requiredIngredients);
            float cookwareFactor = delivered.cookwareUsed == ordered.requiredCookware ? 0.2f : 0f;
            return Mathf.Clamp(ingredientOverlap * 0.6f + cookwareFactor, 0.1f, 0.8f);
        }

        private static bool AreSameMultiset(IReadOnlyList<IngredientType> a, IReadOnlyList<IngredientType> b)
        {
            if (a.Count != b.Count) return false;
            var counts = new Dictionary<IngredientType, int>();
            for (int i = 0; i < a.Count; i++)
            {
                counts[a[i]] = counts.TryGetValue(a[i], out int c) ? c + 1 : 1;
            }
            for (int i = 0; i < b.Count; i++)
            {
                if (!counts.TryGetValue(b[i], out int c) || c <= 0) return false;
                counts[b[i]] = c - 1;
            }
            return true;
        }

        private static float ComputeOverlapRatio(IReadOnlyList<IngredientType> a, IReadOnlyList<IngredientType> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0f;
            var remaining = new Dictionary<IngredientType, int>();
            for (int i = 0; i < b.Count; i++)
            {
                remaining[b[i]] = remaining.TryGetValue(b[i], out int c) ? c + 1 : 1;
            }

            int matches = 0;
            for (int i = 0; i < a.Count; i++)
            {
                if (remaining.TryGetValue(a[i], out int c) && c > 0)
                {
                    matches++;
                    remaining[a[i]] = c - 1;
                }
            }

            int maxLen = Mathf.Max(a.Count, b.Count);
            return (float)matches / maxLen;
        }
    }
}
