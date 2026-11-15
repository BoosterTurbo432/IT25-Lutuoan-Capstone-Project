using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Drop target on a Drink Dispenser tank/nozzle.
    /// Accepts a DraggableCup and begins pouring the configured DrinkType.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class DrinkTapDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private DrinkType drinkType = DrinkType.BerryJuice;
        [SerializeField] private DrinkDispenserStation dispenserStation;

        private void Awake()
        {
            Image img = GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            if (dispenserStation == null)
            {
                dispenserStation = GetComponentInParent<DrinkDispenserStation>();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null || dispenserStation == null) return;

            if (eventData.pointerDrag.TryGetComponent<DraggableCup>(out var cup))
            {
                if (dispenserStation.CanStartPour)
                {
                    cup.NotifyAcceptedByTap();
                    dispenserStation.StartPour(drinkType);
                }
            }
        }
    }
}
