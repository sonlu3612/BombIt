using UnityEngine;
using UnityEngine.Tilemaps;

public class GridSizeDebug : MonoBehaviour
{
    public Tilemap tilemap;

    void Start()
    {
        BoundsInt bounds = tilemap.cellBounds;

        int width = bounds.size.x;
        int height = bounds.size.y;

        Debug.Log("Grid Width (tiles): " + width);
        Debug.Log("Grid Height (tiles): " + height);
        Debug.Log("Ratio: " + (float)width / height);
    }
}