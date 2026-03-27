using _Project.Gameplay.Bomb.Scripts;
using UnityEngine;

namespace _Project.Gameplay.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        private Domain.Player player;
        private Animator anim;
        private PlayerMovement movement;

        public Vector2 inputDir; 
        private Vector2 lastDir = Vector2.down;
        [SerializeField] private GameObject bombPrefab;
        private int currentBomb = 0;

        void Start()
        {
            player = new Domain.Player();
            anim = GetComponent<Animator>();
            movement = GetComponent<PlayerMovement>();
        }

        void Update()
        {
            HandleMovement();
            HandleAnimation();
        }

        void HandleMovement()
        {
            if (inputDir != Vector2.zero)
            {
                lastDir = inputDir;
            }

            movement.Move(inputDir, player.Speed);
        }

        void HandleAnimation()
        {
            anim.SetFloat("MoveX", inputDir.x);
            anim.SetFloat("MoveY", inputDir.y);
            anim.SetFloat("LastMoveX", lastDir.x);
            anim.SetFloat("LastMoveY", lastDir.y);
            anim.SetBool("IsMoving", inputDir != Vector2.zero);
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

            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.y)
            );

            Vector3 spawnPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);

            var bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

            var bombData = new Domain.Bomb(gridPos, 2f, player.BombRange);

            bombObj.GetComponent<BombController>().Init(bombData, () => currentBomb--);

            currentBomb++;
        }

        void Die()
        {
            Debug.Log("Player died");
            Destroy(gameObject);
        }
    }
}