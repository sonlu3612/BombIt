using UnityEngine;
using _Project.Gameplay.Map.Scripts;
using _Project.Gameplay.Player.Scripts;

namespace _Project.Gameplay.Match.Scripts
{
    public class MatchManager : MonoBehaviour
    {
        [Header("Match References")]
        [SerializeField] private MapContext currentMap;

        [Header("Scene Players/Bots")]
        [SerializeField] private PlayerController[] scenePlayers;

        [System.Obsolete]
        private void Awake()
        {
            if (currentMap == null)
            {
                currentMap = FindObjectOfType<MapContext>();
            }

            if (currentMap == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] No MapContext found in scene.", this);
                return;
            }

            BindScenePlayers();
        }

        [System.Obsolete]
        private void BindScenePlayers()
        {
            if (scenePlayers == null || scenePlayers.Length == 0)
            {
                scenePlayers = FindObjectsOfType<PlayerController>();
            }

            foreach (PlayerController player in scenePlayers)
            {
                if (player == null) continue;
                player.Init(currentMap);
            }
        }

        public PlayerController SpawnPlayer(PlayerController prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] Player prefab is null.", this);
                return null;
            }

            if (currentMap == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] Current MapContext is null.", this);
                return null;
            }

            PlayerController player = Instantiate(prefab, position, rotation);
            player.Init(currentMap);
            return player;
        }

        public MapContext GetCurrentMap()
        {
            return currentMap;
        }

        [System.Obsolete]
        public void SetCurrentMap(MapContext mapContext)
        {
            currentMap = mapContext;

            if (currentMap == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] SetCurrentMap received null.", this);
                return;
            }

            BindScenePlayers();
        }
    }
}