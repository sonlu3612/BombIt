using System.Collections.Generic;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotSenseContext
    {
        public Vector3Int CurrentCell;
        public Vector3Int LogicCell;

        public HashSet<Vector3Int> DangerCells = new();
        public HashSet<Vector3Int> BlockedCells = new();
        public Dictionary<Vector3Int, float> DangerTimes = new();

        public List<Vector3Int> SafeCells = new();
        public List<Vector3Int> FreeCells = new();
        public List<Vector3Int> BreakableBlocks = new();
        public List<Vector3Int> ItemCells = new();
        public List<Vector3Int> EnemyCells = new();

        public List<PlayerController> EnemyPlayers = new();
        public List<BombController> ActiveBombs = new();

        public bool IsInDanger => DangerCells.Contains(CurrentCell);
    }
}
