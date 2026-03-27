using UnityEngine;
namespace _Project.Domain
{
    public class Player
    {
        public int Health { get; private set; } = 1;
        public float Speed { get; private set; } = 3f;
        public int BombRange { get; private set; } = 1;
        public int BombCount { get; private set; } = 1;

        public void TakeDamage(int amount = 1)
        {
            Health -= amount;
        }

        public void IncreaseSpeed(float amount)
        {
            Speed += amount;
        }

        public void IncreaseBombRange()
        {
            BombRange++;
        }

        public void IncreaseBombCount()
        {
            BombCount++;
        }

        public Player()
        {
            Health = 1;
            Speed = 3f;
            BombRange = 1;
            BombCount = 1;
        }
    }
}