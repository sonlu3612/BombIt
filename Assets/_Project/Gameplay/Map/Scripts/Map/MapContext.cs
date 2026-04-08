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
        [SerializeField] private GridOccupancyService gridOccupancyService;

        public Tilemap WallTilemap => wallTilemap;
        public Tilemap BlockTilemap => blockTilemap;
        public Tilemap ReferenceTilemap => referenceTilemap;
        public MapBuilder MapBuilder => mapBuilder;
        public GridOccupancyService GridOccupancyService => gridOccupancyService;

        private void Awake()
        {
            if (gridOccupancyService == null)
                gridOccupancyService = GetComponent<GridOccupancyService>();

            if (referenceTilemap == null)
                Debug.LogWarning($"[{nameof(MapContext)}] Reference Tilemap is missing on {gameObject.name}", this);

            if (wallTilemap == null)
                Debug.LogWarning($"[{nameof(MapContext)}] Wall Tilemap is missing on {gameObject.name}", this);

            if (mapBuilder == null)
                Debug.LogWarning($"[{nameof(MapContext)}] MapBuilder is missing on {gameObject.name}", this);

            if (gridOccupancyService == null)
                Debug.LogWarning($"[{nameof(MapContext)}] GridOccupancyService is missing on {gameObject.name}", this);
        }
    }
}
