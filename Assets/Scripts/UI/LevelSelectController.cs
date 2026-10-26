using UnityEngine;
using UnityEngine.InputSystem;
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

        private void Update()
        {
            // Escape goes back to the main menu.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToMainMenu();
        }
        
        private void RefreshLevelButtons()
        {
            var progress = LevelProgressionManager.Instance;
            if (progress == null || levelButtons == null) return;

            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                
                // Levels that are on hold are not offered at all.
                bool isEnabled = progress.IsLevelEnabled(i);
                levelButtons[i].gameObject.SetActive(isEnabled);
                if (!isEnabled) continue;
                
                bool unlocked = progress.IsLevelUnlocked(i);
                bool cleared = progress.IsLevelCleared(i);
                // Keyboard starts on the furthest unlocked level, without marking it as cleared.
                if (unlocked && (i + 1 >= levelButtons.Length || !progress.IsLevelUnlocked(i + 1)))
                    MenuNavigator.PreferFocus(FindPanel(levelButtons[i].transform), levelButtons[i]);
                levelButtons[i].interactable = unlocked;
                
                var colors = levelButtons[i].colors;
                colors.normalColor = !unlocked ? lockedColor : cleared ? PaletteButton.Green : unlockedColor;
                colors.highlightedColor = cleared ? PaletteButton.GreenHover : PaletteButton.SecondaryHover;
                colors.selectedColor = colors.normalColor;
                colors.pressedColor = cleared ? PaletteButton.GreenPressed : new Color32(0x11, 0x1a, 0x2c, 0xff);
                colors.disabledColor = lockedColor;
                colors.colorMultiplier = 1f;
                levelButtons[i].colors = colors;

                var image = levelButtons[i].GetComponent<Image>();
                if (image != null) image.color = Color.white;
                
                TMP_Text buttonText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Level {i + 1}";
                    buttonText.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                }
            }
        }
        
        private static GameObject FindPanel(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent) if (p.name == "LevelSelectPanel") return p.gameObject;
            return null;
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
