using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.UI
{
    /// <summary>
    /// Connects the Web page's pause button to the game's existing pause menu.
    ///
    /// Unity -> page: reports the pause state whenever it changes, through
    /// MazeRunner_ReportState (Assets/Plugins/WebGL/MazeRunnerBridge.jslib), which
    /// calls window.mazeRunnerOnState(state) with one of:
    ///   "none"    - no pause menu in this scene (main menu, level select, ...)
    ///   "playing" - a level is running and can be paused
    ///   "paused"  - the original pause menu is open
    ///   "blocked" - the game is stopped by another menu (game over, level complete)
    ///
    /// Unity -> page: QuitToPreview() (main menu Quit) calls window.mazeRunnerQuit(),
    /// which returns to the preview page.
    ///
    /// Page -> Unity: unityInstance.SendMessage("WebBridge", "TogglePause") opens or
    /// closes the original pause menu through PauseMenuController.Pause()/Resume(),
    /// the same methods the Escape key uses. No new pause UI is created.
    /// </summary>
    public class WebBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MazeRunner_ReportState(string state);

        [DllImport("__Internal")]
        private static extern void MazeRunner_Quit();
#else
        private static void MazeRunner_ReportState(string state) { }
        private static void MazeRunner_Quit() { }
#endif

        /// <summary>
        /// Web builds: Quit (main menu) leaves the game for the preview page, via
        /// MazeRunner_Quit in MazeRunnerBridge.jslib. The page decides the destination.
        /// </summary>
        public static void QuitToPreview()
        {
            MazeRunner_Quit();
        }

        private PauseMenuController pauseMenu;
        private GameObject pausePanel;
        private string lastState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("WebBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<WebBridge>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindPauseMenu();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindPauseMenu();
        }

        private void FindPauseMenu()
        {
            pauseMenu = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            pausePanel = null;
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect.name == "PauseMenuPanel") { pausePanel = rect.gameObject; break; }
            }
        }

        private string CurrentState()
        {
            if (pauseMenu == null || !pauseMenu.isActiveAndEnabled) return "none";
            if (pausePanel != null && pausePanel.activeInHierarchy) return "paused";
            if (Time.timeScale == 0f) return "blocked";
            return "playing";
        }

        private void Update()
        {
            string state = CurrentState();
            if (state == lastState) return;
            lastState = state;
            MazeRunner_ReportState(state);
        }

        // Called from the page via SendMessage.
        public void TogglePause()
        {
            string state = CurrentState();
            if (state == "playing") pauseMenu.Pause();
            else if (state == "paused") pauseMenu.Resume();
        }

        // Called from the page via SendMessage after it (re)registers its listener.
        public void RequestState()
        {
            lastState = null;
        }
    }
}
