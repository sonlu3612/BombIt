using _Project.Domain;
using Assets._Project.Gameplay.Bomb.Scripts;
using System;
using System.Collections;
using UnityEngine;

namespace _Project.Gameplay.Bomb.Scripts
{
    public class BombController : MonoBehaviour
    {
        private Domain.Bomb bombData;
        private Action onExplode;
        [SerializeField] private float tileSize = 1f;

        [Header("Explosion Prefabs")]
        [SerializeField] private GameObject explosionCenter;
        [SerializeField] private GameObject explosionMiddle;
        [SerializeField] private GameObject explosionEnd;

        private static readonly Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        public void Init(Domain.Bomb data, Action onExplodeCallback)
        {
            bombData = data;
            onExplode = onExplodeCallback;

            StartCoroutine(ExplodeAfterTime());
        }

        private IEnumerator ExplodeAfterTime()
        {
            yield return new WaitForSeconds(bombData.explodeTime);

            Explode();
            onExplode?.Invoke();

            Destroy(gameObject);
        }

        private void Explode()
        {
            Vector3 origin = transform.position;

            var center = Instantiate(explosionCenter, origin, Quaternion.identity);

            Debug.Log($"CENTER at {center.transform.position}");

            center.GetComponent<Explosion>()?.Play();

            // 4 directions
            foreach (var dir in directions)
            {
                ExplodeDirection(origin, dir);
            }
        }

        private void ExplodeDirection(Vector3 origin, Vector2 dir)
        {
            Quaternion rotation = GetRotation(dir);
            float offset = 0.7f;

            for (int i = 1; i <= bombData.range; i++)
            {
                Vector3 spawnPos = origin + (Vector3)(dir * i);
                spawnPos -= (Vector3)(dir * offset);

                GameObject prefab;

                if (bombData.range == 1)
                {
                    prefab = explosionEnd;
                }
                else
                {
                    prefab = (i == bombData.range)
                        ? explosionEnd
                        : explosionMiddle;
                }

                var obj = Instantiate(prefab, spawnPos, rotation);
                obj.GetComponent<Explosion>()?.Play();
            }
        }

        Quaternion GetRotation(Vector2 dir)
        {
            float offset = -90f;

            if (dir == Vector2.up) return Quaternion.Euler(0, 0, 90 + offset);
            if (dir == Vector2.down) return Quaternion.Euler(0, 0, -90 + offset);
            if (dir == Vector2.left) return Quaternion.Euler(0, 0, 180 + offset);
            return Quaternion.Euler(0, 0, 0 + offset); // right
        }
    }
}