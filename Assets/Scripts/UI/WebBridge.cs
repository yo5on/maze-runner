using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Settings;

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
    /// Page -> Unity: SendMessage("WebBridge", "SetSafeArea", "l,t,r,b") passes how far the
    /// device's safe area (notch, rounded corners) cuts into the game, in render pixels.
    /// HUD elements anchored to a screen edge (direct children of a root overlay canvas,
    /// e.g. the hearts) are moved in by that amount; centred and full-screen UI such as
    /// the menus is left alone.
    ///
    /// Unity -> page: MazeRunner_SetControlMode calls window.mazeRunnerOnControls(mode) with
    /// "joystick" or "buttons" (Settings > Mobile Controls) at start-up, whenever the setting
    /// changes and on RequestState. The page shows exactly one movement layout.
    ///
    /// Page -> Unity: SendMessage("WebBridge", "SetInputDevice", "touch" | "desktop") tells the
    /// settings menu whether the on-screen controls are in use on this device.
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

        [DllImport("__Internal")]
        private static extern void MazeRunner_SetControlMode(string mode);
#else
        private static void MazeRunner_ReportState(string state) { }
        private static void MazeRunner_Quit() { }
        private static void MazeRunner_SetControlMode(string mode) { }
#endif

        /// <summary>True when the page reports a touch screen (on-screen controls in use).</summary>
        public static bool IsTouchDevice { get; private set; }

        /// <summary>Raised when the page reports its input device.</summary>
        public static event System.Action InputDeviceChanged;

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

        private Vector4 safeArea; // left, top, right, bottom in screen pixels
        private readonly Dictionary<RectTransform, Vector2> hudBasePositions = new Dictionary<RectTransform, Vector2>();

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
            GameSettings.ControlModeChanged += ReportControlMode;
            FindPauseMenu();
            ReportControlMode(GameSettings.ControlMode);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.ControlModeChanged -= ReportControlMode;
        }

        private static void ReportControlMode(TouchControlMode mode)
        {
            MazeRunner_SetControlMode(mode == TouchControlMode.Buttons ? "buttons" : "joystick");
        }

        // Called from the page via SendMessage: "touch" or "desktop".
        public void SetInputDevice(string device)
        {
            bool touch = device == "touch";
            if (touch == IsTouchDevice) return;
            IsTouchDevice = touch;
            InputDeviceChanged?.Invoke();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindPauseMenu();
            hudBasePositions.Clear();
            StartCoroutine(ApplySafeAreaNextFrame());
        }

        // Called from the page via SendMessage: "left,top,right,bottom" in screen pixels.
        public void SetSafeArea(string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length != 4) return;
            var v = new float[4];
            for (int i = 0; i < 4; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i])) return;
            }
            safeArea = new Vector4(v[0], v[1], v[2], v[3]);
            ApplySafeArea();
        }

        private IEnumerator ApplySafeAreaNextFrame()
        {
            yield return null; // let canvas scalers settle after the scene loads
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                var scaler = canvas.GetComponent<CanvasScaler>();
                float scale = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize
                    ? scaler.scaleFactor : canvas.scaleFactor;
                if (scale <= 0f) scale = 1f;

                foreach (Transform child in canvas.transform)
                {
                    if (!(child is RectTransform rect)) continue;
                    if (!hudBasePositions.TryGetValue(rect, out Vector2 basePosition))
                    {
                        basePosition = rect.anchoredPosition;
                        hudBasePositions[rect] = basePosition;
                    }

                    Vector2 min = rect.anchorMin, max = rect.anchorMax;
                    Vector2 offset = Vector2.zero;
                    if (min.x == 0f && max.x == 0f) offset.x = safeArea.x / scale;
                    else if (min.x == 1f && max.x == 1f) offset.x = -safeArea.z / scale;
                    if (min.y == 1f && max.y == 1f) offset.y = -safeArea.y / scale;
                    else if (min.y == 0f && max.y == 0f) offset.y = safeArea.w / scale;

                    rect.anchoredPosition = basePosition + offset;
                }
            }
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
            ReportControlMode(GameSettings.ControlMode);
        }
    }
}
