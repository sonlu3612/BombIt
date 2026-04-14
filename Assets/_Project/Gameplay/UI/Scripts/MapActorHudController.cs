using _Project.Systems.GameFlow;
using UnityEngine;

namespace _Project.Gameplay.UI.Scripts
{
    public class MapActorHudController : MonoBehaviour
    {
        [SerializeField] private GameFlowConfig gameFlowConfig;
        [SerializeField] private float refreshInterval = 0.1f;
        [SerializeField] private MapActorHudSlot[] slots;

        private float refreshTimer;

        private void OnEnable()
        {
            RefreshAll();
        }

        private void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
                return;

            refreshTimer = Mathf.Max(0.02f, refreshInterval);
            RefreshAll();
        }

        public void RefreshAll()
        {
            var entries = ActorHudRegistry.GetEntries();
            bool[] consumedEntries = new bool[entries.Count];

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                ActorHudRegistry.Entry? entry = ResolveEntryForSlot(slots[i], entries, consumedEntries);

                slots[i].Bind(entry, gameFlowConfig);
            }
        }

        private static ActorHudRegistry.Entry? ResolveEntryForSlot(
            MapActorHudSlot slot,
            System.Collections.Generic.IReadOnlyList<ActorHudRegistry.Entry> entries,
            bool[] consumedEntries)
        {
            if (slot.BindingMode == MapActorHudSlot.SlotBindingMode.ByCharacter)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (consumedEntries[i])
                        continue;

                    if (entries[i].CharacterId != slot.CharacterBinding)
                        continue;

                    consumedEntries[i] = true;
                    return entries[i];
                }

                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (consumedEntries[i])
                    continue;

                consumedEntries[i] = true;
                return entries[i];
            }

            return null;
        }
    }
}
