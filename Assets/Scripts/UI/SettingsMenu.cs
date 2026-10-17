using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MazeRunner.Audio;
using MazeRunner.Settings;

namespace MazeRunner.UI
{
    /// <summary>
    /// The Settings menu: Music and Sound Effects volume sliders (0-100%) and the touch
    /// control layout (A: joystick + Jump, B: Left/Right buttons + Jump).
    ///
    /// It is built when a scene loads, from that scene's own menu buttons, so it keeps the
    /// game's menu style:
    /// - Main menu: opened by the existing Settings button (MainMenuController.OpenSettings).
    /// - Levels: a Settings button is added to the pause menu, so volumes and the control
    ///   layout can be changed during play. Leaving the menu returns to the pause menu and the
    ///   new layout shows as soon as play resumes.
    /// Every change applies immediately and is saved by <see cref="GameSettings"/>.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        // Portfolio palette.
        private static readonly Color Navy = new Color32(0x1b, 0x1e, 0x4a, 0xff);
        private static readonly Color Stone = new Color32(0xaf, 0xae, 0xa2, 0xff);
        private static readonly Color Crimson = new Color32(0xd2, 0x13, 0x19, 0xff);
        private static readonly Color CardColor = new Color32(0x1b, 0x1e, 0x4a, 0xf2);
        private static readonly Color TrackColor = new Color(0.686f, 0.682f, 0.635f, 0.35f);

        private const float CardWidth = 720f;
        private const float CardHeight = 580f;

        public static SettingsMenu Current { get; private set; }
        public static bool IsOpen => Current != null && Current.gameObject.activeSelf;

        private GameObject returnPanel;
        private bool inGame;

        private Slider musicSlider;
        private Slider sfxSlider;
        private TMP_Text musicValue;
        private TMP_Text sfxValue;
        private Button joystickButton;
        private Button buttonsButton;
        private TMP_Text hint;
        private Color buttonTextColor = new Color(0.196f, 0.196f, 0.196f, 1f);
        private float lastPreviewTime;

        // ------------------------------------------------------------------ set-up

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded += (scene, mode) => BuildForScene();
            BuildForScene();
        }

        private static void BuildForScene()
        {
            RectTransform mainMenuPanel = null, pausePanel = null;
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect.name == "MainMenuPanel") mainMenuPanel = rect;
                else if (rect.name == "PauseMenuPanel") pausePanel = rect;
            }

            if (mainMenuPanel != null)
            {
                Create(mainMenuPanel, inGame: false);
            }
            else if (pausePanel != null)
            {
                SettingsMenu menu = Create(pausePanel, inGame: true);
                if (menu != null) AddPauseMenuButton(pausePanel, menu);
            }
        }

        /// <summary>Opens the current scene's settings menu (main menu Settings button).</summary>
        public static void OpenCurrent()
        {
            if (Current != null) Current.Open();
        }

        private static SettingsMenu Create(RectTransform panel, bool inGame)
        {
            Button template = panel.GetComponentInChildren<Button>(true);
            if (template == null || panel.parent == null) return null;

            var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(panel.parent, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Same backdrop as the menu it replaces (it also blocks clicks to the game).
            Image backdrop = root.GetComponent<Image>();
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                backdrop.sprite = panelImage.sprite;
                backdrop.type = panelImage.type;
                backdrop.color = panelImage.color;
            }
            else
            {
                backdrop.color = new Color(1f, 1f, 1f, 0.392f);
            }

            var menu = root.AddComponent<SettingsMenu>();
            menu.returnPanel = panel.gameObject;
            menu.inGame = inGame;
            menu.BuildContent(template);
            root.SetActive(false);
            Current = menu;
            return menu;
        }

        private static void AddPauseMenuButton(RectTransform pausePanel, SettingsMenu menu)
        {
            Button template = pausePanel.GetComponentInChildren<Button>(true);
            Button button = Instantiate(template, pausePanel, false);
            button.name = "SettingsButton";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(menu.Open);
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true)) text.text = "Settings";
            // Resume, Restart, Settings, Main Menu (the panel's layout group stacks them).
            button.transform.SetSiblingIndex(Mathf.Max(0, pausePanel.childCount - 2));
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            GameSettings.VolumeChanged -= Refresh;
            GameSettings.ControlModeChanged -= OnControlModeChanged;
            WebBridge.InputDeviceChanged -= RefreshHint;
        }

        // ------------------------------------------------------------- open/close

        public void Open()
        {
            Refresh();
            if (returnPanel != null) returnPanel.SetActive(false);
            gameObject.SetActive(true);
            // Paused game: let the music play while the menu is open so its volume can be heard.
            if (inGame && AudioManager.Instance != null) AudioManager.Instance.ResumeMusic();
            Select(musicSlider);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            if (inGame && AudioManager.Instance != null) AudioManager.Instance.PauseMusic();
            if (returnPanel != null)
            {
                returnPanel.SetActive(true);
                Select(returnPanel.GetComponentInChildren<Button>());
            }
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }

        private void Update()
        {
            // Escape goes back (in levels PauseMenuController leaves Escape to this menu while it is open).
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        // ---------------------------------------------------------------- content

        private void BuildContent(Button template)
        {
            TMP_Text templateText = template.GetComponentInChildren<TMP_Text>(true);
            TMP_FontAsset font = templateText != null ? templateText.font : null;
            if (templateText != null) buttonTextColor = templateText.color;

            var card = NewRect("Card", transform);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = CardColor;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Stone.r, Stone.g, Stone.b, 0.45f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Crimson accent bar along the top edge.
            var accent = NewRect("Accent", card);
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.sizeDelta = new Vector2(0f, 4f);
            accent.gameObject.AddComponent<Image>().color = Crimson;

            float y = -34f;
            Text(card, font, "Settings", 40f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, y), new Vector2(CardWidth, 54f));

            y -= 70f;
            SectionLabel(card, font, "AUDIO", y);
            y -= 58f;
            musicSlider = SliderRow(card, font, "Music", y, out musicValue);
            y -= 58f;
            sfxSlider = SliderRow(card, font, "Sound Effects", y, out sfxValue);

            y -= 62f;
            SectionLabel(card, font, "MOBILE CONTROLS", y);
            y -= 62f;
            joystickButton = CloneButton(template, card, "A  Joystick + Jump", new Vector2(-160f, y), new Vector2(300f, 56f),
                () => GameSettings.SetControlMode(TouchControlMode.Joystick));
            buttonsButton = CloneButton(template, card, "B  Left / Right + Jump", new Vector2(160f, y), new Vector2(300f, 56f),
                () => GameSettings.SetControlMode(TouchControlMode.Buttons));

            y -= 46f;
            hint = Text(card, font, "", 19f, Stone, TextAlignmentOptions.Top, new Vector2(0f, y), new Vector2(620f, 72f));
            hint.textWrappingMode = TextWrappingModes.Normal;

            float buttonsY = -CardHeight + 48f;
            CloneButton(template, card, "Reset to Defaults", new Vector2(-125f, buttonsY), new Vector2(230f, 50f), GameSettings.ResetToDefaults);
            CloneButton(template, card, "Back", new Vector2(125f, buttonsY), new Vector2(230f, 50f), Close);

            musicSlider.onValueChanged.AddListener(v => GameSettings.SetMusicPercent(Mathf.RoundToInt(v)));
            sfxSlider.onValueChanged.AddListener(v =>
            {
                GameSettings.SetSfxPercent(Mathf.RoundToInt(v));
                PreviewSfx();
            });

            GameSettings.VolumeChanged += Refresh;
            GameSettings.ControlModeChanged += OnControlModeChanged;
            WebBridge.InputDeviceChanged += RefreshHint;
            Refresh();
        }

        private void PreviewSfx()
        {
            // A short click so the new level can be heard (throttled while dragging).
            if (Time.unscaledTime - lastPreviewTime < 0.2f) return;
            lastPreviewTime = Time.unscaledTime;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        }

        private void OnControlModeChanged(TouchControlMode mode) => Refresh();

        private void Refresh()
        {
            if (musicSlider == null) return;
            musicSlider.SetValueWithoutNotify(GameSettings.MusicPercent);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxPercent);
            musicValue.text = GameSettings.MusicPercent + "%";
            sfxValue.text = GameSettings.SfxPercent + "%";
            StyleModeButton(joystickButton, GameSettings.ControlMode == TouchControlMode.Joystick);
            StyleModeButton(buttonsButton, GameSettings.ControlMode == TouchControlMode.Buttons);
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (hint == null) return;
            hint.text = WebBridge.IsTouchDevice
                ? "The chosen layout appears on screen while a level is running, and switching takes effect as soon as play resumes."
                : "On-screen controls appear only on touch screens, so this choice has no effect on this computer. " +
                  "Move with A / D or the arrow keys and jump with Space.";
        }

        private void StyleModeButton(Button button, bool selected)
        {
            var image = button.targetGraphic as Image;
            if (image != null) image.color = selected ? Crimson : Color.white;
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = selected ? Color.white : buttonTextColor;
                text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // ---------------------------------------------------------------- helpers

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>A text box whose top-centre sits at position (relative to the card's top).</summary>
        private static TMP_Text Text(Transform parent, TMP_FontAsset font, string value, float size, Color color,
            TextAlignmentOptions alignment, Vector2 position, Vector2 boxSize)
        {
            var rect = NewRect("Text", parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void SectionLabel(Transform parent, TMP_FontAsset font, string value, float y)
        {
            var text = Text(parent, font, value, 18f, Stone, TextAlignmentOptions.Left, new Vector2(0f, y), new Vector2(CardWidth - 100f, 26f));
            text.characterSpacing = 8f;
            text.fontStyle = FontStyles.Bold;

            var rule = NewRect("Rule", parent);
            rule.anchorMin = rule.anchorMax = new Vector2(0.5f, 1f);
            rule.pivot = new Vector2(0.5f, 1f);
            rule.anchoredPosition = new Vector2(0f, y - 30f);
            rule.sizeDelta = new Vector2(CardWidth - 100f, 1f);
            var image = rule.gameObject.AddComponent<Image>();
            image.color = new Color(Stone.r, Stone.g, Stone.b, 0.3f);
            image.raycastTarget = false;
        }

        /// <summary>"Label  [====o-----]  80%" centred on y (relative to the card's top).</summary>
        private static Slider SliderRow(Transform parent, TMP_FontAsset font, string label, float y, out TMP_Text value)
        {
            const float handleWidth = 22f;
            float left = -(CardWidth - 100f) / 2f;

            Text(parent, font, label, 24f, Color.white, TextAlignmentOptions.Left,
                new Vector2(left + 100f, y + 17f), new Vector2(200f, 34f));
            value = Text(parent, font, "", 24f, Color.white, TextAlignmentOptions.Right,
                new Vector2(-left - 40f, y + 17f), new Vector2(80f, 34f));
            value.fontStyle = FontStyles.Bold;

            var root = NewRect(label.Replace(" ", "") + "Slider", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(30f, y);
            root.sizeDelta = new Vector2(320f, 48f); // tall, easy-to-touch hit area
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            RectTransform Inset(string name, Transform p, float height)
            {
                var r = NewRect(name, p);
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(1f, 0.5f);
                r.sizeDelta = new Vector2(-handleWidth, height);
                return r;
            }

            var track = Inset("Track", root, 10f);
            track.gameObject.AddComponent<Image>().color = TrackColor;

            var fillArea = Inset("Fill Area", root, 10f);
            var fill = NewRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = Crimson;

            var handleArea = Inset("Handle Slide Area", root, 0f);
            handleArea.anchorMin = new Vector2(0f, 0f);
            handleArea.anchorMax = new Vector2(1f, 1f);
            handleArea.sizeDelta = new Vector2(-handleWidth, 0f);
            var handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(handleWidth, -12f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleOutline = handle.gameObject.AddComponent<Outline>();
            handleOutline.effectColor = Navy;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            var colors = slider.colors;
            colors.highlightedColor = new Color(0.95f, 0.92f, 0.84f);
            colors.pressedColor = Stone;
            colors.selectedColor = new Color(0.95f, 0.92f, 0.84f);
            slider.colors = colors;
            return slider;
        }

        /// <summary>A copy of one of the scene's menu buttons with a new label and action.</summary>
        private static Button CloneButton(Button template, Transform parent, string label, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            Button button = Instantiate(template, parent, false);
            button.name = label.Replace(" ", "") + "Button";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = true;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = label;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
            button.gameObject.SetActive(true);
            return button;
        }
    }
}
