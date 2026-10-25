using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            // A returning player sees which level Play continues (Level Select replays earlier ones).
            var progress = LevelProgressionManager.Instance;
            if (progress == null || progress.ResumeLevelIndex == 0) return;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!CallsPlayGame(button)) continue;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = $"Play  <size=70%>Level {progress.ResumeLevelIndex + 1}</size>";
            }
        }

        private static bool CallsPlayGame(Button button)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == nameof(PlayGame)) return true;
            return false;
        }

        /// <summary>Continues the saved current level (Level 1 for a new player); never resets progress.</summary>
        public void PlayGame()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.ContinueGame();
            }
            else
            {
                SceneManager.LoadScene("Level1");
            }
        }

        public void OpenCustomization()
        {
            SceneManager.LoadScene("Customization");
        }

        public void OpenLevelSelect()
        {
            SceneManager.LoadScene("LevelSelect");
        }

        public void OpenSettings()
        {
            SettingsMenu.OpenCurrent();
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #elif UNITY_WEBGL
            // A web page cannot close its own tab, so Quit returns to the preview page.
            WebBridge.QuitToPreview();
            #else
            Application.Quit();
            #endif
        }
    }
}
