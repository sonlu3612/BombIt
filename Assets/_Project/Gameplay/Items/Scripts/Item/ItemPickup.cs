using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using static Item;

namespace _Project.Gameplay.Item.Scripts
{
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Config")]
        [SerializeField] private ItemType itemType;
        [SerializeField] private float speedAmount = 0.5f;
        [SerializeField] private int bombAmount = 1;
        [SerializeField] private int rangeAmount = 1;
        [SerializeField] private int hpAmount = 1;

        private bool isPicked = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPicked) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            ApplyEffect(player);
            isPicked = true;
            Destroy(gameObject);
        }

        private void ApplyEffect(PlayerController player)
        {
            switch (itemType)
            {
                case ItemType.Range:
                    player.AddRange(rangeAmount);
                    break;

                case ItemType.Speed:
                    player.AddSpeed(speedAmount);
                    break;

                case ItemType.Health:
                    player.AddHealth(hpAmount);
                    break;

                case ItemType.Bomb:
                    player.AddBomb(bombAmount);
                    break;
            }
        }
    }
}