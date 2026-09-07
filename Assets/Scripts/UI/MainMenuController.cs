using UnityEngine;
using UnityEngine.SceneManagement;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public void PlayGame()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.StartLevel(0);
            }
            else
            {
                SceneManager.LoadScene("TestLevel");
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
            Debug.Log("Settings menu not yet implemented.");
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}