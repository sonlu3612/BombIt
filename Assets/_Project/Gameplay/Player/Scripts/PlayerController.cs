using UnityEngine;
using _Project.Domain;

namespace _Project.Gameplay.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        private Player player;
        private Animator anim;
        private PlayerMovement movement;

        public Vector2 inputDir; 
        private Vector2 lastDir = Vector2.down;

        void Start()
        {
            player = new Player();
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

        void Die()
        {
            Debug.Log("Player died");
            Destroy(gameObject);
        }
    }
}