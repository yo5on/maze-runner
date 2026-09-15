using UnityEngine;
using MazeRunner.Player;
using MazeRunner.Audio;

namespace MazeRunner.UI
{
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private PlayerHealth playerHealth;
        
        private void Start()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }
            
            if (playerHealth != null)
            {
                playerHealth.OnGameOver += OnGameOver;
            }
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnGameOver -= OnGameOver;
            }
        }
        
        private void OnGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            // Pause background music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseMusic();
            }
        }
        
        public void RestartLevel()
        {
            if (playerHealth != null)
            {
                playerHealth.RestartLevel();
            }
        }
        
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
