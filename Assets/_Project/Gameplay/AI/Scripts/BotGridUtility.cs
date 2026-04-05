using System.Collections.Generic;
using _Project.Gameplay.Map.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotGridUtility
    {
        public static readonly Vector3Int[] CardinalDirections =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        public static bool IsWall(Vector3Int cell, MapContext mapContext)
        {
            return mapContext != null &&
                   mapContext.WallTilemap != null &&
                   mapContext.WallTilemap.HasTile(cell);
        }

        public static bool HasBlock(Vector3Int cell, MapContext mapContext)
        {
            return mapContext != null &&
                   mapContext.MapBuilder != null &&
                   mapContext.MapBuilder.HasBlock(cell);
        }

        public static bool IsWalkable(Vector3Int cell, MapContext mapContext)
        {
            return !IsWall(cell, mapContext) && !HasBlock(cell, mapContext);
        }

        public static bool IsAdjacent(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        public static HashSet<Vector3Int> GetBlastCells(Vector3Int bombCell, int range, MapContext mapContext)
        {
            HashSet<Vector3Int> cells = new();
            cells.Add(bombCell);

            foreach (Vector3Int dir in CardinalDirections)
            {
                for (int i = 1; i <= range; i++)
                {
                    Vector3Int next = bombCell + dir * i;

                    if (IsWall(next, mapContext))
                        break;

                    cells.Add(next);

                    if (HasBlock(next, mapContext))
                        break;
                }
            }

            return cells;
        }

        public static bool CanBlastHitTarget(Vector3Int bombCell, Vector3Int targetCell, int range, MapContext mapContext)
        {
            return GetBlastCells(bombCell, range, mapContext).Contains(targetCell);
        }
    }
}
