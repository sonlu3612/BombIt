using UnityEngine;

namespace _Project.Domain
{
    public enum ItemType
    {
        IncreaseBomb,
        IncreaseHealth,
        SpeedUp
    }
    public class Item
    {
        public ItemType type { get; set; }
        public Vector2Int position { get; set; }

        public Item(ItemType itemType, Vector2Int itemPosition)
        {
            type = itemType;
            position = itemPosition;
        }
    }
}
