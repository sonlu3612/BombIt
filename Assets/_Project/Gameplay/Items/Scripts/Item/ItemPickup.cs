using _Project.Gameplay.Player.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Audio.Scripts;
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
        private GridOccupancyService occupancyService;
        private Vector3Int itemCell;

        private void Start()
        {
            MapContext mapContext = Object.FindFirstObjectByType<MapContext>();
            occupancyService = mapContext != null ? mapContext.GridOccupancyService : null;

            if (occupancyService != null)
            {
                itemCell = occupancyService.WorldToCell(transform.position);

                Vector3 centeredPosition = occupancyService.GetCellCenterWorld(itemCell);
                centeredPosition.z = transform.position.z;
                transform.position = centeredPosition;
            }
            else
                itemCell = new Vector3Int(
                    Mathf.RoundToInt(transform.position.x),
                    Mathf.RoundToInt(transform.position.y),
                    0);
        }

        private void Update()
        {
            TryPickupFromCurrentCell();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPicked) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            CompletePickup(player);
        }

        private void TryPickupFromCurrentCell()
        {
            if (isPicked || occupancyService == null)
                return;

            if (!occupancyService.TryGetAnyPlayerAtCell(itemCell, out PlayerController player))
                return;

            CompletePickup(player);
        }

        private void CompletePickup(PlayerController player)
        {
            if (isPicked || player == null)
                return;

            ApplyEffect(player);
            AudioManager.Instance?.PlayItemPickup();
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
