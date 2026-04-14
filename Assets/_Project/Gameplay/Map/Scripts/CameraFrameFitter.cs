using UnityEngine;

namespace _Project.Gameplay.Map.Scripts
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFrameFitter : MonoBehaviour
    {
        private enum FitMode
        {
            Contain,
            Fill
        }

        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Vector2 padding = new(0.6f, 0.6f);
        [SerializeField] private FitMode fitMode = FitMode.Contain;
        [SerializeField] private bool fitOnStart = true;
        [SerializeField] private bool fitInEditMode;
        [SerializeField] private bool keepCurrentRotation = true;

        private Camera cachedCamera;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (fitOnStart)
                FitNow();
        }

        private void OnValidate()
        {
            cachedCamera = GetComponent<Camera>();

            if (!Application.isPlaying && fitInEditMode)
                FitNow();
        }

        [ContextMenu("Fit Now")]
        public void FitNow()
        {
            if (cachedCamera == null)
                cachedCamera = GetComponent<Camera>();

            if (cachedCamera == null || targetRenderers == null || targetRenderers.Length == 0)
                return;

            if (!TryBuildBounds(out Bounds bounds))
                return;

            if (cachedCamera.orthographic)
                FitOrthographic(bounds);
            else
                FitPerspective(bounds);
        }

        private bool TryBuildBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(targetRenderer.bounds);
            }

            return hasBounds;
        }

        private void FitOrthographic(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 position = transform.position;
            position.x = center.x;
            position.y = center.y;

            float halfWidth = bounds.extents.x + padding.x;
            float halfHeight = bounds.extents.y + padding.y;

            float heightSize = halfHeight;
            float widthSize = halfWidth / Mathf.Max(0.0001f, cachedCamera.aspect);
            cachedCamera.orthographicSize = fitMode == FitMode.Fill
                ? Mathf.Min(heightSize, widthSize)
                : Mathf.Max(heightSize, widthSize);
            transform.position = position;
        }

        private void FitPerspective(Bounds bounds)
        {
            if (!keepCurrentRotation)
                transform.rotation = Quaternion.identity;

            Vector3 center = bounds.center;
            float halfWidth = bounds.extents.x + padding.x;
            float halfHeight = bounds.extents.y + padding.y;

            float verticalHalfFov = cachedCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * cachedCamera.aspect);

            float distanceForHeight = halfHeight / Mathf.Tan(verticalHalfFov);
            float distanceForWidth = halfWidth / Mathf.Tan(horizontalHalfFov);
            float distance = fitMode == FitMode.Fill
                ? Mathf.Min(distanceForHeight, distanceForWidth)
                : Mathf.Max(distanceForHeight, distanceForWidth);

            Vector3 position = center - transform.forward * distance;
            transform.position = position;
        }
    }
}
