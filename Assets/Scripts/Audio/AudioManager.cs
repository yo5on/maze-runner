using UnityEngine;
using MazeRunner.Player;
using MazeRunner.Collectibles;
using MazeRunner.Levels;
using MazeRunner.Settings;

namespace MazeRunner.Audio
{
    public class AudioManager : MonoBehaviour
    {
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
        }
        
        private void Start()
        {
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
        
        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
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