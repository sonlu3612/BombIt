using _Project.Gameplay.Bomb.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

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

            // chặn đi chéo
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

            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.y)
            );

            Vector3 spawnPos = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0);

            GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

            Domain.Bomb bombData = new Domain.Bomb(gridPos, 2f, player.BombRange);

            bombObj.GetComponent<BombController>().Init(bombData, () => currentBomb--);

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