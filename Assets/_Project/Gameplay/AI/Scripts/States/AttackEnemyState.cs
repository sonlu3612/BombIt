using System.Collections.Generic;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class AttackEnemyState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;

        private PlayerController targetEnemy;
        private Vector3Int attackCell;
        private List<Vector3Int> plannedEscapePath;
        private bool finished;

        private bool hasPreparedPlan;
        private PlayerController preparedEnemy;
        private Vector3Int preparedAttackCell;
        private List<Vector3Int> preparedApproachPath;
        private List<Vector3Int> preparedEscapePath;

        public string Name => "AttackEnemy";
        public bool IsFinished => finished;

        public AttackEnemyState(
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
                || sense.EnemyPlayers.Count == 0
                || Random.value > config.attackChance)
            {
                return false;
            }

            if (!TryBuildAttackPlan(sense, out preparedEnemy, out preparedAttackCell, out preparedApproachPath, out preparedEscapePath))
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
                targetEnemy = preparedEnemy;
                attackCell = preparedAttackCell;
                approachPath = preparedApproachPath;
                plannedEscapePath = preparedEscapePath;
                ClearPreparedPlan();
            }
            else if (!TryBuildAttackPlan(sense, out targetEnemy, out attackCell, out approachPath, out plannedEscapePath))
            {
                executor.Stop();
                finished = true;
                return;
            }

            blackboard.TargetEnemy = targetEnemy;
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

            if (targetEnemy == null)
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

            if (!executor.Player.IsNavigationSettled)
            {
                executor.Stop();
                return;
            }

            Vector3Int enemyCell = targetEnemy.GetCurrentCell();
            Vector3Int selfCell = executor.Player.GetCurrentCell();

            if (!BotGridUtility.CanBlastHitTarget(
                    selfCell,
                    enemyCell,
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
            blackboard.TargetEnemy = null;
            blackboard.ClearPath();
            ClearPreparedPlan();
        }

        private bool TryBuildAttackPlan(
            BotSenseContext sense,
            out PlayerController bestEnemy,
            out Vector3Int bestAttackCell,
            out List<Vector3Int> bestApproachPath,
            out List<Vector3Int> bestEscapePath)
        {
            bestEnemy = null;
            bestAttackCell = default;
            bestApproachPath = null;
            bestEscapePath = null;

            int bestScore = int.MaxValue;

            foreach (PlayerController enemy in sense.EnemyPlayers)
            {
                if (enemy == null)
                    continue;

                Vector3Int enemyCell = enemy.GetCurrentCell();

                if (Mathf.Abs(enemyCell.x - sense.CurrentCell.x) + Mathf.Abs(enemyCell.y - sense.CurrentCell.y) > config.attackSearchRange)
                    continue;

                foreach (Vector3Int dir in BotGridUtility.CardinalDirections)
                {
                    Vector3Int candidateAttackCell = enemyCell + dir;
                    if (!BotGridUtility.IsWalkable(candidateAttackCell, navigator.MapContext))
                        continue;

                    List<Vector3Int> approachPath = navigator.FindPath(
                        sense.CurrentCell,
                        candidateAttackCell,
                        config.avoidDangerCells ? sense.DangerCells : null,
                        sense.BlockedCells,
                        executor.Player);

                    if (approachPath == null || approachPath.Count == 0)
                        continue;

                    if (!BotGridUtility.CanBlastHitTarget(
                            candidateAttackCell,
                            enemyCell,
                            executor.Player.BombRangeStat,
                            navigator.MapContext))
                        continue;

                    if (!TryBuildEscapePath(candidateAttackCell, sense, out List<Vector3Int> escapePath))
                        continue;

                    int score = approachPath.Count + escapePath.Count;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestEnemy = enemy;
                        bestAttackCell = candidateAttackCell;
                        bestApproachPath = approachPath;
                        bestEscapePath = escapePath;
                    }
                }
            }

            return bestEnemy != null && bestApproachPath != null;
        }

        private bool TryBuildEscapePath(Vector3Int candidateBombCell, BotSenseContext sense, out List<Vector3Int> escapePath)
        {
            escapePath = null;
            return BotBombEscapeUtility.TryFindEscapePath(navigator, executor.Player, sense, candidateBombCell, out escapePath);
        }

        private void ClearPreparedPlan()
        {
            hasPreparedPlan = false;
            preparedEnemy = null;
            preparedAttackCell = default;
            preparedApproachPath = null;
            preparedEscapePath = null;
        }
    }
}

