using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Represents a single staging slot inside the Food Display Tray.
    /// Holds either a plated meal or a filled drink as a DraggableTrayItem.
    /// </summary>
    [DisallowMultipleComponent]
    public class FoodDisplaySlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private DraggableTrayItem draggableItem;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image trayFrameImage;
        [SerializeField] private Sprite closedLidSprite;
        [SerializeField] private Sprite openTraySprite;

        [Header("Display Settings")]
        [SerializeField] private string defaultLabel = "Tray Slot";

        public bool IsOccupied { get; private set; }
        public string CurrentDishName { get; private set; } = string.Empty;

        public UnityEvent<FoodDisplaySlot, string> onSlotCleared;

        private void Awake()
        {
            ClearSlot(notify: false);
        }

        public void SetMeal(DishPayload payload)
        {
            if (payload == null) return;
            IsOccupied = true;
            CurrentDishName = payload.dishName;

            if (draggableItem != null)
            {
                draggableItem.ConfigureMeal(payload, this);
            }

            if (trayFrameImage != null && openTraySprite != null)
            {
                trayFrameImage.sprite = openTraySprite;
            }

            if (labelText != null)
            {
                labelText.text = $"{payload.dishName}\n<color=#FDE047>Drag to Customer</color>";
            }
        }

        public void SetDrink(DrinkType drink)
        {
            if (drink == DrinkType.None) return;
            string drinkName = RecipeDatabase.GetDrinkDisplayName(drink);
            IsOccupied = true;
            CurrentDishName = drinkName;

            if (draggableItem != null)
            {
                draggableItem.ConfigureDrink(drink, this);
            }

            if (trayFrameImage != null && openTraySprite != null)
            {
                trayFrameImage.sprite = openTraySprite;
            }

            if (labelText != null)
            {
                labelText.text = $"{drinkName}\n<color=#FDE047>Drag to Customer</color>";
            }
        }

        public void ClearSlot(bool notify = true)
        {
            string previousDish = CurrentDishName;
            bool wasOccupied = IsOccupied;

            IsOccupied = false;
            CurrentDishName = string.Empty;

            if (trayFrameImage != null && closedLidSprite != null)
            {
                trayFrameImage.sprite = closedLidSprite;
            }

            if (draggableItem != null && draggableItem.Kind != TrayItemKind.None)
            {
                draggableItem.ClearItem();
            }
            else if (draggableItem != null)
            {
                draggableItem.gameObject.SetActive(false);
            }

            if (labelText != null)
            {
                labelText.text = defaultLabel;
            }

            if (notify && wasOccupied)
            {
                onSlotCleared?.Invoke(this, previousDish);
            }
        }
    }
}
