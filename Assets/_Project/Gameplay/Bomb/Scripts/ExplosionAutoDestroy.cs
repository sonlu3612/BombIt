using UnityEngine;

namespace _Project.Gameplay.Bomb.Scripts
{
    public class ExplosionAutoDestroy : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Auto destroy on: " + gameObject.name, gameObject);
            Destroy(gameObject, 0.5f);
        }

        private void OnDestroy()
        {
            Debug.Log("Destroyed: " + gameObject.name);
        }
    }
}