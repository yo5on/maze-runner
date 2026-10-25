using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Player;
using MazeRunner.Collectibles;
using MazeRunner.Levels;
using MazeRunner.Settings;

namespace MazeRunner.Audio
{
    /// <summary>
    /// The game's one music + sound-effects player. It is spawned from Resources/AudioManager before the
    /// first scene and lives for the whole session, so the menus have sound and the music carries on
    /// across scenes instead of restarting (scenes must not hold their own copy).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public const string ResourcePath = "AudioManager";

        private static AudioManager instance;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip collectClip;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private AudioClip enemyDefeatClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip levelCompleteClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip swordHitClip;
        [SerializeField] private AudioClip respawnClip;
        
        public static AudioManager Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnLoad()
        {
            if (instance != null) return;
            var prefab = Resources.Load<AudioManager>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("AudioManager: Resources/" + ResourcePath + " prefab is missing - the game has no sound.");
                return;
            }
            Instantiate(prefab).name = prefab.name;
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (sfxSource != null)
            {
                sfxSource.ignoreListenerPause = true;
            }

            // Both sources follow the player's volume settings; their configured volumes
            // stay the 100% level.
            VolumeChannelSource.Attach(sfxSource, VolumeChannel.Sfx);
            VolumeChannelSource.Attach(musicSource, VolumeChannel.Music);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void Start()
        {
            ResumeMusic();
            Collectible.OnCollected += OnCollectibleCollected;
            
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LevelCompleted += OnLevelCompleted;
            }
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Collectible.OnCollected -= OnCollectibleCollected;
                
                if (LevelProgressionManager.Instance != null)
                {
                    LevelProgressionManager.Instance.LevelCompleted -= OnLevelCompleted;
                }
                
                instance = null;
            }
        }
        
        public void PlayJump()
        {
            PlaySFX(jumpClip);
        }
        
        public void PlayCollect()
        {
            PlaySFX(collectClip);
        }
        
        public void PlayDamage()
        {
            PlaySFX(damageClip);
        }
        
        public void PlayEnemyDefeat()
        {
            PlaySFX(enemyDefeatClip);
        }
        
        public void PlayCheckpoint()
        {
            PlaySFX(checkpointClip);
        }
        
        public void PlayLevelComplete()
        {
            PlaySFX(levelCompleteClip);
        }
        
        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip);
        }
        
        public void PlaySwordHit()
        {
            PlaySFX(swordHitClip);
        }
        
        public void PlayRespawn()
        {
            PlaySFX(respawnClip);
        }
        
        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
        
        /// <summary>Continues the music where it was paused, or starts it if it has not played yet.</summary>
        public void ResumeMusic()
        {
            if (musicSource == null || musicSource.clip == null || musicSource.isPlaying) return;
            musicSource.UnPause();
            if (!musicSource.isPlaying) musicSource.Play();
        }

        // A pause menu or game-over screen pauses the music; restarting, retrying or going back to a menu
        // loads a scene without resuming it, so every new scene brings it back.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResumeMusic();
        }
        
        private void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        
        private void OnCollectibleCollected(CollectibleType type, int value)
        {
            PlayCollect();
        }
        
        private void OnLevelCompleted(int levelIndex)
        {
            PlayLevelComplete();
        }
        
        /// <summary>Sets the Sound Effects setting (0..1); saved and applied to every SFX source.</summary>
        public void SetSFXVolume(float volume)
        {
            GameSettings.SetSfxPercent(Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f));
        }

        /// <summary>Sets the Music setting (0..1); saved and applied to every music source.</summary>
        public void SetMusicVolume(float volume)
        {
            GameSettings.SetMusicPercent(Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f));
        }
    }
}