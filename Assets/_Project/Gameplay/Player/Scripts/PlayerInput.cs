using UnityEngine;

namespace _Project.Gameplay.Scripts
{
    public class PlayerInput : MonoBehaviour
    {
        private PlayerController controller;

        void Start()
        {
            controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector2 dir = new Vector2(h, v);

            if (dir.x != 0)
            {
                dir.y = 0;
            }

            controller.inputDir = dir;
        }
    }
}