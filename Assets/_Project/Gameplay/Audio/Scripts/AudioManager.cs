using _Project.Gameplay.UI.Scripts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using _Project.Systems.GameFlow;

namespace _Project.Gameplay.Audio.Scripts
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicOutputGroup;
        [SerializeField] private AudioMixerGroup sfxOutputGroup;
        [SerializeField] private AudioMixerGroup uiOutputGroup;
        [SerializeField] private GameFlowConfig gameFlowConfig;
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private string uiVolumeParameter = "UiVolume";
        [SerializeField] private float enabledVolumeDb = 0f;
        [SerializeField] private float mutedVolumeDb = -80f;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] [Min(1)] private int uiSourcePoolSize = 4;

        [Header("Defaults")]
        [SerializeField] private AudioClip defaultButtonClickClip;
        [SerializeField] private AudioClip defaultButtonHoverClip;

        [Header("Scene Music")]
        [SerializeField] private bool autoPlaySceneMusic = true;
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] [Range(0f, 1f)] private float menuMusicVolume = 1f;
        [SerializeField] private AudioClip[] mapMusicClips;
        [SerializeField] [Range(0f, 1f)] private float mapMusicVolume = 1f;

        [Header("Gameplay Sfx")]
        [SerializeField] private AudioClip bombExplosionClip;
        [SerializeField] [Range(0f, 1f)] private float bombExplosionVolume = 1f;
        [SerializeField] private AudioClip itemPickupClip;
        [SerializeField] [Range(0f, 1f)] private float itemPickupVolume = 1f;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] [Range(0f, 1f)] private float damageVolume = 1f;
        [SerializeField] private AudioClip winClip;
        [SerializeField] [Range(0f, 1f)] private float winVolume = 1f;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] [Range(0f, 1f)] private float loseVolume = 1f;

        private readonly List<AudioSource> uiSourcePool = new();
        private int lastMapMusicIndex = -1;

        public bool IsMusicEnabled => AudioPreferences.IsMusicEnabled;
        public bool IsSoundEnabled => AudioPreferences.IsSoundEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            PreloadConfiguredClips();
            ApplyPreferencesToMixer();
        }

        private void OnEnable()
        {
            AudioPreferences.PreferencesChanged += ApplyPreferencesToMixer;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            AudioPreferences.PreferencesChanged -= ApplyPreferencesToMixer;
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            TryPlaySceneMusic(SceneManager.GetActiveScene());
        }

        public void SetMusicEnabled(bool enabled)
        {
            AudioPreferences.SetMusicEnabled(enabled);
        }

        public void SetSoundEnabled(bool enabled)
        {
            AudioPreferences.SetSoundEnabled(enabled);
        }

        public void ToggleMusic()
        {
            SetMusicEnabled(!IsMusicEnabled);
        }

        public void ToggleSound()
        {
            SetSoundEnabled(!IsSoundEnabled);
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
        {
            EnsureSources();
            if (musicSource == null || clip == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                musicSource.loop = loop;
                musicSource.volume = volume;
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = volume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
            musicSource.clip = null;
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            EnsureSources();
            if (sfxSource == null || clip == null || !IsSoundEnabled)
                return;

            EnsureClipLoaded(clip);
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayUi(AudioClip clip, float volumeScale = 1f)
        {
            EnsureSources();
            if (clip == null || !IsSoundEnabled)
                return;

            EnsureClipLoaded(clip);
            AudioSource source = GetAvailableUiSource();
            if (source == null)
                return;

            source.PlayOneShot(clip, volumeScale);
        }

        public void PlayDefaultButtonClick(float volumeScale = 1f)
        {
            PlayUi(defaultButtonClickClip, volumeScale);
        }

        public void PlayDefaultButtonHover(float volumeScale = 1f)
        {
            PlayUi(defaultButtonHoverClip, volumeScale);
        }

        public void PlayBombExplosion()
        {
            PlaySfx(bombExplosionClip, bombExplosionVolume);
        }

        public void PlayItemPickup()
        {
            PlaySfx(itemPickupClip, itemPickupVolume);
        }

        public void PlayDamage()
        {
            PlaySfx(damageClip, damageVolume);
        }

        public void PlayWin()
        {
            PlayUi(winClip, winVolume);
        }

        public void PlayLose()
        {
            PlayUi(loseClip, loseVolume);
        }

        public void ApplyPreferencesToMixer()
        {
            EnsureSources();
            SetMixerVolume(musicVolumeParameter, IsMusicEnabled);
            SetMixerVolume(sfxVolumeParameter, IsSoundEnabled);
            SetMixerVolume(uiVolumeParameter, IsSoundEnabled);
            ApplySourceMuteStates();
        }

        private void SetMixerVolume(string parameterName, bool enabled)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            audioMixer.SetFloat(parameterName, enabled ? enabledVolumeDb : mutedVolumeDb);
        }

        private void EnsureSources()
        {
            if (musicSource == null)
                musicSource = CreateChildSource("MusicSource", loop: true);

            if (sfxSource == null)
                sfxSource = CreateChildSource("SfxSource", loop: false);

            if (uiSource == null)
                uiSource = CreateChildSource("UiSource", loop: false);

            EnsureUiSourcePool();

            ApplySourceOutputGroups();
        }

        private AudioSource CreateChildSource(string objectName, bool loop)
        {
            Transform child = transform.Find(objectName);
            GameObject sourceObject = child != null ? child.gameObject : new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.GetComponent<AudioSource>();
            if (source == null)
                source = sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = objectName.StartsWith("UiSource");
            return source;
        }

        private void ApplySourceOutputGroups()
        {
            if (musicSource != null && musicOutputGroup != null)
                musicSource.outputAudioMixerGroup = musicOutputGroup;

            if (sfxSource != null && sfxOutputGroup != null)
                sfxSource.outputAudioMixerGroup = sfxOutputGroup;

            if (uiSource != null && uiOutputGroup != null)
                uiSource.outputAudioMixerGroup = uiOutputGroup;

            for (int i = 0; i < uiSourcePool.Count; i++)
            {
                if (uiSourcePool[i] != null && uiOutputGroup != null)
                    uiSourcePool[i].outputAudioMixerGroup = uiOutputGroup;
            }
        }

        private void EnsureUiSourcePool()
        {
            int targetCount = Mathf.Max(1, uiSourcePoolSize);
            for (int i = uiSourcePool.Count; i < targetCount; i++)
            {
                AudioSource pooledSource = CreateChildSource($"UiSource_{i + 1}", loop: false);
                pooledSource.priority = 0;
                uiSourcePool.Add(pooledSource);
            }
        }

        private AudioSource GetAvailableUiSource()
        {
            EnsureUiSourcePool();

            for (int i = 0; i < uiSourcePool.Count; i++)
            {
                AudioSource source = uiSourcePool[i];
                if (source != null && !source.isPlaying)
                    return source;
            }

            return uiSourcePool.Count > 0 ? uiSourcePool[0] : uiSource;
        }

        private void PreloadDefaultClips()
        {
            EnsureClipLoaded(defaultButtonClickClip);
            EnsureClipLoaded(defaultButtonHoverClip);
        }

        private void ApplySourceMuteStates()
        {
            bool musicMuted = !IsMusicEnabled;
            bool soundMuted = !IsSoundEnabled;

            if (musicSource != null)
                musicSource.mute = musicMuted;

            if (sfxSource != null)
                sfxSource.mute = soundMuted;

            if (uiSource != null)
                uiSource.mute = soundMuted;

            for (int i = 0; i < uiSourcePool.Count; i++)
            {
                if (uiSourcePool[i] != null)
                    uiSourcePool[i].mute = soundMuted;
            }
        }

        private void PreloadConfiguredClips()
        {
            PreloadDefaultClips();
            EnsureClipLoaded(menuMusicClip);
            EnsureClipLoaded(bombExplosionClip);
            EnsureClipLoaded(itemPickupClip);
            EnsureClipLoaded(damageClip);
            EnsureClipLoaded(winClip);
            EnsureClipLoaded(loseClip);

            if (mapMusicClips == null)
                return;

            for (int i = 0; i < mapMusicClips.Length; i++)
                EnsureClipLoaded(mapMusicClips[i]);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryPlaySceneMusic(scene);
        }

        private void TryPlaySceneMusic(Scene scene)
        {
            if (!autoPlaySceneMusic || !scene.IsValid())
                return;

            if (HasSceneMusicOverride(scene))
                return;

            if (IsMapScene(scene.name))
            {
                AudioClip mapClip = GetRandomMapMusicClip();
                if (mapClip != null)
                    PlayMusic(mapClip, true, mapMusicVolume);

                return;
            }

            if (menuMusicClip != null)
                PlayMusic(menuMusicClip, true, menuMusicVolume);
        }

        private bool HasSceneMusicOverride(Scene scene)
        {
            AudioSceneMusic[] sceneMusicOverrides = Object.FindObjectsByType<AudioSceneMusic>();
            for (int i = 0; i < sceneMusicOverrides.Length; i++)
            {
                AudioSceneMusic overrideComponent = sceneMusicOverrides[i];
                if (overrideComponent == null || !overrideComponent.isActiveAndEnabled)
                    continue;

                if (overrideComponent.gameObject.scene == scene)
                    return true;
            }

            return false;
        }

        private bool IsMapScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (gameFlowConfig != null)
            {
                List<ArenaDefinition> arenas = gameFlowConfig.GetSelectableArenas();
                for (int i = 0; i < arenas.Count; i++)
                {
                    ArenaDefinition arena = arenas[i];
                    if (arena == null || string.IsNullOrWhiteSpace(arena.sceneName))
                        continue;

                    if (string.Equals(arena.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return sceneName.StartsWith("Map", System.StringComparison.OrdinalIgnoreCase);
        }

        private AudioClip GetRandomMapMusicClip()
        {
            if (mapMusicClips == null || mapMusicClips.Length == 0)
                return null;

            List<int> validIndices = new();
            for (int i = 0; i < mapMusicClips.Length; i++)
            {
                if (mapMusicClips[i] != null)
                    validIndices.Add(i);
            }

            if (validIndices.Count == 0)
                return null;

            int selectedListIndex = Random.Range(0, validIndices.Count);
            int selectedClipIndex = validIndices[selectedListIndex];

            if (validIndices.Count > 1 && selectedClipIndex == lastMapMusicIndex)
            {
                selectedListIndex = (selectedListIndex + 1) % validIndices.Count;
                selectedClipIndex = validIndices[selectedListIndex];
            }

            lastMapMusicIndex = selectedClipIndex;
            return mapMusicClips[selectedClipIndex];
        }

        private static void EnsureClipLoaded(AudioClip clip)
        {
            if (clip == null)
                return;

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }
    }
}
