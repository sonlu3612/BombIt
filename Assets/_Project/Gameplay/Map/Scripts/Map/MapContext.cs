using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Project.Gameplay.Map.Scripts
{
    public class MapContext : MonoBehaviour
    {
        [Header("Map References")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap blockTilemap;
        [SerializeField] private Tilemap referenceTilemap;
        [SerializeField] private MapBuilder mapBuilder;

        public Tilemap WallTilemap => wallTilemap;
        public Tilemap BlockTilemap => blockTilemap;
        public Tilemap ReferenceTilemap => referenceTilemap;
        public MapBuilder MapBuilder => mapBuilder;

        private void Awake()
        {
            if (referenceTilemap == null)
            {
                Debug.LogWarning($"[{nameof(MapContext)}] Reference Tilemap is missing on {gameObject.name}", this);
            }

            if (wallTilemap == null)
            {
                Debug.LogWarning($"[{nameof(MapContext)}] Wall Tilemap is missing on {gameObject.name}", this);
            }

            if (mapBuilder == null)
            {
                Debug.LogWarning($"[{nameof(MapContext)}] MapBuilder is missing on {gameObject.name}", this);
            }
        }
    }
}