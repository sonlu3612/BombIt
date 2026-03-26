using UnityEngine;
using UnityEngine.Tilemaps;

public class MapBuilder : MonoBehaviour
{
    public Tilemap blockTilemap;
    public GameObject blockPrefab;
    public Transform blockContainer;

    void Start()
    {
        BuildMap();
    }

    void BuildMap()
    {
        foreach (var pos in blockTilemap.cellBounds.allPositionsWithin)
        {
            if (!blockTilemap.HasTile(pos)) continue;

            // chuyển từ cell → world
            Vector3 worldPos = blockTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

            // spawn block prefab
            Instantiate(blockPrefab, worldPos, Quaternion.identity, blockContainer);
        }

        // xoá tile (chỉ dùng làm data)
        //blockTilemap.ClearAllTiles();
    }
}