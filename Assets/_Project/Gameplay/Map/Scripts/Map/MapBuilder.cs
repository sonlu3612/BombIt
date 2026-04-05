using _Project.Gameplay.Block.Scripts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapBuilder : MonoBehaviour
{
    public Tilemap blockTilemap;
    public GameObject blockPrefab;
    public Transform blockContainer;

    private readonly Dictionary<Vector3Int, DestructibleBlock> blockMap = new();

    private void Start()
    {
        BuildMap();
    }

    private void BuildMap()
    {
        blockMap.Clear();

        foreach (Vector3Int pos in blockTilemap.cellBounds.allPositionsWithin)
        {
            if (!blockTilemap.HasTile(pos)) continue;

            Vector3 worldPos = blockTilemap.GetCellCenterWorld(pos);
            GameObject obj = Instantiate(blockPrefab, worldPos, Quaternion.identity, blockContainer);

            DestructibleBlock block = obj.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                blockMap[pos] = block;
            }
        }

        // KHÔNG clear tilemap nữa
        // blockTilemap.ClearAllTiles();
    }

    public bool HasBlock(Vector3Int cell)
    {
        return blockMap.ContainsKey(cell) && blockMap[cell] != null;
    }

    public bool DestroyBlockAt(Vector3Int cell)
    {
        bool destroyed = false;

        if (blockMap.TryGetValue(cell, out DestructibleBlock block) && block != null)
        {
            block.Break();
            blockMap.Remove(cell);
            destroyed = true;
        }

        if (blockTilemap != null && blockTilemap.HasTile(cell))
        {
            blockTilemap.SetTile(cell, null);
            blockTilemap.RefreshTile(cell);
            destroyed = true;
        }

        return destroyed;
    }
}