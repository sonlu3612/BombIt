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
            blackboard.LastStateName = Name;

            List<Vector3Int> pool = new();
            foreach (Vector3Int cell in sense.SafeCells)
            {
                if (cell != sense.CurrentCell)
                    pool.Add(cell);
            }

            Shuffle(pool);

            if (pool.Count > config.wanderMaxSearchCells)
                pool.RemoveRange(config.wanderMaxSearchCells, pool.Count - config.wanderMaxSearchCells);

            List<Vector3Int> path = navigator.FindShortestPathToAny(
                sense.CurrentCell,
                pool,
                config.avoidDangerCells ? sense.DangerCells : null,
                sense.BlockedCells);

            if (path == null || path.Count == 0)
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

            BotActionExecutor.PathFollowResult followResult = executor.FollowPath(blackboard);
            if (followResult != BotActionExecutor.PathFollowResult.InProgress)
            {
                executor.Stop();
                finished = true;
            }
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
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
