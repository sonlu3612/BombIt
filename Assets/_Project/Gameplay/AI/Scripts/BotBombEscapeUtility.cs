using System.Collections.Generic;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotBombEscapeUtility
    {
        private const float DefaultBombFuseTime = 2f;
        private const float SafetyBuffer = 0.2f;
        private const float PerStepTurnPadding = 0.05f;

        public static bool TryFindEscapePath(
            BotNavigator navigator,
            PlayerController player,
            BotSenseContext sense,
            Vector3Int bombCell,
            out List<Vector3Int> escapePath)
        {
            escapePath = null;

            if (navigator == null || player == null || sense == null)
                return false;

            HashSet<Vector3Int> futureBlast = BotGridUtility.GetBlastCells(
                bombCell,
                player.BombRangeStat,
                navigator.MapContext);

            List<Vector3Int> escapeCandidates = new();
            foreach (Vector3Int cell in sense.FreeCells)
            {
                if (cell == bombCell)
                    continue;

                if (futureBlast.Contains(cell))
                    continue;

                if (sense.DangerCells.Contains(cell))
                    continue;

                escapeCandidates.Add(cell);
            }

            if (escapeCandidates.Count == 0)
                return false;

            float bestTravelTime = float.MaxValue;
            List<Vector3Int> bestPath = null;

            foreach (Vector3Int candidate in escapeCandidates)
            {
                List<Vector3Int> candidatePath = navigator.FindPath(
                    bombCell,
                    candidate,
                    sense.DangerCells,
                    sense.BlockedCells,
                    player);

                if (candidatePath == null || candidatePath.Count <= 1)
                    continue;

                float travelTime = EstimateTravelTime(candidatePath, player, navigator);
                if (travelTime >= DefaultBombFuseTime - SafetyBuffer)
                    continue;

                if (travelTime < bestTravelTime)
                {
                    bestTravelTime = travelTime;
                    bestPath = candidatePath;
                }
            }

            escapePath = bestPath;
            return bestPath != null;
        }

        private static float EstimateTravelTime(List<Vector3Int> path, PlayerController player, BotNavigator navigator)
        {
            if (path == null || path.Count <= 1 || player == null)
                return float.MaxValue;

            float speed = Mathf.Max(0.1f, player.MoveSpeedStat);
            float cellDistance = GetCellTravelDistance(navigator);
            int steps = path.Count - 1;

            return (steps * cellDistance / speed) + (steps * PerStepTurnPadding);
        }

        private static float GetCellTravelDistance(BotNavigator navigator)
        {
            if (navigator?.MapContext?.ReferenceTilemap != null)
                return GetTilemapCellDistance(navigator.MapContext.ReferenceTilemap);

            if (navigator?.MapContext?.WallTilemap != null)
                return GetTilemapCellDistance(navigator.MapContext.WallTilemap);

            return 1f;
        }

        private static float GetTilemapCellDistance(Tilemap tilemap)
        {
            Vector3 size = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : tilemap.cellSize;
            float x = Mathf.Abs(size.x);
            float y = Mathf.Abs(size.y);
            float distance = Mathf.Max(x, y);
            return distance > 0f ? distance : 1f;
        }
    }
}
