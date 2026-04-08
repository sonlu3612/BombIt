using System.Collections.Generic;
using _Project.Gameplay.Map.Scripts;
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
            HashSet<Vector3Int> blockedCells = null)
        {
            BeginDebugSearch(start, target);

            if (start == target)
            {
                LastPath = new List<Vector3Int> { start };
                return LastPath;
            }

            Queue<Vector3Int> queue = new();
            Dictionary<Vector3Int, Vector3Int> cameFrom = new();
            HashSet<Vector3Int> visited = new();

            queue.Enqueue(start);
            visited.Add(start);
            LastVisited.Add(start);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int next = current + dir;

                    if (visited.Contains(next))
                        continue;

                    GridOccupancyService occupancy = mapContext != null ? mapContext.GridOccupancyService : null;


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

                        if (occupancy.IsDynamicallyBlocked(next, null, true, true))
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

                    visited.Add(next);
                    LastVisited.Add(next);
                    cameFrom[next] = current;

                    if (next == target)
                    {
                        LastPath = ReconstructPath(cameFrom, start, target);
                        return LastPath;
                    }

                    queue.Enqueue(next);
                }
            }

            LastPath = null;
            return null;
        }

        public List<Vector3Int> FindShortestPathToAny(
            Vector3Int start,
            List<Vector3Int> candidates,
            HashSet<Vector3Int> dangerCells = null,
            HashSet<Vector3Int> blockedCells = null)
        {
            List<Vector3Int> bestPath = null;

            foreach (Vector3Int candidate in candidates)
            {
                List<Vector3Int> path = FindPath(start, candidate, dangerCells, blockedCells);
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
            HashSet<Vector3Int> blockedCells = null)
        {
            List<Vector3Int> bestPath = FindShortestPathToAny(start, candidates, dangerCells, blockedCells);
            if (bestPath == null || bestPath.Count == 0)
                return null;

            return bestPath[bestPath.Count - 1];
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
