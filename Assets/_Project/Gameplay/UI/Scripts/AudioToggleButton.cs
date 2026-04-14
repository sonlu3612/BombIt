using _Project.Gameplay.Audio.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Gameplay.UI.Scripts
{
    [RequireComponent(typeof(Button))]
    public class AudioToggleButton : MonoBehaviour
    {
        public enum AudioToggleType
        {
            Music = 0,
            Sound = 1
        }

        [SerializeField] private AudioToggleType toggleType;
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (targetImage == null && button != null)
                targetImage = button.targetGraphic as Image;

            if (button != null)
            {
                button.onClick.RemoveListener(Toggle);
                button.onClick.AddListener(Toggle);
            }

            RefreshVisual();
        }

        private void OnEnable()
        {
            AudioPreferences.PreferencesChanged += RefreshVisual;
            RefreshVisual();
        }

        private void OnDisable()
        {
            AudioPreferences.PreferencesChanged -= RefreshVisual;
        }

        public void Toggle()
        {
            switch (toggleType)
            {
                case AudioToggleType.Music:
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.ToggleMusic();
                    else
                        AudioPreferences.IsMusicEnabled = !AudioPreferences.IsMusicEnabled;
                    break;

                case AudioToggleType.Sound:
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.ToggleSound();
                    else
                        AudioPreferences.IsSoundEnabled = !AudioPreferences.IsSoundEnabled;
                    break;
            }
        }

        public void RefreshVisual()
        {
            if (targetImage == null)
                return;

            Sprite sprite = IsEnabled() ? onSprite : offSprite;
            if (sprite == null)
                return;

            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
            targetImage.enabled = true;
        }

        private bool IsEnabled()
        {
            return toggleType == AudioToggleType.Music
                ? AudioPreferences.IsMusicEnabled
                : AudioPreferences.IsSoundEnabled;
        }
    }
}
