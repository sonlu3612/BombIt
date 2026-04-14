using _Project.Systems.GameFlow;
using _Project.Gameplay.Match.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Gameplay.UI.Scripts
{
    public class MapRoundHudController : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private Text levelValueText;
        [SerializeField] private Text timeValueText;

        [Header("Timer")]
        [SerializeField] private float roundDurationSeconds = 120f;
        [SerializeField] private bool autoStartOnEnable = true;

        [Header("Style")]
        [SerializeField] private Color textColor = new Color32(32, 96, 184, 255);
        [SerializeField] private bool autoAddWhiteOutline = true;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private Vector2 outlineDistance = new Vector2(1.2f, -1.2f);

        private float remainingSeconds;
        private bool running;
        private bool expired;

        public bool IsExpired => expired;
        public float RemainingSeconds => remainingSeconds;

        private void Awake()
        {
            ApplyTextStyle(levelValueText);
            ApplyTextStyle(timeValueText);
        }

        private void OnEnable()
        {
            RefreshLevelText();

            if (autoStartOnEnable)
                RestartTimer();
            else
                RefreshTimeText();
        }

        private void Update()
        {
            if (!running || expired)
                return;

            if (Map.Scripts.MapPauseButtonController.IsGamePaused || RoundIntroState.IsActive)
                return;

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            RefreshTimeText();

            if (remainingSeconds > 0f)
                return;

            expired = true;
            running = false;
        }

        public void RestartTimer()
        {
            remainingSeconds = Mathf.Max(0f, roundDurationSeconds);
            expired = false;
            running = true;
            RefreshLevelText();
            RefreshTimeText();
        }

        public void RefreshLevelText()
        {
            if (levelValueText == null)
                return;

            string levelValue = GameSession.IsConfigured
                ? $"{GameSession.CurrentRound}/{GameSession.TotalRounds}"
                : "1";

            levelValueText.text = levelValue.ToUpperInvariant();
        }

        public void RefreshTimeText()
        {
            if (timeValueText == null)
                return;

            int totalSeconds = Mathf.CeilToInt(remainingSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeValueText.text = $"{minutes:0}:{seconds:00}";
        }

        private void ApplyTextStyle(Text label)
        {
            if (label == null)
                return;

            label.color = textColor;

            if (!autoAddWhiteOutline)
                return;

            Outline outline = label.GetComponent<Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<Outline>();

            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
            outline.useGraphicAlpha = true;
        }
    }
}
