using UnityEngine;
using TMPro;
using MazeRunner.Player;
using MazeRunner.Levels;
using MazeRunner.Collectibles;

namespace MazeRunner.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        
        [Header("UI Elements")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text levelText;
        
        private int coinCount;
        
        private void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealth;
                UpdateHealth(playerHealth.CurrentHealth);
            }
            
            Collectible.OnCollected += OnCollectibleCollected;
            
            UpdateCoinCount();
            UpdateLevelName();
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealth;
            }
            
            Collectible.OnCollected -= OnCollectibleCollected;
        }
        
        private void UpdateHealth(int currentHealth)
        {
            if (healthText != null && playerHealth != null)
            {
                healthText.text = $"Health: {currentHealth} / {playerHealth.MaxHealth}";
            }
        }
        
        private void OnCollectibleCollected(CollectibleType type, int value)
        {
            if (type == CollectibleType.Coin || type == CollectibleType.Gem)
            {
                coinCount += value;
                UpdateCoinCount();
            }
        }
        
        private void UpdateCoinCount()
        {
            if (coinText != null)
            {
                coinText.text = $"Coins: {coinCount}";
            }
        }
        
        private void UpdateLevelName()
        {
            if (levelText != null)
            {
                if (LevelProgressionManager.Instance != null)
                {
                    int levelNumber = LevelProgressionManager.Instance.CurrentLevelNumber;
                    levelText.text = $"Level {levelNumber}";
                }
                else
                {
                    levelText.text = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                }
            }
        }
    }
}