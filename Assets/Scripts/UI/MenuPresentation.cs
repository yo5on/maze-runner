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
    /// - The main menu, level select and pause menus get a title in the game's display face.
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

            ApplyTheme();
            ScaleWebUI();
        }

        /// <summary>Restyles authored and runtime menu UI after each scene load.</summary>
        private static void ApplyTheme()
        {
            foreach (RectTransform rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect.name == "MainMenuPanel" || rect.name == "LevelSelectPanel")
                {
                    var panelImage = rect.GetComponent<Image>();
                    if (panelImage != null) panelImage.color = PaletteButton.SteelBlue;
                    if (rect.name == "MainMenuPanel") AddLogo(rect);
                    else AddTitle(rect, "Select Level", 64f);
                }
                else if (rect.name == "PauseMenuPanel" || rect.name == "LevelCompletePanel" || rect.name == "GameOverPanel")
                {
                    var panelImage = rect.GetComponent<Image>();
                    if (panelImage != null) panelImage.color = new Color(PaletteButton.SteelBlue.r, PaletteButton.SteelBlue.g, PaletteButton.SteelBlue.b, 0.94f);
                    if (rect.name == "PauseMenuPanel") AddTitle(rect, "Paused", 64f);
                }
            }

            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Character selection owns its layout and styles in CustomizationUIController.
                // (The controller sits on its own object, so the panel is found by name.)
                if (IsInside(button.transform, "CustomizationPanel")) continue;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                string text = label != null ? label.text.Trim().ToUpperInvariant() : button.name.ToUpperInvariant();
                bool pauseSettings = text == "SETTINGS" && IsInside(button.transform, "PauseMenuPanel");
                bool primary = !pauseSettings && (text == "PLAY" || text == "APPLY" || text.Contains("RESUME") || text.Contains("NEXT LEVEL"));
                var image = button.GetComponent<Image>();
                if (image != null) image.color = Color.white; // prevents Button tint multiplying against a colored sprite

                ColorBlock colors = button.colors;
                colors.normalColor = primary ? PaletteButton.Green : PaletteButton.Navy;
                colors.highlightedColor = primary ? PaletteButton.GreenHover : PaletteButton.SecondaryHover;
                colors.pressedColor = primary ? PaletteButton.GreenPressed : new Color32(0x11, 0x1a, 0x2c, 0xff);
                colors.selectedColor = colors.normalColor;
                colors.disabledColor = new Color32(0x59, 0x64, 0x77, 0x80);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                button.colors = colors;
                if (label != null)
                {
                    label.color = Color.white;
                }
            }
        }

        /// <summary>Adds the wordless brand mark above the main menu buttons.</summary>
        private static void AddLogo(RectTransform panel)
        {
            if (panel.GetComponent<VerticalLayoutGroup>() == null || panel.Find("ScreenLogo") != null) return;
            Texture2D logo = Resources.Load<Texture2D>("Brand/MazeRunnerLogo");
            if (logo == null)
            {
                AddTitle(panel, "Maze Runner", 104f);
                return;
            }

            var rect = PaletteUI.NewRect("ScreenLogo", panel);
            rect.SetAsFirstSibling();
            rect.sizeDelta = new Vector2(156f, 156f);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = logo;
            image.raycastTarget = false;
        }

        /// <summary>
        /// A moon-cream title as the first entry of a menu panel's vertical layout, so it sits centred
        /// above the buttons. Skipped if the panel already has one.
        /// </summary>
        private static void AddTitle(RectTransform panel, string value, float size)
        {
            if (panel.GetComponent<VerticalLayoutGroup>() == null || panel.Find("ScreenTitle") != null) return;
            var rect = PaletteUI.NewRect("ScreenTitle", panel);
            rect.SetAsFirstSibling();
            rect.sizeDelta = new Vector2(1000f, size * 1.5f);
            var title = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (PaletteUI.DisplayFont != null) title.font = PaletteUI.DisplayFont;
            title.text = value;
            title.fontSize = size;
            title.color = PaletteButton.Cream;
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.raycastTarget = false;
        }

        private static bool IsInside(Transform child, string ancestorName)
        {
            for (Transform current = child; current != null; current = current.parent)
                if (current.name == ancestorName) return true;
            return false;
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
            if (PaletteUI.DisplayFont != null) title.font = PaletteUI.DisplayFont; // as Level 1's GameOverText
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
