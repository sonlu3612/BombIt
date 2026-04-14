using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class PlantBombState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;

        private bool finished;
        private bool hasPreparedPlan;
        private List<Vector3Int> preparedEscapePath;
        private Vector3Int? preparedEscapeCell;

        public string Name => "PlantBomb";
        public bool IsFinished => finished;

        public PlantBombState(
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
                || Time.time < blackboard.LastBombTime + config.bombCooldown
                || Random.value > config.plantBombChance)
            {
                return false;
            }

            Vector3Int currentCell = sense.CurrentCell;
            HashSet<Vector3Int> blastCells = BotGridUtility.GetBlastCells(
                currentCell,
                executor.Player.BombRangeStat,
                navigator.MapContext);

            bool canHitEnemy = false;
            foreach (Vector3Int enemyCell in sense.EnemyCells)
            {
                if (blastCells.Contains(enemyCell))
                {
                    canHitEnemy = true;
                    break;
                }
            }

            bool canBreakBlock = false;
            foreach (Vector3Int blockCell in sense.BreakableBlocks)
            {
                if (blastCells.Contains(blockCell))
                {
                    canBreakBlock = true;
                    break;
                }
            }

            if (!canHitEnemy && !canBreakBlock)
                return false;

            if (!TryBuildEscapePlan(currentCell, sense, out preparedEscapePath, out preparedEscapeCell))
                return false;

            hasPreparedPlan = true;
            return true;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;
            executor.Stop();

            if (hasPreparedPlan)
            {
                blackboard.PlannedBombCell = sense.CurrentCell;
                blackboard.EscapePath = preparedEscapePath;
                blackboard.EscapeCell = preparedEscapeCell;
                ClearPreparedPlan();
                return;
            }

            if (!TryBuildEscapePlan(sense.CurrentCell, sense, out List<Vector3Int> escapePath, out Vector3Int? escapeCell))
            {
                finished = true;
                return;
            }

            blackboard.PlannedBombCell = sense.CurrentCell;
            blackboard.EscapePath = escapePath;
            blackboard.EscapeCell = escapeCell;
        }

        public void Tick(BotSenseContext sense)
        {
            if (finished)
                return;

            executor.Stop();

            if (sense.IsInDanger || executor.Player == null || !executor.Player.CanPlaceBomb)
            {
                finished = true;
                return;
            }

            if (Time.time < blackboard.LastBombTime + config.bombCooldown)
                return;

            Vector3Int currentCell = sense.CurrentCell;
            if (!TryBuildEscapePlan(currentCell, sense, out List<Vector3Int> escapePath, out Vector3Int? escapeCell))
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (executor.TryPlaceBomb())
            {
                blackboard.LastBombTime = Time.time;
                blackboard.PlannedBombCell = currentCell;
                blackboard.EscapePath = escapePath;
                blackboard.EscapeCell = escapeCell;
            }

            finished = true;
        }

        public void Exit()
        {
            executor.Stop();
            ClearPreparedPlan();
        }

        private bool TryBuildEscapePlan(
            Vector3Int bombCell,
            BotSenseContext sense,
            out List<Vector3Int> escapePath,
            out Vector3Int? escapeCell)
        {
            escapePath = null;
            escapeCell = null;

            if (!BotBombEscapeUtility.TryFindEscapePath(navigator, executor.Player, sense, bombCell, out escapePath))
                return false;

            escapeCell = escapePath[escapePath.Count - 1];
            return true;
        }

        private void ClearPreparedPlan()
        {
            hasPreparedPlan = false;
            preparedEscapePath = null;
            preparedEscapeCell = null;
        }
    }
}

