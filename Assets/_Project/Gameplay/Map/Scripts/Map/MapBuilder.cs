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
    [SerializeField] private bool debugBuildLogs = true;

    private readonly Dictionary<Vector3Int, DestructibleBlock> blockMap = new();

    private void Start()
    {
        LogBuildState("Before Build");
        BuildMap();
        LogBuildState("After Build");

        MapContext mapContext = Object.FindFirstObjectByType<MapContext>();
        if (mapContext != null)
            BotRuntimeDebugLog.LogMapSnapshot(mapContext);
    }

    private void BuildMap()
    {
        blockMap.Clear();

        if (blockTilemap == null)
        {
            Debug.LogError($"[{nameof(MapBuilder)}] Block Tilemap is missing in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'.", this);
            return;
        }

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

    private void LogBuildState(string label)
    {
        if (!debugBuildLogs)
            return;

        int tileCount = 0;

        if (blockTilemap != null)
        {
            foreach (Vector3Int pos in blockTilemap.cellBounds.allPositionsWithin)
            {
                if (blockTilemap.HasTile(pos))
                    tileCount++;
            }
        }

        Debug.Log(
            $"[{nameof(MapBuilder)}] {label} | Scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} | Tilemap={(blockTilemap != null ? blockTilemap.name : "NULL")} | TileCount={tileCount} | BuiltBlocks={blockMap.Count} | ContainerChildren={(blockContainer != null ? blockContainer.childCount : -1)}",
            this);
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
