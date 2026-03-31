using UnityEngine;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;

    public int mapWidth = 17;
    public int mapHeight = 15;

    private Vector2Int[] spawnPoints;

    void Start()
    {
        InitSpawnPoints();
        SpawnPlayers();
    }

    void InitSpawnPoints()
    {
        spawnPoints = new Vector2Int[]
        {
            // new Vector2Int(mapWidth/2-1, mapHeight/2-1),
            new Vector2Int(-mapWidth/2+1, -mapHeight/2+1),
            // new Vector2Int(mapWidth/2-1, -mapHeight/2+1),
            // new Vector2Int(-mapWidth/2+1, mapHeight/2-1)
        };
    }

    void SpawnPlayers()
    {
        foreach (var point in spawnPoints)
        {
            ClearSpawnArea(point);

            Vector3 pos = new Vector3(
                point.x + 0.5f,
                point.y + 0.5f,
                0
            );

            // Instantiate(playerPrefab, pos, Quaternion.identity);

            // var player = Instantiate(playerPrefab, pos, Quaternion.identity);

            // StartCoroutine(SpawnSafe(player));
            var player = Instantiate(playerPrefab, pos, Quaternion.identity);

            // ép về đúng tâm ô
            player.transform.position = new Vector3(
                Mathf.Round(player.transform.position.x - 0.5f) + 0.5f,
                Mathf.Round(player.transform.position.y - 0.5f) + 0.5f,
                0
            );
           

            StartCoroutine(SpawnSafe(player));
        }
    }

    void ClearSpawnArea(Vector2Int point)
    {
        Vector2Int[] dirs =
        {
            Vector2Int.zero,
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.down
        };

        foreach (var d in dirs)
        {
            Vector2 pos = new Vector2(
                point.x + d.x + 0.5f,
                point.y + d.y + 0.5f
            );

            // Collider2D hit = Physics2D.OverlapBox(pos, Vector2.one * 0.8f, 0);

            // if (hit != null && hit.CompareTag("Block"))
            // {
            //     Destroy(hit.gameObject);
            // }

            Collider2D[] hits = Physics2D.OverlapBoxAll(pos, Vector2.one * 0.8f, 0);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Block"))
                {
                    Destroy(hit.gameObject);
                }
            }
        }

        
    }

    IEnumerator SpawnSafe(GameObject player)
    {
        Collider2D col = player.GetComponent<Collider2D>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        col.enabled = false;             // tắt va chạm
        rb.linearVelocity = Vector2.zero;      // reset lực
        rb.angularVelocity = 0;
        rb.Sleep();                      // cực quan trọng

        yield return new WaitForFixedUpdate(); // 

        col.enabled = true;           // bật lại
    }
}