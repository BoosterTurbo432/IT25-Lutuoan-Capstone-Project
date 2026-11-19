using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Trash Can drop zone that disposes burnt food from extinguished stoves,
    /// discarded prepared vessels, or unwanted plated items from the Food Display Tray.
    /// </summary>
    [DisallowMultipleComponent]
    public class TrashCanDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("2D State Sprites")]
        [SerializeField] private Image trashImage;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        private void Awake()
        {
            SetOpenVisual(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            if (IsAcceptableDrag(eventData.pointerDrag))
            {
                SetOpenVisual(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetOpenVisual(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            SetOpenVisual(false);
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

        private static bool IsAcceptableDrag(GameObject dragged)
        {
            return dragged.GetComponent<DraggableBurntFood>() != null ||
                   dragged.GetComponent<DraggablePreparedDish>() != null ||
                   dragged.GetComponent<DraggableTrayItem>() != null ||
                   dragged.GetComponent<DraggableIngredient>() != null;
        }

        private void SetOpenVisual(bool open)
        {
            if (trashImage == null) return;
            Sprite target = open ? openSprite : closedSprite;
            if (target != null)
            {
                trashImage.sprite = target;
            }
        }
    }
}
