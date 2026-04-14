using System.Collections.Generic;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotNavigator
    {
        private readonly MapContext mapContext;

        public MapContext MapContext => mapContext;
        public Vector3Int? LastStart { get; private set; }
        public Vector3Int? LastTarget { get; private set; }
        public List<Vector3Int> LastPath { get; private set; }
        public HashSet<Vector3Int> LastVisited { get; } = new();
        public HashSet<Vector3Int> LastRejectedSolid { get; } = new();
        public HashSet<Vector3Int> LastRejectedBlocked { get; } = new();
        public HashSet<Vector3Int> LastRejectedDanger { get; } = new();

        public BotNavigator(MapContext context)
        {
            mapContext = context;
        }

        public List<Vector3Int> FindPath(
            Vector3Int start,
            Vector3Int target,
            HashSet<Vector3Int> dangerCells = null,
            HashSet<Vector3Int> blockedCells = null,
            PlayerController ignorePlayer = null)
        {
            BeginDebugSearch(start, target);

            if (start == target)
            {
                LastPath = new List<Vector3Int> { start };
                BotMovementTraceLog.LogPathSummary(
                    "start-is-target",
                    start,
                    target,
                    mapContext,
                    1,
                    0,
                    0,
                    0,
                    LastPath.Count);
                return LastPath;
            }

            GridOccupancyService occupancy = mapContext != null ? mapContext.GridOccupancyService : null;

            HashSet<Vector3Int> openSet = new() { start };
            HashSet<Vector3Int> closedSet = new();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new();
            Dictionary<Vector3Int, int> gScore = new();
            Dictionary<Vector3Int, int> fScore = new();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, target);

            LastVisited.Add(start);

            while (openSet.Count > 0)
            {
                Vector3Int current = GetLowestFScoreNode(openSet, fScore, gScore);

                if (current == target)
                {
                    LastPath = ReconstructPath(cameFrom, start, target);
                    BotMovementTraceLog.LogPathSummary(
                        "found",
                        start,
                        target,
                        mapContext,
                        LastVisited.Count,
                        LastRejectedSolid.Count,
                        LastRejectedBlocked.Count,
                        LastRejectedDanger.Count,
                        LastPath.Count);
                    return LastPath;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int next = current + dir;

                    if (closedSet.Contains(next))
                        continue;

                    if (!BotGridUtility.IsWithinBounds(next, mapContext))
                    {
                        LastRejectedSolid.Add(next);
                        continue;
                    }

                    if (occupancy != null)
                    {
                        if (occupancy.IsStaticallyBlocked(next))
                        {
                            LastRejectedSolid.Add(next);
                            continue;
                        }

                        if (blockedCells != null && blockedCells.Contains(next))
                        {
                            LastRejectedBlocked.Add(next);
                            continue;
                        }

                        if (occupancy.IsDynamicallyBlocked(next, ignorePlayer, true, true))
                        {
                            LastRejectedBlocked.Add(next);
                            continue;
                        }
                    }
                    else
                    {
                        if (!BotGridUtility.IsWalkable(next, mapContext))
                        {
                            LastRejectedSolid.Add(next);
                            continue;
                        }

                        if (blockedCells != null && blockedCells.Contains(next))
                        {
                            LastRejectedBlocked.Add(next);
                            continue;
                        }
                    }

                    if (dangerCells != null && dangerCells.Contains(next))
                    {
                        LastRejectedDanger.Add(next);
                        continue;
                    }

                    int tentativeGScore = gScore[current] + 1;

                    if (!gScore.TryGetValue(next, out int existingGScore) || tentativeGScore < existingGScore)
                    {
                        cameFrom[next] = current;
                        gScore[next] = tentativeGScore;
                        fScore[next] = tentativeGScore + Heuristic(next, target);

                        if (!openSet.Contains(next))
                        {
                            openSet.Add(next);
                            LastVisited.Add(next);
                        }
                    }
                }
            }

            LastPath = null;
            BotMovementTraceLog.LogPathSummary(
                "failed",
                start,
                target,
                mapContext,
                LastVisited.Count,
                LastRejectedSolid.Count,
                LastRejectedBlocked.Count,
                LastRejectedDanger.Count,
                0);
            return null;
        }

        public List<Vector3Int> FindShortestPathToAny(
            Vector3Int start,
            List<Vector3Int> candidates,
            HashSet<Vector3Int> dangerCells = null,
            HashSet<Vector3Int> blockedCells = null,
            PlayerController ignorePlayer = null)
        {
            List<Vector3Int> bestPath = null;

            foreach (Vector3Int candidate in candidates)
            {
                List<Vector3Int> path = FindPath(start, candidate, dangerCells, blockedCells, ignorePlayer);
                if (path == null)
                    continue;

                if (bestPath == null || path.Count < bestPath.Count)
                    bestPath = path;
            }

            LastPath = bestPath != null ? new List<Vector3Int>(bestPath) : null;
            return bestPath;
        }

        public Vector3Int? FindNearestReachableCell(
            Vector3Int start,
            List<Vector3Int> candidates,
            HashSet<Vector3Int> dangerCells = null,
            HashSet<Vector3Int> blockedCells = null,
            PlayerController ignorePlayer = null)
        {
            List<Vector3Int> bestPath = FindShortestPathToAny(start, candidates, dangerCells, blockedCells, ignorePlayer);
            if (bestPath == null || bestPath.Count == 0)
                return null;

            return bestPath[bestPath.Count - 1];
        }

        private int Heuristic(Vector3Int from, Vector3Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        private Vector3Int GetLowestFScoreNode(
            HashSet<Vector3Int> openSet,
            Dictionary<Vector3Int, int> fScore,
            Dictionary<Vector3Int, int> gScore)
        {
            bool hasBest = false;
            Vector3Int bestNode = default;
            int bestF = int.MaxValue;
            int bestG = int.MaxValue;

            foreach (Vector3Int node in openSet)
            {
                int nodeF = fScore.TryGetValue(node, out int f) ? f : int.MaxValue;
                int nodeG = gScore.TryGetValue(node, out int g) ? g : int.MaxValue;

                if (!hasBest || nodeF < bestF || (nodeF == bestF && nodeG < bestG))
                {
                    hasBest = true;
                    bestNode = node;
                    bestF = nodeF;
                    bestG = nodeG;
                }
            }

            return bestNode;
        }

        private List<Vector3Int> ReconstructPath(
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

        private void BeginDebugSearch(Vector3Int start, Vector3Int target)
        {
            LastStart = start;
            LastTarget = target;
            LastPath = null;
            LastVisited.Clear();
            LastRejectedSolid.Clear();
            LastRejectedBlocked.Clear();
            LastRejectedDanger.Clear();
        }
    }
}