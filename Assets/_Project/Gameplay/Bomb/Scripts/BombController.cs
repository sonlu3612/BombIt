using UnityEngine;
using System.Collections;
using _Project.Domain;

namespace _Project.Gameplay.Bomb.Scripts
{
    public class BombController : MonoBehaviour
    {
        private Domain.Bomb bombData;
        private System.Action onExplode;

        public GameObject explosionCenter;
        public GameObject explosionMiddle;
        public GameObject explosionEnd;

        public void Init(Domain.Bomb data, System.Action onExplodeCallback)
        {
            bombData = data;
            onExplode = onExplodeCallback;
            StartCoroutine(ExplodeAfterTime());
        }

        IEnumerator ExplodeAfterTime()
        {
            yield return new WaitForSeconds(bombData.explodeTime);

            Explode();

            onExplode?.Invoke(); 

            Destroy(gameObject);
        }

        void Explode()
        {
            Vector3 pos = transform.position;

            Instantiate(explosionCenter, pos, Quaternion.identity);

            ExplodeDir(Vector2.up);
            ExplodeDir(Vector2.down);
            ExplodeDir(Vector2.left);
            ExplodeDir(Vector2.right);
        }

        Quaternion GetRotation(Vector2 dir)
        {
            float offset = -90f;

            if (dir == Vector2.up) return Quaternion.Euler(0, 0, 90 + offset);
            if (dir == Vector2.down) return Quaternion.Euler(0, 0, -90 + offset);
            if (dir == Vector2.left) return Quaternion.Euler(0, 0, 180 + offset);
            return Quaternion.Euler(0, 0, 0 + offset); // right
        }

        void ExplodeDir(Vector2 dir)
        {
            Quaternion rot = GetRotation(dir);

            for (int i = 1; i <= bombData.range; i++)
            {
                Vector3 pos = transform.position + (Vector3)(dir * i);

                if (i == bombData.range)
                    Instantiate(explosionEnd, pos, rot);
                else
                    Instantiate(explosionMiddle, pos, rot);
            }
        }
    }
}