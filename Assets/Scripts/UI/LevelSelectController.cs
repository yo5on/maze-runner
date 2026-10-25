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

            // The furthest unlocked level is the one to play next: it gets the green primary style.
            int next = 0;
            for (int i = 0; i < levelButtons.Length; i++)
                if (progress.IsLevelUnlocked(i)) next = i;

            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                
                // Levels that are on hold are not offered at all.
                bool isEnabled = progress.IsLevelEnabled(i);
                levelButtons[i].gameObject.SetActive(isEnabled);
                if (!isEnabled) continue;
                
                bool unlocked = progress.IsLevelUnlocked(i);
                // Clearing a level unlocks the one after it (the last level's clear is not recorded).
                bool cleared = unlocked && progress.IsLevelUnlocked(i + 1);
                bool primary = unlocked && i == next;
                // The keyboard starts on the level to play next.
                if (primary) MenuNavigator.PreferFocus(FindPanel(levelButtons[i].transform), levelButtons[i]);
                levelButtons[i].interactable = unlocked;
                
                var colors = levelButtons[i].colors;
                colors.normalColor = !unlocked ? lockedColor : primary ? PaletteButton.Green : unlockedColor;
                colors.highlightedColor = primary ? PaletteButton.GreenHover : PaletteButton.SecondaryHover;
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = primary ? PaletteButton.GreenPressed : new Color32(0x11, 0x1a, 0x2c, 0xff);
                colors.disabledColor = lockedColor;
                colors.colorMultiplier = 1f;
                levelButtons[i].colors = colors;

                var image = levelButtons[i].GetComponent<Image>();
                if (image != null) image.color = Color.white;
                
                TMP_Text buttonText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    // One line: the 50 px button has no room for a second.
                    string state = !unlocked ? "Locked" : cleared ? "Cleared" : null;
                    buttonText.text = state == null ? $"Level {i + 1}" : $"Level {i + 1}  <size=75%><color=#B4C2D6>{state}</color></size>";
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
