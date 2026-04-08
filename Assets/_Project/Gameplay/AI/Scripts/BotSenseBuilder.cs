using System.Collections.Generic;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Item.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotSenseBuilder
    {
        [System.Obsolete]
        public static BotSenseContext Build(PlayerController self, MapContext mapContext, BotConfig config)
        {
            BotSenseContext sense = new();

            if (self == null || mapContext == null)
                return sense;

            sense.CurrentCell = self.GetCurrentCell();

            FindEnemies(self, sense);
            FindBombsAndDanger(sense, mapContext);
            BuildReachableCells(self, sense, mapContext, config);
            FindItems(sense, mapContext);
            BuildSafeCells(sense);

            return sense;
        }

        private static void BuildReachableCells(PlayerController self, BotSenseContext sense, MapContext mapContext, BotConfig config)
        {
            HashSet<Vector3Int> visited = new();
            Queue<(Vector3Int cell, int dist)> queue = new();

            queue.Enqueue((sense.CurrentCell, 0));
            visited.Add(sense.CurrentCell);

            while (queue.Count > 0)
            {
                (Vector3Int cell, int dist) = queue.Dequeue();

                if (dist > config.findRange)
                    continue;

                if (IsTraversable(self, cell, sense, mapContext))
                    sense.FreeCells.Add(cell);

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int near = cell + dir;
                    if (BotGridUtility.HasBlock(near, mapContext) && !sense.BreakableBlocks.Contains(near))
                        sense.BreakableBlocks.Add(near);
                }

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int next = cell + dir;
                    if (visited.Contains(next))
                        continue;

                    if (!IsTraversable(self, next, sense, mapContext))
                        continue;

                    visited.Add(next);
                    queue.Enqueue((next, dist + 1));
                }
            }

            if (!sense.FreeCells.Contains(sense.CurrentCell))
                sense.FreeCells.Add(sense.CurrentCell);
        }

        [System.Obsolete]
        private static void FindEnemies(PlayerController self, BotSenseContext sense)
        {
            PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
            foreach (PlayerController player in players)
            {
                if (player == null || player == self)
                    continue;

                Vector3Int enemyCell = player.GetCurrentCell();
                sense.EnemyPlayers.Add(player);
                sense.EnemyCells.Add(enemyCell);

                if (enemyCell != sense.CurrentCell)
                    sense.BlockedCells.Add(enemyCell);
            }
        }

        [System.Obsolete]
        private static void FindItems(BotSenseContext sense, MapContext mapContext)
        {
            ItemPickup[] items = Object.FindObjectsOfType<ItemPickup>();
            foreach (ItemPickup item in items)
            {
                if (item == null)
                    continue;

                Vector3Int cell = mapContext.ReferenceTilemap != null
                    ? mapContext.ReferenceTilemap.WorldToCell(item.transform.position)
                    : new Vector3Int(
                        Mathf.RoundToInt(item.transform.position.x),
                        Mathf.RoundToInt(item.transform.position.y),
                        0);

                if (!sense.ItemCells.Contains(cell))
                    sense.ItemCells.Add(cell);
            }
        }

        [System.Obsolete]
        private static void FindBombsAndDanger(BotSenseContext sense, MapContext mapContext)
        {
            BombController[] bombs = Object.FindObjectsOfType<BombController>();
            foreach (BombController bomb in bombs)
            {
                if (bomb == null)
                    continue;

                sense.ActiveBombs.Add(bomb);

                if (bomb.CurrentCell != sense.CurrentCell)
                    sense.BlockedCells.Add(bomb.CurrentCell);

                HashSet<Vector3Int> blastCells = BotGridUtility.GetBlastCells(
                    bomb.CurrentCell,
                    bomb.Range,
                    mapContext);

                foreach (Vector3Int cell in blastCells)
                {
                    sense.DangerCells.Add(cell);

                    if (!sense.DangerTimes.ContainsKey(cell))
                        sense.DangerTimes[cell] = bomb.RemainingTime;
                    else
                        sense.DangerTimes[cell] = Mathf.Min(sense.DangerTimes[cell], bomb.RemainingTime);
                }
            }
        }

        private static void BuildSafeCells(BotSenseContext sense)
        {
            sense.SafeCells.Clear();

            foreach (Vector3Int cell in sense.FreeCells)
            {
                if (!sense.DangerCells.Contains(cell))
                    sense.SafeCells.Add(cell);
            }
        }

        private static bool IsTraversable(PlayerController self, Vector3Int cell, BotSenseContext sense, MapContext mapContext)
        {
            GridOccupancyService occupancy = mapContext != null ? mapContext.GridOccupancyService : null;

            if (occupancy != null)
            {
                if (occupancy.IsStaticallyBlocked(cell))
                    return false;

                if (cell != sense.CurrentCell && occupancy.IsDynamicallyBlocked(cell, self, true, true))
                    return false;

                return true;
            }

            if (!BotGridUtility.IsWalkable(cell, mapContext))
                return false;

            if (cell != sense.CurrentCell && sense.BlockedCells.Contains(cell))
                return false;

            return true;
        }

    }
}
