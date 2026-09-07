using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class LevelCompleteController : MonoBehaviour
    {
        [SerializeField] private GameObject levelCompletePanel;
        
        private void Start()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
            }
            
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LevelCompleted += OnLevelCompleted;
            }
        }
        
        private void OnDestroy()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LevelCompleted -= OnLevelCompleted;
            }
        }
        
        private void OnLevelCompleted(int levelIndex)
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
            }
            Time.timeScale = 0f;
        }
        
        public void NextLevel()
        {
            Time.timeScale = 1f;
            if (LevelProgressionManager.Instance != null)
            {
                int nextLevel = LevelProgressionManager.Instance.CurrentLevelIndex + 1;
                LevelProgressionManager.Instance.StartLevel(nextLevel);
            }
        }
        
        public void ReplayLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}