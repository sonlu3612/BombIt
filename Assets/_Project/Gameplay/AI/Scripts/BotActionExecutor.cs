using _Project.Gameplay.Map.Scripts;
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

        private Vector2 lastLoggedMoveDirection = new(float.NaN, float.NaN);
        private Vector3Int? lastLoggedMoveTargetCell;
        private int lastLoggedPathIndex = -1;

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

            if (player.inputDir != Vector2.zero)
                BotRuntimeDebugLog.LogBotStop(player, player.GetLogicCell());

            player.SetMoveDirection(Vector2.zero);
            lastLoggedMoveDirection = new Vector2(float.NaN, float.NaN);
            lastLoggedMoveTargetCell = null;
            lastLoggedPathIndex = -1;
        }

        public void MoveTowardsCell(Vector3Int targetCell, int pathIndex = -1, int pathCount = -1)
        {
            if (player == null || referenceTilemap == null)
                return;

            Vector3 targetWorld = player.GetNavigationAnchorWorld(targetCell);
            Vector3 currentWorld = player.GetNavigationWorldPosition();
            Vector3Int occupancyCell = player.GetCurrentCell();

            // Use logicCell (settled/physics-committed cell) for direction math.
            // occupancyCell can advance 1-2 cells ahead during movement, making
            // targetCell diagonally offset → both axes non-zero → alignment oscillation.
            Vector3Int currentCell = player.GetLogicCell();
            Vector3 currentCellCenter = player.GetNavigationAnchorWorld(currentCell);
            Vector2 delta = targetWorld - currentWorld;
            Vector2 alignDelta = currentCellCenter - currentWorld;

            // Only stop once the navigator position is actually near the cell center.
            // PlayerMovement updates CurrentCell before the actor fully settles on center,
            // so stopping on CurrentCell == targetCell makes bots drift between cells.
            if (Mathf.Abs(delta.x) <= reachThreshold && Mathf.Abs(delta.y) <= reachThreshold)
            {
                player.SetMoveDirection(Vector2.zero);
                return;
            }

            Vector2 primaryMoveDir = GetPrimaryMoveDirection(currentCell, targetCell, delta);
            Vector2 moveDir = primaryMoveDir;
            bool usedAlignment = false;

            if (TryGetAlignmentDirection(currentCell, targetCell, alignDelta, out Vector2 alignmentDir)
                && CanTraverseToward(currentCell, alignmentDir))
            {
                moveDir = alignmentDir;
                usedAlignment = true;
            }

            if (moveDir == Vector2.zero)
                moveDir = primaryMoveDir != Vector2.zero ? primaryMoveDir : GetDeltaMoveDirection(delta);

            if (ShouldLogMove(targetCell, moveDir, pathIndex))
            {
                BotRuntimeDebugLog.LogBotMoveCommand(player, currentCell, targetCell, moveDir, pathIndex, pathCount);
                LogExecutorDecision(player.GetLogicCell(), occupancyCell, targetCell, currentWorld, delta, alignDelta, moveDir, usedAlignment, pathIndex, pathCount);
                lastLoggedMoveDirection = moveDir;
                lastLoggedMoveTargetCell = targetCell;
                lastLoggedPathIndex = pathIndex;
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
            Vector3Int logicCell = player != null ? player.GetLogicCell() : Vector3Int.zero;

            // Advance pathIndex if the bot's settled occupancy cell (logicCell) has reached nextCell,
            // OR if the distance-based threshold is satisfied.
            // Using logicCell prevents MoveTowardsCell being called with currentCell == targetCell,
            // which would produce zero cell-delta and fall through to raw-delta direction (causing reversals).
            while (blackboard.CurrentPathIndex < blackboard.CurrentPath.Count
                   && (logicCell == nextCell || IsAtCell(nextCell)))
            {
                blackboard.CurrentPathIndex++;

                if (blackboard.CurrentPathIndex >= blackboard.CurrentPath.Count)
                {
                    Stop();
                    return true;
                }

                nextCell = blackboard.CurrentPath[blackboard.CurrentPathIndex];
                logicCell = player != null ? player.GetLogicCell() : Vector3Int.zero;
            }

            if (IsStuck(blackboard))
            {
                BotRuntimeDebugLog.LogBotStuck(player, player.GetLogicCell(), nextCell, blackboard.CurrentPathIndex, blackboard.CurrentPath.Count);
                Stop();
                return true;
            }

            MoveTowardsCell(nextCell, blackboard.CurrentPathIndex, blackboard.CurrentPath.Count);
            return false;
        }

        public bool IsAtCell(Vector3Int cell)
        {
            if (player == null)
                return false;

            Vector3 targetWorld = player.GetNavigationAnchorWorld(cell);
            return Vector2.Distance(player.GetNavigationWorldPosition(), targetWorld) <= reachThreshold * 1.5f;
        }

        public bool TryPlaceBomb()
        {
            if (player == null || !player.CanPlaceBomb || !player.IsNavigationSettled)
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

        private bool ShouldLogMove(Vector3Int targetCell, Vector2 moveDir, int pathIndex)
        {
            if (!lastLoggedMoveTargetCell.HasValue)
                return true;

            return lastLoggedMoveTargetCell.Value != targetCell
                   || lastLoggedMoveDirection != moveDir
                   || lastLoggedPathIndex != pathIndex;
        }

        private Vector2 GetPrimaryMoveDirection(Vector3Int currentCell, Vector3Int targetCell, Vector2 delta)
        {
            int cellDeltaX = targetCell.x - currentCell.x;
            int cellDeltaY = targetCell.y - currentCell.y;

            if (cellDeltaX != 0)
                return new Vector2(Mathf.Sign(cellDeltaX), 0f);

            if (cellDeltaY != 0)
                return new Vector2(0f, Mathf.Sign(cellDeltaY));

            // currentCell == targetCell: bot is already on this cell.
            // FollowPath should have advanced past this step, but if it hasn't yet,
            // return zero so the caller can handle it cleanly rather than
            // using a near-zero delta that may produce a reversed direction.
            return Vector2.zero;
        }

        private Vector2 GetDeltaMoveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return new Vector2(Mathf.Sign(delta.x), 0f);

            if (Mathf.Abs(delta.y) > 0f)
                return new Vector2(0f, Mathf.Sign(delta.y));

            return Vector2.zero;
        }

        private bool TryGetAlignmentDirection(Vector3Int currentCell, Vector3Int targetCell, Vector2 alignDelta, out Vector2 alignmentDir)
        {
            alignmentDir = Vector2.zero;

            // Only align when targetCell is exactly adjacent (Manhattan distance = 1).
            // If currentCell is diagonally offset from targetCell (both axes differ),
            // alignment would override the primary axis and cause direction oscillation.
            int manhattanDist = Mathf.Abs(targetCell.x - currentCell.x) + Mathf.Abs(targetCell.y - currentCell.y);
            if (manhattanDist != 1)
                return false;

            bool movingHorizontally = targetCell.x != currentCell.x;
            bool movingVertically = targetCell.y != currentCell.y;

            if (movingHorizontally && Mathf.Abs(alignDelta.y) > reachThreshold)
            {
                alignmentDir = new Vector2(0f, Mathf.Sign(alignDelta.y));
                return true;
            }

            if (movingVertically && Mathf.Abs(alignDelta.x) > reachThreshold)
            {
                alignmentDir = new Vector2(Mathf.Sign(alignDelta.x), 0f);
                return true;
            }

            return false;
        }

        private bool CanTraverseToward(Vector3Int currentCell, Vector2 direction)
        {
            if (direction == Vector2.zero)
                return false;

            MapContext mapContext = player != null ? player.CurrentMapContext : null;
            if (mapContext == null)
                return false;

            Vector3Int nextCell = currentCell + new Vector3Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y), 0);
            GridOccupancyService occupancy = mapContext.GridOccupancyService;
            if (occupancy != null)
                return occupancy.IsCellWalkable(nextCell, player, true, true);

            return BotGridUtility.IsWalkable(nextCell, mapContext);
        }

        private void LogExecutorDecision(
            Vector3Int logicCell,
            Vector3Int occupancyCell,
            Vector3Int targetCell,
            Vector3 worldPosition,
            Vector2 delta,
            Vector2 alignDelta,
            Vector2 moveDirection,
            bool usedAlignment,
            int pathIndex,
            int pathCount)
        {
            MapContext mapContext = player != null ? player.CurrentMapContext : null;
            GridOccupancyService occupancy = mapContext != null ? mapContext.GridOccupancyService : null;

            bool targetWithinBounds = BotGridUtility.IsWithinBounds(targetCell, mapContext);
            bool targetWalkable = occupancy != null
                ? occupancy.IsCellWalkable(targetCell, player, true, true)
                : BotGridUtility.IsWalkable(targetCell, mapContext);

            BotMovementTraceLog.LogExecutorDecision(
                player,
                logicCell,
                occupancyCell,
                targetCell,
                worldPosition,
                delta,
                alignDelta,
                moveDirection,
                usedAlignment,
                targetWithinBounds,
                targetWalkable,
                pathIndex,
                pathCount);
        }
    }
}
