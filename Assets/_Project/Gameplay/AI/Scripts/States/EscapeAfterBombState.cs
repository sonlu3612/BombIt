using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts.States
{
    public class EscapeAfterBombState : IBotState
    {
        private readonly BotBlackboard blackboard;
        private readonly BotNavigator navigator;
        private readonly BotActionExecutor executor;

        private bool finished;

        public string Name => "EscapeAfterBomb";
        public bool IsFinished => finished;

        public EscapeAfterBombState(
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
            return blackboard.EscapeCell.HasValue || (blackboard.EscapePath != null && blackboard.EscapePath.Count > 0);
        }

        public void Enter(BotSenseContext sense)
        {
            finished = false;
            blackboard.LastStateName = Name;

            if (blackboard.EscapePath != null && blackboard.EscapePath.Count > 0)
            {
                blackboard.SetPath(new List<Vector3Int>(blackboard.EscapePath));
                return;
            }

            if (!blackboard.EscapeCell.HasValue)
            {
                finished = true;
                return;
            }

            List<Vector3Int> path = navigator.FindPath(
                sense.CurrentCell,
                blackboard.EscapeCell.Value,
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

            if (!blackboard.EscapeCell.HasValue)
            {
                executor.Stop();
                finished = true;
                return;
            }

            if (!sense.IsInDanger && sense.CurrentCell == blackboard.EscapeCell.Value)
            {
                executor.Stop();
                finished = true;
                return;
            }

            bool done = executor.FollowPath(blackboard);
            if (done)
            {
                // Path following finished (reached destination or got stuck)
                if (sense.IsInDanger)
                {
                    // Still in danger after path finished - try fallback escape
                    // Attempt to find any safe cell we can reach
                    if (TryEscapeToAnySafeCell(sense))
                    {
                        // Continue with fallback path
                        return;
                    }
                }

                executor.Stop();
                finished = true;
            }
        }

        private bool TryEscapeToAnySafeCell(BotSenseContext sense)
        {
            if (sense.SafeCells.Count == 0)
                return false;

            // Find the closest safe cell and try to reach it
            List<Vector3Int> path = navigator.FindShortestPathToAny(
                sense.CurrentCell,
                sense.SafeCells,
                sense.DangerCells,
                sense.BlockedCells,
                executor.Player);

            if (path == null || path.Count <= 1)
                return false;

            blackboard.SetPath(path);
            return true;
        }

        public void Exit()
        {
            executor.Stop();
            blackboard.ClearPath();
            blackboard.ClearEscapePlan();
        }
    }
}
