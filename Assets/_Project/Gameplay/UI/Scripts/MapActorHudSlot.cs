using _Project.Gameplay.Player.Scripts;
using _Project.Systems.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Gameplay.UI.Scripts
{
    public class MapActorHudSlot : MonoBehaviour
    {
        public enum SlotBindingMode
        {
            ByOrder = 0,
            ByCharacter = 1
        }

        [Header("Binding")]
        [SerializeField] private SlotBindingMode bindingMode = SlotBindingMode.ByOrder;
        [SerializeField] private CharacterId characterBinding = CharacterId.Character1;

        [Header("Visibility")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private bool hideWhenUnused = true;

        [Header("Portrait")]
        [SerializeField] private Image portraitImage;

        [Header("Labels")]
        [SerializeField] private Text nameText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text bombText;
        [SerializeField] private Text rangeText;
        [SerializeField] private Text speedText;

        [Header("State Colors")]
        [SerializeField] private Color activeColor = new Color32(32, 96, 184, 255);
        [SerializeField] private Color inactiveColor = new Color32(32, 96, 184, 125);
        [SerializeField] private bool autoAddWhiteOutline = true;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private Vector2 outlineDistance = new Vector2(1.2f, -1.2f);

        private ActorHudRegistry.Entry? currentEntry;
        private int cachedBomb;
        private int cachedRange;
        private float cachedSpeed;

        public SlotBindingMode BindingMode => bindingMode;
        public CharacterId CharacterBinding => characterBinding;

        private void Awake()
        {
            ApplyThemeDefaultsIfNeeded();
            EnsureTextOutline(nameText);
            EnsureTextOutline(healthText);
            EnsureTextOutline(bombText);
            EnsureTextOutline(rangeText);
            EnsureTextOutline(speedText);
        }

        public void Bind(ActorHudRegistry.Entry? entry, GameFlowConfig config)
        {
            currentEntry = entry;

            if (!entry.HasValue)
            {
                SetContentVisible(false);

                return;
            }

            SetContentVisible(true);

            ApplyCharacterPortrait(entry.Value, config);
            ApplyName(entry.Value.DisplayName);
            RefreshValues();
        }

        public void RefreshValues()
        {
            if (!currentEntry.HasValue)
                return;

            PlayerController actor = currentEntry.Value.PlayerController;
            bool hasLiveActor = actor != null;

            if (hasLiveActor)
            {
                cachedBomb = actor.BombCapacity;
                cachedRange = actor.BombRangeStat;
                cachedSpeed = actor.MoveSpeedStat;
            }

            SetText(healthText, hasLiveActor ? actor.HealthStat.ToString() : "0");
            SetText(bombText, hasLiveActor ? actor.BombCapacity.ToString() : cachedBomb.ToString());
            SetText(rangeText, hasLiveActor ? actor.BombRangeStat.ToString() : cachedRange.ToString());
            SetText(speedText, hasLiveActor ? actor.MoveSpeedStat.ToString("0.#") : cachedSpeed.ToString("0.#"));

            Color tint = hasLiveActor ? activeColor : inactiveColor;
            ApplyTint(tint);
        }

        private void ApplyCharacterPortrait(ActorHudRegistry.Entry entry, GameFlowConfig config)
        {
            if (portraitImage == null || config == null)
                return;

            CharacterDefinition character = config.GetCharacter(entry.CharacterId);
            if (character == null || character.portraitSprite == null)
                return;

            portraitImage.sprite = character.portraitSprite;
            portraitImage.enabled = true;
            portraitImage.preserveAspect = true;
        }

        private void ApplyName(string displayName)
        {
            SetText(nameText, string.IsNullOrWhiteSpace(displayName)
                ? string.Empty
                : displayName.ToUpperInvariant());
        }

        private void ApplyTint(Color tint)
        {
            if (portraitImage != null)
                portraitImage.color = tint;

            if (nameText != null)
                nameText.color = tint;

            if (healthText != null)
                healthText.color = tint;

            if (bombText != null)
                bombText.color = tint;

            if (rangeText != null)
                rangeText.color = tint;

            if (speedText != null)
                speedText.color = tint;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
                label.text = string.IsNullOrWhiteSpace(value) ? string.Empty : value.ToUpperInvariant();
        }

        private void EnsureTextOutline(Text label)
        {
            if (!autoAddWhiteOutline || label == null)
                return;

            Outline outline = label.GetComponent<Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<Outline>();

            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
            outline.useGraphicAlpha = true;
        }

        private void SetContentVisible(bool isVisible)
        {
            if (!hideWhenUnused)
                isVisible = true;

            if (contentRoot == null)
                return;

            if (contentRoot == gameObject)
            {
                if (portraitImage != null)
                    portraitImage.enabled = isVisible;

                if (nameText != null)
                    nameText.enabled = isVisible;

                if (healthText != null)
                    healthText.enabled = isVisible;

                if (bombText != null)
                    bombText.enabled = isVisible;

                if (rangeText != null)
                    rangeText.enabled = isVisible;

                if (speedText != null)
                    speedText.enabled = isVisible;

                return;
            }

            contentRoot.SetActive(isVisible);
        }

        private void ApplyThemeDefaultsIfNeeded()
        {
            if (Approximately(activeColor, Color.white))
                activeColor = new Color32(32, 96, 184, 255);

            Color defaultInactive = new Color(1f, 1f, 1f, 0.45f);
            if (Approximately(inactiveColor, defaultInactive))
                inactiveColor = new Color32(32, 96, 184, 125);
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= 0.01f
                   && Mathf.Abs(a.g - b.g) <= 0.01f
                   && Mathf.Abs(a.b - b.b) <= 0.01f
                   && Mathf.Abs(a.a - b.a) <= 0.01f;
        }
    }
}
