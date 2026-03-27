using UnityEngine;
namespace _Project.Domain
{
    public class Player
    {
        public int health { get; set; }
        public float speed { get; set; }
        public int bombRange { get; set; }
        public int bombCount { get; set; }

        public Player()
        {
            health = 1;
            speed = 5.0f;
            bombRange = 2;
            bombCount = 1;
        }
        public void TakeDamage()
        {
            health -= 1;
            Debug.Log($"Player took 1 damage. Remaining health: {health}");
        }
        public void setSpeed(float newSpeed)
        {
            speed = newSpeed;
            Debug.Log($"Player speed set to: {speed}");
        }
        public void setBombRange(int newRange)
        {
            bombRange = newRange;
            Debug.Log($"Player bomb range set to: {bombRange}");
        }
        public void setBombCount(int newCount)
        {
            bombCount = newCount;
            Debug.Log($"Player bomb count set to: {bombCount}");
        }
        public void setHealth(int newHealth)
        {
            health = newHealth;
            Debug.Log($"Player health set to: {health}");
        }
        public void getStatus()
        {
            Debug.Log($"Player Status - Health: {health}, Speed: {speed}, Bomb Range: {bombRange}, Bomb Count: {bombCount}");
        }
    }
}