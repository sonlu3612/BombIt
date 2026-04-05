using UnityEngine;

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
                Instantiate(itemPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}