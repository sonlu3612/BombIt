using _Project.Gameplay.Player.Scripts;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemType
    {
        Speed,
        Bomb,
        Range,
        Health
    }

    public ItemType type;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
            Destroy(gameObject);
        }
    }

    void ApplyEffect(GameObject playerObj)
    {
        var pc = playerObj.GetComponent<PlayerController>();

        if (pc == null) return;

        switch (type)
        {
            case ItemType.Speed:
                pc.AddSpeed(1f);
                break;

            case ItemType.Bomb:
                pc.AddBomb(1);
                break;

            case ItemType.Range:
                pc.AddRange(1);
                break;

            case ItemType.Health:
                pc.AddHealth(1);
                break;
        }
    }
}