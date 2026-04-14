using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Map.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.Player.Scripts
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerController : MonoBehaviour
    {
        private Domain.Player player;
        private Animator anim;
        private PlayerMovement movement;
        private BotBrain botBrain;

        private MapContext mapContext;
        private Collider2D cachedCollider;
        private Vector3 cachedNavigationLocalOffset;
        private bool hasCachedNavigationLocalOffset;

        public Vector2 inputDir;
        private Vector2 lastDir = Vector2.down;

        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Transform feetPoint;
        [SerializeField] private float navigationBottomInset = 0.08f;

        public int BombCapacity => player != null ? player.BombCount : 1;
        public int BombRangeStat => player != null ? player.BombRange : 1;
        public float MoveSpeedStat => player != null ? player.Speed : 0f;
        public int HealthStat => player != null ? player.Health : 1;
        public int ActiveBombCount => currentBomb;
        public bool CanPlaceBomb => player != null && currentBomb < player.BombCount;
        public MapContext CurrentMapContext => mapContext;
        public bool IsBotControlled => botBrain != null && botBrain.enabled;
        public bool IsNavigationSettled => movement == null || !movement.IsInitialized || movement.IsSettledOnCurrentCell();

        [Header("Debug Player Hitbox")]
        [SerializeField] private bool debugPlayerCell = true;
        [SerializeField] private Color debugCellColor = Color.green;
        [SerializeField] private Vector3 debugCellSize = new Vector3(0.9f, 0.9f, 0.1f);

        [SerializeField] private bool debugCollider = true;
        [SerializeField] private Color debugColliderColor = Color.cyan;

        private int currentBomb;
        private SpriteRenderer spriteRenderer;
        private bool isDying = false;

        public bool IsDying => isDying;

        public void Init(MapContext context)
        {
            mapContext = context;
        }

        [System.Obsolete]
        private void Start()
        {
            player = new();
            anim = GetComponent<Animator>();
            movement = GetComponent<PlayerMovement>();
            botBrain = GetComponent<BotBrain>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            cachedCollider = GetComponent<Collider2D>();
            CacheNavigationLocalOffset();

            inputDir = Vector2.zero;

            if (mapContext == null)
            {
                mapContext = FindAnyObjectByType<MapContext>();

                if (mapContext == null)
                    Debug.LogError($"[{nameof(PlayerController)}] No MapContext found for {gameObject.name}.", this);
            }

            movement?.InitializeWithMap(mapContext);
        }


        private void Update()
        {
            HandleAnimation();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;

            if (debugPlayerCell)
            {
                Vector3Int cell;

                if (Application.isPlaying)
                {
                    cell = GetCurrentCell();
                }
                else
                {
                    Vector3 samplePos = feetPoint != null ? feetPoint.position : transform.position;

                    Tilemap refMap = GetReferenceTilemapInEditor();
                    Tilemap wallMap = GetWallTilemapInEditor();

                    if (refMap != null)
                    {
                        cell = refMap.WorldToCell(samplePos);
                    }
                    else if (wallMap != null)
                    {
                        cell = wallMap.WorldToCell(samplePos);
                    }
                    else
                    {
                        cell = new Vector3Int(
                            Mathf.FloorToInt(samplePos.x),
                            Mathf.FloorToInt(samplePos.y),
                            0);
                    }
                }

                Vector3 center = GetCellCenterWorld(cell);

                Gizmos.color = debugCellColor;
                Gizmos.DrawWireCube(center, debugCellSize);
            }

            if (debugCollider)
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                {
                    Gizmos.color = debugColliderColor;

                    if (col is BoxCollider2D box)
                    {
                        Gizmos.matrix = transform.localToWorldMatrix;
                        Gizmos.DrawWireCube(box.offset, box.size);
                        Gizmos.matrix = Matrix4x4.identity;
                    }
                    else if (col is CapsuleCollider2D capsule)
                    {
                        Gizmos.matrix = transform.localToWorldMatrix;
                        Gizmos.DrawWireCube(capsule.offset, capsule.size);
                        Gizmos.matrix = Matrix4x4.identity;
                    }
                }
            }
        }

        private Tilemap GetReferenceTilemapInEditor()
        {
            if (mapContext != null && mapContext.ReferenceTilemap != null)
                return mapContext.ReferenceTilemap;

            MapContext foundContext = GetComponentInParent<MapContext>();
            if (foundContext != null && foundContext.ReferenceTilemap != null)
                return foundContext.ReferenceTilemap;

            return null;
        }

        private Tilemap GetWallTilemapInEditor()
        {
            if (mapContext != null && mapContext.WallTilemap != null)
                return mapContext.WallTilemap;

            MapContext foundContext = GetComponentInParent<MapContext>();
            if (foundContext != null && foundContext.WallTilemap != null)
                return foundContext.WallTilemap;

            return null;
        }

        public Vector3Int GetCurrentCell()
        {
            if (movement != null && movement.IsInitialized)
                return movement.CurrentCell;

            Vector3 samplePos = GetNavigationWorldPosition();

            if (mapContext != null && mapContext.ReferenceTilemap != null)
                return mapContext.ReferenceTilemap.WorldToCell(samplePos);

            if (mapContext != null && mapContext.WallTilemap != null)
                return mapContext.WallTilemap.WorldToCell(samplePos);

            return new Vector3Int(
                Mathf.RoundToInt(samplePos.x),
                Mathf.RoundToInt(samplePos.y),
                0);
        }

        public Vector3Int GetLogicCell()
        {
            if (movement != null && movement.IsInitialized)
                return movement.SettledCell;

            return GetCurrentCell();
        }


        public Vector3 GetNavigationWorldPosition()
        {
            if (!hasCachedNavigationLocalOffset)
                CacheNavigationLocalOffset();

            Vector3 worldPosition = transform.TransformPoint(cachedNavigationLocalOffset);
            worldPosition.z = transform.position.z;
            return worldPosition;
        }

        public Vector3 GetNavigationAnchorWorld(Vector3Int cell)
        {
            if (movement != null && movement.IsInitialized)
                return movement.GetNavigationAnchorWorld(cell);

            return GetCellCenterWorld(cell);
        }

        private Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (mapContext != null && mapContext.ReferenceTilemap != null)
                return mapContext.ReferenceTilemap.GetCellCenterWorld(cell);

            if (mapContext != null && mapContext.WallTilemap != null)
                return mapContext.WallTilemap.GetCellCenterWorld(cell);

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private void HandleMovement()
        {
            if (inputDir != Vector2.zero)
                lastDir = inputDir;

            movement.Move(inputDir, player.Speed);
        }

        private void HandleAnimation()
        {
            anim.SetFloat("MoveX", inputDir.x);
            anim.SetFloat("MoveY", inputDir.y);
            anim.SetFloat("LastMoveX", lastDir.x);
            anim.SetFloat("LastMoveY", lastDir.y);
            anim.SetBool("IsMoving", inputDir != Vector2.zero);
        }

        public void OnMove(InputValue value)
        {
            inputDir = value.Get<Vector2>();

            if (inputDir.x != 0)
                inputDir.y = 0;
        }

        public void OnBomb()
        {
            PlaceBomb();
        }

        public void TakeDamage()
        {
            player.TakeDamage();

            if (player.Health <= 0)
                Die();
        }

        public void PlaceBomb()
        {
            if (currentBomb >= player.BombCount)
                return;

            if (mapContext == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] Cannot place bomb because MapContext is null.", this);
                return;
            }

            Vector3Int cell = GetCurrentCell();
            Vector2Int gridPos = new Vector2Int(cell.x, cell.y);

            Vector3 spawnPos = GetCellCenterWorld(cell);
            spawnPos.z = 0f;

            GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

            Domain.Bomb bombData = new Domain.Bomb(gridPos, 2f, player.BombRange);

            BombController bombController = bombObj.GetComponent<BombController>();
            if (bombController == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] Bomb prefab has no BombController.", bombObj);
                Destroy(bombObj);
                return;
            }

            bombController.Init(
                bombData,
                mapContext.WallTilemap,
                mapContext.ReferenceTilemap,
                mapContext.MapBuilder,
                () => currentBomb--);

            currentBomb++;
            BotRuntimeDebugLog.LogBombPlaced(this, cell, player.BombRange, currentBomb, player.BombCount);
        }

        private void Die()
        {
            isDying = true;
            Debug.Log("Player died");
            Destroy(gameObject);
        }

        public void PlaySpawnIntro(float duration, int blinkCount)
        {
            if (spriteRenderer == null)
                return;

            StartCoroutine(PlaySpawnIntroCoroutine(duration, blinkCount));
        }

        private System.Collections.IEnumerator PlaySpawnIntroCoroutine(float duration, int blinkCount)
        {
            float blinkInterval = duration / (blinkCount * 2f);
            for (int i = 0; i < blinkCount; i++)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(blinkInterval);
                spriteRenderer.enabled = true;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        public void SetMoveDirection(Vector2 dir)
        {
            inputDir = dir;

            if (inputDir.x != 0)
                inputDir.y = 0;
        }

        public void StopMoving()
        {
            inputDir = Vector2.zero;
        }

        public void AddSpeed(float amount) => player.AddSpeed(amount);
        public void AddBomb(int amount) => player.AddBombCount(amount);
        public void AddRange(int amount) => player.AddBombRange(amount);
        public void AddHealth(int amount) => player.AddHealth(amount);

        private void CacheNavigationLocalOffset()
        {
            if (feetPoint != null)
            {
                cachedNavigationLocalOffset = transform.InverseTransformPoint(feetPoint.position);
                cachedNavigationLocalOffset.z = 0f;
                hasCachedNavigationLocalOffset = true;
                return;
            }

            if (cachedCollider == null)
                cachedCollider = GetComponent<Collider2D>();

            if (cachedCollider != null)
            {
                Bounds bounds = cachedCollider.bounds;
                Vector3 navigationWorld = new(bounds.center.x, bounds.min.y + navigationBottomInset, transform.position.z);
                cachedNavigationLocalOffset = transform.InverseTransformPoint(navigationWorld);
                cachedNavigationLocalOffset.z = 0f;
                hasCachedNavigationLocalOffset = true;
                return;
            }

            cachedNavigationLocalOffset = Vector3.zero;
            hasCachedNavigationLocalOffset = true;
        }
    }
}


