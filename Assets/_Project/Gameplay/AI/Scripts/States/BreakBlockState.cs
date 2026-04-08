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
            ClearPreparedPlan();

            if (sense.IsInDanger
                || executor.Player == null
                || !executor.Player.CanPlaceBomb
                || sense.BreakableBlocks.Count == 0
                || Random.value > config.breakBlockChance)
            {
                return false;
            }

            if (!TryBuildPlan(sense, out preparedBlockCell, out preparedBombCell, out preparedApproachPath, out preparedEscapePath))
                return false;

            hasPreparedPlan = true;
            return true;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
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
            else if (!TryBuildPlan(sense, out targetBlockCell, out bombCell, out approachPath, out plannedEscapePath))
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

            bool arrived = executor.FollowPath(blackboard);
            if (!arrived)
                return;

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

            if (executor.TryPlaceBomb())
            {
                blackboard.LastBombTime = Time.time;
                blackboard.PlannedBombCell = bombCell;
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
            ClearPreparedPlan();
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
            return BotBombEscapeUtility.TryFindEscapePath(navigator, executor.Player, sense, candidateBombCell, out escapePath);
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

