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

        private int arrivalFrameCount = 0;

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
                || sense.BreakableBlocks.Count == 0)
            {
                return false;
            }

            // Try to build approach path to any breakable block position
            if (!TryBuildApproachPlan(sense, out preparedBlockCell, out preparedBombCell, out preparedApproachPath))
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
                ClearPreparedPlan();
            }
            else if (!TryBuildApproachPlan(sense, out targetBlockCell, out bombCell, out approachPath))
            {
                executor.Stop();
                finished = true;
                return;
            }

            // Try to build escape path from bomb location
            plannedEscapePath = null;
            TryBuildEscapePath(bombCell, sense, out plannedEscapePath);

            blackboard.SetPath(approachPath);
        }

        public void Tick(BotSenseContext sense)
        {
            if (finished || sense.IsInDanger)
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

            // Arrived at target - wait for movement system to settle
            arrivalFrameCount++;
            if (arrivalFrameCount == 1)
            {
                // First frame after arrival - ensure movement stops and settles
                executor.Stop();
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
            {
                executor.Stop();
                return;
            }

            if (!executor.Player.IsNavigationSettled)
            {
                executor.Stop();
                return;
            }

            if (executor.TryPlaceBomb())
            {
                blackboard.LastBombTime = Time.time;
                blackboard.PlannedBombCell = bombCell;
                blackboard.EscapePath = plannedEscapePath;
                
                // Set escape cell: prefer planned path end, otherwise use any safe cell
                if (plannedEscapePath != null && plannedEscapePath.Count > 0)
                {
                    blackboard.EscapeCell = plannedEscapePath[plannedEscapePath.Count - 1];
                }
                else if (sense.SafeCells.Count > 0)
                {
                    // Fallback: pick any safe cell for emergency escape
                    blackboard.EscapeCell = sense.SafeCells[0];
                }
                else
                {
                    blackboard.EscapeCell = null;
                }
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
            arrivalFrameCount = 0;
        }

        private bool TryBuildApproachPlan(
            BotSenseContext sense,
            out Vector3Int bestBlockCell,
            out Vector3Int bestBombCell,
            out List<Vector3Int> bestApproachPath)
        {
            bestBlockCell = default;
            bestBombCell = default;
            bestApproachPath = null;

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
                        sense.BlockedCells,
                        executor.Player);

                    if (approachPath == null || approachPath.Count == 0)
                        continue;

                    int score = approachPath.Count;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestBlockCell = blockCell;
                        bestBombCell = candidateBombCell;
                        bestApproachPath = approachPath;
                    }
                }
            }

            return bestApproachPath != null;
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
                        sense.BlockedCells,
                        executor.Player);

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

