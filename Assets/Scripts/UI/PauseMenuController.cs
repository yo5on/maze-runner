using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace MazeRunner.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private bool startPaused = false;
        
        private bool isPaused;
        private InputAction pauseAction;
        
        private void Awake()
        {
            pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pauseAction.performed += OnPausePerformed;
        }
        
        private void OnEnable()
        {
            pauseAction.Enable();
        }
        
        private void OnDisable()
        {
            pauseAction.Disable();
        }
        
        private void OnDestroy()
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Dispose();
        }
        
        private void Start()
        {
            if (startPaused)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }
        
        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
        
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }
        
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }
        
        public void RestartLevel()
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