using UnityEngine;

namespace _Project.Domain
{
    public class Bomb
    {
        public Vector2Int position { get; set; }
        public int timer { get; set; }
        public Bomb()
        {
            position = Vector2Int.zero;
            timer = 3;
        }
        public void PlaceBomb(Vector2Int newPosition)
        {
            position = newPosition;
            timer = 3; // Bomb will explode after 3 turns
            Debug.Log($"Bomb placed at {position} with timer set to {timer}");
        }

        public void Tick()
        {
            if (timer > 0)
            {
                timer -= 1;
                Debug.Log($"Bomb at {position} ticking down. Timer: {timer}");
            }
            else
            {
                Explode();
            }
        }

        public void Explode()
        {
            Debug.Log($"Bomb at {position} exploded!");
            // Here you would add logic to damage players and destroy destructible walls within the bomb's range
        }

        public Vector2Int getPoisition()
        {
            return position;
        }
    }
}
