using System.Collections.Generic;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotBlackboard
    {
        public Vector3Int? CurrentTargetCell { get; set; }
        public List<Vector3Int> CurrentPath { get; set; }
        public int CurrentPathIndex { get; set; }

        public Vector3Int? EscapeCell { get; set; }
        public List<Vector3Int> EscapePath { get; set; }
        public Vector3Int? PlannedBombCell { get; set; }

        public PlayerController TargetEnemy { get; set; }

        public float LastThinkTime { get; set; }
        public float LastBombTime { get; set; }
        public string LastStateName { get; set; }
        public Vector3 LastProgressPosition { get; set; }
        public float LastProgressTime { get; set; }

        public void SetPath(List<Vector3Int> path)
        {
            CurrentPath = path;
            CurrentPathIndex = path != null && path.Count > 1 ? 1 : 0;
            CurrentTargetCell = path != null && path.Count > 0 ? path[path.Count - 1] : (Vector3Int?)null;
            LastProgressPosition = default;
            LastProgressTime = 0f;
        }

        public void ClearPath()
        {
            CurrentPath = null;
            CurrentPathIndex = 0;
            CurrentTargetCell = null;
            LastProgressPosition = default;
            LastProgressTime = 0f;
        }

        public void ClearEscapePlan()
        {
            EscapeCell = null;
            EscapePath = null;
            PlannedBombCell = null;
        }
    }
}
