using System.Collections;
using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Audio.Scripts;
using _Project.Gameplay.Match.Scripts;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.UI.Scripts;
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
        private SpriteRenderer spriteRenderer;

        private MapContext mapContext;
        private Collider2D cachedCollider;
        private Coroutine damageFlashRoutine;
        private Coroutine deathRoutine;
        private Coroutine spawnIntroRoutine;
        private bool isDying;
        private SpriteRenderer[] visualSpriteRenderers;

        public Vector2 inputDir;
        private Vector2 lastDir = Vector2.down;

        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Transform feetPoint;
        [SerializeField] private float navigationBottomInset = 0.08f;
        [SerializeField] private float damageFlashDuration = 0.3f;
        [SerializeField] private int damageFlashBlinkCount = 3;
        [SerializeField] private float deathBlinkDuration = 0.75f;
        [SerializeField] private int deathBlinkCount = 5;

        public int BombCapacity => player != null ? player.BombCount : 1;
        public int BombRangeStat => player != null ? player.BombRange : 1;
        public float MoveSpeedStat => player != null ? player.Speed : 0f;
        public int HealthStat => player != null ? player.Health : 1;
        public int ActiveBombCount => currentBomb;
        public bool CanPlaceBomb => player != null && currentBomb < player.BombCount;
        public MapContext CurrentMapContext => mapContext;
        public bool IsDying => isDying;

        [Header("Debug Player Hitbox")]
        [SerializeField] private bool debugPlayerCell = true;
        [SerializeField] private Color debugCellColor = Color.green;
        [SerializeField] private Vector3 debugCellSize = new Vector3(0.9f, 0.9f, 0.1f);

        [SerializeField] private bool debugCollider = true;
        [SerializeField] private Color debugColliderColor = Color.cyan;

        private int currentBomb;

        public void Init(MapContext context)
        {
            mapContext = context;
        }

        private void Start()
        {
            player = new Domain.Player();
            anim = GetComponent<Animator>();
            movement = GetComponent<PlayerMovement>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            visualSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            cachedCollider = GetComponent<Collider2D>();

            inputDir = Vector2.zero;

            if (mapContext == null)
            {
                mapContext = Object.FindFirstObjectByType<MapContext>();

                if (mapContext == null)
                    Debug.LogError($"[{nameof(PlayerController)}] No MapContext found for {gameObject.name}.", this);
            }

            movement?.InitializeWithMap(mapContext);
        }


        private void Update()
        {
            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive)
            {
                inputDir = Vector2.zero;
                return;
            }

            HandleAnimation();
        }

        private void FixedUpdate()
        {
            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive)
                return;

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


        public Vector3 GetNavigationWorldPosition()
        {
            if (feetPoint != null)
                return feetPoint.position;

            if (cachedCollider == null)
                cachedCollider = GetComponent<Collider2D>();

            if (cachedCollider != null)
            {
                Bounds bounds = cachedCollider.bounds;
                return new Vector3(bounds.center.x, bounds.min.y + navigationBottomInset, transform.position.z);
            }

            return transform.position;
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
            if (isDying)
                return;

            if (inputDir != Vector2.zero)
                lastDir = inputDir;

            movement.Move(inputDir, player.Speed);
        }

        private void HandleAnimation()
        {
            if (isDying)
                return;

            Vector2 animationDirection = movement != null && movement.IsMoving
                ? movement.CurrentMoveDirection
                : Vector2.zero;

            if (animationDirection != Vector2.zero)
                lastDir = animationDirection;
            else if (inputDir != Vector2.zero)
                lastDir = inputDir;

            anim.SetFloat("MoveX", animationDirection.x);
            anim.SetFloat("MoveY", animationDirection.y);
            anim.SetFloat("LastMoveX", lastDir.x);
            anim.SetFloat("LastMoveY", lastDir.y);
            anim.SetBool("IsMoving", movement != null && movement.IsMoving);
        }

        public void OnMove(InputValue value)
        {
            if (isDying)
                return;

            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive)
            {
                inputDir = Vector2.zero;
                return;
            }

            inputDir = value.Get<Vector2>();

            if (inputDir.x != 0)
                inputDir.y = 0;
        }

        public void OnBomb()
        {
            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive || isDying)
                return;

            PlaceBomb();
        }

        public void TakeDamage()
        {
            if (isDying)
                return;

            player.TakeDamage();
            AudioManager.Instance?.PlayDamage();

            if (player.Health <= 0)
            {
                Die();
                return;
            }

            PlayDamageFlash();
        }

        public bool PlaceBomb()
        {
            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive || isDying)
                return false;

            if (currentBomb >= player.BombCount)
                return false;

            if (mapContext == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] Cannot place bomb because MapContext is null.", this);
                return false;
            }

            Vector3Int cell = GetCurrentCell();
            GridOccupancyService occupancyService = mapContext.GridOccupancyService;
            if (occupancyService != null && occupancyService.HasBomb(cell))
                return false;

            Vector2Int gridPos = new Vector2Int(cell.x, cell.y);

            Vector3 spawnPos = GetCellCenterWorld(cell);
            spawnPos.z = 0f;

            GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

            _Project.Domain.Bomb bombData = new _Project.Domain.Bomb(
                gridPos,
                _Project.Domain.Bomb.DefaultExplodeTime,
                player.BombRange);

            BombController bombController = bombObj.GetComponent<BombController>();
            if (bombController == null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] Bomb prefab has no BombController.", bombObj);
                Destroy(bombObj);
                return false;
            }

            bombController.Init(
                bombData,
                mapContext.WallTilemap,
                mapContext.ReferenceTilemap,
                mapContext.MapBuilder,
                () => currentBomb--);

            currentBomb++;
            BotRuntimeDebugLog.LogBombPlaced(this, cell, player.BombRange, currentBomb, player.BombCount);
            return true;
        }

        private void Die()
        {
            if (isDying)
                return;

            isDying = true;
            inputDir = Vector2.zero;

            if (damageFlashRoutine != null)
            {
                StopCoroutine(damageFlashRoutine);
                damageFlashRoutine = null;
            }

            if (anim == null)
                anim = GetComponent<Animator>();

            if (movement == null)
                movement = GetComponent<PlayerMovement>();

            if (cachedCollider == null)
                cachedCollider = GetComponent<Collider2D>();

            if (movement != null)
            {
                movement.FreezeForDeath();
                movement.enabled = false;
            }

            if (cachedCollider != null)
                cachedCollider.enabled = false;

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
                playerInput.enabled = false;

            BotBrain botBrain = GetComponent<BotBrain>();
            if (botBrain != null)
                botBrain.enabled = false;

            if (anim != null)
                anim.enabled = false;

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            if (deathRoutine != null)
                StopCoroutine(deathRoutine);

            deathRoutine = StartCoroutine(DeathSequenceCoroutine());
        }

        private void PlayDamageFlash()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null || damageFlashBlinkCount <= 0 || damageFlashDuration <= 0f)
                return;

            if (damageFlashRoutine != null)
                StopCoroutine(damageFlashRoutine);

            damageFlashRoutine = StartCoroutine(DamageFlashCoroutine());
        }

        private IEnumerator DamageFlashCoroutine()
        {
            float interval = damageFlashDuration / Mathf.Max(1, damageFlashBlinkCount * 2);

            for (int i = 0; i < damageFlashBlinkCount; i++)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(interval);

                spriteRenderer.enabled = true;
                yield return new WaitForSeconds(interval);
            }

            spriteRenderer.enabled = true;
            damageFlashRoutine = null;
        }

        private IEnumerator DeathSequenceCoroutine()
        {
            if (spriteRenderer == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float interval = deathBlinkDuration / Mathf.Max(1, deathBlinkCount * 2);

            for (int i = 0; i < deathBlinkCount; i++)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(interval);

                spriteRenderer.enabled = true;
                yield return new WaitForSeconds(interval);
            }

            spriteRenderer.enabled = true;
            deathRoutine = null;
            Destroy(gameObject);
        }

        public void SetMoveDirection(Vector2 dir)
        {
            if (MapPauseButtonController.IsGamePaused || MatchResultOverlayController.IsShowingResult || RoundIntroState.IsActive)
            {
                inputDir = Vector2.zero;
                return;
            }

            inputDir = dir;

            if (inputDir.x != 0)
                inputDir.y = 0;
        }

        public void StopMoving()
        {
            inputDir = Vector2.zero;
        }

        public void PlaySpawnIntro(float duration, int blinkCount)
        {
            if (isDying)
                return;

            if (spawnIntroRoutine != null)
                StopCoroutine(spawnIntroRoutine);

            spawnIntroRoutine = StartCoroutine(SpawnIntroCoroutine(duration, blinkCount));
        }

        public void AddSpeed(float amount) => player.AddSpeed(amount);
        public void AddBomb(int amount) => player.AddBombCount(amount);
        public void AddRange(int amount) => player.AddBombRange(amount);
        public void AddHealth(int amount) => player.AddHealth(amount);

        private void OnDisable()
        {
            inputDir = Vector2.zero;
            SetAllVisualsEnabled(true);
        }

        private IEnumerator SpawnIntroCoroutine(float duration, int blinkCount)
        {
            if (duration <= 0f || blinkCount <= 0)
            {
                SetAllVisualsEnabled(true);
                spawnIntroRoutine = null;
                yield break;
            }

            float interval = duration / Mathf.Max(1, blinkCount * 2);

            for (int i = 0; i < blinkCount; i++)
            {
                SetAllVisualsEnabled(false);
                yield return new WaitForSecondsRealtime(interval);

                SetAllVisualsEnabled(true);
                yield return new WaitForSecondsRealtime(interval);
            }

            SetAllVisualsEnabled(true);
            spawnIntroRoutine = null;
        }

        private void SetAllVisualsEnabled(bool enabled)
        {
            if (visualSpriteRenderers == null || visualSpriteRenderers.Length == 0)
                visualSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in visualSpriteRenderers)
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }
    }
}


