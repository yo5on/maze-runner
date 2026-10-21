using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Customization;

namespace MazeRunner.UI
{
    /// <summary>
    /// Drives the character selection screen: the player picks one complete character
    /// (Previous / Next), sees a large preview of it, and confirms with Apply. Reset returns the
    /// view to the default character and Back returns to the main menu. The choice is saved
    /// through PlayableCharacters and shown on the Player in the levels.
    ///
    /// The scene's original rows were laid out for the old per-part editor, so at start the screen
    /// is rearranged into a centred layout: heading, framed preview with Previous / Next beside it,
    /// name and status under it, and one action row at the bottom. The scene's own buttons and
    /// labels are reused (their click handlers and button sounds are kept); only their parents,
    /// sizes and colours change.
    /// </summary>
    public class CustomizationUIController : MonoBehaviour
    {
        [Header("Category Navigation (hidden: characters are chosen whole)")]
        [SerializeField] private Button categoryPrevButton;
        [SerializeField] private Button categoryNextButton;
        [SerializeField] private TMP_Text categoryLabel;

        [Header("Character Navigation")]
        [SerializeField] private Button optionPrevButton;
        [SerializeField] private Button optionNextButton;
        [SerializeField] private TMP_Text optionLabel;

        [Header("Actions")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        [Header("Layout (reference 1920 x 1080)")]
        [SerializeField] private Vector2 previewFrameSize = new Vector2(500f, 560f);
        [Tooltip("Part of the 900 x 900 idle frame shown in the preview (x, y, width, height, 0-1 from bottom-left). " +
                 "The same crop is used for every character, so all are framed alike and none is distorted.")]
        [SerializeField] private Rect previewCrop = new Rect(166f / 900f, 120f / 900f, 550f / 900f, 650f / 900f);

        [Header("Colours")]
        [Tooltip("This screen's backdrop: the game's steel blue, dark enough for white text.")]
        [SerializeField] private Color backdropColor = new Color32(0x26, 0x3f, 0x60, 0xff);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color mutedTextColor = new Color32(0xaf, 0xae, 0xa2, 0xff);
        [SerializeField] private Color frameColor = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        [SerializeField] private Color buttonColor = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        [SerializeField] private Color accentColor = new Color32(0x4d, 0xa8, 0x61, 0xff);

        private int currentIndex;
        private bool isSelectionScreen;

        private Image previewImage;
        private GameObject selectionBorder;
        private Image statusPill;
        private TMP_Text counterLabel;
        private TMP_Text statusLabel;
        private TMP_FontAsset font;
        private Sprite roundedSprite;
        private readonly Dictionary<string, Sprite> croppedPreviews = new Dictionary<string, Sprite>();

        private void Awake()
        {
            if (optionPrevButton != null) optionPrevButton.onClick.AddListener(SelectPrevious);
            if (optionNextButton != null) optionNextButton.onClick.AddListener(SelectNext);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (backButton != null) backButton.onClick.AddListener(Back);

            // Level scenes carry a leftover copy of this panel (inactive in Level 1, left active in Levels 2
            // and 3, where it covered the game). Characters are chosen on the Customization screen only.
            bool isCustomizationScene = gameObject.scene.name == "Customization";
            Transform panel = optionLabel != null && optionLabel.transform.parent != null
                ? optionLabel.transform.parent.parent : null;
            if (!isCustomizationScene && panel != null && panel.name == "CustomizationPanel")
            {
                panel.gameObject.SetActive(false);
            }

            isSelectionScreen = isCustomizationScene && panel != null && optionLabel.gameObject.activeInHierarchy;
            if (!isSelectionScreen) return;

            BuildLayout((RectTransform)panel);
        }

        private void Start()
        {
            if (!isSelectionScreen) return;

            currentIndex = Mathf.Max(0, PlayableCharacters.IndexOf(PlayableCharacters.Selected.Id));
            RefreshUI();
        }

        private PlayableCharacter Current => PlayableCharacters.All[currentIndex];

        private void SelectNext()
        {
            currentIndex = (currentIndex + 1) % PlayableCharacters.All.Count;
            RefreshUI();
        }

        private void SelectPrevious()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = PlayableCharacters.All.Count - 1;
            RefreshUI();
        }

        private void ResetToDefault()
        {
            currentIndex = PlayableCharacters.IndexOf(PlayableCharacters.Default.Id);
            RefreshUI();
        }

        private void Apply()
        {
            PlayableCharacters.SaveSelection(Current);
            RefreshUI();
        }

        private void Back()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void RefreshUI()
        {
            PlayableCharacter character = Current;
            bool isSelected = PlayableCharacters.Selected.Id == character.Id;

            if (optionLabel != null) optionLabel.text = character.DisplayName;
            if (counterLabel != null) counterLabel.text = $"{currentIndex + 1} / {PlayableCharacters.All.Count}";

            if (previewImage != null)
            {
                Sprite preview = GetPreview(character);
                previewImage.sprite = preview;
                previewImage.enabled = preview != null;
            }

            if (statusLabel != null)
            {
                statusLabel.text = isSelected ? "SELECTED" : "Press Apply to play as this character";
                statusLabel.color = isSelected ? textColor : mutedTextColor;
                statusLabel.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }

            if (statusPill != null) statusPill.enabled = isSelected;
            if (selectionBorder != null) selectionBorder.SetActive(isSelected);

            // Apply only does something for a character that is not already selected;
            // Reset only does something when another character is shown.
            if (applyButton != null) applyButton.interactable = !isSelected;
            if (resetButton != null) resetButton.interactable = character.Id != PlayableCharacters.Default.Id;

            // If the scene shows a Player, preview the character on it too.
            var skin = FindAnyObjectByType<PlayerCharacterSkin>();
            if (skin != null) skin.Apply(character);
        }

        // ------------------------------------------------------------------ layout

        private void BuildLayout(RectTransform panel)
        {
            font = optionLabel.font;
            roundedSprite = optionPrevButton != null ? optionPrevButton.GetComponent<Image>().sprite : null;

            ScaleWithScreen(panel);

            // The panel's vertical layout stacked the old rows; the new layout is anchored instead.
            var oldLayout = panel.GetComponent<LayoutGroup>();
            if (oldLayout != null) oldLayout.enabled = false;

            var backdrop = panel.GetComponent<Image>();
            if (backdrop != null) backdrop.color = backdropColor;

            // A stray "Health: 3 / 3" HUD label was saved into this menu scene.
            Transform canvas = panel.parent;
            Transform healthText = canvas != null ? canvas.Find("HealthText") : null;
            if (healthText != null) healthText.gameObject.SetActive(false);

            Transform optionRow = optionLabel.transform.parent;
            Transform buttonRow = panel.Find("ButtonRow");
            Transform categoryRow = categoryLabel != null ? categoryLabel.transform.parent : null;

            // Root area: centred, never larger than the 1920 x 1080 reference, so it fits every aspect ratio.
            RectTransform root = CreateRect("SelectionLayout", panel);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1600f, 1000f);

            // Heading.
            TMP_Text title = CreateText("Title", root, "CHARACTER SELECTION", 64f, FontStyles.Bold, textColor);
            title.characterSpacing = 8f;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 90f));

            RectTransform rule = CreateImage("TitleRule", root, accentColor, null).rectTransform;
            Place(rule, new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(140f, 6f));

            // Framed preview.
            Image frame = CreateImage("PreviewFrame", root, frameColor, roundedSprite);
            Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), previewFrameSize, centrePivot: true);
            selectionBorder = CreateBorder(frame.rectTransform, accentColor, 4f);

            previewImage = CreateImage("CharacterPreview", frame.rectTransform, Color.white, null);
            previewImage.preserveAspect = true;
            RectTransform previewRect = previewImage.rectTransform;
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = new Vector2(24f, 24f);
            previewRect.offsetMax = new Vector2(-24f, -24f);

            counterLabel = CreateText("Counter", frame.rectTransform, string.Empty, 28f, FontStyles.Normal, mutedTextColor);
            counterLabel.alignment = TextAlignmentOptions.TopRight;
            Place(counterLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(160f, 40f), pivot: new Vector2(1f, 1f));

            // Previous / Next beside the preview.
            float navOffset = previewFrameSize.x * 0.5f + 48f + 110f;
            StyleButton(optionPrevButton, "<  PREV", buttonColor);
            StyleButton(optionNextButton, "NEXT  >", buttonColor);
            PlaceButton(optionPrevButton, root, new Vector2(-navOffset, 50f), new Vector2(220f, 104f));
            PlaceButton(optionNextButton, root, new Vector2(navOffset, 50f), new Vector2(220f, 104f));

            // Name and status under the preview.
            optionLabel.transform.SetParent(root, false);
            optionLabel.fontSize = 54f;
            optionLabel.fontStyle = FontStyles.Bold;
            optionLabel.color = textColor;
            optionLabel.alignment = TextAlignmentOptions.Center;
            optionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            optionLabel.enableAutoSizing = false;
            Place(optionLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f - previewFrameSize.y * 0.5f - 48f),
                new Vector2(900f, 70f), centrePivot: true);

            // Selected indicator: a green "SELECTED" pill, or a hint when this character is not selected.
            statusPill = CreateImage("SelectedIndicator", root, accentColor, roundedSprite);
            Place(statusPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f - previewFrameSize.y * 0.5f - 108f),
                new Vector2(200f, 46f), centrePivot: true);
            statusLabel = CreateText("Status", root, string.Empty, 28f, FontStyles.Bold, textColor);
            statusLabel.characterSpacing = 3f;
            Place(statusLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f - previewFrameSize.y * 0.5f - 108f),
                new Vector2(900f, 46f), centrePivot: true);

            // One action row at the bottom: Back, Reset, Apply (primary).
            RectTransform actions = CreateRect("ActionRow", root);
            Place(actions, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(900f, 104f), pivot: new Vector2(0.5f, 0f));
            var row = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 36f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            StyleButton(backButton, "BACK", buttonColor);
            StyleButton(resetButton, "RESET", buttonColor);
            StyleButton(applyButton, "APPLY", accentColor);
            foreach (Button button in new[] { backButton, resetButton, applyButton })
            {
                if (button == null) continue;
                button.transform.SetParent(actions, false);
                var element = button.GetComponent<LayoutElement>();
                if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 260f;
                element.preferredHeight = 104f;
            }

            // The old rows are now empty (or held the per-part category picker).
            foreach (Transform oldRow in new[] { categoryRow, optionRow, buttonRow })
            {
                if (oldRow != null && oldRow != root) oldRow.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Lays this screen out against a 1920 x 1080 reference that always fits inside the screen
        /// (Expand), instead of the menus' fixed pixel size, so nothing overlaps or clips on wide
        /// phones or narrow windows.
        /// </summary>
        private static void ScaleWithScreen(Transform panel)
        {
            var scaler = panel.GetComponentInParent<CanvasScaler>();
            if (scaler == null) return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        private Sprite GetPreview(PlayableCharacter character)
        {
            if (!croppedPreviews.TryGetValue(character.Id, out Sprite preview))
            {
                preview = CropPreview(PlayableCharacters.LoadPreview(character));
                croppedPreviews[character.Id] = preview;
            }
            return preview;
        }

        private Sprite CropPreview(Sprite source)
        {
            if (source == null) return null;

            Texture2D texture = source.texture;
            var rect = new Rect(previewCrop.x * texture.width, previewCrop.y * texture.height,
                previewCrop.width * texture.width, previewCrop.height * texture.height);
            Sprite cropped = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
            cropped.name = source.name;
            return cropped;
        }

        private void StyleButton(Button button, string label, Color fill)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = fill;
            bool primary = fill == accentColor;
            colors.highlightedColor = primary ? new Color32(0x6d, 0xb8, 0x7d, 0xff) : new Color32(0x40, 0x49, 0x60, 0xff);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = primary ? new Color32(0x39, 0x7b, 0x4b, 0xff) : new Color32(0x11, 0x1a, 0x2c, 0xff);
            colors.disabledColor = new Color32(0x59, 0x64, 0x77, 0x80);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = 32f;
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 4f;
                text.color = textColor;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>A thin frame drawn just outside a rectangle (four edges), used to mark the selected character.</summary>
        private GameObject CreateBorder(RectTransform target, Color color, float thickness)
        {
            RectTransform border = CreateRect("SelectionBorder", target);
            border.anchorMin = Vector2.zero;
            border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(-thickness * 0.5f, -thickness * 0.5f);
            border.offsetMax = new Vector2(thickness * 0.5f, thickness * 0.5f);

            void Edge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
            {
                RectTransform edge = CreateImage(name, border, color, null).rectTransform;
                edge.anchorMin = anchorMin;
                edge.anchorMax = anchorMax;
                edge.pivot = new Vector2(0.5f, 0.5f);
                edge.anchoredPosition = Vector2.zero;
                edge.sizeDelta = size;
            }

            Edge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
            Edge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
            Edge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
            Edge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));
            return border.gameObject;
        }

        private static void PlaceButton(Button button, RectTransform parent, Vector2 position, Vector2 size)
        {
            if (button == null) return;
            button.transform.SetParent(parent, false);
            Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), position, size, centrePivot: true);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size,
            bool centrePivot = false, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? (centrePivot ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, anchor.y));
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }
    }
}
