using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    private enum MovementAxis
    {
        None,
        Horizontal,
        Vertical
    }

    [SerializeField] private float laneSnapSpeed = 8f;
    [SerializeField] private float laneThreshold = 0.03f;
    [SerializeField] private float cellReachThreshold = 0.04f;
    [SerializeField] private bool blockPlayerCells = true;
    [SerializeField] private bool forceTriggerCollider = true;
    [SerializeField] private bool allowHeldMoveQueueForBots = false;

    private Rigidbody2D rb;
    private Collider2D hitbox;
    private PlayerController controller;

    private MapContext mapContext;
    private GridOccupancyService occupancy;

    private Vector2 requestedDirection;
    private float moveSpeed;
    private bool initialized;
    private bool registeredWithOccupancy;

    private Vector3 navigationOffsetFromRoot;
    private Vector3 navigationCellOffset;
    private Vector3Int settledCell;
    private Vector3Int currentCell;
    private Vector3Int? targetCell;
    private Vector2 currentMoveDirection;
    private bool isMoving;
    private MovementAxis activeMoveAxis;

    public bool IsInitialized => initialized;
    public Vector3Int SettledCell => settledCell;
    public Vector3Int CurrentCell => currentCell;
    public Vector2 CurrentMoveDirection => currentMoveDirection;
    public bool IsMoving => isMoving;
    public Vector3Int? TargetCell => targetCell;
    public Vector3 GetNavigationAnchorWorld(Vector3Int cell) => GetCellNavigationAnchor(cell);
    public bool IsSettledOnCurrentCell()
    {
        if (!initialized || controller == null)
            return true;

        if (targetCell.HasValue || currentCell != settledCell)
            return false;

        return Vector2.Distance(controller.GetNavigationWorldPosition(), GetCellNavigationAnchor(currentCell)) <= cellReachThreshold * 1.5f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        controller = GetComponent<PlayerController>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (forceTriggerCollider && hitbox != null)
            hitbox.isTrigger = true;
    }

    public void InitializeWithMap(MapContext context)
    {
        if (initialized)
            return;

        mapContext = context;
        occupancy = mapContext != null ? mapContext.GridOccupancyService : null;

        if (controller == null || occupancy == null)
        {
            Debug.LogError($"[{nameof(PlayerMovement)}] Missing controller or occupancy on {gameObject.name}", this);
            return;
        }

        Vector3 initialNavigationPosition = controller.GetNavigationWorldPosition();
        navigationOffsetFromRoot = transform.position - initialNavigationPosition;
        currentCell = occupancy.WorldToCell(initialNavigationPosition);
        // navigationCellOffset is intentionally zero: the nav anchor IS the cell center.
        // Any visual offset (feet below sprite center) is already encoded in navigationOffsetFromRoot.
        // Capturing spawn misalignment here would permanently bake the offset into all movement.
        navigationCellOffset = Vector3.zero;
        settledCell = currentCell;

        SnapRootToCell(currentCell);
        occupancy.RegisterPlayer(controller, currentCell);
        registeredWithOccupancy = true;
        initialized = true;

        // BotMovementTraceLog.LogPlayerMovement(
        //     controller,
        //     "INIT",
        //     settledCell,
        //     currentCell,
        //     targetCell,
        //     controller.GetNavigationWorldPosition(),
        //     GetCellNavigationAnchor(currentCell),
        //     requestedDirection,
        //     currentMoveDirection,
        //     isMoving,
        //     $"rootOffset={navigationOffsetFromRoot.x:0.###},{navigationOffsetFromRoot.y:0.###},{navigationOffsetFromRoot.z:0.###} navOffset={navigationCellOffset.x:0.###},{navigationCellOffset.y:0.###},{navigationCellOffset.z:0.###}");
    }

    public void Move(Vector2 dir, float speed)
    {
        requestedDirection = NormalizeToCardinal(dir);
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void FreezeForDeath()
    {
        requestedDirection = Vector2.zero;
        targetCell = null;
        currentMoveDirection = Vector2.zero;
        isMoving = false;
        activeMoveAxis = MovementAxis.None;

        if (registeredWithOccupancy && occupancy != null && controller != null)
        {
            occupancy.UnregisterPlayer(controller);
            registeredWithOccupancy = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep();
        }
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        TickMovement(Time.fixedDeltaTime);
    }

    private void TickMovement(float deltaTime)
    {
        if (targetCell.HasValue)
        {
            ContinueMoveToTarget(deltaTime);
            return;
        }

        if (requestedDirection == Vector2.zero)
        {
            SnapBackToCellCenter(deltaTime);
            return;
        }

        Vector3 navPosition = controller.GetNavigationWorldPosition();
        Vector3 currentCenter = GetCellNavigationAnchor(currentCell);

        bool horizontalMove = Mathf.Abs(requestedDirection.x) > 0.1f;

        if (horizontalMove)
        {
            if (Mathf.Abs(navPosition.y - currentCenter.y) > laneThreshold)
            {
                MoveNavigationTowards(
                    new Vector3(navPosition.x, currentCenter.y, navPosition.z),
                    Mathf.Max(moveSpeed, laneSnapSpeed),
                    deltaTime,
                    MovementAxis.Vertical);
                return;
            }

            Vector3Int nextCell = currentCell + new Vector3Int(Mathf.RoundToInt(requestedDirection.x), 0, 0);

            if (!occupancy.IsCellWalkable(nextCell, controller, true, blockPlayerCells))
            {
                // BotMovementTraceLog.LogBlockedAttempt(
                //     controller,
                //     settledCell,
                //     currentCell,
                //     nextCell,
                //     requestedDirection,
                //     BotGridUtility.IsWithinBounds(nextCell, occupancy.MapContext),
                //     occupancy.IsStaticallyBlocked(nextCell),
                //     occupancy.IsDynamicallyBlocked(nextCell, controller, true, blockPlayerCells));
                SnapBackToCellCenter(deltaTime);
                return;
            }

            targetCell = nextCell;
            activeMoveAxis = MovementAxis.Horizontal;
            // BotMovementTraceLog.LogPlayerMovement(
            //     controller,
            //     "START_STEP",
            //     settledCell,
            //     currentCell,
            //     targetCell,
            //     navPosition,
            //     GetCellNavigationAnchor(currentCell),
            //     requestedDirection,
            //     currentMoveDirection,
            //     isMoving,
            //     "horizontal");
            ContinueMoveToTarget(deltaTime);
            return;
        }

        if (Mathf.Abs(navPosition.x - currentCenter.x) > laneThreshold)
        {
            MoveNavigationTowards(
                new Vector3(currentCenter.x, navPosition.y, navPosition.z),
                Mathf.Max(moveSpeed, laneSnapSpeed),
                deltaTime,
                MovementAxis.Horizontal);
            return;
        }

        Vector3Int verticalCell = currentCell + new Vector3Int(0, Mathf.RoundToInt(requestedDirection.y), 0);

        if (!occupancy.IsCellWalkable(verticalCell, controller, true, blockPlayerCells))
        {
            // BotMovementTraceLog.LogBlockedAttempt(
            //     controller,
            //     settledCell,
            //     currentCell,
            //     verticalCell,
            //     requestedDirection,
            //     BotGridUtility.IsWithinBounds(verticalCell, occupancy.MapContext),
            //     occupancy.IsStaticallyBlocked(verticalCell),
            //     occupancy.IsDynamicallyBlocked(verticalCell, controller, true, blockPlayerCells));
            SnapBackToCellCenter(deltaTime);
            return;
        }

        targetCell = verticalCell;
        activeMoveAxis = MovementAxis.Vertical;
        // BotMovementTraceLog.LogPlayerMovement(
        //     controller,
        //     "START_STEP",
        //     settledCell,
        //     currentCell,
        //     targetCell,
        //     navPosition,
        //     GetCellNavigationAnchor(currentCell),
        //     requestedDirection,
        //     currentMoveDirection,
        //     isMoving,
        //     "vertical");
        ContinueMoveToTarget(deltaTime);
    }

    private void ContinueMoveToTarget(float deltaTime)
    {
        if (!targetCell.HasValue)
            return;

        Vector3Int destinationCell = targetCell.Value;
        MovementAxis axis = activeMoveAxis != MovementAxis.None
            ? activeMoveAxis
            : destinationCell.x != currentCell.x ? MovementAxis.Horizontal : MovementAxis.Vertical;

        Vector3 currentCenter = GetCellNavigationAnchor(currentCell);
        Vector3 navTarget = GetCellNavigationAnchor(destinationCell);

        if (axis == MovementAxis.Horizontal)
            navTarget.y = currentCenter.y;
        else if (axis == MovementAxis.Vertical)
            navTarget.x = currentCenter.x;

        Vector3 nextNav = MoveNavigationTowards(navTarget, moveSpeed, deltaTime, axis);
        UpdateCurrentCellFromNavigation(nextNav, destinationCell);

        if (HasReachedTarget(navTarget, axis))
        {
            // Debug.Log($"[PM] COMMIT from settled={settledCell} to dest={destinationCell}");
            Vector3Int previousCell = settledCell;

            currentCell = destinationCell;
            settledCell = destinationCell;
            targetCell = null;
            activeMoveAxis = MovementAxis.None;

            occupancy.MovePlayer(controller, previousCell, destinationCell);
            bool continuesMoving = ShouldAutoQueueHeldMove() && TryQueueHeldMoveFromCurrentCell();

            if (!continuesMoving && controller != null && controller.IsBotControlled)
            {
                requestedDirection = Vector2.zero;
                moveSpeed = 0f;
                currentMoveDirection = Vector2.zero;
                isMoving = false;
                controller.ClearMoveInput();
            }

            SnapRootToCell(currentCell, !continuesMoving);

            // BotMovementTraceLog.LogPlayerMovement(
            //     controller,
            //     "COMMIT_CELL",
            //     settledCell,
            //     currentCell,
            //     targetCell,
            //     controller.GetNavigationWorldPosition(),
            //     GetCellNavigationAnchor(currentCell),
            //     requestedDirection,
            //     currentMoveDirection,
            //     isMoving,
            //     $"continues={continuesMoving}");
        }
    }

    public void StopImmediately()
    {
        // Debug.Log($"[PM] StopImmediately settled={settledCell} current={currentCell} target={targetCell}");

        requestedDirection = Vector2.zero;
        moveSpeed = 0f;
        targetCell = null;
        activeMoveAxis = MovementAxis.None;
        currentMoveDirection = Vector2.zero;
        isMoving = false;

        currentCell = settledCell;
        SnapRootToCell(settledCell, true);
    }

    public void StopSoftly()
    {
        requestedDirection = Vector2.zero;
        moveSpeed = 0f;

        if (!targetCell.HasValue)
        {
            activeMoveAxis = MovementAxis.None;
            currentMoveDirection = Vector2.zero;
            isMoving = false;
        }
    }

    private void SnapBackToCellCenter(float deltaTime)
    {
        Vector3 navTarget = GetCellNavigationAnchor(currentCell);

        if (Vector2.Distance(controller.GetNavigationWorldPosition(), navTarget) <= cellReachThreshold)
        {
            currentMoveDirection = Vector2.zero;
            isMoving = false;
            activeMoveAxis = MovementAxis.None;
            SnapRootToCell(currentCell);
            return;
        }

        MoveNavigationTowards(navTarget, Mathf.Max(moveSpeed, laneSnapSpeed), deltaTime, MovementAxis.None);
    }

    private Vector3 MoveNavigationTowards(Vector3 navTarget, float speed, float deltaTime, MovementAxis axis)
    {
        Vector3 currentNav = controller.GetNavigationWorldPosition();
        Vector3 nextNav = Vector3.MoveTowards(currentNav, navTarget, speed * deltaTime);

        if (axis == MovementAxis.Horizontal)
            nextNav.y = navTarget.y;
        else if (axis == MovementAxis.Vertical)
            nextNav.x = navTarget.x;

        UpdateMovementState(nextNav - currentNav);

        Vector3 nextRoot = nextNav + navigationOffsetFromRoot;
        nextRoot.z = transform.position.z;

        rb.MovePosition(nextRoot);
        return nextNav;
    }

    private bool HasReachedTarget(Vector3 navTarget, MovementAxis axis)
    {
        Vector3 navPosition = controller.GetNavigationWorldPosition();

        if (axis == MovementAxis.Horizontal)
            return Mathf.Abs(navPosition.x - navTarget.x) <= cellReachThreshold;

        if (axis == MovementAxis.Vertical)
            return Mathf.Abs(navPosition.y - navTarget.y) <= cellReachThreshold;

        return Vector2.Distance(navPosition, navTarget) <= cellReachThreshold;
    }

    private bool TryQueueHeldMoveFromCurrentCell()
    {
        if (requestedDirection == Vector2.zero)
            return false;

        bool horizontalMove = Mathf.Abs(requestedDirection.x) > 0.1f;
        Vector3Int nextCell = horizontalMove
            ? currentCell + new Vector3Int(Mathf.RoundToInt(requestedDirection.x), 0, 0)
            : currentCell + new Vector3Int(0, Mathf.RoundToInt(requestedDirection.y), 0);

        if (!occupancy.IsCellWalkable(nextCell, controller, true, blockPlayerCells))
            return false;

        targetCell = nextCell;
        activeMoveAxis = horizontalMove ? MovementAxis.Horizontal : MovementAxis.Vertical;
        currentMoveDirection = requestedDirection;
        isMoving = true;
        return true;
    }

    private bool ShouldAutoQueueHeldMove()
    {
        if (controller == null)
            return true;

        if (!controller.IsBotControlled)
            return true;

        return allowHeldMoveQueueForBots;
    }

    private void SnapRootToCell(Vector3Int cell, bool resetMovementState = true)
    {
        // Debug.Log($"[PM] SnapRootToCell -> {cell} reset={resetMovementState}");

        Vector3 navTarget = GetCellNavigationAnchor(cell);
        Vector3 rootTarget = navTarget + navigationOffsetFromRoot;
        rootTarget.z = transform.position.z;

        rb.position = rootTarget;

        currentCell = cell;
        settledCell = cell;

        if (resetMovementState)
        {
            currentMoveDirection = Vector2.zero;
            isMoving = false;
        }
    }

    private void UpdateCurrentCellFromNavigation(Vector3 navigationPosition, Vector3Int destinationCell)
    {

        // BotMovementTraceLog.LogPlayerMovement(
        //     controller,
        //     "CROSS_BOUNDARY",
        //     settledCell,
        //     currentCell,
        //     targetCell,
        //     navigationPosition,
        //     GetCellNavigationAnchor(destinationCell),
        //     requestedDirection,
        //     currentMoveDirection,
        //     isMoving,
        //     "occupancy advanced before settle");
    }

    private Vector3 GetCellNavigationAnchor(Vector3Int cell)
    {
        Vector3 center = occupancy.GetCellCenterWorld(cell);
        return center + navigationCellOffset;
    }

    private void UpdateMovementState(Vector3 navigationDelta)
    {
        Vector2 delta = new Vector2(navigationDelta.x, navigationDelta.y);
        if (delta.sqrMagnitude <= 0.000001f)
        {
            currentMoveDirection = Vector2.zero;
            isMoving = false;
            return;
        }

        isMoving = true;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            currentMoveDirection = new Vector2(Mathf.Sign(delta.x), 0f);
        else
            currentMoveDirection = new Vector2(0f, Mathf.Sign(delta.y));
    }

    private Vector2 NormalizeToCardinal(Vector2 dir)
    {
        if (dir == Vector2.zero)
            return Vector2.zero;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2(Mathf.Sign(dir.x), 0f);

        return new Vector2(0f, Mathf.Sign(dir.y));
    }

    private void OnDisable()
    {
        requestedDirection = Vector2.zero;
        targetCell = null;
        currentMoveDirection = Vector2.zero;
        isMoving = false;
        activeMoveAxis = MovementAxis.None;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (registeredWithOccupancy && occupancy != null && controller != null)
            occupancy.UnregisterPlayer(controller);
    }
}


