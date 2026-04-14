using UnityEngine;

namespace _Project.Gameplay.Audio.Scripts
{
    public class AudioSceneMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool stopMusicOnDisable;

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        public void Play()
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlayMusic(musicClip, loop, volume);
        }

        private void OnDisable()
        {
            if (!stopMusicOnDisable || AudioManager.Instance == null)
                return;

            AudioManager.Instance.StopMusic();
        }
    }
}
