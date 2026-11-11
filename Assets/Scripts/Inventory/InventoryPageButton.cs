using Collector.Interaction;
using UnityEngine;

namespace Collector.Inventory
{
    /// <summary>Envanter penceresinde önceki / sonraki sayfa butonu.</summary>
    public class InventoryPageButton : InteractableHotspot
    {
        [SerializeField] InventoryUI owner;
        [SerializeField] int direction = 1;

        public void Configure(InventoryUI inventoryUI, int pageDirection)
        {
            owner = inventoryUI;
            direction = pageDirection;
        }

        public override void Interact() => owner.ChangePage(direction);
    }
}
