using System;
using System.IO;
using System.Text;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Gameplay.AI.Scripts
{
    public static class BotMovementTraceLog
    {
        private static readonly object Sync = new();

        private static bool initialized;
        private static string sessionFilePath;

        public static string SessionFilePath => sessionFilePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            initialized = false;
            sessionFilePath = null;
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

                sessionFilePath = Path.Combine(directory, $"bot-movement-trace-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                initialized = true;

                WriteRaw("SESSION", $"Created movement trace log. Scene={SceneManager.GetActiveScene().name}");
                Debug.Log($"[{nameof(BotMovementTraceLog)}] Writing movement trace log to: {sessionFilePath}");
            }
        }

        public static void LogPlayerMovement(
            PlayerController player,
            string phase,
            Vector3Int logicCell,
            Vector3Int occupancyCell,
            Vector3Int? targetCell,
            Vector3 navPosition,
            Vector3 navAnchor,
            Vector2 requestedDirection,
            Vector2 currentMoveDirection,
            bool isMoving,
            string note = null)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "MOTOR",
                $"{DescribePlayer(player)} phase={phase} logic={FormatCell(logicCell)} occupancy={FormatCell(occupancyCell)} target={FormatNullableCell(targetCell)} nav={FormatVector(navPosition)} anchor={FormatVector(navAnchor)} req={FormatVector(requestedDirection)} move={FormatVector(currentMoveDirection)} moving={isMoving} note={note ?? "-"}");
        }

        public static void LogBlockedAttempt(
            PlayerController player,
            Vector3Int logicCell,
            Vector3Int occupancyCell,
            Vector3Int attemptedCell,
            Vector2 requestedDirection,
            bool isWithinBounds,
            bool staticallyBlocked,
            bool dynamicallyBlocked)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "BLOCK",
                $"{DescribePlayer(player)} logic={FormatCell(logicCell)} occupancy={FormatCell(occupancyCell)} next={FormatCell(attemptedCell)} req={FormatVector(requestedDirection)} withinBounds={isWithinBounds} staticBlocked={staticallyBlocked} dynamicBlocked={dynamicallyBlocked}");
        }

        public static void LogExecutorDecision(
            PlayerController player,
            Vector3Int logicCell,
            Vector3Int occupancyCell,
            Vector3Int targetCell,
            Vector3 worldPosition,
            Vector2 delta,
            Vector2 alignDelta,
            Vector2 moveDirection,
            bool usedAlignment,
            bool targetWithinBounds,
            bool targetWalkable,
            int pathIndex,
            int pathCount)
        {
            if (player == null)
                return;

            EnsureSession();
            WriteRaw(
                "EXEC",
                $"{DescribePlayer(player)} logic={FormatCell(logicCell)} occupancy={FormatCell(occupancyCell)} target={FormatCell(targetCell)} world={FormatVector(worldPosition)} delta={FormatVector(delta)} align={FormatVector(alignDelta)} dir={FormatVector(moveDirection)} alignment={usedAlignment} targetWithinBounds={targetWithinBounds} targetWalkable={targetWalkable} path={pathIndex}/{pathCount}");
        }

        public static void LogPathSummary(
            string phase,
            Vector3Int start,
            Vector3Int target,
            MapContext mapContext,
            int visitedCount,
            int rejectedSolidCount,
            int rejectedBlockedCount,
            int rejectedDangerCount,
            int pathCount)
        {
            EnsureSession();
            WriteRaw(
                "PATH",
                $"phase={phase} start={FormatCell(start)} target={FormatCell(target)} withinStart={BotGridUtility.IsWithinBounds(start, mapContext)} withinTarget={BotGridUtility.IsWithinBounds(target, mapContext)} visited={visitedCount} solid={rejectedSolidCount} blocked={rejectedBlockedCount} danger={rejectedDangerCount} pathCount={pathCount}");
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
                Debug.LogError($"[{nameof(BotMovementTraceLog)}] Failed to write movement trace log: {ex.Message}");
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

        private static string DescribePlayer(PlayerController player)
        {
            return $"{player.name}#{player.GetEntityId()}";
        }

        private static string FormatNullableCell(Vector3Int? cell)
        {
            return cell.HasValue ? FormatCell(cell.Value) : "none";
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
    }
}
