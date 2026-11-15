using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingSystem
{
    /// <summary>
    /// Trash Can drop zone that disposes burnt food from extinguished stoves,
    /// discarded prepared vessels, or unwanted plated items from the Food Display Tray.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrashCanDropZone : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            if (eventData.pointerDrag.TryGetComponent<DraggableBurntFood>(out var burntFood))
            {
                burntFood.NotifyTrashed();
                DishNotificationUI.Instance?.ShowNotification("Burnt food trashed! Stove is clean.");
                return;
            }

            if (eventData.pointerDrag.TryGetComponent<DraggablePreparedDish>(out var preparedDish))
            {
                preparedDish.NotifyConsumed();
                DishNotificationUI.Instance?.ShowNotification("Discarded prepped vessel.");
                return;
            }

            if (eventData.pointerDrag.TryGetComponent<DraggableTrayItem>(out var trayItem))
            {
                trayItem.NotifyConsumed();
                DishNotificationUI.Instance?.ShowNotification("Cleared item from Display Tray.");
                return;
            }

            if (eventData.pointerDrag.TryGetComponent<DraggableIngredient>(out var rawIngredient))
            {
                rawIngredient.NotifyAcceptedByStation(null);
                Destroy(rawIngredient.gameObject);
            }
        }
    }
}
