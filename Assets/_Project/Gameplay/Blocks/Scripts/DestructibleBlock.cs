using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    public GameObject[] itemPrefabs;
    public float dropRate = 0.8f;
    public Transform itemContainer;

    public void DestroyBlock()
    {
        TrySpawnItem();
        Destroy(gameObject);
    }

    private void TrySpawnItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) return;
        if (Random.value >= dropRate) return;

        int index = Random.Range(0, itemPrefabs.Length);

        Vector3 spawnPos = transform.position;

        if (itemContainer != null)
        {
            Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity, itemContainer);
        }
        else
        {
            Instantiate(itemPrefabs[index], spawnPos, Quaternion.identity);
        }
    }
}