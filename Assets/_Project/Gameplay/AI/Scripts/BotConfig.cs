using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    [System.Serializable]
    public class BotConfig
    {
        [Header("Think")]
        public float thinkInterval = 0.12f;
        public float repathInterval = 0.15f;

        [Header("Sense")]
        public int findRange = 8;
        public bool avoidDangerCells = true;

        [Header("Movement")]
        public float reachThreshold = 0.05f;

        [Header("Wander")]
        public int wanderMaxSearchCells = 24;
        public float idleChance = 0.08f;
        public Vector2 idleDurationRange = new Vector2(0.15f, 0.45f);

        [Header("Decisions")]
        public float itemChance = 0.85f;
        public float attackChance = 0.80f;
        public float breakBlockChance = 0.90f;

        [Header("Bomb")]
        public float bombCooldown = 0.35f;
        public int attackSearchRange = 8;
        public int escapeSearchRange = 8;
    }
}
