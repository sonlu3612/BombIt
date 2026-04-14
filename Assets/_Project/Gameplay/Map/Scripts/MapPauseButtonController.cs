using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Project.Gameplay.Audio.Scripts;
using _Project.Gameplay.UI.Scripts;
using _Project.Systems.GameFlow;

namespace _Project.Gameplay.Map.Scripts
{
    [RequireComponent(typeof(Button))]
    public class MapPauseButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static bool IsGamePaused { get; private set; }
        public static bool IsMusicEnabled => AudioPreferences.IsMusicEnabled;
        public static bool IsSoundEnabled => AudioPreferences.IsSoundEnabled;

        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button resumeMenuButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Image musicToggleIcon;
        [SerializeField] private Image soundToggleIcon;

        [Header("Scene Config")]
        [SerializeField] private GameFlowConfig gameFlowConfig;
        [SerializeField] private string fallbackMainMenuScene = "MainMenu";

        [Header("Pause Sprites")]
        [SerializeField] private Sprite pauseNormalSprite;
        [SerializeField] private Sprite pauseHoverSprite;

        [Header("Resume Sprites")]
        [SerializeField] private Sprite resumeNormalSprite;
        [SerializeField] private Sprite resumeHoverSprite;

        [Header("Audio Toggle Sprites")]
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Options")]
        [SerializeField] private bool pauseAudioListener = true;
        [SerializeField] private bool resumeOnDisable = true;
        [SerializeField] private bool toggleWithEscape = true;

        private bool isPaused;
        private bool isPointerOver;
        private bool suppressHoverUntilExit;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (targetImage == null && button != null)
                targetImage = button.targetGraphic as Image;

            if (button != null)
            {
                button.onClick.RemoveListener(TogglePause);
                button.onClick.AddListener(TogglePause);
                button.transition = Selectable.Transition.None;
            }

            WireMenuButtons();
            ApplyVisualState();
            ApplyAudioToggleVisuals();
            SetOverlayVisible(false);
            ApplyPauseState(false);
        }

        private void OnEnable()
        {
            AudioPreferences.PreferencesChanged += ApplyAudioToggleVisuals;
            ApplyAudioToggleVisuals();
        }

        private void Start()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.ApplyPreferencesToMixer();
        }

        private void Update()
        {
            if (!toggleWithEscape)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (overlayRoot != null && overlayRoot.activeSelf)
                {
                    ResumeFromMenu();
                    return;
                }

                TogglePause();
            }
        }

        public void TogglePause()
        {
            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            suppressHoverUntilExit = true;
            ApplyPauseState(paused);
            SetOverlayVisible(paused);
            ApplyVisualState();
        }

        public void ResumeFromMenu()
        {
            SetPaused(false);
        }

        public void GoToMainMenu()
        {
            SetPaused(false);
            GameSession.ClearSession();

            string sceneName = gameFlowConfig != null && !string.IsNullOrWhiteSpace(gameFlowConfig.MainMenuScene)
                ? gameFlowConfig.MainMenuScene
                : fallbackMainMenuScene;

            SceneManager.LoadScene(sceneName);
        }

        public void ToggleMusic()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.ToggleMusic();
            else
                AudioPreferences.IsMusicEnabled = !AudioPreferences.IsMusicEnabled;
        }

        public void ToggleSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.ToggleSound();
            else
                AudioPreferences.IsSoundEnabled = !AudioPreferences.IsSoundEnabled;
        }

        private void ApplyPauseState(bool paused)
        {
            IsGamePaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pauseAudioListener)
                AudioListener.pause = paused;
        }

        private void ApplyVisualState()
        {
            if (button == null || targetImage == null)
                return;

            Sprite normalSprite = isPaused ? resumeNormalSprite : pauseNormalSprite;
            Sprite hoverSprite = isPaused ? resumeHoverSprite : pauseHoverSprite;
            bool shouldUseHover = isPointerOver && !suppressHoverUntilExit;

            Sprite resolvedSprite = shouldUseHover && hoverSprite != null
                ? hoverSprite
                : normalSprite;

            if (resolvedSprite != null)
                targetImage.sprite = resolvedSprite;
        }

        private void ApplyAudioToggleVisuals()
        {
            ApplyToggleIcon(musicToggleIcon, IsMusicEnabled ? musicOnSprite : musicOffSprite);
            ApplyToggleIcon(soundToggleIcon, IsSoundEnabled ? soundOnSprite : soundOffSprite);
        }

        private static void ApplyToggleIcon(Image icon, Sprite sprite)
        {
            if (icon == null || sprite == null)
                return;

            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.enabled = true;
        }

        private void SetOverlayVisible(bool visible)
        {
            if (overlayRoot == null)
                return;

            overlayRoot.SetActive(visible);

            if (visible)
                overlayRoot.transform.SetAsLastSibling();
        }

        private void WireMenuButtons()
        {
            if (resumeMenuButton != null)
            {
                resumeMenuButton.onClick.RemoveListener(ResumeFromMenu);
                resumeMenuButton.onClick.AddListener(ResumeFromMenu);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(GoToMainMenu);
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (musicToggleButton != null)
            {
                musicToggleButton.onClick.RemoveListener(ToggleMusic);
                musicToggleButton.onClick.AddListener(ToggleMusic);
            }

            if (soundToggleButton != null)
            {
                soundToggleButton.onClick.RemoveListener(ToggleSound);
                soundToggleButton.onClick.AddListener(ToggleSound);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            suppressHoverUntilExit = false;
            ApplyVisualState();
        }

        private void OnDisable()
        {
            AudioPreferences.PreferencesChanged -= ApplyAudioToggleVisuals;

            if (resumeOnDisable)
            {
                SetOverlayVisible(false);
                ApplyPauseState(false);
            }
        }

        private void OnDestroy()
        {
            if (resumeOnDisable)
            {
                SetOverlayVisible(false);
                ApplyPauseState(false);
            }
        }
    }
}
