using System.Collections.Generic;
using UnityEngine;

namespace CookingSystem
{
    /// <summary>
    /// Staging counter holding plated meals and filled drinks ready to be dragged to customers.
    /// </summary>
    [DisallowMultipleComponent]
    public class FoodDisplayTray : MonoBehaviour
    {
        [Header("Tray Slots & Sources")]
        [SerializeField] private List<FoodDisplaySlot> slots = new List<FoodDisplaySlot>();
        [SerializeField] private List<CookingStation> cookingStations = new List<CookingStation>();

        public bool HasFreeSlot
        {
            get
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null && !slots[i].IsOccupied) return true;
                }
                return false;
            }
        }

        private void OnEnable()
        {
            foreach (CookingStation station in cookingStations)
            {
                if (station != null)
                {
                    station.onDishServed.AddListener(HandleLegacyDishServed);
                }
            }
        }

        private void OnDisable()
        {
            foreach (CookingStation station in cookingStations)
            {
                if (station != null)
                {
                    station.onDishServed.RemoveListener(HandleLegacyDishServed);
                }
            }
        }

        public bool TryPlaceMeal(DishPayload payload)
        {
            if (payload == null) return false;

            foreach (FoodDisplaySlot slot in slots)
            {
                if (slot != null && !slot.IsOccupied)
                {
                    slot.SetMeal(payload);
                    return true;
                }
            }

            return false;
        }

        public bool TryPlaceDrink(DrinkType drink)
        {
            if (drink == DrinkType.None) return false;

            foreach (FoodDisplaySlot slot in slots)
            {
                if (slot != null && !slot.IsOccupied)
                {
                    slot.SetDrink(drink);
                    return true;
                }
            }

            return false;
        }

        private void HandleLegacyDishServed(string dishName)
        {
            // Backwards-compatible fallback when a CookingStation fires string-only onDishServed
            if (string.IsNullOrEmpty(dishName)) return;
            var payload = new DishPayload
            {
                dishName = dishName,
                recipeId = string.Empty,
                cookwareUsed = CookwareType.Pot,
                prepQuality = 0.85f,
                cookQuality = 1f,
                placeholderColor = new Color(0.88f, 0.48f, 0.37f, 1f)
            };
            TryPlaceMeal(payload);
        }
    }
}
