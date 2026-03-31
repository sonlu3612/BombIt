using _Project.Gameplay.Bomb.Scripts;
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

        public Vector2 inputDir;
        private Vector2 lastDir = Vector2.down;

        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap blockTilemap;
        [SerializeField] private MapBuilder mapBuilder;
        [SerializeField] private Tilemap referenceTilemap;
        [SerializeField] private Transform feetPoint;

        [Header("Debug Player Hitbox")]
        [SerializeField] private bool debugPlayerCell = true;
        [SerializeField] private Color debugCellColor = Color.green;
        [SerializeField] private Vector3 debugCellSize = new Vector3(0.9f, 0.9f, 0.1f);

        [SerializeField] private bool debugCollider = true;
        [SerializeField] private Color debugColliderColor = Color.cyan;

        private int currentBomb = 0;

        private void Start()
        {
            player = new Domain.Player();
            anim = GetComponent<Animator>();
            movement = GetComponent<PlayerMovement>();

            inputDir = Vector2.zero;
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

                    if (referenceTilemap != null)
                        cell = referenceTilemap.WorldToCell(samplePos);
                    else if (wallTilemap != null)
                        cell = wallTilemap.WorldToCell(samplePos);
                    else
                        cell = new Vector3Int(
                            Mathf.FloorToInt(samplePos.x),
                            Mathf.FloorToInt(samplePos.y),
                            0
                        );
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

        public Vector3Int GetCurrentCell()
        {
            Vector3 samplePos = feetPoint != null ? feetPoint.position : transform.position;

            if (referenceTilemap != null)
            {
                return referenceTilemap.WorldToCell(samplePos);
            }

            if (wallTilemap != null)
            {
                return wallTilemap.WorldToCell(samplePos);
            }

            return new Vector3Int(
                Mathf.FloorToInt(samplePos.x),
                Mathf.FloorToInt(samplePos.y),
                0
            );
        }

        private Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (referenceTilemap != null)
                return referenceTilemap.GetCellCenterWorld(cell);

            if (wallTilemap != null)
                return wallTilemap.GetCellCenterWorld(cell);

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private void HandleMovement()
        {
            if (inputDir != Vector2.zero)
            {
                lastDir = inputDir;
            }

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
            {
                Die();
            }
        }

        public void PlaceBomb()
        {
            if (currentBomb >= player.BombCount) return;

            Vector3Int cell = GetCurrentCell();
            Vector2Int gridPos = new Vector2Int(cell.x, cell.y);

            Vector3 spawnPos = GetCellCenterWorld(cell);
            spawnPos.z = 0f;

            GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

            Domain.Bomb bombData = new Domain.Bomb(gridPos, 2f, player.BombRange);

            bombObj.GetComponent<BombController>().Init(
                bombData,
                wallTilemap,
                referenceTilemap,
                mapBuilder,
                () => currentBomb--
            );

            currentBomb++;
        }

        private void Die()
        {
            Debug.Log("Player died");
            Destroy(gameObject);
        }

        public void AddSpeed(float amount) => player.AddSpeed(amount);
        public void AddBomb(int amount) => player.AddBombCount(amount);
        public void AddRange(int amount) => player.AddBombRange(amount);
        public void AddHealth(int amount) => player.AddHealth(amount);
    }
}