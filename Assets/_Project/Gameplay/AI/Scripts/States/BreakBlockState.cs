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

        private Vector3Int targetBlockCell;
        private Vector3Int bombCell;
        private List<Vector3Int> plannedEscapePath;
        private bool finished;

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
            return !sense.IsInDanger
                   && executor.Player != null
                   && executor.Player.CanPlaceBomb
                   && sense.BreakableBlocks.Count > 0
                   && Random.value <= config.breakBlockChance;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;

            if (!TryBuildPlan(sense, out targetBlockCell, out bombCell, out List<Vector3Int> approachPath, out plannedEscapePath))
            {
                executor.Stop();
                finished = true;
                return;
            }

            blackboard.SetPath(approachPath);
        }

        public void Tick(BotSenseContext sense)
        {
            if (finished || sense.IsInDanger)
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (!sense.BreakableBlocks.Contains(targetBlockCell))
            {
                executor.Stop();
                finished = true;
                return;
            }

            BotActionExecutor.PathFollowResult followResult = executor.FollowPath(blackboard);
            if (followResult == BotActionExecutor.PathFollowResult.InProgress)
                return;

            if (followResult != BotActionExecutor.PathFollowResult.Arrived)
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (!executor.Player.CanPlaceBomb)
            {
                finished = true;
                return;
            }

            if (Time.time < blackboard.LastBombTime + config.bombCooldown)
            {
                executor.Stop();
                return;
            }

            Vector3Int selfCell = executor.Player.GetCurrentCell();
            if (selfCell != bombCell
                || !BotGridUtility.CanBlastHitTarget(
                    selfCell,
                    targetBlockCell,
                    executor.Player.BombRangeStat,
                    navigator.MapContext))
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (executor.TryPlaceBomb())
            {
                blackboard.LastBombTime = Time.time;
                blackboard.PlannedBombCell = selfCell;
                blackboard.EscapePath = plannedEscapePath;
                blackboard.EscapeCell = plannedEscapePath != null && plannedEscapePath.Count > 0
                    ? plannedEscapePath[plannedEscapePath.Count - 1]
                    : (Vector3Int?)null;
            }

            executor.Stop();
            finished = true;
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
        }

        private bool TryBuildPlan(
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
                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int candidateBombCell = blockCell + dir;
                    if (!BotGridUtility.IsWalkable(candidateBombCell, navigator.MapContext))
                        continue;

                    List<Vector3Int> approachPath = navigator.FindPath(
                        sense.CurrentCell,
                        candidateBombCell,
                        config.avoidDangerCells ? sense.DangerCells : null,
                        sense.BlockedCells);

                    if (approachPath == null || approachPath.Count == 0)
                        continue;

                    if (!TryBuildEscapePath(candidateBombCell, sense, out List<Vector3Int> escapePath))
                        continue;

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

            return bestApproachPath != null;
        }

        private bool TryBuildEscapePath(Vector3Int candidateBombCell, BotSenseContext sense, out List<Vector3Int> escapePath)
        {
            escapePath = null;

            return BotBombEscapeUtility.TryFindEscapePath(
                navigator,
                executor.Player,
                sense,
                candidateBombCell,
                out escapePath)
                && escapePath != null
                && escapePath.Count > 1;
        }
    }
}
