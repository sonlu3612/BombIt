using System.Collections;
using System.Collections.Generic;
using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Block.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using _Project.Gameplay.UI.Scripts;
using _Project.Gameplay.Match.Scripts;
using _Project.Systems.GameFlow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PlayerSpawner : MonoBehaviour
{
    private enum RoundOutcome
    {
        None = 0,
        Win = 1,
        Lose = 2
    }

    [Header("Legacy Spawn")]
    public GameObject playerPrefab;
    public int mapWidth = 17;
    public int mapHeight = 15;

    [Header("Session Spawn")]
    [SerializeField] private GameFlowConfig gameFlowConfig;
    [SerializeField] private bool preferGameSession = true;
    [SerializeField] private bool destroyExistingScenePlayers = true;
    [SerializeField] private float roundTransitionDelay = 1.5f;
    [SerializeField] private float resultDisplayDuration = 5f;
    [SerializeField] private float roundIntroDuration = 2f;
    [SerializeField] private int roundIntroBlinkCount = 6;

    [Header("Custom Spawn Overrides")]
    [SerializeField] private bool useCustomWorldSpawnPoints;
    [SerializeField] private Vector3[] customWorldSpawnPoints;

    [Header("Debug Spawn")]
    [SerializeField] private bool drawSpawnGizmos = true;
    [SerializeField] private Color spawnGizmoColor = Color.green;
    [SerializeField] [TextArea(4, 12)] private string lastResolvedSpawnInfo;

    private readonly List<GameObject> spawnedActors = new();
    private Vector2Int[] spawnPoints;
    private Vector3[] spawnWorldPositions;
    private MapContext mapContext;
    private MapRoundHudController roundHudController;
    private MatchResultOverlayController resultOverlayController;
    private bool sessionModeActive;
    private bool roundEnding;
    private Coroutine roundIntroRoutine;

    private void Start()
    {
        mapContext = Object.FindAnyObjectByType<MapContext>();
        roundHudController = Object.FindAnyObjectByType<MapRoundHudController>();
        resultOverlayController = Object.FindAnyObjectByType<MatchResultOverlayController>();
        ActorHudRegistry.Clear();
        InitSpawnPoints();
        UpdateResolvedSpawnInfo();

        if (preferGameSession && GameSession.IsConfigured && gameFlowConfig != null)
        {
            sessionModeActive = TrySpawnSessionActors();
            if (sessionModeActive)
                BeginRoundIntro();

            if (sessionModeActive)
                return;
        }

        SpawnLegacyPlayers();
        BeginRoundIntro();
    }

    [ContextMenu("Debug/Refresh Spawn Info")]
    private void RefreshSpawnInfo()
    {
        mapContext = FindMapContextForDebug();
        InitSpawnPoints();
        UpdateResolvedSpawnInfo();

        if (!string.IsNullOrWhiteSpace(lastResolvedSpawnInfo))
            Debug.Log(lastResolvedSpawnInfo, this);
    }

    private void OnDrawGizmos()
    {
        if (!drawSpawnGizmos)
            return;

        MapContext debugMapContext = Application.isPlaying ? mapContext : FindMapContextForDebug();
        Vector2Int[] debugSpawnPoints = BuildSpawnPointsForDebug(debugMapContext);
        if (debugSpawnPoints == null)
            return;

        EnsureSpawnWorldPositions(debugMapContext, debugSpawnPoints);

        Gizmos.color = spawnGizmoColor;

        for (int i = 0; i < debugSpawnPoints.Length; i++)
        {
            Vector3 worldPosition = GetSpawnWorldPosition(i, debugMapContext);
            Gizmos.DrawWireSphere(worldPosition, 0.3f);
            Gizmos.DrawLine(worldPosition + Vector3.left * 0.35f, worldPosition + Vector3.right * 0.35f);
            Gizmos.DrawLine(worldPosition + Vector3.up * 0.35f, worldPosition + Vector3.down * 0.35f);
        }
    }

    private void Update()
    {
        if (!sessionModeActive || roundEnding || RoundIntroState.IsActive)
            return;

        RoundOutcome outcome = EvaluateRoundOutcome();
        if (outcome != RoundOutcome.None)
        {
            StartCoroutine(HandleRoundEnd(outcome));
            return;
        }

        if (roundHudController != null && roundHudController.IsExpired)
        {
            StartCoroutine(HandleRoundEnd(RoundOutcome.Lose));
            return;
        }

        if (GameSession.EnemyCount > 0)
            return;

        int aliveActors = CountAliveActors();
        if (aliveActors > 1)
            return;

        StartCoroutine(HandleRoundEnd(RoundOutcome.None));
    }

    private void InitSpawnPoints()
    {
        if (TryInitCustomWorldSpawnPoints())
            return;

        if (TryInitSpawnPointsFromTilemap())
            return;

        spawnPoints = new[]
        {
            new Vector2Int(-mapWidth / 2 + 1, -mapHeight / 2 + 1),
            new Vector2Int(mapWidth / 2 - 1, -mapHeight / 2 + 1),
            new Vector2Int(-mapWidth / 2 + 1, mapHeight / 2 - 1),
            new Vector2Int(mapWidth / 2 - 1, mapHeight / 2 - 1)
        };

        ResolveSpawnWorldPositions(mapContext);
    }

    private Vector2Int[] BuildSpawnPointsForDebug(MapContext debugMapContext)
    {
        if (TryBuildCustomSpawnPoints(debugMapContext, out Vector2Int[] customPoints))
            return customPoints;

        Tilemap tilemap = null;

        if (debugMapContext != null)
            tilemap = debugMapContext.ReferenceTilemap != null ? debugMapContext.ReferenceTilemap : debugMapContext.WallTilemap;

        if (tilemap != null)
        {
            BoundsInt bounds = tilemap.cellBounds;

            int left = bounds.xMin + 1;
            int right = bounds.xMax - 2;
            int bottom = bounds.yMin + 1;
            int top = bounds.yMax - 2;

            return new[]
            {
                new Vector2Int(left, bottom),
                new Vector2Int(right, bottom),
                new Vector2Int(left, top),
                new Vector2Int(right, top)
            };
        }

        return new[]
        {
            new Vector2Int(-mapWidth / 2 + 1, -mapHeight / 2 + 1),
            new Vector2Int(mapWidth / 2 - 1, -mapHeight / 2 + 1),
            new Vector2Int(-mapWidth / 2 + 1, mapHeight / 2 - 1),
            new Vector2Int(mapWidth / 2 - 1, mapHeight / 2 - 1)
        };
    }

    private bool TryBuildCustomSpawnPoints(MapContext context, out Vector2Int[] customPoints)
    {
        customPoints = null;

        if (!HasCustomWorldSpawnPoints())
            return false;

        customPoints = new Vector2Int[customWorldSpawnPoints.Length];
        for (int i = 0; i < customWorldSpawnPoints.Length; i++)
            customPoints[i] = ResolveCellForWorldSpawn(customWorldSpawnPoints[i], context);

        return true;
    }

    private bool HasCustomWorldSpawnPoints()
    {
        return useCustomWorldSpawnPoints
            && customWorldSpawnPoints != null
            && customWorldSpawnPoints.Length >= 1;
    }

    private bool TryInitCustomWorldSpawnPoints()
    {
        if (!HasCustomWorldSpawnPoints())
            return false;

        spawnPoints = new Vector2Int[customWorldSpawnPoints.Length];
        spawnWorldPositions = new Vector3[customWorldSpawnPoints.Length];

        for (int i = 0; i < customWorldSpawnPoints.Length; i++)
        {
            Vector3 worldPosition = customWorldSpawnPoints[i];
            worldPosition.z = 0f;

            spawnWorldPositions[i] = worldPosition;
            spawnPoints[i] = ResolveCellForWorldSpawn(worldPosition, mapContext);
        }

        return true;
    }

    private bool TryInitSpawnPointsFromTilemap()
    {
        Tilemap tilemap = null;

        if (mapContext != null)
            tilemap = mapContext.ReferenceTilemap != null ? mapContext.ReferenceTilemap : mapContext.WallTilemap;

        if (tilemap == null)
            return false;

        BoundsInt bounds = tilemap.cellBounds;

        int left = bounds.xMin + 1;
        int right = bounds.xMax - 2;
        int bottom = bounds.yMin + 1;
        int top = bounds.yMax - 2;

        spawnPoints = new[]
        {
            new Vector2Int(left, bottom),
            new Vector2Int(right, bottom),
            new Vector2Int(left, top),
            new Vector2Int(right, top)
        };

        ResolveSpawnWorldPositions(mapContext);
        return true;
    }

    private void UpdateResolvedSpawnInfo()
    {
        MapContext debugMapContext = mapContext != null ? mapContext : FindMapContextForDebug();
        Vector2Int[] debugSpawnPoints = spawnPoints != null && spawnPoints.Length > 0
            ? spawnPoints
            : BuildSpawnPointsForDebug(debugMapContext);

        if (debugSpawnPoints == null || debugSpawnPoints.Length == 0)
        {
            lastResolvedSpawnInfo = "No spawn points resolved.";
            return;
        }

        EnsureSpawnWorldPositions(debugMapContext, debugSpawnPoints);

        System.Text.StringBuilder builder = new();
        builder.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
        builder.AppendLine($"Using Custom Spawn Points: {HasCustomWorldSpawnPoints()}");

        if (debugMapContext != null)
        {
            Tilemap tilemap = debugMapContext.ReferenceTilemap != null
                ? debugMapContext.ReferenceTilemap
                : debugMapContext.WallTilemap;

            if (tilemap != null)
                builder.AppendLine($"Tilemap Bounds: x[{tilemap.cellBounds.xMin}..{tilemap.cellBounds.xMax - 1}] y[{tilemap.cellBounds.yMin}..{tilemap.cellBounds.yMax - 1}]");
        }

        for (int i = 0; i < debugSpawnPoints.Length; i++)
        {
            Vector3 worldPosition = GetSpawnWorldPosition(i, debugMapContext);
            builder.AppendLine($"Spawn {i + 1}: cell=({debugSpawnPoints[i].x}, {debugSpawnPoints[i].y}) world=({worldPosition.x:0.###}, {worldPosition.y:0.###}, {worldPosition.z:0.###})");
        }

        lastResolvedSpawnInfo = builder.ToString();
    }

    private void EnsureSpawnWorldPositions(MapContext context, Vector2Int[] points)
    {
        if (HasCustomWorldSpawnPoints())
        {
            if (spawnWorldPositions == null || spawnWorldPositions.Length != customWorldSpawnPoints.Length)
            {
                spawnWorldPositions = new Vector3[customWorldSpawnPoints.Length];
                for (int i = 0; i < customWorldSpawnPoints.Length; i++)
                {
                    Vector3 worldPosition = customWorldSpawnPoints[i];
                    worldPosition.z = 0f;
                    spawnWorldPositions[i] = worldPosition;
                }
            }

            return;
        }

        if (points == null)
        {
            spawnWorldPositions = null;
            return;
        }

        if (spawnWorldPositions != null && spawnWorldPositions.Length == points.Length)
            return;

        spawnWorldPositions = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            spawnWorldPositions[i] = GetWorldPositionForPoint(points[i], context);
    }

    private Vector3 GetWorldPositionForPoint(Vector2Int point, MapContext context)
    {
        Tilemap tilemap = null;

        if (context != null)
            tilemap = context.ReferenceTilemap != null ? context.ReferenceTilemap : context.WallTilemap;

        if (tilemap != null)
        {
            Vector3 worldPosition = tilemap.GetCellCenterWorld(new Vector3Int(point.x, point.y, 0));
            worldPosition.z = 0f;
            return worldPosition;
        }

        return new Vector3(point.x + 0.5f, point.y + 0.5f, 0f);
    }

    private Vector3 GetSpawnWorldPosition(int spawnIndex, MapContext context = null)
    {
        if (spawnWorldPositions != null && spawnIndex >= 0 && spawnIndex < spawnWorldPositions.Length)
        {
            Vector3 worldPosition = spawnWorldPositions[spawnIndex];
            worldPosition.z = 0f;
            return worldPosition;
        }

        if (spawnPoints != null && spawnIndex >= 0 && spawnIndex < spawnPoints.Length)
            return GetWorldPositionForPoint(spawnPoints[spawnIndex], context != null ? context : mapContext);

        return Vector3.zero;
    }

    private Vector2Int ResolveCellForWorldSpawn(Vector3 worldPosition, MapContext context)
    {
        Tilemap tilemap = null;

        if (context != null)
            tilemap = context.ReferenceTilemap != null ? context.ReferenceTilemap : context.WallTilemap;

        if (tilemap != null)
        {
            Vector3Int cell = tilemap.WorldToCell(worldPosition);
            return new Vector2Int(cell.x, cell.y);
        }

        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x - 0.5f),
            Mathf.RoundToInt(worldPosition.y - 0.5f));
    }

    private void ResolveSpawnWorldPositions(MapContext context)
    {
        if (spawnPoints == null)
        {
            spawnWorldPositions = null;
            return;
        }

        spawnWorldPositions = new Vector3[spawnPoints.Length];
        for (int i = 0; i < spawnPoints.Length; i++)
            spawnWorldPositions[i] = GetWorldPositionForPoint(spawnPoints[i], context);
    }

    private MapContext FindMapContextForDebug()
    {
        if (mapContext != null)
            return mapContext;

        return FindAnyObjectByType<MapContext>();
    }

    private bool TrySpawnSessionActors()
    {
        if (gameFlowConfig.PlayerPrefab == null || gameFlowConfig.BotPrefab == null)
        {
            Debug.LogError($"[{nameof(PlayerSpawner)}] Player prefab or bot prefab is missing on GameFlowConfig.", this);
            return false;
        }

        if (!GameSession.HasAllHumanCharactersSelected())
        {
            Debug.LogWarning($"[{nameof(PlayerSpawner)}] Session exists but character selection is incomplete. Falling back to legacy spawn.", this);
            return false;
        }

        if (destroyExistingScenePlayers)
            DestroyExistingActorsInScene();

        spawnedActors.Clear();

        int spawnIndex = 0;
        for (int playerIndex = 0; playerIndex < GameSession.PlayerCount; playerIndex++)
        {
            CharacterId? selectedCharacter = GameSession.GetSelectedCharacter(playerIndex);
            if (!selectedCharacter.HasValue)
                continue;

            SpawnActor(
                gameFlowConfig.PlayerPrefab,
                spawnIndex++,
                selectedCharacter.Value,
                playerIndex,
                displayIndex: playerIndex + 1,
                isBot: false,
                botConfig: null);
        }

        List<CharacterId> remainingCharacters = GameSession.GetRemainingCharacters(gameFlowConfig);
        BotConfig botConfigForMatch = gameFlowConfig.GetBotConfig(GameSession.Difficulty);

        for (int botIndex = 0; botIndex < GameSession.EnemyCount; botIndex++)
        {
            CharacterId characterId = botIndex < remainingCharacters.Count
                ? remainingCharacters[botIndex]
                : gameFlowConfig.GetCharacterIdAt(botIndex % Mathf.Max(1, gameFlowConfig.CharacterCount));

            SpawnActor(
                gameFlowConfig.BotPrefab,
                spawnIndex++,
                characterId,
                humanIndex: -1,
                displayIndex: botIndex + 1,
                isBot: true,
                botConfig: botConfigForMatch);
        }

        return spawnedActors.Count > 0;
    }

    private void DestroyExistingActorsInScene()
    {
        PlayerController[] scenePlayers = Object.FindObjectsByType<PlayerController>();
        foreach (PlayerController player in scenePlayers)
        {
            if (player == null)
                continue;

            player.gameObject.SetActive(false);
            Destroy(player.gameObject);
        }
    }

    private void SpawnActor(
        GameObject prefab,
        int spawnIndex,
        CharacterId characterId,
        int humanIndex,
        int displayIndex,
        bool isBot,
        BotConfig botConfig)
    {
        if (spawnIndex >= spawnPoints.Length)
        {
            Debug.LogWarning($"[{nameof(PlayerSpawner)}] Not enough spawn points for all actors.", this);
            return;
        }

        Vector2Int spawnPoint = spawnPoints[spawnIndex];
        ClearSpawnArea(spawnPoint);

        Vector3 worldPosition = GetSpawnWorldPosition(spawnIndex);

        GameObject actor = Instantiate(prefab, worldPosition, Quaternion.identity);
        actor.transform.position = new Vector3(
            Mathf.Round(actor.transform.position.x - 0.5f) + 0.5f,
            Mathf.Round(actor.transform.position.y - 0.5f) + 0.5f,
            0f);

        if (actor.TryGetComponent(out PlayerController playerController) && mapContext != null)
            playerController.Init(mapContext);

        ApplyCharacterVisual(actor, characterId);
        ConfigureHudIdentity(actor, characterId, spawnIndex, humanIndex, displayIndex, isBot);
        ConfigureInput(actor, humanIndex, isBot);
        ConfigureBot(actor, isBot, botConfig);

        spawnedActors.Add(actor);
        StartCoroutine(SpawnSafe(actor));
    }

    private void ApplyCharacterVisual(GameObject actor, CharacterId characterId)
    {
        if (gameFlowConfig == null)
            return;

        CharacterDefinition character = gameFlowConfig.GetCharacter(characterId);
        if (character == null || character.animatorController == null)
            return;

        if (!actor.TryGetComponent<Animator>(out var animator))
            return;

        animator.runtimeAnimatorController = character.animatorController;
        animator.Rebind();
        animator.Update(0f);
    }

    private static void ConfigureInput(GameObject actor, int humanIndex, bool isBot)
    {
        if (!actor.TryGetComponent<PlayerInput>(out var playerInput))
            return;

        if (isBot)
        {
            playerInput.enabled = false;
            return;
        }

        string bindingGroup = humanIndex == 0 ? "Player 1" : "Player 2";
        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.actions.bindingMask = InputBinding.MaskByGroup(bindingGroup);
        playerInput.ActivateInput();
    }

    private static void ConfigureBot(GameObject actor, bool isBot, BotConfig botConfig)
    {
        if (!actor.TryGetComponent<BotBrain>(out var botBrain))
            return;

        botBrain.enabled = isBot;

        if (isBot && botConfig != null)
            botBrain.SetConfig(botConfig);
    }

    private static void ConfigureHudIdentity(
        GameObject actor,
        CharacterId characterId,
        int spawnOrder,
        int humanIndex,
        int displayIndex,
        bool isBot)
    {
        if (actor == null)
            return;

        ActorHudIdentity identity = actor.GetComponent<ActorHudIdentity>();
        if (identity == null)
            identity = actor.AddComponent<ActorHudIdentity>();

        string label = isBot
            ? $"BOT {Mathf.Max(1, displayIndex)}"
            : $"PLAYER {Mathf.Max(1, humanIndex + 1)}";

        identity.Configure(spawnOrder, label, characterId, isBot);
    }

    private void SpawnLegacyPlayers()
    {
        if (playerPrefab == null)
            return;

        spawnedActors.Clear();

        for (int i = 0; i < 1; i++)
        {
            Vector2Int point = spawnPoints[i];
            ClearSpawnArea(point);

            Vector3 position = GetSpawnWorldPosition(i);
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.transform.position = new Vector3(
                Mathf.Round(player.transform.position.x - 0.5f) + 0.5f,
                Mathf.Round(player.transform.position.y - 0.5f) + 0.5f,
                0f);
            ConfigureHudIdentity(player, CharacterId.Character1, i, i, i + 1, isBot: false);
            spawnedActors.Add(player);

            StartCoroutine(SpawnSafe(player));
        }
    }

    private int CountAliveActors()
    {
        int aliveCount = 0;

        for (int i = spawnedActors.Count - 1; i >= 0; i--)
        {
            GameObject actor = spawnedActors[i];
            if (actor == null)
            {
                spawnedActors.RemoveAt(i);
                continue;
            }

            if (TryGetLivePlayerController(actor, out _))
                aliveCount++;
        }

        return aliveCount;
    }

    private RoundOutcome EvaluateRoundOutcome()
    {
        if (GameSession.EnemyCount <= 0)
            return RoundOutcome.None;

        int aliveHumans = 0;
        int aliveBots = 0;

        for (int i = spawnedActors.Count - 1; i >= 0; i--)
        {
            GameObject actor = spawnedActors[i];
            if (actor == null)
            {
                spawnedActors.RemoveAt(i);
                continue;
            }

            ActorHudIdentity identity = actor.GetComponent<ActorHudIdentity>();
            if (identity == null)
                continue;

            if (!TryGetLivePlayerController(actor, out _))
                continue;

            if (identity.IsBot)
                aliveBots++;
            else
                aliveHumans++;
        }

        if (aliveHumans <= 0 && aliveBots > 0)
            return RoundOutcome.Lose;

        if (aliveBots <= 0 && aliveHumans > 0)
            return RoundOutcome.Win;

        if (aliveHumans <= 0 && aliveBots <= 0)
            return RoundOutcome.Lose;

        return RoundOutcome.None;
    }

    private static bool TryGetLivePlayerController(GameObject actor, out PlayerController playerController)
    {
        playerController = null;
        if (actor == null)
            return false;

        playerController = actor.GetComponent<PlayerController>();
        return playerController != null && !playerController.IsDying;
    }

    private IEnumerator HandleRoundEnd(RoundOutcome outcome)
    {
        roundEnding = true;
        RoundIntroState.EndIntro();

        bool showingOverlay = TryShowResultOverlay(outcome);
        if (showingOverlay)
            yield return new WaitForSecondsRealtime(resultDisplayDuration);
        else
            yield return new WaitForSeconds(roundTransitionDelay);

        if (resultOverlayController != null)
            resultOverlayController.HideImmediate();

        if (GameSession.AdvanceRound())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield break;
        }

        string targetScene = gameFlowConfig != null && !string.IsNullOrWhiteSpace(gameFlowConfig.GameSettingScene)
            ? gameFlowConfig.GameSettingScene
            : "GameSetting";

        GameSession.ClearSession();
        SceneManager.LoadScene(targetScene);
    }

    private bool TryShowResultOverlay(RoundOutcome outcome)
    {
        if (resultOverlayController == null)
            return false;

        MatchResultOverlayController.MatchResultType overlayResult = outcome switch
        {
            RoundOutcome.Win => MatchResultOverlayController.MatchResultType.Win,
            RoundOutcome.Lose => MatchResultOverlayController.MatchResultType.Lose,
            _ => MatchResultOverlayController.MatchResultType.None
        };

        return resultOverlayController.ShowResult(overlayResult);
    }

    private void ClearSpawnArea(Vector2Int point)
    {
        Vector2Int[] directions =
        {
            Vector2Int.zero,
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.down
        };

        foreach (Vector2Int direction in directions)
        {
            Vector3Int cell = new(point.x + direction.x, point.y + direction.y, 0);

            if (mapContext != null && mapContext.MapBuilder != null)
            {
                mapContext.MapBuilder.DestroyBlockAt(cell);
                continue;
            }

            Vector2 position = new(cell.x + 0.5f, cell.y + 0.5f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(position, Vector2.one * 0.8f, 0f);

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                DestructibleBlock block = hit.GetComponentInParent<DestructibleBlock>();
                if (block != null)
                    Destroy(block.gameObject);
            }
        }
    }

    private IEnumerator SpawnSafe(GameObject actor)
    {
        Collider2D collider2D = actor.GetComponent<Collider2D>();
        Rigidbody2D rigidbody2D = actor.GetComponent<Rigidbody2D>();

        if (collider2D == null || rigidbody2D == null)
            yield break;

        collider2D.enabled = false;
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
        rigidbody2D.Sleep();

        yield return new WaitForFixedUpdate();

        collider2D.enabled = true;
    }

    private void BeginRoundIntro()
    {
        RoundIntroState.BeginIntro();

        if (roundIntroRoutine != null)
            StopCoroutine(roundIntroRoutine);

        roundIntroRoutine = StartCoroutine(RoundIntroCoroutine());
    }

    private IEnumerator RoundIntroCoroutine()
    {
        float introDuration = Mathf.Max(0f, roundIntroDuration);

        for (int i = 0; i < spawnedActors.Count; i++)
        {
            GameObject actor = spawnedActors[i];
            if (actor == null || !actor.TryGetComponent(out PlayerController controller))
                continue;

            controller.StopMoving();
            controller.PlaySpawnIntro(introDuration, roundIntroBlinkCount);
        }

        if (introDuration > 0f)
            yield return new WaitForSecondsRealtime(introDuration);

        RoundIntroState.EndIntro();
        roundIntroRoutine = null;
    }

    private void OnDisable()
    {
        RoundIntroState.EndIntro();
    }
}
