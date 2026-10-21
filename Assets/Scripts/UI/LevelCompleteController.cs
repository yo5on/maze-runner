using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Levels;

namespace MazeRunner.UI
{
    public class LevelCompleteController : MonoBehaviour
    {
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private TMP_FontAsset titleFont;

        private TMP_Text title;
        private TMP_Text subtitle;

        private void Start()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(false);
            }

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LevelCompleted += OnLevelCompleted;
            }
        }

        private void OnDestroy()
        {
            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.LevelCompleted -= OnLevelCompleted;
            }
        }

        private void OnLevelCompleted(int levelIndex)
        {
            if (levelCompletePanel != null)
            {
                bool hasNext = HasNextLevel();
                levelCompletePanel.SetActive(true);
                ShowNextLevelButton(hasNext);
                FixButtonLabels();
                if (hasNext)
                {
                    ShowTitle($"LEVEL {levelIndex + 1} COMPLETE", $"Level {levelIndex + 2} is unlocked.");
                }
                else
                {
                    // Final level: the victory screen.
                    ShowTitle("VICTORY!", "The Goblin Boss is defeated and all three levels are cleared.");
                }
            }
            Time.timeScale = 0f;
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            if (LevelProgressionManager.Instance != null)
            {
                int nextLevel = LevelProgressionManager.Instance.CurrentLevelIndex + 1;
                if (LevelProgressionManager.Instance.IsLevelEnabled(nextLevel))
                {
                    LevelProgressionManager.Instance.StartLevel(nextLevel);
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }

        private static bool HasNextLevel()
        {
            var progression = LevelProgressionManager.Instance;
            return progression != null && progression.IsLevelEnabled(progression.CurrentLevelIndex + 1);
        }

        /// <summary>Hides the panel's "Next Level" button when there is no playable level after this one.</summary>
        private void ShowNextLevelButton(bool show)
        {
            foreach (var button in levelCompletePanel.GetComponentsInChildren<Button>(true))
            {
                if (HasPersistentCall(button, nameof(NextLevel)))
                {
                    button.gameObject.SetActive(show);
                }
            }
        }

        /// <summary>The scene's Replay button was labelled "Reply".</summary>
        private void FixButtonLabels()
        {
            foreach (var button in levelCompletePanel.GetComponentsInChildren<Button>(true))
            {
                if (!HasPersistentCall(button, nameof(ReplayLevel))) continue;
                foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.text.Trim() == "Reply") text.text = "Replay";
                }
            }
        }

        private static bool HasPersistentCall(Button button, string method)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == method) return true;
            }
            return false;
        }

        /// <summary>A heading above the panel's buttons (created the first time it is needed).</summary>
        private void ShowTitle(string heading, string detail)
        {
            var panel = (RectTransform)levelCompletePanel.transform;
            if (title == null)
            {
                TMP_Text buttonText = levelCompletePanel.GetComponentInChildren<TMP_Text>(true);
                title = CreateText(panel, "CompleteTitle", buttonText, 54f, FontStyles.Bold, Color.white);
                if (titleFont != null) title.font = titleFont;
                subtitle = CreateText(panel, "CompleteSubtitle", buttonText, 26f, FontStyles.Normal, new Color32(0xf3, 0xea, 0xd7, 0xff));
            }

            // Place them above the highest visible button.
            Canvas.ForceUpdateCanvases();
            float top = 0f;
            bool found = false;
            var corners = new Vector3[4];
            foreach (var button in levelCompletePanel.GetComponentsInChildren<Button>(false))
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                float y = panel.InverseTransformPoint(corners[1]).y;
                if (!found || y > top) { top = y; found = true; }
            }

            title.text = heading;
            subtitle.text = detail;
            title.rectTransform.anchoredPosition = new Vector2(0f, top + 100f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, top + 45f);
        }

        private static TMP_Text CreateText(RectTransform parent, string name, TMP_Text style, float size, FontStyles fontStyle, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 64f);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var text = go.AddComponent<TextMeshProUGUI>();
            if (style != null) text.font = style.font;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        public void ReplayLevel()
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
