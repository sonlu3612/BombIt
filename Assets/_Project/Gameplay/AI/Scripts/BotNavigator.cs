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

            Dictionary<Vector3Int, Vector3Int> cameFrom = new();
            Dictionary<Vector3Int, int> gScore = new();
            Dictionary<Vector3Int, int> fScore = new();
            HashSet<Vector3Int> openSet = new();
            HashSet<Vector3Int> closedSet = new();

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = GetHeuristic(start, target);
            LastVisited.Add(start);

            while (openSet.Count > 0)
            {
                Vector3Int current = GetBestOpenNode(openSet, fScore, gScore);
                if (current == target)
                {
                    LastPath = ReconstructPath(cameFrom, start, target);
                    return LastPath;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int next = current + dir;

                    if (closedSet.Contains(next))
                        continue;

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

                    if (dangerCells != null && dangerCells.Contains(next))
                    {
                        LastRejectedDanger.Add(next);
                        continue;
                    }

                    int tentativeG = GetScoreOrDefault(gScore, current) + 1;
                    bool hasExistingScore = gScore.TryGetValue(next, out int existingG);
                    if (hasExistingScore && tentativeG >= existingG)
                        continue;

                    LastVisited.Add(next);
                    cameFrom[next] = current;
                    gScore[next] = tentativeG;
                    fScore[next] = tentativeG + GetHeuristic(next, target);
                    openSet.Add(next);

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

        private static int GetHeuristic(Vector3Int from, Vector3Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        private static int GetScoreOrDefault(Dictionary<Vector3Int, int> scores, Vector3Int cell)
        {
            return scores.TryGetValue(cell, out int score) ? score : int.MaxValue;
        }

        private static Vector3Int GetBestOpenNode(
            HashSet<Vector3Int> openSet,
            Dictionary<Vector3Int, int> fScore,
            Dictionary<Vector3Int, int> gScore)
        {
            Vector3Int bestNode = default;
            int bestF = int.MaxValue;
            int bestG = int.MaxValue;
            bool foundAny = false;

            foreach (Vector3Int node in openSet)
            {
                int nodeF = GetScoreOrDefault(fScore, node);
                int nodeG = GetScoreOrDefault(gScore, node);

                if (!foundAny
                    || nodeF < bestF
                    || (nodeF == bestF && nodeG < bestG))
                {
                    bestNode = node;
                    bestF = nodeF;
                    bestG = nodeG;
                    foundAny = true;
                }
            }

            return bestNode;
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
