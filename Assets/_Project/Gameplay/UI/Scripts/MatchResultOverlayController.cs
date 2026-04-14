using UnityEngine;
using UnityEngine.UI;
using _Project.Gameplay.Audio.Scripts;

namespace _Project.Gameplay.UI.Scripts
{
    public class MatchResultOverlayController : MonoBehaviour
    {
        public enum MatchResultType
        {
            None = 0,
            Win = 1,
            Lose = 2
        }

        public static bool IsShowingResult { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image dimmerImage;
        [SerializeField] private Image resultImage;

        [Header("Sprites")]
        [SerializeField] private Sprite winSprite;
        [SerializeField] private Sprite loseSprite;

        [Header("Style")]
        [SerializeField] private Color dimmerColor = new Color(0f, 0f, 0f, 0.78f);
        [SerializeField] private bool freezeTimeScale = true;
        [SerializeField] private bool pauseAudioListener = true;

        private float previousTimeScale = 1f;
        private bool previousAudioPause;
        private Color hiddenDimmerColor;
        private Color hiddenResultColor;

        private void Awake()
        {
            CacheHiddenColors();
            HideImmediate();
        }

        public bool ShowResult(MatchResultType resultType)
        {
            if (resultType == MatchResultType.None)
                return false;

            Sprite sprite = ResolveSprite(resultType);
            if (resultImage == null || sprite == null)
            {
                Debug.LogWarning($"[{nameof(MatchResultOverlayController)}] Missing result image or sprite for {resultType}.", this);
                return false;
            }

            SetOverlayVisible(true);
            transform.SetAsLastSibling();

            if (dimmerImage != null)
            {
                dimmerImage.color = dimmerColor;
                dimmerImage.enabled = true;
            }

            resultImage.sprite = sprite;
            resultImage.preserveAspect = true;
            resultImage.color = Color.white;
            resultImage.enabled = true;
            PlayResultAudio(resultType);

            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;

            if (freezeTimeScale)
                Time.timeScale = 0f;

            if (pauseAudioListener)
                AudioListener.pause = true;

            IsShowingResult = true;
            return true;
        }

        public void HideImmediate()
        {
            if (dimmerImage != null)
            {
                dimmerImage.color = hiddenDimmerColor;
                dimmerImage.enabled = false;
            }

            if (resultImage != null)
            {
                resultImage.color = hiddenResultColor;
                resultImage.enabled = false;
            }

            SetOverlayVisible(false);

            if (freezeTimeScale)
                Time.timeScale = 1f;

            if (pauseAudioListener)
                AudioListener.pause = previousAudioPause;

            IsShowingResult = false;
        }

        private Sprite ResolveSprite(MatchResultType resultType)
        {
            return resultType switch
            {
                MatchResultType.Win => winSprite,
                MatchResultType.Lose => loseSprite,
                _ => null
            };
        }

        private void OnDisable()
        {
            IsShowingResult = false;
        }

        private void CacheHiddenColors()
        {
            hiddenDimmerColor = dimmerImage != null
                ? new Color(dimmerColor.r, dimmerColor.g, dimmerColor.b, 0f)
                : Color.clear;

            hiddenResultColor = resultImage != null
                ? new Color(1f, 1f, 1f, 0f)
                : Color.clear;
        }

        private static void PlayResultAudio(MatchResultType resultType)
        {
            if (AudioManager.Instance == null)
                return;

            switch (resultType)
            {
                case MatchResultType.Win:
                    AudioManager.Instance.PlayWin();
                    break;

                case MatchResultType.Lose:
                    AudioManager.Instance.PlayLose();
                    break;
            }
        }

        private void SetOverlayVisible(bool visible)
        {
            if (overlayRoot == null)
                return;

            if (overlayRoot == gameObject)
            {
                CanvasGroup canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = visible ? 1f : 0f;
                    canvasGroup.interactable = visible;
                    canvasGroup.blocksRaycasts = visible;
                }

                return;
            }

            overlayRoot.SetActive(visible);
        }
    }
}
