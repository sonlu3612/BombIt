using UnityEngine;
using _Project.Gameplay.Map.Scripts;

namespace _Project.Gameplay.Block.Scripts
{
    public class DestructibleBlock : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Item Drop")]
        [SerializeField] private GameObject[] itemPrefabs;
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.35f;

        private bool isBroken = false;

        public void Break()
        {
            if (isBroken) return;
            isBroken = true;

            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            TryDropItem();

            Destroy(gameObject);
        }

        private void TryDropItem()
        {
            if (itemPrefabs == null || itemPrefabs.Length == 0) return;

            float roll = Random.value;
            if (roll > dropChance) return;

            int index = Random.Range(0, itemPrefabs.Length);
            GameObject itemPrefab = itemPrefabs[index];

            if (itemPrefab != null)
            {
                Instantiate(itemPrefab, ResolveDropPosition(), Quaternion.identity);
            }
        }

        private Vector3 ResolveDropPosition()
        {
            MapContext mapContext = Object.FindFirstObjectByType<MapContext>();
            GridOccupancyService occupancyService = mapContext != null ? mapContext.GridOccupancyService : null;

            if (occupancyService != null)
            {
                Vector3Int cell = occupancyService.WorldToCell(transform.position);
                Vector3 centeredPosition = occupancyService.GetCellCenterWorld(cell);
                centeredPosition.z = 0f;
                return centeredPosition;
            }

            Vector3 fallbackPosition = transform.position;
            fallbackPosition.x = Mathf.Floor(fallbackPosition.x) + 0.5f;
            fallbackPosition.y = Mathf.Floor(fallbackPosition.y) + 0.5f;
            fallbackPosition.z = 0f;
            return fallbackPosition;
        }
    }
}
