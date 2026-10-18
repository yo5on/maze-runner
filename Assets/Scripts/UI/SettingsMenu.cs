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
    /// Styled after the yo5on portfolio and drawn only in its three colours: midnight navy
    /// #1b1e4a (surfaces), warm stone #afaea2 (text, borders, inactive tracks) and crimson
    /// #d21319 (active slider fill, chosen option, focus). The heading uses the portfolio's
    /// Instrument Serif Italic (Resources/Fonts, see UiFontSetup).
    ///
    /// It is built when a scene loads:
    /// - Main menu: opened by the existing Settings button (MainMenuController.OpenSettings),
    ///   over a solid navy screen.
    /// - Levels: a Settings button is added to the pause menu; the menu opens over the paused
    ///   game dimmed with navy. Back returns to the pause menu, and a new control layout shows
    ///   as soon as play resumes.
    /// Every change applies immediately and is saved by <see cref="GameSettings"/>.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        private static Color Navy => PaletteButton.Navy;
        private static Color Stone => PaletteButton.Stone;
        private static Color Crimson => PaletteButton.Crimson;

        private const float CardWidth = 760f;
        private const float CardHeight = 620f;
        private const float Margin = 56f;            // inner left/right margin of the card
        private const float ContentWidth = CardWidth - 2f * Margin;
        private const string HeadingFont = "Fonts/InstrumentSerif-Italic SDF";

        public static SettingsMenu Current { get; private set; }
        public static bool IsOpen => Current != null && Current.gameObject.activeSelf;

        private GameObject returnPanel;
        private bool inGame;
        private RectTransform card;

        private Slider musicSlider;
        private Slider sfxSlider;
        private TMP_Text musicValue;
        private TMP_Text sfxValue;
        private PaletteButton joystickButton;
        private PaletteButton buttonsButton;
        private TMP_Text hint;
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
            TMP_Text templateText = template.GetComponentInChildren<TMP_Text>(true);

            var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.SetParent(panel.parent, false);
            rect.SetAsLastSibling();
            Stretch(rect);

            // Main menu: a clean navy screen. In a level: the paused game, dimmed with navy.
            Image backdrop = root.GetComponent<Image>();
            backdrop.color = inGame ? new Color(Navy.r, Navy.g, Navy.b, 0.9f) : Navy;

            var menu = root.AddComponent<SettingsMenu>();
            menu.returnPanel = panel.gameObject;
            menu.inGame = inGame;
            menu.BuildContent(templateText != null ? templateText.font : null);
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
            FitCard();
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

        private void OnRectTransformDimensionsChange()
        {
            FitCard();
        }

        /// <summary>Shrinks the card (never enlarges it) so it always fits inside the screen with a margin.</summary>
        private void FitCard()
        {
            if (card == null) return;
            Rect area = ((RectTransform)transform).rect;
            if (area.width <= 0f || area.height <= 0f) return;
            float scale = Mathf.Min(1f, (area.width - 48f) / CardWidth, (area.height - 48f) / CardHeight);
            card.localScale = new Vector3(scale, scale, 1f);
        }

        // ---------------------------------------------------------------- content

        private void BuildContent(TMP_FontAsset bodyFont)
        {
            var headingFont = Resources.Load<TMP_FontAsset>(HeadingFont);
            if (headingFont == null) headingFont = bodyFont;

            // Card: a thin stone frame around a navy surface.
            card = NewRect("Card", transform);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(CardWidth, CardHeight);
            card.gameObject.AddComponent<Image>().color = Stone;
            var surface = NewRect("Surface", card);
            Stretch(surface, 1.5f);
            surface.gameObject.AddComponent<Image>().color = Navy;
            Transform c = surface;

            float left = -ContentWidth / 2f;

            Label(c, bodyFont, "MAZE RUNNER", 15f, Stone, left, -36f, ContentWidth, 22f, TextAlignmentOptions.Left, 14f, FontStyles.Bold);
            Label(c, headingFont, "Settings", 72f, Stone, left, -52f, ContentWidth, 86f, TextAlignmentOptions.Left);
            Bar(c, left, -140f, 48f, 3f, Crimson);

            Section(c, bodyFont, "AUDIO", -168f);
            musicSlider = SliderRow(c, bodyFont, "Music", -240f, out musicValue);
            sfxSlider = SliderRow(c, bodyFont, "Sound Effects", -298f, out sfxValue);

            Section(c, bodyFont, "MOBILE CONTROLS", -344f);
            float optionWidth = (ContentWidth - 16f) / 2f;
            joystickButton = MakeButton(c, bodyFont, "A", "Joystick + Jump", PaletteButton.Style.Option,
                new Vector2(left + optionWidth / 2f, -424f), new Vector2(optionWidth, 64f),
                () => GameSettings.SetControlMode(TouchControlMode.Joystick));
            buttonsButton = MakeButton(c, bodyFont, "B", "Left / Right + Jump", PaletteButton.Style.Option,
                new Vector2(-left - optionWidth / 2f, -424f), new Vector2(optionWidth, 64f),
                () => GameSettings.SetControlMode(TouchControlMode.Buttons));

            hint = Label(c, bodyFont, "", 18f, Stone, left, -468f, ContentWidth, 56f, TextAlignmentOptions.TopLeft);
            hint.textWrappingMode = TextWrappingModes.Normal;

            float buttonsY = -CardHeight + 60f;
            MakeButton(c, bodyFont, null, "Reset to Defaults", PaletteButton.Style.Quiet,
                new Vector2(left + 120f, buttonsY), new Vector2(240f, 54f), GameSettings.ResetToDefaults);
            MakeButton(c, bodyFont, null, "Back", PaletteButton.Style.Strong,
                new Vector2(-left - 120f, buttonsY), new Vector2(240f, 54f), Close);

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
            joystickButton.IsOn = GameSettings.ControlMode == TouchControlMode.Joystick;
            buttonsButton.IsOn = GameSettings.ControlMode == TouchControlMode.Buttons;
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (hint == null) return;
            hint.text = WebBridge.IsTouchDevice
                ? "The chosen layout appears on screen while a level is running and switches as soon as play resumes."
                : "On-screen controls appear only on touch screens, so this choice has no effect on this computer. " +
                  "Move with A / D or the arrow keys and jump with Space.";
        }

        // ---------------------------------------------------------------- helpers

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Places a rect by its top-left corner, relative to the card's top centre.</summary>
        private static RectTransform TopLeft(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static TMP_Text Label(Transform parent, TMP_FontAsset font, string value, float size, Color color,
            float x, float y, float width, float height, TextAlignmentOptions alignment, float spacing = 0f,
            FontStyles style = FontStyles.Normal)
        {
            var rect = TopLeft(parent, "Text", x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.characterSpacing = spacing;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Image Bar(Transform parent, float x, float y, float width, float height, Color color)
        {
            var image = TopLeft(parent, "Bar", x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Spaced capitals with a hairline rule under them.</summary>
        private static void Section(Transform parent, TMP_FontAsset font, string title, float y)
        {
            float left = -ContentWidth / 2f;
            Label(parent, font, title, 15f, Stone, left, y, ContentWidth, 22f, TextAlignmentOptions.Left, 14f, FontStyles.Bold);
            Bar(parent, left, y - 30f, ContentWidth, 1.5f, Stone);
        }

        /// <summary>"Label  [====o-----]  80%" centred on y (relative to the card's top).</summary>
        private static Slider SliderRow(Transform parent, TMP_FontAsset font, string label, float y, out TMP_Text value)
        {
            const float labelWidth = 210f;
            const float valueWidth = 84f;
            const float handleWidth = 16f;
            float left = -ContentWidth / 2f;

            Label(parent, font, label, 24f, Stone, left, y + 17f, labelWidth, 34f, TextAlignmentOptions.Left);
            value = Label(parent, font, "", 24f, Stone, -left - valueWidth, y + 17f, valueWidth, 34f, TextAlignmentOptions.Right, 0f, FontStyles.Bold);

            float sliderWidth = ContentWidth - labelWidth - valueWidth - 24f;
            var root = TopLeft(parent, label.Replace(" ", "") + "Slider", left + labelWidth, y + 24f, sliderWidth, 48f);
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(Navy.r, Navy.g, Navy.b, 0f); // invisible, makes the whole row easy to touch

            RectTransform Inset(string name, Transform p, float height)
            {
                var r = NewRect(name, p);
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(1f, 0.5f);
                r.sizeDelta = new Vector2(-handleWidth, height);
                return r;
            }

            var track = Inset("Track", root, 4f);
            track.gameObject.AddComponent<Image>().color = Stone;

            var fillArea = Inset("Fill Area", root, 4f);
            var fill = NewRect("Fill", fillArea);
            Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = Crimson;

            var handleArea = NewRect("Handle Slide Area", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.sizeDelta = new Vector2(-handleWidth, 0f);
            var handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(handleWidth, -18f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Stone;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            root.gameObject.AddComponent<PaletteSliderHandle>().Init(handleImage);
            return slider;
        }

        private static PaletteButton MakeButton(Transform parent, TMP_FontAsset font, string key, string label,
            PaletteButton.Style style, Vector2 center, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = NewRect(label.Replace(" ", "") + "Button", parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            var frame = rect.gameObject.AddComponent<Image>();
            var fillRect = NewRect("Fill", rect);
            Stretch(fillRect, 1.5f);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.raycastTarget = false;

            if (key != null)
            {
                var keyText = NewRect("Key", rect);
                keyText.anchorMin = new Vector2(0f, 0f);
                keyText.anchorMax = new Vector2(0f, 1f);
                keyText.pivot = new Vector2(0f, 0.5f);
                keyText.anchoredPosition = new Vector2(22f, 0f);
                keyText.sizeDelta = new Vector2(30f, 0f);
                var k = keyText.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) k.font = font;
                k.text = key;
                k.fontSize = 22f;
                k.alignment = TextAlignmentOptions.Left;
                k.raycastTarget = false;
            }

            var textRect = NewRect("Label", rect);
            Stretch(textRect);
            textRect.offsetMin = new Vector2(key != null ? 52f : 12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = label;
            text.fontSize = key != null ? 21f : 22f;
            text.alignment = key != null ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            var button = rect.gameObject.AddComponent<PaletteButton>();
            button.Init(style, frame, fill);
            button.onClick.AddListener(action);
            return button;
        }
    }
}
