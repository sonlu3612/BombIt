using _Project.Gameplay.AI.Scripts.States;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.AI.Scripts
{
    [RequireComponent(typeof(PlayerController))]
    public class BotBrain : MonoBehaviour
    {
        [SerializeField] private BotConfig config = new();

        [Header("Debug Draw")]
        [SerializeField] private bool debugDrawAI = true;
        [SerializeField] private bool debugDrawSense = true;
        [SerializeField] private bool debugDrawPath = true;
        [SerializeField] private bool debugDrawSearch = true;
        [SerializeField] private bool debugDrawBlocked = true;
        [SerializeField] private bool debugDrawDanger = true;
        [SerializeField] private bool debugDrawSafeCells = false;
        [SerializeField] private bool debugDrawVisitedCells = false;
        [SerializeField] private float debugCubeSize = 0.72f;

        private PlayerController playerController;
        private MapContext mapContext;

        private BotBlackboard blackboard;
        private BotNavigator navigator;
        private BotActionExecutor executor;
        private BotStateMachine stateMachine;
        private BotSenseContext lastSense;
        private string lastLoggedPathSignature = string.Empty;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            mapContext = FindObjectOfType<MapContext>();

            if (playerController == null)
            {
                Debug.LogError($"[{nameof(BotBrain)}] Missing PlayerController on {gameObject.name}", this);
                enabled = false;
                return;
            }

            if (mapContext == null)
            {
                Debug.LogError($"[{nameof(BotBrain)}] No MapContext found in scene.", this);
                enabled = false;
                return;
            }

            blackboard = new BotBlackboard();
            navigator = new BotNavigator(mapContext);
            executor = new BotActionExecutor(
                playerController,
                mapContext.ReferenceTilemap != null ? mapContext.ReferenceTilemap : mapContext.WallTilemap,
                config.reachThreshold);

            List<IBotState> states = new()
            {
                new EscapeAfterBombState(blackboard, navigator, executor),
                new EvadeBombState(blackboard, navigator, executor),
                new PlantBombState(blackboard, navigator, executor, config),
                new AttackEnemyState(blackboard, navigator, executor, config),
                new GetItemState(blackboard, navigator, executor, config),
                new BreakBlockState(blackboard, navigator, executor, config),
                new WanderState(blackboard, navigator, executor, config),
                new IdleState(blackboard, executor, config)
            };

            stateMachine = new BotStateMachine(states, blackboard);
            stateMachine.StateChanged += OnStateChanged;

            BotRuntimeDebugLog.EnsureSession();
            BotRuntimeDebugLog.LogBotSpawn(playerController);
        }

        private void OnDestroy()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (blackboard == null)
                return;

            if (Time.time < blackboard.LastThinkTime + config.thinkInterval)
                return;

            blackboard.LastThinkTime = Time.time;

            BotSenseContext sense = BotSenseBuilder.Build(playerController, mapContext, config);
            lastSense = sense;
            stateMachine.Update(sense);

            BotRuntimeDebugLog.LogBotThink(
                playerController,
                sense,
                blackboard,
                stateMachine.CurrentState != null ? stateMachine.CurrentState.Name : "None");

            LogPathIfChanged();
        }

        private void OnStateChanged(IBotState previousState, IBotState nextState)
        {
            BotRuntimeDebugLog.LogBotStateChange(
                playerController,
                previousState != null ? previousState.Name : "None",
                nextState != null ? nextState.Name : "None",
                playerController != null ? playerController.GetCurrentCell() : Vector3Int.zero);
        }

        private void LogPathIfChanged()
        {
            string signature = BuildPathSignature();
            if (signature == lastLoggedPathSignature)
                return;

            lastLoggedPathSignature = signature;
            BotRuntimeDebugLog.LogBotPath(
                playerController,
                stateMachine != null && stateMachine.CurrentState != null ? stateMachine.CurrentState.Name : "None",
                blackboard != null ? blackboard.CurrentPath : null,
                blackboard != null ? blackboard.CurrentPathIndex : -1,
                blackboard != null ? blackboard.CurrentTargetCell : null,
                blackboard != null ? blackboard.EscapeCell : null);
        }

        private string BuildPathSignature()
        {
            if (blackboard == null || blackboard.CurrentPath == null || blackboard.CurrentPath.Count == 0)
                return "none";

            return $"{blackboard.CurrentPathIndex}|{string.Join(">", blackboard.CurrentPath)}|target={blackboard.CurrentTargetCell}|escape={blackboard.EscapeCell}";
        }

        private void OnDrawGizmos()
        {
            if (!debugDrawAI || !Application.isPlaying)
                return;

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (mapContext == null)
                mapContext = FindObjectOfType<MapContext>();

            Tilemap tilemap = mapContext != null && mapContext.ReferenceTilemap != null
                ? mapContext.ReferenceTilemap
                : mapContext != null ? mapContext.WallTilemap : null;

            if (tilemap == null)
                return;

            if (debugDrawSense && lastSense != null)
            {
                if (debugDrawDanger)
                    DrawCells(tilemap, lastSense.DangerCells, new Color(1f, 0.2f, 0.2f, 0.85f), debugCubeSize * 0.92f);

                if (debugDrawBlocked)
                    DrawCells(tilemap, lastSense.BlockedCells, new Color(0.35f, 0.35f, 0.35f, 0.9f), debugCubeSize * 0.82f);

                if (debugDrawSafeCells)
                    DrawCells(tilemap, lastSense.SafeCells, new Color(0.2f, 0.9f, 0.35f, 0.45f), debugCubeSize * 0.4f);

                DrawCell(tilemap, lastSense.CurrentCell, Color.green, debugCubeSize);
            }

            if (debugDrawPath && blackboard != null)
            {
                if (blackboard.CurrentTargetCell.HasValue)
                    DrawCell(tilemap, blackboard.CurrentTargetCell.Value, Color.cyan, debugCubeSize * 0.95f);

                if (blackboard.EscapeCell.HasValue)
                    DrawCell(tilemap, blackboard.EscapeCell.Value, Color.blue, debugCubeSize * 0.95f);

                if (blackboard.CurrentPath != null && blackboard.CurrentPath.Count > 0)
                {
                    DrawPath(tilemap, blackboard.CurrentPath, new Color(1f, 0.6f, 0.1f, 1f));

                    int currentIndex = Mathf.Clamp(blackboard.CurrentPathIndex, 0, blackboard.CurrentPath.Count - 1);
                    DrawCell(tilemap, blackboard.CurrentPath[currentIndex], new Color(1f, 0f, 1f, 1f), debugCubeSize * 0.9f);
                }
            }

            if (debugDrawSearch && navigator != null)
            {
                if (navigator.LastStart.HasValue)
                    DrawCell(tilemap, navigator.LastStart.Value, new Color(1f, 1f, 0.1f, 1f), debugCubeSize * 0.9f);

                if (navigator.LastTarget.HasValue)
                    DrawCell(tilemap, navigator.LastTarget.Value, new Color(1f, 0.5f, 0f, 1f), debugCubeSize * 0.9f);

                if (debugDrawVisitedCells)
                    DrawCells(tilemap, navigator.LastVisited, new Color(1f, 1f, 0f, 0.35f), debugCubeSize * 0.28f);

                DrawCells(tilemap, navigator.LastRejectedSolid, new Color(0.55f, 0.1f, 0.1f, 0.45f), debugCubeSize * 0.38f);
                DrawCells(tilemap, navigator.LastRejectedBlocked, new Color(0.4f, 0.2f, 0.75f, 0.45f), debugCubeSize * 0.34f);
                DrawCells(tilemap, navigator.LastRejectedDanger, new Color(1f, 0.1f, 0.65f, 0.45f), debugCubeSize * 0.3f);

                if (navigator.LastPath != null && navigator.LastPath.Count > 0)
                    DrawPath(tilemap, navigator.LastPath, new Color(1f, 1f, 0f, 0.75f));
            }
        }

        private void DrawCells(Tilemap tilemap, IEnumerable<Vector3Int> cells, Color color, float size)
        {
            Gizmos.color = color;
            foreach (Vector3Int cell in cells)
            {
                Vector3 center = tilemap.GetCellCenterWorld(cell);
                center.z = 0f;
                Gizmos.DrawWireCube(center, new Vector3(size, size, 0.02f));
            }
        }

        private void DrawCell(Tilemap tilemap, Vector3Int cell, Color color, float size)
        {
            Gizmos.color = color;
            Vector3 center = tilemap.GetCellCenterWorld(cell);
            center.z = 0f;
            Gizmos.DrawWireCube(center, new Vector3(size, size, 0.02f));
        }

        private void DrawPath(Tilemap tilemap, List<Vector3Int> path, Color color)
        {
            if (path == null || path.Count == 0)
                return;

            Gizmos.color = color;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 center = tilemap.GetCellCenterWorld(path[i]);
                center.z = 0f;
                Gizmos.DrawSphere(center, 0.08f);

                if (i + 1 < path.Count)
                {
                    Vector3 next = tilemap.GetCellCenterWorld(path[i + 1]);
                    next.z = 0f;
                    Gizmos.DrawLine(center, next);
                }
            }
        }
    }
}


