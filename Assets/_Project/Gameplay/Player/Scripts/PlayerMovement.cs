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
    private Vector3Int currentCell;
    private Vector3Int? targetCell;

    public bool IsInitialized => initialized;
    public Vector3Int CurrentCell => currentCell;

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

        navigationOffsetFromRoot = transform.position - controller.GetNavigationWorldPosition();
        currentCell = occupancy.WorldToCell(controller.GetNavigationWorldPosition());

        SnapRootToCell(currentCell);
        occupancy.RegisterPlayer(controller, currentCell);
        registeredWithOccupancy = true;
        initialized = true;
    }

    public void Move(Vector2 dir, float speed)
    {
        requestedDirection = NormalizeToCardinal(dir);
        moveSpeed = Mathf.Max(0f, speed);
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
        Vector3 currentCenter = occupancy.GetCellCenterWorld(currentCell);

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
                SnapBackToCellCenter(deltaTime);
                return;
            }

            targetCell = nextCell;
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
            SnapBackToCellCenter(deltaTime);
            return;
        }

        targetCell = verticalCell;
        ContinueMoveToTarget(deltaTime);
    }

    private void ContinueMoveToTarget(float deltaTime)
    {
        if (!targetCell.HasValue)
            return;

        Vector3Int destinationCell = targetCell.Value;
        MovementAxis axis = destinationCell.x != currentCell.x ? MovementAxis.Horizontal : MovementAxis.Vertical;

        Vector3 currentCenter = occupancy.GetCellCenterWorld(currentCell);
        Vector3 navTarget = occupancy.GetCellCenterWorld(destinationCell);

        if (axis == MovementAxis.Horizontal)
            navTarget.y = currentCenter.y;
        else if (axis == MovementAxis.Vertical)
            navTarget.x = currentCenter.x;

        MoveNavigationTowards(navTarget, moveSpeed, deltaTime, axis);

        if (HasReachedTarget(navTarget, axis))
        {
            Vector3Int previousCell = currentCell;
            currentCell = destinationCell;
            targetCell = null;

            occupancy.MovePlayer(controller, previousCell, currentCell);
            SnapRootToCell(currentCell);
        }
    }

    private void SnapBackToCellCenter(float deltaTime)
    {
        Vector3 navTarget = occupancy.GetCellCenterWorld(currentCell);

        if (Vector2.Distance(controller.GetNavigationWorldPosition(), navTarget) <= cellReachThreshold)
        {
            SnapRootToCell(currentCell);
            return;
        }

        MoveNavigationTowards(navTarget, Mathf.Max(moveSpeed, laneSnapSpeed), deltaTime, MovementAxis.None);
    }

    private void MoveNavigationTowards(Vector3 navTarget, float speed, float deltaTime, MovementAxis axis)
    {
        Vector3 currentNav = controller.GetNavigationWorldPosition();
        Vector3 nextNav = Vector3.MoveTowards(currentNav, navTarget, speed * deltaTime);

        if (axis == MovementAxis.Horizontal)
            nextNav.y = navTarget.y;
        else if (axis == MovementAxis.Vertical)
            nextNav.x = navTarget.x;

        Vector3 nextRoot = nextNav + navigationOffsetFromRoot;
        nextRoot.z = transform.position.z;

        rb.MovePosition(nextRoot);
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

    private void SnapRootToCell(Vector3Int cell)
    {
        Vector3 navTarget = occupancy.GetCellCenterWorld(cell);
        Vector3 rootTarget = navTarget + navigationOffsetFromRoot;
        rootTarget.z = transform.position.z;

        rb.position = rootTarget;
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

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (registeredWithOccupancy && occupancy != null && controller != null)
            occupancy.UnregisterPlayer(controller);
    }
}