using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotActionExecutor
    {
        private readonly PlayerController player;
        private readonly Tilemap referenceTilemap;
        private readonly float reachThreshold;
        private const float StuckDistanceThreshold = 0.02f;
        private const float StuckTimeout = 0.4f;

        public PlayerController Player => player;

        public BotActionExecutor(PlayerController player, Tilemap referenceTilemap, float reachThreshold)
        {
            this.player = player;
            this.referenceTilemap = referenceTilemap;
            this.reachThreshold = reachThreshold;
        }

        public void Stop()
        {
            if (player == null)
                return;

            player.SetMoveDirection(Vector2.zero);
        }

        public void MoveTowardsCell(Vector3Int targetCell)
        {
            if (player == null || referenceTilemap == null)
                return;

            Vector3 targetWorld = referenceTilemap.GetCellCenterWorld(targetCell);
            Vector3 currentWorld = player.GetNavigationWorldPosition();
            Vector3Int currentCell = player.GetCurrentCell();
            Vector3 currentCellCenter = referenceTilemap.GetCellCenterWorld(currentCell);
            Vector2 delta = targetWorld - currentWorld;
            Vector2 alignDelta = currentCellCenter - currentWorld;

            if (Mathf.Abs(delta.x) <= reachThreshold && Mathf.Abs(delta.y) <= reachThreshold)
            {
                player.SetMoveDirection(Vector2.zero);
                return;
            }

            Vector2 moveDir;

            bool wantsHorizontalStep = targetCell.x != currentCell.x;
            bool wantsVerticalStep = targetCell.y != currentCell.y;

            if (wantsHorizontalStep && Mathf.Abs(alignDelta.y) > reachThreshold)
            {
                moveDir = new Vector2(0f, Mathf.Sign(alignDelta.y));
            }
            else if (wantsVerticalStep && Mathf.Abs(alignDelta.x) > reachThreshold)
            {
                moveDir = new Vector2(Mathf.Sign(alignDelta.x), 0f);
            }
            else if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                moveDir = new Vector2(Mathf.Sign(delta.x), 0f);
            }
            else
            {
                moveDir = new Vector2(0f, Mathf.Sign(delta.y));
            }

            player.SetMoveDirection(moveDir);
        }

        public bool FollowPath(BotBlackboard blackboard)
        {
            if (blackboard == null || blackboard.CurrentPath == null || blackboard.CurrentPath.Count == 0)
            {
                Stop();
                return true;
            }

            if (blackboard.CurrentPathIndex >= blackboard.CurrentPath.Count)
            {
                Stop();
                return true;
            }

            UpdateProgressTracker(blackboard);

            Vector3Int nextCell = blackboard.CurrentPath[blackboard.CurrentPathIndex];

            if (IsAtCell(nextCell))
            {
                blackboard.CurrentPathIndex++;

                if (blackboard.CurrentPathIndex >= blackboard.CurrentPath.Count)
                {
                    Stop();
                    return true;
                }

                nextCell = blackboard.CurrentPath[blackboard.CurrentPathIndex];
            }

            if (IsStuck(blackboard))
            {
                Stop();
                return true;
            }

            MoveTowardsCell(nextCell);
            return false;
        }

        public bool IsAtCell(Vector3Int cell)
        {
            if (referenceTilemap == null || player == null)
                return false;

            Vector3 targetWorld = referenceTilemap.GetCellCenterWorld(cell);
            return Vector2.Distance(player.GetNavigationWorldPosition(), targetWorld) <= reachThreshold;
        }

        public bool TryPlaceBomb()
        {
            if (player == null || !player.CanPlaceBomb)
                return false;

            player.PlaceBomb();
            return true;
        }

        private void UpdateProgressTracker(BotBlackboard blackboard)
        {
            Vector3 currentPosition = player.GetNavigationWorldPosition();

            if (blackboard.LastProgressTime <= 0f)
            {
                blackboard.LastProgressPosition = currentPosition;
                blackboard.LastProgressTime = Time.time;
                return;
            }

            if (Vector3.Distance(currentPosition, blackboard.LastProgressPosition) >= StuckDistanceThreshold)
            {
                blackboard.LastProgressPosition = currentPosition;
                blackboard.LastProgressTime = Time.time;
            }
        }

        private bool IsStuck(BotBlackboard blackboard)
        {
            if (blackboard.LastProgressTime <= 0f)
                return false;

            return Time.time - blackboard.LastProgressTime >= StuckTimeout;
        }
    }
}

