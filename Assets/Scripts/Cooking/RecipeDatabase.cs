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
            new RecipeDefinition
            {
                recipeId = "tomato_soup",
                displayName = "Classic Tomato Soup",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Tomato, IngredientType.Tomato },
                basePoints = 150,
                placeholderColor = new Color(0.91f, 0.36f, 0.29f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "mushroom_chowder",
                displayName = "Mushroom Cheese Chowder",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Mushroom, IngredientType.Cheese },
                basePoints = 180,
                placeholderColor = new Color(0.83f, 0.75f, 0.60f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "beef_stew",
                displayName = "Hearty Beef Stew",
                requiredCookware = CookwareType.Pot,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Tomato, IngredientType.Mushroom },
                basePoints = 250,
                placeholderColor = new Color(0.65f, 0.28f, 0.23f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "cheesy_tomato_melt",
                displayName = "Cheesy Tomato Melt",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Tomato, IngredientType.Cheese },
                basePoints = 180,
                placeholderColor = new Color(0.96f, 0.65f, 0.30f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "steak_mushroom",
                displayName = "Seared Steak & Mushroom",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Mushroom },
                basePoints = 210,
                placeholderColor = new Color(0.55f, 0.24f, 0.18f, 1f)
            },
            new RecipeDefinition
            {
                recipeId = "meat_skillet",
                displayName = "Savory Meat Skillet",
                requiredCookware = CookwareType.Pan,
                requiredIngredients = new List<IngredientType> { IngredientType.Meat, IngredientType.Cheese, IngredientType.Tomato },
                basePoints = 260,
                placeholderColor = new Color(0.85f, 0.42f, 0.26f, 1f)
            }
        };

        public static IReadOnlyList<RecipeDefinition> Recipes => AllRecipes;

        public static RecipeDefinition GetRandomRecipe()
        {
            return AllRecipes[UnityEngine.Random.Range(0, AllRecipes.Count)];
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
            if (ingredients == null || ingredients.Count == 0) return "House Special";
            string suffix = cookware == CookwareType.Pot ? "Soup" : "Skillet";
            return $"Chef's {ingredients[0]} {suffix}";
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
