using UnityEngine;

namespace DinosaurRunner.Audio
{
    /// <summary>
    /// Central audio manager managing background music, 2D sound effects,
    /// footstep cadence, 3D spatial dinosaur sounds, and volume settings.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource footstepSource;

        [Header("Background Music")]
        [SerializeField] private AudioClip bgmClip;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip slideClip;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private AudioClip powerupClip;
        [SerializeField] private AudioClip buttonClip;
        [SerializeField] private AudioClip footstepClip;

        [Header("Volume Defaults")]
        [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float defaultSfxVolume = 0.85f;

        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureAudioSources();
            LoadVolumeSettings();
        }

        private void Start()
        {
            if (bgmSource != null && bgmClip != null && !bgmSource.isPlaying)
            {
                PlayBGM(bgmClip);
            }
        }

        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            if (footstepSource == null)
            {
                footstepSource = gameObject.AddComponent<AudioSource>();
                footstepSource.loop = false;
                footstepSource.playOnAwake = false;
            }
        }

        private void LoadVolumeSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);

            ApplyMusicVolume(MusicVolume);
            ApplySfxVolume(SfxVolume);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            ApplyMusicVolume(MusicVolume);
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
            ApplySfxVolume(SfxVolume);
        }

        private void ApplyMusicVolume(float volume)
        {
            if (bgmSource != null)
                bgmSource.volume = volume;
        }

        private void ApplySfxVolume(float volume)
        {
            if (sfxSource != null)
                sfxSource.volume = volume;
            if (footstepSource != null)
                footstepSource.volume = volume * 0.45f;
        }

        public void PlayBGM(AudioClip clip = null)
        {
            if (clip != null)
                bgmClip = clip;

            if (bgmClip != null && bgmSource != null)
            {
                bgmSource.clip = bgmClip;
                bgmSource.volume = MusicVolume;
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null)
                bgmSource.Stop();
        }

        public void PlayCoinSFX() => PlayOneShot(coinClip, 0.9f);
        public void PlayJumpSFX() => PlayOneShot(jumpClip, 0.85f);
        public void PlaySlideSFX() => PlayOneShot(slideClip, 0.8f);
        public void PlayImpactSFX() => PlayOneShot(impactClip, 1f);
        public void PlayGameOverSFX() => PlayOneShot(gameOverClip, 1f);
        public void PlayPowerupSFX() => PlayOneShot(powerupClip, 1f);
        public void PlayButtonSFX() => PlayOneShot(buttonClip, 0.7f);

        public void PlayFootstepSFX()
        {
            if (footstepClip == null || footstepSource == null || !footstepSource.enabled)
                return;

            footstepSource.pitch = Random.Range(0.9f, 1.15f);
            footstepSource.PlayOneShot(footstepClip, SfxVolume * 0.4f);
        }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, SfxVolume * volumeScale);
            }
        }

        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, SfxVolume * volume);
        }

        public void AssignClips(
            AudioClip bgm,
            AudioClip coin,
            AudioClip jump,
            AudioClip slide,
            AudioClip impact,
            AudioClip gameOver,
            AudioClip powerup,
            AudioClip button,
            AudioClip footstep)
        {
            bgmClip = bgm;
            coinClip = coin;
            jumpClip = jump;
            slideClip = slide;
            impactClip = impact;
            gameOverClip = gameOver;
            powerupClip = powerup;
            buttonClip = button;
            footstepClip = footstep;
        }
    }
}
