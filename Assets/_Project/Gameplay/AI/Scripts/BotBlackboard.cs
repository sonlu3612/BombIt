using System.Collections.Generic;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotBlackboard
    {
        public sealed class Snapshot
        {
            public Vector3Int? CurrentTargetCell;
            public List<Vector3Int> CurrentPath;
            public int CurrentPathIndex;
            public Vector3Int? EscapeCell;
            public List<Vector3Int> EscapePath;
            public Vector3Int? PlannedBombCell;
            public PlayerController TargetEnemy;
            public float LastThinkTime;
            public float LastBombTime;
            public string LastStateName;
            public Vector3 LastProgressPosition;
            public float LastProgressTime;
        }

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

        public Snapshot CreateSnapshot()
        {
            return new Snapshot
            {
                CurrentTargetCell = CurrentTargetCell,
                CurrentPath = CurrentPath != null ? new List<Vector3Int>(CurrentPath) : null,
                CurrentPathIndex = CurrentPathIndex,
                EscapeCell = EscapeCell,
                EscapePath = EscapePath != null ? new List<Vector3Int>(EscapePath) : null,
                PlannedBombCell = PlannedBombCell,
                TargetEnemy = TargetEnemy,
                LastThinkTime = LastThinkTime,
                LastBombTime = LastBombTime,
                LastStateName = LastStateName,
                LastProgressPosition = LastProgressPosition,
                LastProgressTime = LastProgressTime
            };
        }

        public void RestoreSnapshot(Snapshot snapshot)
        {
            if (snapshot == null)
                return;

            CurrentTargetCell = snapshot.CurrentTargetCell;
            CurrentPath = snapshot.CurrentPath != null ? new List<Vector3Int>(snapshot.CurrentPath) : null;
            CurrentPathIndex = snapshot.CurrentPathIndex;
            EscapeCell = snapshot.EscapeCell;
            EscapePath = snapshot.EscapePath != null ? new List<Vector3Int>(snapshot.EscapePath) : null;
            PlannedBombCell = snapshot.PlannedBombCell;
            TargetEnemy = snapshot.TargetEnemy;
            LastThinkTime = snapshot.LastThinkTime;
            LastBombTime = snapshot.LastBombTime;
            LastStateName = snapshot.LastStateName;
            LastProgressPosition = snapshot.LastProgressPosition;
            LastProgressTime = snapshot.LastProgressTime;
        }
    }
}
