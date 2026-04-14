using _Project.Gameplay.Audio.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Gameplay.UI.Scripts
{
    [RequireComponent(typeof(Button))]
    public class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float hoverVolume = 1f;
        [SerializeField] private bool useDefaultAudioManagerClips;

        private bool hovered;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveListener(PlayClick);
                button.onClick.AddListener(PlayClick);
            }

            PreloadClip(clickClip);
            PreloadClip(hoverClip);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hovered)
                return;

            hovered = true;

            if (AudioManager.Instance == null)
                return;

            if (hoverClip != null)
                AudioManager.Instance.PlayUi(hoverClip, hoverVolume);
            else if (useDefaultAudioManagerClips)
                AudioManager.Instance.PlayDefaultButtonHover(hoverVolume);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        public void PlayClick()
        {
            if (AudioManager.Instance == null)
                return;

            if (clickClip != null)
                AudioManager.Instance.PlayUi(clickClip, clickVolume);
            else if (useDefaultAudioManagerClips)
                AudioManager.Instance.PlayDefaultButtonClick(clickVolume);
        }

        private static void PreloadClip(AudioClip clip)
        {
            if (clip == null)
                return;

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }
    }
}
