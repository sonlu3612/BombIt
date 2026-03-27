using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace Assets._Project.Gameplay.Bomb.Scripts
{
    public class ExplosionDamage : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
            }
        }
    }
}
