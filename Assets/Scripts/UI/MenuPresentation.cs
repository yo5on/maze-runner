using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Player;

namespace MazeRunner.UI
{
    /// <summary>
    /// Presentation-only styling for the in-level overlay menus (pause, game over,
    /// level complete). Applied at runtime when a scene loads so no scene files need
    /// to change. It only touches colors, a backing card and (in Web builds) UI scale;
    /// gameplay, physics, progression and the existing button actions are untouched.
    /// Levels without a game-over panel get one that calls the same
    /// PlayerHealth.RestartLevel / Main Menu actions as GameOverController.
    /// </summary>
    public static class MenuPresentation
    {
        private static readonly Color ScrimColor = new Color(0.02f, 0.03f, 0.05f, 0.86f);
        private static readonly Color PauseScrimColor = new Color(0.02f, 0.03f, 0.05f, 0.7f);
        private static readonly Color CardColor = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        private static readonly Color ButtonNormal = new Color(0.17f, 0.21f, 0.29f, 1f);
        private static readonly Color ButtonHighlighted = new Color(0.23f, 0.29f, 0.39f, 1f);
        private static readonly Color ButtonPressed = new Color(0.11f, 0.14f, 0.20f, 1f);
        private static readonly Color LabelColor = new Color(0.93f, 0.95f, 0.98f, 1f);
        private static readonly Color GameOverColor = new Color(0.97f, 0.38f, 0.38f, 1f);
        private static readonly Color TitleColor = new Color(0.93f, 0.95f, 0.98f, 1f);

        private static readonly HashSet<string> OverlayPanels = new HashSet<string>
        {
            "PauseMenuPanel", "GameOverPanel", "LevelCompletePanel"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) => Apply();
            Apply();
        }

        private static void Apply()
        {
            ScaleWebUI();

            RectTransform pausePanel = null;
            bool hasGameOverPanel = false;
            var rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (RectTransform rect in rects)
            {
                if (!OverlayPanels.Contains(rect.name)) continue;
                if (rect.name == "PauseMenuPanel") pausePanel = rect;
                if (rect.name == "GameOverPanel") hasGameOverPanel = true;
                StylePanel(rect);
            }

            if (!hasGameOverPanel && pausePanel != null)
            {
                PlayerHealth playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
                if (playerHealth != null)
                {
                    CreateGameOverPanel(pausePanel, playerHealth);
                }
            }
        }

        /// <summary>
        /// The Web page renders at a fixed 1920x1080, which makes the Constant Pixel Size
        /// menus very small (and hard to tap on phones). Enlarge them in Web builds only.
        /// </summary>
        private static void ScaleWebUI()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float factor = Application.isMobilePlatform ? 1.8f : 1.4f;
            var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CanvasScaler scaler in scalers)
            {
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    scaler.scaleFactor = factor;
                }
            }
#endif
        }

        private static void StylePanel(RectTransform panel)
        {
            if (panel.GetComponent<MenuCard>() != null) return;

            Image scrim = panel.GetComponent<Image>();
            Sprite roundedSprite = null;
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                StyleButton(button);
                if (roundedSprite == null && button.image != null) roundedSprite = button.image.sprite;
            }

            if (scrim != null)
            {
                scrim.sprite = null;
                scrim.color = panel.name == "PauseMenuPanel" ? PauseScrimColor : ScrimColor;
            }

            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<Button>(true) != null) continue;
                bool isGameOver = text.text.ToUpperInvariant().Contains("GAME OVER");
                text.color = isGameOver ? GameOverColor : TitleColor;
                text.fontStyle |= FontStyles.Bold;
            }

            panel.gameObject.AddComponent<MenuCard>().Init(roundedSprite, CardColor);
        }

        private static void StyleButton(Button button)
        {
            if (button.image != null) button.image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHighlighted;
            colors.selectedColor = ButtonNormal;
            colors.pressedColor = ButtonPressed;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.color = LabelColor;
                label.fontStyle |= FontStyles.Bold;
            }
        }

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

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var titleGo = new GameObject("GameOverText", typeof(RectTransform));
            titleGo.transform.SetParent(panel, false);
            ((RectTransform)titleGo.transform).sizeDelta = new Vector2(320f, 64f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "GAME OVER";
            title.fontSize = 48f;
            title.alignment = TextAlignmentOptions.Center;

            AddButton(templateButton, panel, "RESTART", () => playerHealth.RestartLevel());
            AddButton(templateButton, panel, "MAIN MENU", () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            });

            panelGo.AddComponent<GeneratedGameOverPanel>().Bind(playerHealth);
            StylePanel(panel);
            panelGo.SetActive(false);
        }

        private static void AddButton(Button template, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = Object.Instantiate(template, parent, false);
            button.name = label.Replace(" ", "") + "Button";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            ((RectTransform)button.transform).sizeDelta = new Vector2(250f, 50f);
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = label;
            }
        }
    }
}
