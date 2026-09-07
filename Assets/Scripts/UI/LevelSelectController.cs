using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private Button[] levelButtons;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockedColor = Color.white;
        
        private void Start()
        {
            RefreshLevelButtons();
        }
        
        private void RefreshLevelButtons()
        {
            if (LevelProgressionManager.Instance == null || levelButtons == null) return;
            
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                
                bool unlocked = LevelProgressionManager.Instance.IsLevelUnlocked(i);
                levelButtons[i].interactable = unlocked;
                
                var colors = levelButtons[i].colors;
                colors.normalColor = unlocked ? unlockedColor : lockedColor;
                levelButtons[i].colors = colors;
                
                TMP_Text buttonText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = unlocked ? $"Level {i + 1}" : $"Level {i + 1}\n(Locked)";
                }
            }
        }
        
        public void SelectLevel(int levelIndex)
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.StartLevel(levelIndex);
            }
        }
        
        public void ReturnToMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}