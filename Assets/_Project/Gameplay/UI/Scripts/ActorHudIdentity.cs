using System.Collections.Generic;
using _Project.Gameplay.Player.Scripts;
using _Project.Systems.GameFlow;
using UnityEngine;

namespace _Project.Gameplay.UI.Scripts
{
    [DisallowMultipleComponent]
    public class ActorHudIdentity : MonoBehaviour
    {
        [SerializeField] private int slotOrder;
        [SerializeField] private string displayName;
        [SerializeField] private CharacterId characterId;
        [SerializeField] private bool isBot;

        private PlayerController playerController;

        public int SlotOrder => slotOrder;
        public string DisplayName => displayName;
        public CharacterId CharacterId => characterId;
        public bool IsBot => isBot;
        public PlayerController PlayerController => playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                ActorHudRegistry.RegisterOrUpdate(this);
        }

        public void Configure(int order, string label, CharacterId actorCharacterId, bool actorIsBot)
        {
            slotOrder = Mathf.Max(0, order);
            displayName = string.IsNullOrWhiteSpace(label)
                ? (actorIsBot ? "BOT" : "PLAYER")
                : label.Trim().ToUpperInvariant();
            characterId = actorCharacterId;
            isBot = actorIsBot;

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            ActorHudRegistry.RegisterOrUpdate(this);
        }

        private void OnDestroy()
        {
            ActorHudRegistry.MarkDestroyed(slotOrder);
        }
    }

    public static class ActorHudRegistry
    {
        public readonly struct Entry
        {
            public Entry(int slotOrder, string displayName, CharacterId characterId, bool isBot, PlayerController playerController)
            {
                SlotOrder = slotOrder;
                DisplayName = displayName;
                CharacterId = characterId;
                IsBot = isBot;
                PlayerController = playerController;
            }

            public int SlotOrder { get; }
            public string DisplayName { get; }
            public CharacterId CharacterId { get; }
            public bool IsBot { get; }
            public PlayerController PlayerController { get; }
        }

        private sealed class MutableEntry
        {
            public int SlotOrder;
            public string DisplayName;
            public CharacterId CharacterId;
            public bool IsBot;
            public PlayerController PlayerController;
        }

        private static readonly Dictionary<int, MutableEntry> entries = new();
        private static readonly List<Entry> cachedEntries = new();

        public static void Clear()
        {
            entries.Clear();
            cachedEntries.Clear();
        }

        public static void RegisterOrUpdate(ActorHudIdentity identity)
        {
            if (identity == null)
                return;

            if (!entries.TryGetValue(identity.SlotOrder, out MutableEntry entry))
            {
                entry = new MutableEntry();
                entries.Add(identity.SlotOrder, entry);
            }

            entry.SlotOrder = identity.SlotOrder;
            entry.DisplayName = identity.DisplayName;
            entry.CharacterId = identity.CharacterId;
            entry.IsBot = identity.IsBot;
            entry.PlayerController = identity.PlayerController;
        }

        public static void MarkDestroyed(int slotOrder)
        {
            if (!entries.TryGetValue(slotOrder, out MutableEntry entry))
                return;

            entry.PlayerController = null;
        }

        public static IReadOnlyList<Entry> GetEntries()
        {
            cachedEntries.Clear();

            foreach (KeyValuePair<int, MutableEntry> pair in entries)
            {
                MutableEntry entry = pair.Value;
                cachedEntries.Add(new Entry(
                    entry.SlotOrder,
                    entry.DisplayName,
                    entry.CharacterId,
                    entry.IsBot,
                    entry.PlayerController));
            }

            cachedEntries.Sort((a, b) => a.SlotOrder.CompareTo(b.SlotOrder));
            return cachedEntries;
        }
    }
}
