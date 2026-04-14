using UnityEngine;

namespace _Project.Domain
{
    public class Player
    {
        public int Health { get; private set; } = 1;
        public float Speed { get; private set; } = 2f;
        public int BombRange { get; private set; } = 1;
        public int BombCount { get; private set; } = 1;

        public void TakeDamage(int amount = 1)
        {
            Health -= amount;
        }

        public void AddHealth(int amount)
        {
            Health += amount;
        }

        public void AddSpeed(float amount)
        {
            Speed += amount;
        }

        public void AddBombRange(int amount)
        {
            BombRange += amount;
        }

        public void AddBombCount(int amount)
        {
            BombCount += amount;
        }

        public Player()
        {
            Health = 1;
            Speed = 2f;
            BombRange = 1;
            BombCount = 1;
        }
    }
}