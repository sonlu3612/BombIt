using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Block.Scripts;
using _Project.Gameplay.Map.Scripts;
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

        MapContext mapContext = FindObjectOfType<MapContext>();
        if (mapContext != null)
            BotRuntimeDebugLog.LogMapSnapshot(mapContext);
    }

    private void BuildMap()
    {
        blockMap.Clear();

        foreach (Vector3Int pos in blockTilemap.cellBounds.allPositionsWithin)
        {
            if (!blockTilemap.HasTile(pos))
                continue;

            Vector3 worldPos = blockTilemap.GetCellCenterWorld(pos);
            GameObject obj = Instantiate(blockPrefab, worldPos, Quaternion.identity, blockContainer);

            DestructibleBlock block = obj.GetComponent<DestructibleBlock>();
            if (block != null)
                blockMap[pos] = block;
        }
    }

    public bool HasBlock(Vector3Int cell)
    {
        return blockMap.ContainsKey(cell) && blockMap[cell] != null;
    }

    public List<Vector3Int> GetActiveBlockCells()
    {
        List<Vector3Int> cells = new();

        foreach (KeyValuePair<Vector3Int, DestructibleBlock> entry in blockMap)
        {
            if (entry.Value != null)
                cells.Add(entry.Key);
        }

        cells.Sort((left, right) =>
        {
            int yCompare = right.y.CompareTo(left.y);
            if (yCompare != 0)
                return yCompare;

            return left.x.CompareTo(right.x);
        });

        return cells;
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

        if (destroyed)
            BotRuntimeDebugLog.LogBlockDestroyed(cell);

        return destroyed;
    }
}
