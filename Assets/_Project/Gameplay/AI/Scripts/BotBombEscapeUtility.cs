using System.Collections.Generic;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotBombEscapeUtility
    {
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

            Dictionary<Vector3Int, float> dangerDeadlines = BuildDangerDeadlines(
                sense,
                bombCell,
                player.BombRangeStat,
                navigator.MapContext);

            return TryFindTimedEscapePath(navigator, player, bombCell, sense, dangerDeadlines, out escapePath);
        }

        public static bool TryFindTimedEscapePath(
            BotNavigator navigator,
            PlayerController player,
            Vector3Int startCell,
            BotSenseContext sense,
            Dictionary<Vector3Int, float> dangerDeadlines,
            out List<Vector3Int> escapePath)
        {
            escapePath = null;

            if (navigator == null || navigator.MapContext == null || player == null || sense == null)
                return false;

            float stepTravelTime = EstimateSingleStepTravelTime(player, navigator);
            HashSet<Vector3Int> traversableCells = new(sense.FreeCells);
            traversableCells.Add(startCell);

            Queue<Vector3Int> queue = new();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new();
            Dictionary<Vector3Int, int> stepsByCell = new();

            queue.Enqueue(startCell);
            stepsByCell[startCell] = 0;

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                int currentSteps = stepsByCell[current];

                if (current != startCell && !dangerDeadlines.ContainsKey(current))
                {
                    escapePath = ReconstructPath(cameFrom, startCell, current);
                    return true;
                }

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int next = current + dir;
                    if (stepsByCell.ContainsKey(next))
                        continue;

                    if (!traversableCells.Contains(next))
                        continue;

                    if (!IsTraversalCellWalkable(navigator, player, next, sense.BlockedCells))
                        continue;

                    int nextSteps = currentSteps + 1;
                    float arrivalTime = nextSteps * stepTravelTime;
                    if (!CanReachCellBeforeDanger(next, arrivalTime, dangerDeadlines))
                        continue;

                    cameFrom[next] = current;
                    stepsByCell[next] = nextSteps;

                    if (!dangerDeadlines.ContainsKey(next))
                    {
                        escapePath = ReconstructPath(cameFrom, startCell, next);
                        return true;
                    }

                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static Dictionary<Vector3Int, float> BuildDangerDeadlines(
            BotSenseContext sense,
            Vector3Int futureBombCell,
            int futureBombRange,
            MapContext mapContext)
        {
            Dictionary<Vector3Int, float> deadlines = new(sense.DangerTimes);
            HashSet<Vector3Int> futureBlast = BotGridUtility.GetBlastCells(futureBombCell, futureBombRange, mapContext);

            foreach (Vector3Int cell in futureBlast)
            {
                float existingDeadline = deadlines.TryGetValue(cell, out float deadline)
                    ? deadline
                    : float.MaxValue;

                deadlines[cell] = Mathf.Min(existingDeadline, _Project.Domain.Bomb.DefaultExplodeTime);
            }

            return deadlines;
        }

        private static bool IsTraversalCellWalkable(
            BotNavigator navigator,
            PlayerController player,
            Vector3Int cell,
            HashSet<Vector3Int> blockedCells)
        {
            MapContext mapContext = navigator.MapContext;

            if (!BotGridUtility.IsWithinBounds(cell, mapContext))
                return false;

            GridOccupancyService occupancy = mapContext != null ? mapContext.GridOccupancyService : null;

            if (occupancy != null)
            {
                if (occupancy.IsStaticallyBlocked(cell))
                    return false;

                if (blockedCells != null && blockedCells.Contains(cell))
                    return false;

                if (occupancy.IsDynamicallyBlocked(cell, player, true, true))
                    return false;
            }
            else
            {
                if (!BotGridUtility.IsWalkable(cell, mapContext))
                    return false;

                if (blockedCells != null && blockedCells.Contains(cell))
                    return false;
            }

            return true;
        }

        private static bool CanReachCellBeforeDanger(
            Vector3Int cell,
            float arrivalTime,
            Dictionary<Vector3Int, float> dangerDeadlines)
        {
            if (dangerDeadlines == null || !dangerDeadlines.TryGetValue(cell, out float deadline))
                return true;

            if (deadline <= 0f)
                return false;

            return arrivalTime < deadline - SafetyBuffer;
        }

        private static List<Vector3Int> ReconstructPath(
            Dictionary<Vector3Int, Vector3Int> cameFrom,
            Vector3Int start,
            Vector3Int target)
        {
            List<Vector3Int> path = new();
            Vector3Int current = target;
            path.Add(current);

            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static float EstimateTravelTime(List<Vector3Int> path, PlayerController player, BotNavigator navigator)
        {
            if (path == null || path.Count <= 1 || player == null)
                return float.MaxValue;

            int steps = path.Count - 1;

            return steps * EstimateSingleStepTravelTime(player, navigator);
        }

        private static float EstimateSingleStepTravelTime(PlayerController player, BotNavigator navigator)
        {
            float speed = Mathf.Max(0.1f, player.MoveSpeedStat);
            float cellDistance = GetCellTravelDistance(navigator);
            return (cellDistance / speed) + PerStepTurnPadding;
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
