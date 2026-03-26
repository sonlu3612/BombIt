using UnityEngine;

public class DestructibleBlock : MonoBehaviour
{
    public GameObject itemPrefab;

    public void DestroyBlock()
    {
        Instantiate(itemPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}