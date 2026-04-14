using UnityEngine;

namespace _Project.Gameplay.UI.Scripts
{
    public static class AudioPreferences
    {
        private const string MusicKey = "AudioPreferences.MusicEnabled";
        private const string SoundKey = "AudioPreferences.SoundEnabled";
        public static event System.Action PreferencesChanged;

        public static bool IsMusicEnabled
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => SetMusicEnabled(value);
        }

        public static bool IsSoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => SetSoundEnabled(value);
        }

        public static void SetMusicEnabled(bool enabled)
        {
            SetBool(MusicKey, enabled);
        }

        public static void SetSoundEnabled(bool enabled)
        {
            SetBool(SoundKey, enabled);
        }

        private static void SetBool(string key, bool enabled)
        {
            int nextValue = enabled ? 1 : 0;
            if (PlayerPrefs.GetInt(key, 1) == nextValue)
                return;

            PlayerPrefs.SetInt(key, nextValue);
            PlayerPrefs.Save();
            PreferencesChanged?.Invoke();
        }
    }
}
