using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    public GameObject[] itemPrefabs;   // danh sách item
    public float dropRate = 0.8f;      // tỉ lệ rơi
    public Transform itemContainer;    // nơi chứa item

    public void DestroyBlock()
    {
        TrySpawnItem();   // pawn trước
        Destroy(gameObject);
    }

    void TrySpawnItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) return;

        if (Random.value < dropRate)
        {
            int index = Random.Range(0, itemPrefabs.Length);

            // Vector3 spawnPos = new Vector3(
            //     Mathf.Floor(transform.position.x) + 0.5f,
            //     Mathf.Floor(transform.position.y) + 0.5f,
            //     0
            // );
            Debug.Log(transform.position);
            Vector3 spawnPos = transform.position - new Vector3(0.5f, 0.5f, 0); // spawn ở giữa ôS;

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
}