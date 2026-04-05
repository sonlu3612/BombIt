using UnityEngine;

namespace _Project.Gameplay.Item.Scripts.Item
{
    public class FloatingVisual : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.08f;
        [SerializeField] private float frequency = 2f;
        [SerializeField] private bool randomStartOffset = true;

        private Vector3 startLocalPos;
        private float timeOffset;

        private void Awake()
        {
            startLocalPos = transform.localPosition;

            if (randomStartOffset)
            {
                timeOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            float yOffset = Mathf.Sin((Time.time + timeOffset) * frequency) * amplitude;
            transform.localPosition = startLocalPos + new Vector3(0f, yOffset, 0f);
        }
    }
}