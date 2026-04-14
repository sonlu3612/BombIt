using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class EvadeBombState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;

        private bool finished;

        public string Name => "EvadeBomb";
        public bool IsFinished => finished;

        public EvadeBombState(
            BotBlackboard blackboard,
            BotNavigator navigator,
            BotActionExecutor executor)
        {
            this.blackboard = blackboard;
            this.navigator = navigator;
            this.executor = executor;
        }

        public bool CanEnter(BotSenseContext sense)
        {
            return sense.IsInDanger;
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;

            List<Vector3Int> candidates = new();
            foreach (Vector3Int cell in sense.SafeCells)
            {
                if (cell != sense.CurrentCell)
                    candidates.Add(cell);
            }

            List<Vector3Int> path = navigator.FindShortestPathToAny(
                sense.CurrentCell,
                candidates,
                sense.DangerCells,
                sense.BlockedCells,
                executor.Player);

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
            if (finished)
            {
                executor.Stop();
                return;
            }

            if (!sense.IsInDanger && (blackboard.CurrentTargetCell == null || sense.CurrentCell == blackboard.CurrentTargetCell.Value))
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (blackboard.CurrentPath == null)
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
