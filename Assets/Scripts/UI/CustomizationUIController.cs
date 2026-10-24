using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using MazeRunner.Customization;

namespace MazeRunner.UI
{
    /// <summary>
    /// Drives the character selection screen. The player browses the six complete characters (Previous /
    /// Next beside a large framed preview, or a card per character in the roster on the right), and
    /// Confirm saves the one shown. Reset shows the default character again and Back returns to the main
    /// menu. The choice is saved through PlayableCharacters and shown on the Player in the levels.
    /// Keyboard: Left / Right (or A / D) browse, Enter confirms, Esc goes back.
    ///
    /// The scene's original rows were laid out for the old per-part editor, so at start the screen is
    /// rebuilt in code in the game's palette (<see cref="PaletteButton"/>) and display face: heading,
    /// preview with its name and type under it, roster and one action row. The scene's own buttons and
    /// labels are reused (their click handlers and button sounds are kept); only their parents, sizes
    /// and colours change.
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
        [SerializeField] private Color mutedTextColor = new Color32(0xb4, 0xc2, 0xd6, 0xff);
        [SerializeField] private Color frameColor = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        [SerializeField] private Color buttonColor = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        [SerializeField] private Color accentColor = new Color32(0x4d, 0xa8, 0x61, 0xff);

        private const float StageX = -400f;      // centre of the preview column
        private const float RosterX = 440f;      // centre of the roster column
        private const float TopY = 300f;         // top of the preview and the roster
        private static readonly Vector2 CardSize = new Vector2(210f, 240f);
        private const float CardGap = 24f;

        private class RosterCard
        {
            public RectTransform Rect;
            public Image Frame;
            public GameObject EquippedTag;
        }

        private int currentIndex;
        private bool isSelectionScreen;

        private Image previewImage;
        private Image previewFrame;
        private TMP_Text typeLabel;
        private Image statusPill;
        private TMP_Text statusLabel;
        private TMP_FontAsset font;
        private TMP_FontAsset displayFont;
        private Sprite roundedSprite;
        private readonly List<RosterCard> cards = new List<RosterCard>();
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

        private void Update()
        {
            if (!isSelectionScreen) return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Press(optionPrevButton);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Press(optionNextButton);
                else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Press(applyButton);
                else if (keyboard.escapeKey.wasPressedThisFrame) Press(backButton);
            }

            // Selection feedback: the shown character's card grows a little, and the preview and the status
            // badge settle back to full size after their pop. Unscaled, so it also runs if time is paused.
            float t = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            for (int i = 0; i < cards.Count; i++)
                cards[i].Rect.localScale = Vector3.Lerp(cards[i].Rect.localScale, Vector3.one * (i == currentIndex ? 1.06f : 1f), t);
            if (previewImage != null) previewImage.rectTransform.localScale = Vector3.Lerp(previewImage.rectTransform.localScale, Vector3.one, t);
            if (statusPill != null) statusPill.rectTransform.localScale = Vector3.Lerp(statusPill.rectTransform.localScale, Vector3.one, t);
        }

        /// <summary>Keyboard shortcuts go through the buttons, so they make the same click sound.</summary>
        private static void Press(Button button)
        {
            if (button != null && button.isActiveAndEnabled && button.interactable) button.onClick.Invoke();
        }

        private PlayableCharacter Current => PlayableCharacters.All[currentIndex];

        private void SelectNext() => Show((currentIndex + 1) % PlayableCharacters.All.Count);

        private void SelectPrevious() => Show((currentIndex - 1 + PlayableCharacters.All.Count) % PlayableCharacters.All.Count);

        private void ResetToDefault() => Show(PlayableCharacters.IndexOf(PlayableCharacters.Default.Id));

        private void Show(int index)
        {
            if (index != currentIndex && previewImage != null) previewImage.rectTransform.localScale = Vector3.one * 0.9f;
            currentIndex = index;
            RefreshUI();
        }

        private void Apply()
        {
            PlayableCharacters.SaveSelection(Current);
            if (statusPill != null) statusPill.rectTransform.localScale = Vector3.one * 1.2f;
            RefreshUI();
        }

        private void Back()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void RefreshUI()
        {
            PlayableCharacter character = Current;
            string selectedId = PlayableCharacters.Selected.Id;
            bool isSelected = selectedId == character.Id;

            if (optionLabel != null) optionLabel.text = character.DisplayName;
            if (typeLabel != null) typeLabel.text = $"{TypeName(character.RigId)}   {currentIndex + 1} / {PlayableCharacters.All.Count}";

            if (previewImage != null)
            {
                Sprite preview = GetPreview(character);
                previewImage.sprite = preview;
                previewImage.enabled = preview != null;
            }
            if (previewFrame != null) previewFrame.color = isSelected ? accentColor : mutedTextColor;

            if (statusLabel != null)
            {
                statusLabel.text = isSelected ? "EQUIPPED" : "Press Confirm to play as this hero";
                statusLabel.color = isSelected ? textColor : mutedTextColor;
                statusLabel.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
            if (statusPill != null) statusPill.enabled = isSelected;

            for (int i = 0; i < cards.Count; i++)
            {
                bool shown = i == currentIndex;
                cards[i].Frame.color = shown ? accentColor : new Color(mutedTextColor.r, mutedTextColor.g, mutedTextColor.b, 0.35f);
                cards[i].EquippedTag.SetActive(PlayableCharacters.All[i].Id == selectedId);
            }

            // Confirm only does something for a character that is not already equipped;
            // Reset only does something when another character is shown.
            if (applyButton != null) applyButton.interactable = !isSelected;
            if (resetButton != null) resetButton.interactable = character.Id != PlayableCharacters.Default.Id;

            // A clicked button keeps the selection, and Enter would then press it as well as Confirm.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            // If the scene shows a Player, preview the character on it too.
            var skin = FindAnyObjectByType<PlayerCharacterSkin>();
            if (skin != null) skin.Apply(character);
        }

        private static string TypeName(string rigId)
        {
            switch (rigId)
            {
                case PlayerRigSettings.DefaultRigId: return "FALLEN ANGEL";
                case "Dark_Oracle": return "DARK ORACLE";
                default: return rigId.Replace('_', ' ').ToUpperInvariant();
            }
        }

        // ------------------------------------------------------------------ layout

        private void BuildLayout(RectTransform panel)
        {
            font = optionLabel.font;
            displayFont = PaletteUI.DisplayFont != null ? PaletteUI.DisplayFont : font;
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
            root.sizeDelta = new Vector2(1760f, 1000f);

            // Heading.
            TMP_Text eyebrow = CreateText("Eyebrow", root, "CHARACTER SELECTION", 22f, FontStyles.Bold, mutedTextColor);
            eyebrow.characterSpacing = 12f;
            Place(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1000f, 32f));
            TMP_Text title = CreateText("Title", root, "Choose Your Hero", 72f, FontStyles.Normal, PaletteButton.Cream);
            title.font = displayFont;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1400f, 96f));
            RectTransform rule = CreateImage("TitleRule", root, accentColor, null).rectTransform;
            Place(rule, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(120f, 5f));

            BuildStage(root);
            BuildRoster(root);

            // Action row under the roster: Back, Reset, Confirm (primary).
            RectTransform actions = CreateRect("ActionRow", root);
            Place(actions, new Vector2(0.5f, 0.5f), new Vector2(RosterX, -300f), new Vector2(3f * CardSize.x + 2f * CardGap, 100f), centrePivot: true);
            var row = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = CardGap;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;

            StyleButton(backButton, "BACK", buttonColor);
            StyleButton(resetButton, "RESET", buttonColor);
            StyleButton(applyButton, "CONFIRM", accentColor);
            foreach (Button button in new[] { backButton, resetButton, applyButton })
            {
                if (button == null) continue;
                button.transform.SetParent(actions, false);
                if (button.GetComponent<LayoutElement>() is LayoutElement old) Destroy(old);
            }

            // Keyboard shortcuts, only where there is a keyboard to press them (the web player registers a
            // touchscreen even on desktop, so the platform is asked instead).
            if (!Application.isMobilePlatform)
            {
                TMP_Text keys = CreateText("KeyHint", root, "Left / Right  Browse        Enter  Confirm        Esc  Back", 22f, FontStyles.Normal, mutedTextColor);
                Place(keys.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(RosterX, -400f), new Vector2(700f, 34f), centrePivot: true);
            }

            // The old rows are now empty (or held the per-part category picker).
            foreach (Transform oldRow in new[] { categoryRow, optionRow, buttonRow })
            {
                if (oldRow != null && oldRow != root) oldRow.gameObject.SetActive(false);
            }
        }

        /// <summary>The large preview in a framed night-sky panel, Previous / Next beside it, name and type under it.</summary>
        private void BuildStage(RectTransform root)
        {
            float centreY = TopY - previewFrameSize.y * 0.5f;

            // Frame: a coloured edge (mist, green when this character is equipped) around a sky that fades
            // from the backdrop's steel blue down to navy, with a soft shadow for the character to stand on.
            previewFrame = CreateImage("PreviewFrame", root, mutedTextColor, roundedSprite);
            Place(previewFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(StageX, centreY), previewFrameSize, centrePivot: true);
            Image sky = CreateImage("Sky", previewFrame.rectTransform, Color.white, null);
            sky.sprite = SkySprite(backdropColor, frameColor);
            Stretch(sky.rectTransform, 5f);
            Image shadow = CreateImage("GroundShadow", sky.rectTransform, new Color(0f, 0f, 0f, 0.35f), null);
            shadow.sprite = EllipseSprite();
            Place(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(previewFrameSize.x * 0.6f, 44f), pivot: new Vector2(0.5f, 0f));

            previewImage = CreateImage("CharacterPreview", sky.rectTransform, Color.white, null);
            previewImage.preserveAspect = true;
            Stretch(previewImage.rectTransform, 20f);

            // Previous / Next arrows on either side of the preview.
            float arrowOffset = previewFrameSize.x * 0.5f + 90f;
            StyleButton(optionPrevButton, "<", buttonColor);
            StyleButton(optionNextButton, ">", buttonColor);
            PlaceButton(optionPrevButton, root, new Vector2(StageX - arrowOffset, centreY), new Vector2(110f, 140f));
            PlaceButton(optionNextButton, root, new Vector2(StageX + arrowOffset, centreY), new Vector2(110f, 140f));
            foreach (Button arrow in new[] { optionPrevButton, optionNextButton })
            {
                TMP_Text glyph = arrow != null ? arrow.GetComponentInChildren<TMP_Text>(true) : null;
                if (glyph != null) { glyph.font = displayFont; glyph.fontSize = 64f; }
            }

            // Type and position, then the name in the display face, then the status badge.
            float underY = TopY - previewFrameSize.y;
            typeLabel = CreateText("Type", root, string.Empty, 22f, FontStyles.Bold, mutedTextColor);
            typeLabel.characterSpacing = 8f;
            Place(typeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(StageX, underY - 34f), new Vector2(800f, 32f), centrePivot: true);

            optionLabel.transform.SetParent(root, false);
            optionLabel.font = displayFont;
            optionLabel.fontSize = 56f;
            optionLabel.fontStyle = FontStyles.Normal;
            optionLabel.color = PaletteButton.Cream;
            optionLabel.alignment = TextAlignmentOptions.Center;
            optionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            // The longest name must fit the column; shrink rather than clip.
            optionLabel.enableAutoSizing = true;
            optionLabel.fontSizeMin = 32f;
            optionLabel.fontSizeMax = 56f;
            Place(optionLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(StageX, underY - 88f), new Vector2(760f, 70f), centrePivot: true);

            statusPill = CreateImage("SelectedIndicator", root, accentColor, roundedSprite);
            Place(statusPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(StageX, underY - 158f), new Vector2(220f, 48f), centrePivot: true);
            statusLabel = CreateText("Status", root, string.Empty, 26f, FontStyles.Bold, textColor);
            statusLabel.characterSpacing = 3f;
            Place(statusLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(StageX, underY - 158f), new Vector2(800f, 48f), centrePivot: true);
        }

        /// <summary>One card per character (portrait, name, an Equipped tag), three to a row. A card shows its character.</summary>
        private void BuildRoster(RectTransform root)
        {
            IReadOnlyList<PlayableCharacter> all = PlayableCharacters.All;
            const int columns = 3;
            float gridWidth = columns * CardSize.x + (columns - 1) * CardGap;

            for (int i = 0; i < all.Count; i++)
            {
                int column = i % columns, rowIndex = i / columns;
                Vector2 centre = new Vector2(RosterX - gridWidth * 0.5f + CardSize.x * 0.5f + column * (CardSize.x + CardGap),
                    TopY - CardSize.y * 0.5f - rowIndex * (CardSize.y + CardGap));

                var card = new RosterCard();
                card.Frame = CreateImage("Card " + all[i].DisplayName, root, mutedTextColor, roundedSprite);
                card.Rect = card.Frame.rectTransform;
                Place(card.Rect, new Vector2(0.5f, 0.5f), centre, CardSize, centrePivot: true);

                // The surface is the button's tinted graphic: navy, lighter on hover, darker when pressed.
                Image surface = CreateImage("Surface", card.Rect, Color.white, roundedSprite);
                Stretch(surface.rectTransform, 4f);
                surface.raycastTarget = true;
                var button = card.Rect.gameObject.AddComponent<Button>();
                button.targetGraphic = surface;
                ColorBlock colors = button.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = PaletteButton.SecondaryHover;
                colors.selectedColor = buttonColor;
                colors.pressedColor = new Color32(0x11, 0x1a, 0x2c, 0xff);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                int index = i;
                button.onClick.AddListener(() => Show(index));
                card.Rect.gameObject.AddComponent<UIButtonSound>();

                Image portrait = CreateImage("Portrait", surface.rectTransform, Color.white, null);
                portrait.sprite = GetPreview(all[i]);
                portrait.preserveAspect = true;
                Stretch(portrait.rectTransform, 12f);
                portrait.rectTransform.offsetMin = new Vector2(12f, 52f);

                TMP_Text name = CreateText("Name", surface.rectTransform, all[i].DisplayName, 22f, FontStyles.Bold, textColor);
                name.enableAutoSizing = true;
                name.fontSizeMin = 14f;
                name.fontSizeMax = 22f;
                Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(CardSize.x - 20f, 36f), pivot: new Vector2(0.5f, 0f));

                Image tag = CreateImage("Equipped", card.Rect, accentColor, roundedSprite);
                Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(110f, 30f), pivot: new Vector2(0f, 1f));
                TMP_Text tagText = CreateText("Label", tag.rectTransform, "EQUIPPED", 16f, FontStyles.Bold, textColor);
                tagText.characterSpacing = 2f;
                Stretch(tagText.rectTransform, 0f);
                card.EquippedTag = tag.gameObject;

                cards.Add(card);
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

        /// <summary>A 1 x 64 vertical gradient, top to bottom, stretched over the preview.</summary>
        private static Sprite SkySprite(Color top, Color bottom)
        {
            var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "PreviewSky" };
            for (int y = 0; y < 64; y++) texture.SetPixel(0, y, Color.Lerp(bottom, top, y / 63f));
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }

        /// <summary>A white ellipse with a soft edge (tint it), for the ground shadow.</summary>
        private static Sprite EllipseSprite()
        {
            const int w = 64, h = 16;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "PreviewShadow" };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private void StyleButton(Button button, string label, Color fill)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = fill;
            bool primary = fill == accentColor;
            colors.highlightedColor = primary ? PaletteButton.GreenHover : PaletteButton.SecondaryHover;
            colors.selectedColor = fill;
            colors.pressedColor = primary ? PaletteButton.GreenPressed : new Color32(0x11, 0x1a, 0x2c, 0xff);
            colors.disabledColor = new Color32(0x59, 0x64, 0x77, 0x80);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            // Arrow keys browse characters (see Update); they must not also move the UI selection.
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = 30f;
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 4f;
                text.color = textColor;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
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

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
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
