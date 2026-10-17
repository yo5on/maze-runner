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
                ShowNextLevelButton(HasNextLevel());
            }
            Time.timeScale = 0f;
        }
        
        public void NextLevel()
        {
            Time.timeScale = 1f;
            if (LevelProgressionManager.Instance != null)
            {
                int nextLevel = LevelProgressionManager.Instance.CurrentLevelIndex + 1;
                if (LevelProgressionManager.Instance.IsLevelEnabled(nextLevel))
                {
                    LevelProgressionManager.Instance.StartLevel(nextLevel);
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }
        
        private static bool HasNextLevel()
        {
            var progression = LevelProgressionManager.Instance;
            return progression != null && progression.IsLevelEnabled(progression.CurrentLevelIndex + 1);
        }
        
        /// <summary>Hides the panel's "Next Level" button when there is no playable level after this one.</summary>
        private void ShowNextLevelButton(bool show)
        {
            foreach (var button in levelCompletePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentMethodName(i) == nameof(NextLevel))
                    {
                        button.gameObject.SetActive(show);
                    }
                }
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