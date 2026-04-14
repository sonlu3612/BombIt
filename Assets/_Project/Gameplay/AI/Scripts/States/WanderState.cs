using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class WanderState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;

        private bool finished;
        private float idleStartTime;
        private const float IdleDurationAtWaypoint = 0.3f;

        public string Name => "Wander";
        public bool IsFinished => finished;

        public WanderState(
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
            return !sense.IsInDanger && sense.SafeCells.Count > 1;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            idleStartTime = 0f;
            blackboard.LastStateName = Name;

            List<Vector3Int> pool = BuildCandidatePool(sense);
            List<Vector3Int> path = ChooseWanderPath(sense, pool);

            if (path == null || path.Count <= 1)
            {
                executor.Stop();
                finished = true;
                return;
            }

            blackboard.SetPath(path);
        }

        public void Tick(BotSenseContext sense)
        {
            if (finished || sense.IsInDanger)
            {
                executor.Stop();
                finished = true;
                return;
            }

            bool done = executor.FollowPath(blackboard);
            if (done)
            {
                // Path reached - idle at waypoint before allowing next state
                if (idleStartTime <= 0f)
                {
                    executor.Stop();
                    idleStartTime = Time.time;
                    return;
                }

                // Check if idle duration has elapsed
                if (Time.time - idleStartTime >= IdleDurationAtWaypoint)
                {
                    finished = true;
                }
            }
            else
            {
                // Still following path - reset idle timer
                idleStartTime = 0f;
            }
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
            idleStartTime = 0f;
        }

        private List<Vector3Int> BuildCandidatePool(BotSenseContext sense)
        {
            List<Vector3Int> pool = new();

            foreach (Vector3Int cell in sense.SafeCells)
            {
                if (cell == sense.CurrentCell)
                    continue;

                pool.Add(cell);
            }

            Shuffle(pool);

            if (pool.Count > config.wanderMaxSearchCells)
                pool.RemoveRange(config.wanderMaxSearchCells, pool.Count - config.wanderMaxSearchCells);

            return pool;
        }

        private List<Vector3Int> ChooseWanderPath(BotSenseContext sense, List<Vector3Int> candidates)
        {
            List<Vector3Int> bestLongPath = null;
            int bestLongScore = int.MinValue;

            List<Vector3Int> bestFallbackPath = null;
            int bestFallbackScore = int.MinValue;

            foreach (Vector3Int candidate in candidates)
            {
                List<Vector3Int> path = navigator.FindPath(
                    sense.CurrentCell,
                    candidate,
                    config.avoidDangerCells ? sense.DangerCells : null,
                    sense.BlockedCells,
                    executor.Player);

                if (path == null || path.Count <= 1)
                    continue;

                int score = ScoreCandidate(candidate, path, sense);

                if (path.Count >= config.wanderMinPathLength && score > bestLongScore)
                {
                    bestLongScore = score;
                    bestLongPath = path;
                }

                if (score > bestFallbackScore)
                {
                    bestFallbackScore = score;
                    bestFallbackPath = path;
                }
            }

            return bestLongPath ?? bestFallbackPath;
        }

        private int ScoreCandidate(Vector3Int candidate, List<Vector3Int> path, BotSenseContext sense)
        {
            int score = path.Count * 10;

            foreach (Vector3Int blockCell in sense.BreakableBlocks)
            {
                if (BotGridUtility.IsAdjacent(candidate, blockCell))
                {
                    score += 25;
                    break;
                }
            }

            foreach (Vector3Int enemyCell in sense.EnemyCells)
            {
                int distance = Mathf.Abs(candidate.x - enemyCell.x) + Mathf.Abs(candidate.y - enemyCell.y);
                if (distance <= executor.Player.BombRangeStat + 1)
                {
                    score += 15;
                    break;
                }
            }

            return score;
        }

        private void Shuffle(List<Vector3Int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
