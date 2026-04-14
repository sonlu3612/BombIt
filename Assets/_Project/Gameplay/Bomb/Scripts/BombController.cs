using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using _Project.Gameplay.AI.Scripts;
using _Project.Gameplay.Audio.Scripts;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;
using Assets._Project.Gameplay.Bomb.Scripts;

namespace _Project.Gameplay.Bomb.Scripts
{
    public class BombController : MonoBehaviour
    {
        private Domain.Bomb bombData;
        private Action onExplode;
        private readonly HashSet<PlayerController> damagedPlayersThisExplosion = new();
        private float spawnedAt;
        private GridOccupancyService occupancyService;
        private Coroutine explodeRoutine;
        private bool hasExploded;
        private bool registeredInOccupancy;
        
        [SerializeField] private float postExplosionDamageDelay = 0.1f;
        private float explosionHazardEndTime;

        public Vector3Int CurrentCell => GetBombCell();
        public int Range => bombData != null ? bombData.range : 1;
        public float RemainingTime => bombData != null ? Mathf.Max(0f, bombData.explodeTime - (Time.time - spawnedAt)) : 0f;
        public bool HasExploded => hasExploded;
        public bool IsExplosionHazardActive => hasExploded && Time.time < explosionHazardEndTime;

        [Header("Map References")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private MapBuilder mapBuilder;
        [SerializeField] private Tilemap referenceTilemap;

        [Header("Explosion Prefabs")]
        [SerializeField] private GameObject explosionCenter;
        [SerializeField] private GameObject explosionMiddle;
        [SerializeField] private GameObject explosionEnd;
        [SerializeField] private GameObject explosionHitWall;
        [SerializeField] private GameObject explosionHitBlock;

        [Header("Visual Offset")]
        [SerializeField] private float middleVisualOffset;
        [SerializeField] private float endVisualOffset;
        [SerializeField] private float hitWallVisualOffset;
        [SerializeField] private float hitBlockVisualOffset;

        [Header("Debug")]
        [SerializeField] private bool debugExplosionHitbox = false;
        [SerializeField] private Color debugCenterColor = Color.red;
        [SerializeField] private Color debugRangeColor = Color.yellow;
        [SerializeField] private Color debugBlockedColor = Color.gray;
        [SerializeField] private Vector3 debugBoxSize = new Vector3(0.9f, 0.9f, 0.1f);

        private static readonly Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        [Obsolete]
        public void Init(
            Domain.Bomb data,
            Tilemap wallMap,
            Tilemap referenceMap,
            MapBuilder builder,
            Action onExplodeCallback)
        {
            bombData = data;
            wallTilemap = wallMap;
            referenceTilemap = referenceMap;
            mapBuilder = builder;
            onExplode = onExplodeCallback;
            spawnedAt = Time.time;

            explodeRoutine = StartCoroutine(ExplodeAfterTime());

            MapContext context = UnityEngine.Object.FindFirstObjectByType<MapContext>();
            if (context != null)
            {
                occupancyService = context.GridOccupancyService;

                if (occupancyService != null)
                {
                    occupancyService.RegisterBomb(this, CurrentCell);
                    registeredInOccupancy = true;
                }
            }
        }

        private void UnregisterFromOccupancy()
        {
            if (!registeredInOccupancy || occupancyService == null || bombData == null)
                return;

            occupancyService.UnregisterBomb(this, CurrentCell);
            registeredInOccupancy = false;
        }

        private void DamagePlayersAtCell(Vector3Int explosionCell)
        {
            PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>();
            if (players == null || players.Length == 0)
                return;

            foreach (PlayerController player in players)
            {
                if (player == null)
                    continue;

                if (damagedPlayersThisExplosion.Contains(player))
                    continue;

                Vector3Int playerCell = player.GetCurrentCell();
                // Debug.Log($"Explosion cell = {explosionCell}, Player cell = {playerCell}, Player = {player.name}");

                if (playerCell != explosionCell)
                    continue;

                // Debug.Log($"PLAYER HIT: {player.name}");
                damagedPlayersThisExplosion.Add(player);
                player.TakeDamage();
            }
        }

        private IEnumerator ExplodeAfterTime()
        {
            yield return new WaitForSeconds(bombData.explodeTime);

            TriggerImmediateExplosion();
        }

        private void OnDestroy()
        {
            UnregisterFromOccupancy();
        }

        public void TriggerImmediateExplosion()
        {
            if (hasExploded || bombData == null)
                return;

            hasExploded = true;
            explosionHazardEndTime = Time.time + postExplosionDamageDelay;

            if (explodeRoutine != null)
            {
                StopCoroutine(explodeRoutine);
                explodeRoutine = null;
            }

            UnregisterFromOccupancy();
            Explode();
            onExplode?.Invoke();

            Destroy(gameObject, postExplosionDamageDelay + 0.05f);
        }

        private Vector3Int GetBombCell()
        {
            return new Vector3Int(bombData.position.x, bombData.position.y, 0);
        }

        private Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (referenceTilemap != null)
                return referenceTilemap.GetCellCenterWorld(cell);

            if (wallTilemap != null)
                return wallTilemap.GetCellCenterWorld(cell);

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        private Vector3 GetOldVisualSpawnPosition(Vector2Int dir, int step)
        {
            float offset = 0.7f;
            Vector3 origin = transform.position;

            Vector3 spawnPos = origin + (Vector3)((Vector2)dir * step);
            spawnPos -= (Vector3)((Vector2)dir * offset);

            return spawnPos;
        }

        private void Explode()
        {
            damagedPlayersThisExplosion.Clear();

            Vector3Int centerCell = GetBombCell();
            // BotRuntimeDebugLog.LogBombExploded(this, centerCell, bombData != null ? bombData.range : 1);
            AudioManager.Instance?.PlayBombExplosion();

            GameObject center = Instantiate(explosionCenter, transform.position, Quaternion.identity);
            center.GetComponent<Explosion>()?.Play();

            DamagePlayersAtCell(centerCell);

            foreach (Vector2Int dir in directions)
                ExplodeDirectionVisualOld(dir);
        }

        private void ExplodeDirectionVisualOld(Vector2Int dir)
        {
            Quaternion rotation = GetRotation(dir);

            for (int i = 1; i <= bombData.range; i++)
            {
                Vector2Int cell2D = bombData.position + dir * i;
                Vector3Int cell = new Vector3Int(cell2D.x, cell2D.y, 0);

                Vector3 visualPos = GetOldVisualSpawnPosition(dir, i);

                if (wallTilemap != null && wallTilemap.HasTile(cell))
                {
                    GameObject wallPrefab = explosionHitWall != null ? explosionHitWall : explosionEnd;
                    SpawnExplosion(wallPrefab, visualPos, rotation);
                    break;
                }

                if (mapBuilder != null && mapBuilder.HasBlock(cell))
                {
                    mapBuilder.DestroyBlockAt(cell);
                    DamagePlayersAtCell(cell);

                    GameObject blockPrefab = explosionHitBlock != null ? explosionHitBlock : explosionEnd;
                    Vector3 blockPos = GetCellCenterWorld(cell);
                    SpawnExplosion(blockPrefab, blockPos, rotation);
                    break;
                }

                TriggerBombAtCell(cell);
                DamagePlayersAtCell(cell);

                GameObject prefab = i == bombData.range ? explosionEnd : explosionMiddle;
                SpawnExplosion(prefab, visualPos, rotation);
            }
        }

        private void TriggerBombAtCell(Vector3Int cell)
        {
            BombController[] bombs = UnityEngine.Object.FindObjectsByType<BombController>();
            if (bombs == null || bombs.Length == 0)
                return;

            foreach (BombController bomb in bombs)
            {
                if (bomb == null || bomb == this || bomb.HasExploded)
                    continue;

                if (bomb.CurrentCell != cell)
                    continue;

                bomb.TriggerImmediateExplosion();
            }
        }

        private void SpawnExplosion(GameObject prefab, Vector3 pos, Quaternion rotation)
        {
            if (prefab == null)
                return;

            GameObject obj = Instantiate(prefab, pos, rotation);
            obj.GetComponent<Explosion>()?.Play();
        }

        private Quaternion GetRotation(Vector2 dir)
        {
            float offset = -90f;

            if (dir == Vector2.up) return Quaternion.Euler(0, 0, 90 + offset);
            if (dir == Vector2.down) return Quaternion.Euler(0, 0, -90 + offset);
            if (dir == Vector2.left) return Quaternion.Euler(0, 0, 180 + offset);

            return Quaternion.Euler(0, 0, 0 + offset);
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugExplosionHitbox)
                return;

            Vector2Int bombPos = Application.isPlaying && bombData != null
                ? bombData.position
                : new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

            DrawCell(bombPos, debugCenterColor);

            foreach (Vector2Int dir in directions)
            {
                for (int i = 1; i <= GetDebugRange(); i++)
                {
                    Vector2Int cell2D = bombPos + dir * i;
                    Vector3Int cell = new Vector3Int(cell2D.x, cell2D.y, 0);

                    if (wallTilemap != null && wallTilemap.HasTile(cell))
                    {
                        DrawCell(cell2D, debugBlockedColor);
                        break;
                    }

                    DrawCell(cell2D, debugRangeColor);

                    if (mapBuilder != null && mapBuilder.HasBlock(cell))
                        break;
                }
            }
        }

        private int GetDebugRange()
        {
            if (Application.isPlaying && bombData != null)
                return bombData.range;

            return 1;
        }

        private void DrawCell(Vector2Int cell, Color color)
        {
            Gizmos.color = color;
            Vector3 worldCenter = GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            Gizmos.DrawWireCube(worldCenter, debugBoxSize);
        }
    }
}
