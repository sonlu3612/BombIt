using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.AI.Scripts
{
    public class BotStateMachine
    {
        private readonly List<IBotState> states;
        private readonly BotBlackboard blackboard;
        private IBotState currentState;

        public IBotState CurrentState => currentState;
        public event Action<IBotState, IBotState> StateChanged;

        public BotStateMachine(List<IBotState> states, BotBlackboard blackboard)
        {
            this.states = states;
            this.blackboard = blackboard;
        }

        public void Update(BotSenseContext sense)
        {
            int currentIndex = currentState != null ? states.IndexOf(currentState) : int.MaxValue;

            IBotState candidate = null;
            int candidateIndex = int.MaxValue;

            for (int i = 0; i < states.Count; i++)
            {
                if (!states[i].CanEnter(sense))
                    continue;

                candidate = states[i];
                candidateIndex = i;
                break;
            }

            if (currentState == null)
            {
                TryActivateFromIdle(candidate, sense);
            }
            else if (currentState.IsFinished)
            {
                IBotState finishedState = currentState;
                currentState.Exit();
                currentState = null;
                StateChanged?.Invoke(finishedState, null);
                TryActivateFromIdle(candidate, sense);
            }
            else if (candidate != null && candidate != currentState && candidateIndex < currentIndex)
            {
                TryPromoteHigherPriorityState(candidate, sense);
            }

            if (currentState == null)
                return;

            currentState.Tick(sense);

            if (currentState.IsFinished)
            {
                IBotState finishedState = currentState;
                currentState.Exit();
                currentState = null;
                StateChanged?.Invoke(finishedState, null);
            }
        }

        private void TryActivateFromIdle(IBotState candidate, BotSenseContext sense)
        {
            if (candidate == null)
                return;

            candidate.Enter(sense);
            if (candidate.IsFinished)
            {
                candidate.Exit();
                return;
            }

            currentState = candidate;
            StateChanged?.Invoke(null, currentState);
        }

        private void TryPromoteHigherPriorityState(IBotState candidate, BotSenseContext sense)
        {
            if (candidate == null || currentState == null)
                return;

            IBotState previousState = currentState;
            BotBlackboard.Snapshot snapshot = blackboard != null ? blackboard.CreateSnapshot() : null;

            previousState.Exit();
            candidate.Enter(sense);

            if (candidate.IsFinished)
            {
                candidate.Exit();
                blackboard?.RestoreSnapshot(snapshot);
                currentState = previousState;
                return;
            }

            currentState = candidate;
            StateChanged?.Invoke(previousState, currentState);
        }
    }

    public static class BotRuntimeDebugLog
    {
        private static readonly object Sync = new();
        private static readonly HashSet<int> DumpedMapContexts = new();

        private static bool initialized;
        private static string sessionFilePath;

        public static string SessionFilePath => sessionFilePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            initialized = false;
            sessionFilePath = null;
            DumpedMapContexts.Clear();
        }

        public static void EnsureSession()
        {
            if (initialized)
                return;

            lock (Sync)
            {
                if (initialized)
                    return;

                string directory = ResolveLogDirectory();
                Directory.CreateDirectory(directory);

                sessionFilePath = Path.Combine(directory, $"bot-debug-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                initialized = true;

                WriteRaw("SESSION", $"Created runtime debug log. Scene={SceneManager.GetActiveScene().name}");
                Debug.Log($"[{nameof(BotRuntimeDebugLog)}] Writing runtime debug log to: {sessionFilePath}");
            }
        }

        public static void LogMapSnapshot(MapContext mapContext)
        {
            if (mapContext == null)
                return;

            EnsureSession();

            lock (Sync)
            {
                if (!DumpedMapContexts.Add(mapContext.GetEntityId()))
                    return;
            }

            Tilemap referenceMap = mapContext.ReferenceTilemap != null ? mapContext.ReferenceTilemap : mapContext.WallTilemap;
            List<Vector3Int> wallCells = GetTileCells(mapContext.WallTilemap);
            List<Vector3Int> blockTileCells = GetTileCells(mapContext.BlockTilemap);
            List<Vector3Int> liveBlockCells = mapContext.MapBuilder != null ? mapContext.MapBuilder.GetActiveBlockCells() : new();

            WriteRaw("MAP", $"ReferenceTilemap={DescribeTilemap(referenceMap)}");
            WriteRaw("MAP", $"WallTilemap={DescribeTilemap(mapContext.WallTilemap)}");
            WriteRaw("MAP", $"BlockTilemap={DescribeTilemap(mapContext.BlockTilemap)}");
            WriteRaw("MAP", $"WallCoords count={wallCells.Count} coords={FormatCells(wallCells)}");
            WriteRaw("MAP", $"BlockTilemapCoords count={blockTileCells.Count} coords={FormatCells(blockTileCells)}");
            WriteRaw("MAP", $"LiveBlockCoords count={liveBlockCells.Count} coords={FormatCells(liveBlockCells)}");
        }

        public static void LogBotSpawn(PlayerController player)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw("BOT", $"{DescribePlayer(player)} SPAWN world={FormatVector(player.transform.position)} cell={FormatCell(player.GetCurrentCell())}");
        }

        public static void LogBotThink(PlayerController player, BotSenseContext sense, BotBlackboard blackboard, string stateName)
        {
            if (player == null || sense == null)
                return;

            EnsureSession();

            int pathCount = blackboard != null && blackboard.CurrentPath != null ? blackboard.CurrentPath.Count : 0;
            int pathIndex = blackboard != null ? blackboard.CurrentPathIndex : -1;
            string target = blackboard != null && blackboard.CurrentTargetCell.HasValue ? FormatCell(blackboard.CurrentTargetCell.Value) : "none";
            string escape = blackboard != null && blackboard.EscapeCell.HasValue ? FormatCell(blackboard.EscapeCell.Value) : "none";

            WriteRaw(
                "BOT",
                $"{DescribePlayer(player)} THINK state={stateName} cell={FormatCell(sense.CurrentCell)} logic={FormatCell(sense.LogicCell)} target={target} escape={escape} path={pathIndex}/{pathCount} danger={sense.DangerCells.Count} blocked={sense.BlockedCells.Count} items={sense.ItemCells.Count} enemies={sense.EnemyCells.Count} breakable={sense.BreakableBlocks.Count}");
        }

        public static void LogBotStateChange(PlayerController player, string previousState, string nextState, Vector3Int currentCell)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw("STATE", $"{DescribePlayer(player)} {previousState} -> {nextState} at {FormatCell(currentCell)}");
        }

        public static void LogBotPath(
            PlayerController player,
            string stateName,
            List<Vector3Int> path,
            int pathIndex,
            Vector3Int? targetCell,
            Vector3Int? escapeCell)
        {
            if (player == null)
                return;

            EnsureSession();

            string target = targetCell.HasValue ? FormatCell(targetCell.Value) : "none";
            string escape = escapeCell.HasValue ? FormatCell(escapeCell.Value) : "none";

            if (path == null || path.Count == 0)
            {
                WriteRaw("PATH", $"{DescribePlayer(player)} state={stateName} path=<none> target={target} escape={escape}");
                return;
            }

            WriteRaw(
                "PATH",
                $"{DescribePlayer(player)} state={stateName} pathIndex={pathIndex}/{path.Count} target={target} escape={escape} path={FormatCells(path)}");
        }

        public static void LogBotMoveCommand(PlayerController player, Vector3Int currentCell, Vector3Int targetCell, Vector2 moveDirection, int pathIndex, int pathCount)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "MOVE",
                $"{DescribePlayer(player)} from={FormatCell(currentCell)} toward={FormatCell(targetCell)} dir={FormatVector(moveDirection)} path={pathIndex}/{pathCount} world={FormatVector(player.GetNavigationWorldPosition())}");
        }

        public static void LogBotStop(PlayerController player, Vector3Int currentCell)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw("MOVE", $"{DescribePlayer(player)} STOP at {FormatCell(currentCell)} world={FormatVector(player.GetNavigationWorldPosition())}");
        }

        public static void LogBotStuck(PlayerController player, Vector3Int currentCell, Vector3Int targetCell, int pathIndex, int pathCount)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "MOVE",
                $"{DescribePlayer(player)} STUCK at={FormatCell(currentCell)} target={FormatCell(targetCell)} path={pathIndex}/{pathCount} world={FormatVector(player.GetNavigationWorldPosition())}");
        }

        public static void LogBombPlaced(PlayerController player, Vector3Int cell, int range, int activeBombCount, int bombCapacity)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "BOMB",
                $"{DescribePlayer(player)} PLACE cell={FormatCell(cell)} range={range} activeBombs={activeBombCount}/{bombCapacity}");
        }

        public static void LogBombExploded(BombController bomb, Vector3Int cell, int range)
        {
            EnsureSession();
            string bombName = bomb != null ? $"{bomb.name}#{bomb.GetEntityId()}" : "Bomb";
            WriteRaw("BOMB", $"{bombName} EXPLODE cell={FormatCell(cell)} range={range}");
        }

        public static void LogBlockDestroyed(Vector3Int cell)
        {
            EnsureSession();
            WriteRaw("MAP", $"DESTROY_BLOCK cell={FormatCell(cell)}");
        }

        private static void WriteRaw(string category, string message)
        {
            try
            {
                EnsureSession();

                string line = $"{DateTime.Now:HH:mm:ss.fff} [{category}] {message}{Environment.NewLine}";
                lock (Sync)
                {
                    File.AppendAllText(sessionFilePath, line, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(BotRuntimeDebugLog)}] Failed to write debug log: {ex.Message}");
            }
        }

        private static string ResolveLogDirectory()
        {
            try
            {
                string projectDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.Combine(projectDirectory, "RuntimeDebugLogs");
            }
            catch
            {
                return Path.Combine(Application.persistentDataPath, "RuntimeDebugLogs");
            }
        }

        private static string DescribeTilemap(Tilemap tilemap)
        {
            if (tilemap == null)
                return "none";

            BoundsInt bounds = tilemap.cellBounds;
            return $"{tilemap.name} bounds=({bounds.xMin},{bounds.yMin}) -> ({bounds.xMax},{bounds.yMax})";
        }

        private static List<Vector3Int> GetTileCells(Tilemap tilemap)
        {
            List<Vector3Int> cells = new();
            if (tilemap == null)
                return cells;

            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cell))
                    cells.Add(cell);
            }

            cells.Sort(CompareCells);
            return cells;
        }

        private static int CompareCells(Vector3Int left, Vector3Int right)
        {
            int yCompare = right.y.CompareTo(left.y);
            if (yCompare != 0)
                return yCompare;

            int xCompare = left.x.CompareTo(right.x);
            if (xCompare != 0)
                return xCompare;

            return left.z.CompareTo(right.z);
        }

        private static string DescribePlayer(PlayerController player)
        {
            return $"{player.name}#{player.GetEntityId()}";
        }

        private static string FormatCell(Vector3Int cell)
        {
            return $"({cell.x},{cell.y},{cell.z})";
        }

        private static string FormatVector(Vector2 value)
        {
            return $"({value.x:0.###},{value.y:0.###})";
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";
        }

        private static string FormatCells(IReadOnlyList<Vector3Int> cells)
        {
            if (cells == null || cells.Count == 0)
                return "[]";

            StringBuilder builder = new();
            builder.Append('[');

            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(FormatCell(cells[i]));
            }

            builder.Append(']');
            return builder.ToString();
        }
    }
}
