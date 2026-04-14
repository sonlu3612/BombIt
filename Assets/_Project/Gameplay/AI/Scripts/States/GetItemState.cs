using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class GetItemState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;
        private readonly BotConfig config;

        private Vector3Int targetItemCell;
        private bool finished;

        public string Name => "GetItem";
        public bool IsFinished => finished;

        public GetItemState(
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
                   && sense.ItemCells.Count > 0
                   && Random.value <= config.itemChance;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;

            List<Vector3Int> candidates = new(sense.ItemCells);
            List<Vector3Int> path = navigator.FindShortestPathToAny(
                sense.CurrentCell,
                candidates,
                config.avoidDangerCells ? sense.DangerCells : null,
                sense.BlockedCells,
                executor.Player);

            if (path == null || path.Count == 0)
            {
                executor.Stop();
                finished = true;
                return;
            }

            targetItemCell = path[path.Count - 1];
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

            if (!sense.ItemCells.Contains(targetItemCell))
            {
                executor.Stop();
                finished = true;
                return;
            }

            bool done = executor.FollowPath(blackboard);
            if (done)
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
    }
}
