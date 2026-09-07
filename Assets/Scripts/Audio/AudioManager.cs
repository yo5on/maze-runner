using UnityEngine;
using MazeRunner.Player;
using MazeRunner.Collectibles;
using MazeRunner.Levels;

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
        
        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null)
            {
                sfxSource.volume = Mathf.Clamp01(volume);
            }
        }
        
        public void SetMusicVolume(float volume)
        {
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}