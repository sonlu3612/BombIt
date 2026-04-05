using UnityEngine;

public class AutoDestroyAfterAnimation : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}