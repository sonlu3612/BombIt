using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class BreakBlockState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;
        private const float FailedCandidateCooldown = 1f;

        private Vector3Int targetBlockCell;
        private Vector3Int bombCell;
        private List<Vector3Int> plannedEscapePath;

        private bool finished;
        private int arrivalFrameCount;

        private bool hasPreparedPlan;
        private Vector3Int preparedBlockCell;
        private Vector3Int preparedBombCell;
        private List<Vector3Int> preparedApproachPath;
        private List<Vector3Int> preparedEscapePath;

        public string Name => "BreakBlock";
        public bool IsFinished => finished;

        public BreakBlockState(
            BotBlackboard blackboard,
            BotNavigator navigator,
            BotActionExecutor executor,
            BotConfig config)
        {
            this.blackboard = blackboard;
            this.navigator = navigator;
            this.executor = executor;
            this.config = config;
        }

        public bool CanEnter(BotSenseContext sense)
        {
            // Nếu đang ở state này và chưa xong thì giữ nguyên plan, không replan liên tục
            if (blackboard.LastStateName == Name && !finished)
                return true;

            ClearPreparedPlan();

            if (sense == null
                || sense.IsInDanger
                || executor.Player == null
                || !executor.Player.CanPlaceBomb
                || Time.time < blackboard.LastBombTime + config.bombCooldown
                || sense.BreakableBlocks == null
                || sense.BreakableBlocks.Count == 0)
            {
                return false;
            }

            bool ok = TryBuildApproachPlan(
                sense,
                out preparedBlockCell,
                out preparedBombCell,
                out preparedApproachPath,
                out preparedEscapePath);

            // Debug.Log($"[BB] CanEnter BreakBlock ok={ok} current={sense.CurrentCell} breakable={sense.BreakableBlocks.Count}");
            hasPreparedPlan = ok;
            return ok;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            arrivalFrameCount = 0;
            blackboard.LastStateName = Name;

            List<Vector3Int> approachPath;

            if (hasPreparedPlan)
            {
                targetBlockCell = preparedBlockCell;
                bombCell = preparedBombCell;
                approachPath = preparedApproachPath;
                plannedEscapePath = preparedEscapePath;
                ClearPreparedPlan();
            }
            else if (!TryBuildApproachPlan(
                         sense,
                         out targetBlockCell,
                         out bombCell,
                         out approachPath,
                         out plannedEscapePath))
            {
                executor.Stop();
                finished = true;
                return;
            }

            blackboard.SetPath(approachPath);
        }

        public void Tick(BotSenseContext sense)
        {
            if (finished)
                return;

            if (sense == null || sense.IsInDanger)
            {
                executor.Stop();
                finished = true;
                arrivalFrameCount = 0;
                return;
            }

            if (!sense.BreakableBlocks.Contains(targetBlockCell))
            {
                executor.Stop();
                finished = true;
                arrivalFrameCount = 0;
                return;
            }

            bool arrived = executor.FollowPath(blackboard);
            if (!arrived)
            {
                arrivalFrameCount = 0;
                return;
            }

            // Đã tới nơi, chờ thêm 1 frame để nav settle hẳn
            arrivalFrameCount++;
            if (arrivalFrameCount == 1)
                return;

            // Safety: path may report "arrived" slightly before the bot truly settles
            // on the intended bomb cell. Give it one extra frame, then fail fast so
            // the state machine can rebuild a fresh plan instead of waiting forever.
            if (sense.CurrentCell != bombCell)
            {
                // Debug.Log($"[BB] Abort plan: arrived by path but not on bombCell. current={sense.CurrentCell} bombCell={bombCell} targetBlock={targetBlockCell} arrivalFrames={arrivalFrameCount}");

                blackboard.LastFailedBombCell = bombCell;
                blackboard.LastFailedBombCellTime = Time.time;
                blackboard.LastFailedBlockCell = targetBlockCell;
                blackboard.LastFailedBlockCellTime = Time.time;

                executor.Stop();
                finished = true;
                arrivalFrameCount = 0;
                return;
            }

            if (!executor.Player.CanPlaceBomb)
            {
                executor.Stop();
                finished = true;
                arrivalFrameCount = 0;
                return;
            }

            if (Time.time < blackboard.LastBombTime + config.bombCooldown)
                return;

            if (!executor.Player.IsNavigationSettled)
                return;

            // Re-check escape ngay trước lúc đặt bom
            bool hasEscape = plannedEscapePath != null && plannedEscapePath.Count > 1;
            if (!hasEscape)
            {
                hasEscape = TryBuildEscapePath(bombCell, sense, out plannedEscapePath);
            }

            // Debug.Log($"[BB] EscapeCheck bombCell={bombCell} hasEscape={hasEscape} pathCount={(plannedEscapePath != null ? plannedEscapePath.Count : -1)}");

            if (!hasEscape || plannedEscapePath == null || plannedEscapePath.Count <= 1)
            {
                // Debug.Log($"[BB] Skip bomb: no valid escape for bombCell={bombCell}");

                blackboard.LastFailedBombCell = bombCell;
                blackboard.LastFailedBombCellTime = Time.time;
                blackboard.LastFailedBlockCell = targetBlockCell;
                blackboard.LastFailedBlockCellTime = Time.time;

                executor.Stop();
                finished = true;
                arrivalFrameCount = 0;
                return;
            }

            bool placed = executor.TryPlaceBomb();
            // Debug.Log($"[BB] TryPlaceBomb result={placed} canPlace={executor.Player.CanPlaceBomb} navSettled={executor.Player.IsNavigationSettled}");

            if (placed)
            {
                blackboard.LastBombTime = Time.time;
                blackboard.PlannedBombCell = bombCell;
                blackboard.EscapePath = new List<Vector3Int>(plannedEscapePath);
                blackboard.EscapeCell = plannedEscapePath[plannedEscapePath.Count - 1];
            }
            else
            {
                // Debug.Log($"[BB] Bomb placement failed at bombCell={bombCell}");

                blackboard.LastFailedBombCell = bombCell;
                blackboard.LastFailedBombCellTime = Time.time;
            }

            executor.Stop();
            finished = true;
            arrivalFrameCount = 0;
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
            ClearPreparedPlan();
            plannedEscapePath = null;
            arrivalFrameCount = 0;
        }

        private bool TryBuildApproachPlan(
    BotSenseContext sense,
    out Vector3Int bestBlockCell,
    out Vector3Int bestBombCell,
    out List<Vector3Int> bestApproachPath,
    out List<Vector3Int> bestEscapePath)
        {
            bestBlockCell = default;
            bestBombCell = default;
            bestApproachPath = null;
            bestEscapePath = null;

            int bestScore = int.MaxValue;

            foreach (Vector3Int blockCell in sense.BreakableBlocks)
            {
                if (WasRecentlyRejectedBlock(blockCell))
                    continue;

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int candidateBombCell = blockCell + dir;
                    // Debug.Log($"[PLAN] check block={blockCell} bombCell={candidateBombCell}");

                    if (WasRecentlyRejectedBombCell(candidateBombCell))
                    {
                        // Debug.Log($"[PLAN] reject recent failed bombCell={candidateBombCell}");
                        continue;
                    }

                    if (!BotGridUtility.IsWalkable(candidateBombCell, navigator.MapContext))
                    {
                        // Debug.Log($"[PLAN] reject not walkable bombCell={candidateBombCell}");
                        continue;
                    }

                    List<Vector3Int> approachPath = navigator.FindPath(
                        sense.CurrentCell,
                        candidateBombCell,
                        config.avoidDangerCells ? sense.DangerCells : null,
                        sense.BlockedCells,
                        executor.Player);

                    if (approachPath == null || approachPath.Count == 0)
                    {
                        // Debug.Log($"[PLAN] reject no path bombCell={candidateBombCell}");
                        continue;
                    }

                    if (!TryBuildEscapePath(candidateBombCell, sense, out List<Vector3Int> escapePath))
                    {
                        // Debug.Log($"[PLAN] reject no escape bombCell={candidateBombCell}");
                        continue;
                    }

                    // Debug.Log($"[PLAN] accept path bombCell={candidateBombCell} len={approachPath.Count}");

                    int score = approachPath.Count + escapePath.Count;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestBlockCell = blockCell;
                        bestBombCell = candidateBombCell;
                        bestApproachPath = approachPath;
                        bestEscapePath = escapePath;
                    }
                }
            }

            bool ok = bestApproachPath != null;
            // Debug.Log($"[PLAN] final ok={ok} bestBomb={bestBombCell} bestBlock={bestBlockCell} bestLen={(bestApproachPath != null ? bestApproachPath.Count : -1)}");
            return ok;
        }

        private bool HasImmediateEscapeNeighbor(Vector3Int bombCell, BotSenseContext sense)
        {
            HashSet<Vector3Int> futureBlast = BotGridUtility.GetBlastCells(
                bombCell,
                executor.Player.BombRangeStat,
                navigator.MapContext);

            foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
            {
                Vector3Int neighbor = bombCell + dir;

                if (futureBlast.Contains(neighbor))
                    continue;

                if (sense.DangerCells.Contains(neighbor))
                    continue;

                if (!BotGridUtility.IsWalkable(neighbor, navigator.MapContext))
                    continue;

                return true;
            }

            return false;
        }

        private bool TryBuildEscapePath(
            Vector3Int candidateBombCell,
            BotSenseContext sense,
            out List<Vector3Int> escapePath)
        {
            // Debug.Log($"[BB] Arrived bombCell={candidateBombCell} targetBlock={targetBlockCell} settled={executor.Player.GetLogicCell()} canPlace={executor.Player.CanPlaceBomb} navSettled={executor.Player.IsNavigationSettled}");
            escapePath = null;

            bool found = BotBombEscapeUtility.TryFindEscapePath(
                navigator,
                executor.Player,
                sense,
                candidateBombCell,
                out escapePath);

            if (found && escapePath != null && escapePath.Count > 1)
                return true;

            // Fallback: allow a simple 1-step escape if there is a safe neighbor.
            HashSet<Vector3Int> futureBlast = BotGridUtility.GetBlastCells(
                candidateBombCell,
                executor.Player.BombRangeStat,
                navigator.MapContext);

            foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
            {
                Vector3Int neighbor = candidateBombCell + dir;

                if (futureBlast.Contains(neighbor))
                    continue;

                if (sense.DangerCells.Contains(neighbor))
                    continue;

                if (!BotGridUtility.IsWalkable(neighbor, navigator.MapContext))
                    continue;

                if (sense.BlockedCells.Contains(neighbor))
                    continue;

                escapePath = new List<Vector3Int> { candidateBombCell, neighbor };
                return true;
            }

            return false;
        }

        private bool WasRecentlyRejectedBombCell(Vector3Int candidateBombCell)
        {
            return blackboard.LastFailedBombCell.HasValue
                && blackboard.LastFailedBombCell.Value == candidateBombCell
                && Time.time - blackboard.LastFailedBombCellTime < FailedCandidateCooldown;
        }

        private bool WasRecentlyRejectedBlock(Vector3Int blockCell)
        {
            return blackboard.LastFailedBlockCell.HasValue
                && blackboard.LastFailedBlockCell.Value == blockCell
                && Time.time - blackboard.LastFailedBlockCellTime < FailedCandidateCooldown;
        }

        private void ClearPreparedPlan()
        {
            hasPreparedPlan = false;
            preparedBlockCell = default;
            preparedBombCell = default;
            preparedApproachPath = null;
            preparedEscapePath = null;
        }
    }
}
