using System.Collections.Generic;
using _Project.Gameplay.Bomb.Scripts;
using _Project.Gameplay.Player.Scripts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.Map.Scripts
{
    public class GridOccupancyService : MonoBehaviour
    {
        [SerializeField] private MapContext mapContext;

        private readonly Dictionary<Vector3Int, BombController> bombsByCell = new();
        private readonly Dictionary<PlayerController, Vector3Int> playerCells = new();
        private readonly Dictionary<Vector3Int, HashSet<PlayerController>> playersByCell = new();

        public MapContext MapContext => mapContext;

        [System.Obsolete]
        private void Awake()
        {
            if (mapContext == null)
                mapContext = GetComponent<MapContext>();

            if (mapContext == null)
                mapContext = FindObjectOfType<MapContext>();
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            if (mapContext != null && mapContext.ReferenceTilemap != null)
                return mapContext.ReferenceTilemap.WorldToCell(worldPosition);

            if (mapContext != null && mapContext.WallTilemap != null)
                return mapContext.WallTilemap.WorldToCell(worldPosition);

            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y),
                0);
        }

        public Vector3 GetCellCenterWorld(Vector3Int cell)
        {
            if (mapContext != null && mapContext.ReferenceTilemap != null)
                return mapContext.ReferenceTilemap.GetCellCenterWorld(cell);

            if (mapContext != null && mapContext.WallTilemap != null)
                return mapContext.WallTilemap.GetCellCenterWorld(cell);

            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        public bool IsWall(Vector3Int cell)
        {
            return mapContext != null
                   && mapContext.WallTilemap != null
                   && mapContext.WallTilemap.HasTile(cell);
        }

        public bool HasBlock(Vector3Int cell)
        {
            return mapContext != null
                   && mapContext.MapBuilder != null
                   && mapContext.MapBuilder.HasBlock(cell);
        }

        public bool HasBomb(Vector3Int cell)
        {
            return bombsByCell.ContainsKey(cell);
        }

        public bool HasPlayer(Vector3Int cell, PlayerController ignorePlayer = null)
        {
            if (!playersByCell.TryGetValue(cell, out HashSet<PlayerController> players))
                return false;

            foreach (PlayerController player in players)
            {
                if (player == null)
                    continue;

                if (ignorePlayer != null && player == ignorePlayer)
                    continue;

                return true;
            }

            return false;
        }

        public bool IsStaticallyBlocked(Vector3Int cell)
        {
            return IsWall(cell) || HasBlock(cell);
        }

        public bool IsDynamicallyBlocked(
            Vector3Int cell,
            PlayerController ignorePlayer = null,
            bool blockOnBomb = true,
            bool blockOnPlayers = true)
        {
            if (blockOnBomb && HasBomb(cell))
                return true;

            if (blockOnPlayers && HasPlayer(cell, ignorePlayer))
                return true;

            return false;
        }

        public bool IsCellWalkable(
            Vector3Int cell,
            PlayerController ignorePlayer = null,
            bool blockOnBomb = true,
            bool blockOnPlayers = true)
        {
            if (IsStaticallyBlocked(cell))
                return false;

            if (IsDynamicallyBlocked(cell, ignorePlayer, blockOnBomb, blockOnPlayers))
                return false;

            return true;
        }

        public void RegisterPlayer(PlayerController player, Vector3Int cell)
        {
            if (player == null)
                return;

            UnregisterPlayer(player);

            playerCells[player] = cell;

            if (!playersByCell.TryGetValue(cell, out HashSet<PlayerController> players))
            {
                players = new HashSet<PlayerController>();
                playersByCell[cell] = players;
            }

            players.Add(player);
        }

        public void MovePlayer(PlayerController player, Vector3Int fromCell, Vector3Int toCell)
        {
            if (player == null)
                return;

            if (playerCells.TryGetValue(player, out Vector3Int registeredCell))
                fromCell = registeredCell;

            if (fromCell == toCell)
            {
                RegisterPlayer(player, toCell);
                return;
            }

            if (playersByCell.TryGetValue(fromCell, out HashSet<PlayerController> oldPlayers))
            {
                oldPlayers.Remove(player);

                if (oldPlayers.Count == 0)
                    playersByCell.Remove(fromCell);
            }

            playerCells[player] = toCell;

            if (!playersByCell.TryGetValue(toCell, out HashSet<PlayerController> newPlayers))
            {
                newPlayers = new HashSet<PlayerController>();
                playersByCell[toCell] = newPlayers;
            }

            newPlayers.Add(player);
        }

        public void UnregisterPlayer(PlayerController player)
        {
            if (player == null)
                return;

            if (!playerCells.TryGetValue(player, out Vector3Int cell))
                return;

            playerCells.Remove(player);

            if (playersByCell.TryGetValue(cell, out HashSet<PlayerController> players))
            {
                players.Remove(player);

                if (players.Count == 0)
                    playersByCell.Remove(cell);
            }
        }

        public void RegisterBomb(BombController bomb, Vector3Int cell)
        {
            if (bomb == null)
                return;

            bombsByCell[cell] = bomb;
        }

        public void UnregisterBomb(BombController bomb, Vector3Int cell)
        {
            if (!bombsByCell.TryGetValue(cell, out BombController registeredBomb))
                return;

            if (registeredBomb != bomb)
                return;

            bombsByCell.Remove(cell);
        }
    }
}
