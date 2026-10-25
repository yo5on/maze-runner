using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class MainMenuController : MonoBehaviour
    {
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
