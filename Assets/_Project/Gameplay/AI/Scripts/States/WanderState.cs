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
            return !sense.IsInDanger && sense.FreeCells.Count > 1;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;

            // Pick random cell from reachable cells (excluding current cell)
            List<Vector3Int> reachableCells = new(sense.FreeCells);
            reachableCells.Remove(sense.CurrentCell);

            if (reachableCells.Count == 0)
            {
                executor.Stop();
                finished = true;
                return;
            }

            Vector3Int targetCell = reachableCells[Random.Range(0, reachableCells.Count)];

            // Find path to target
            List<Vector3Int> path = navigator.FindPath(
                sense.CurrentCell,
                targetCell,
                config.avoidDangerCells ? sense.DangerCells : null,
                sense.BlockedCells,
                executor.Player);

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
                finished = true;
            }
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
        }
    }
}
