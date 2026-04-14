using System.Collections.Generic;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotRuntimeDebugLog
    {
        public static void LogBotDecision(string currentStateName, string candidateStateName, List<(string name, bool canEnter)> decisions) { }
        public static void LogMapSnapshot(MapContext mapContext) { }
        public static void LogBotSpawn(PlayerController player) { }
        public static void LogBotThink(PlayerController player, BotSenseContext sense, BotBlackboard blackboard, string stateName) { }
        public static void LogBotStateChange(PlayerController player, string previousState, string nextState, Vector3Int currentCell) { }
        public static void LogBotPath(PlayerController player, string stateName, List<Vector3Int> path, int pathIndex, Vector3Int? targetCell, Vector3Int? escapeCell) { }
        public static void LogBotMoveCommand(PlayerController player, Vector3Int currentCell, Vector3Int targetCell, Vector2 moveDirection, int pathIndex, int pathCount) { }
        public static void LogBotStop(PlayerController player, Vector3Int currentCell) { }
        public static void LogBotStuck(PlayerController player, Vector3Int currentCell, Vector3Int targetCell, int pathIndex, int pathCount) { }
        public static void LogBombPlaced(PlayerController player, Vector3Int cell, int range, int activeBombCount, int bombCapacity) { }
        public static void LogBombExploded(BombController bomb, Vector3Int cell, int range) { }
        public static void LogBlockDestroyed(Vector3Int cell) { }
    }
}
