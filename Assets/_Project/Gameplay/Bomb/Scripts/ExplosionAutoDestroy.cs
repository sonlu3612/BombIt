using UnityEngine;

namespace _Project.Gameplay.Bomb.Scripts
{
    public class ExplosionAutoDestroy : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject, 0.5f);
        }
    }
}