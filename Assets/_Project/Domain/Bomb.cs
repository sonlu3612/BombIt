using UnityEngine;

namespace _Project.Domain
{
    public class Bomb
    {
        public Vector2Int position { get; private set; }
        public float explodeTime { get; private set; }
        public int range { get; private set; }

        public Bomb(Vector2Int pos, float time, int bombRange)
        {
            position = pos;
            explodeTime = time;
            range = bombRange;
        }
    }
}