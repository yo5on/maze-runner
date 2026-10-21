using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private Button[] levelButtons;
        [SerializeField] private Color lockedColor = new Color32(0x59, 0x64, 0x77, 0x80);
        [SerializeField] private Color unlockedColor = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        
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
                
                // Levels that are on hold are not offered at all.
                bool isEnabled = LevelProgressionManager.Instance.IsLevelEnabled(i);
                levelButtons[i].gameObject.SetActive(isEnabled);
                if (!isEnabled) continue;
                
                bool unlocked = LevelProgressionManager.Instance.IsLevelUnlocked(i);
                levelButtons[i].interactable = unlocked;
                
                var colors = levelButtons[i].colors;
                bool firstLevel = i == 0 && unlocked;
                colors.normalColor = unlocked ? (firstLevel ? new Color32(0x4d, 0xa8, 0x61, 0xff) : unlockedColor) : lockedColor;
                colors.highlightedColor = firstLevel
                    ? new Color32(0x6d, 0xb8, 0x7d, 0xff)
                    : new Color32(0x40, 0x49, 0x60, 0xff);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = firstLevel
                    ? new Color32(0x39, 0x7b, 0x4b, 0xff)
                    : new Color32(0x11, 0x1a, 0x2c, 0xff);
                colors.disabledColor = lockedColor;
                colors.colorMultiplier = 1f;
                levelButtons[i].colors = colors;

                var image = levelButtons[i].GetComponent<Image>();
                if (image != null) image.color = Color.white;
                
                TMP_Text buttonText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = unlocked ? $"Level {i + 1}" : $"Level {i + 1}\n(Locked)";
                    buttonText.color = Color.white;
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
