using UnityEngine;

namespace _Project.Gameplay.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        public void Move(Vector2 dir, float speed)
        {
            transform.Translate(dir.normalized * speed * Time.deltaTime);
        }
    }
}