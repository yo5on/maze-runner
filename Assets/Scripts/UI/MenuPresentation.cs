using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Player;

namespace MazeRunner.UI
{
    /// <summary>
    /// Runtime UI support applied when a scene loads. It does not restyle the
    /// original Unity menus:
    /// - Web builds render at a fixed 1920x1080, which makes the Constant Pixel Size
    ///   UI very small; it is enlarged by one uniform factor so menus keep the same
    ///   proportions on desktop and mobile and stay easy to tap.
    /// - Levels 2 and 3 have no game-over UI (a game over froze the screen). They get
    ///   a panel built to match Level 1's original GameOverPanel, using the same
    ///   PlayerHealth.RestartLevel / Main Menu actions as GameOverController.
    /// </summary>
    public static class MenuPresentation
    {
        private const float WebUIScale = 1.4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) => Apply();
            Apply();
        }

        private static void Apply()
        {
            RectTransform pausePanel = null;
            bool hasGameOverPanel = false;
            var rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (RectTransform rect in rects)
            {
                if (rect.name == "PauseMenuPanel") pausePanel = rect;
                if (rect.name == "GameOverPanel") hasGameOverPanel = true;
            }

            if (!hasGameOverPanel && pausePanel != null)
            {
                PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
                if (playerHealth != null)
                {
                    CreateGameOverPanel(pausePanel, playerHealth);
                }
            }

            ScaleWebUI();
        }

        private static void ScaleWebUI()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CanvasScaler scaler in scalers)
            {
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaler.scaleFactor = WebUIScale;
                }
            }
#endif
        }

        /// <summary>
        /// Mirrors Level 1's GameOverPanel: full-screen backdrop identical to the
        /// level's pause panel, a white 48pt "GAME OVER" title and two 200x50
        /// buttons cloned from the pause menu, at the same positions.
        /// </summary>
        private static void CreateGameOverPanel(RectTransform pausePanel, PlayerHealth playerHealth)
        {
            Button templateButton = pausePanel.GetComponentInChildren<Button>(true);
            if (templateButton == null) return;

            var panelGo = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)panelGo.transform;
            panel.SetParent(pausePanel.parent, false);
            panel.SetAsLastSibling();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Image backdrop = panelGo.GetComponent<Image>();
            Image pauseBackdrop = pausePanel.GetComponent<Image>();
            if (pauseBackdrop != null)
            {
                backdrop.sprite = pauseBackdrop.sprite;
                backdrop.type = pauseBackdrop.type;
                backdrop.color = pauseBackdrop.color;
            }

            var titleGo = new GameObject("GameOverText", typeof(RectTransform));
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.SetParent(panel, false);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 100f);
            titleRect.sizeDelta = new Vector2(300f, 60f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "GAME OVER";
            title.fontSize = 48f;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.NoWrap;

            AddButton(templateButton, panel, "RESTART", new Vector2(0f, 0f), () => playerHealth.RestartLevel());
            AddButton(templateButton, panel, "MAIN MENU", new Vector2(0f, -70f), () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            });

            panelGo.AddComponent<GeneratedGameOverPanel>().Bind(playerHealth);
            panelGo.SetActive(false);
        }

        private static void AddButton(Button template, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            Button button = Object.Instantiate(template, parent, false);
            button.name = label.Replace(" ", "") + "Button";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200f, 50f);
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = label;
            }
        }
    }
}
