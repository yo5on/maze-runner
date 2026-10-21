using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// Building blocks for menus drawn in code in the portfolio palette (<see cref="PaletteButton"/>
    /// colours). Positions are in a card's own space: x from the card's left edge, y down from its top,
    /// so a layout reads like the design it came from.
    /// </summary>
    public static class PaletteUI
    {
        public static Color Navy => PaletteButton.Navy;
        public static Color Stone => PaletteButton.Stone;
        public static Color Green => PaletteButton.Green;

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>A rect placed by its top-left corner, measured from the parent's top-left corner.</summary>
        public static RectTransform Place(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static Image Fill(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image Box(Transform parent, string name, float x, float y, float width, float height, Color color) =>
            Fill(Place(parent, name, x, y, width, height), color);

        /// <summary>A navy surface inside a thin frame of <paramref name="frame"/> colour. Returns the surface.</summary>
        public static RectTransform Framed(Transform parent, string name, float x, float y, float width, float height,
            Color frame, float thickness, out Image frameImage)
        {
            var rect = Place(parent, name, x, y, width, height);
            frameImage = Fill(rect, frame);
            var surface = NewRect("Surface", rect);
            Stretch(surface, thickness);
            Fill(surface, Navy);
            return surface;
        }

        public static TMP_Text Text(Transform parent, TMP_FontAsset font, string value, float size, Color color,
            float x, float y, float width, float height, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft,
            FontStyles style = FontStyles.Normal, float spacing = 0f, bool wrap = false)
        {
            var rect = Place(parent, "Text", x, y, width, height);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.characterSpacing = spacing;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            // Overflow, not Ellipsis: a display face's line height can exceed a tight box, and Ellipsis
            // then hides the whole line.
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Spaced capitals: the section label style shared with the Settings menu.</summary>
        public static TMP_Text Eyebrow(Transform parent, TMP_FontAsset font, string value, float x, float y, float width,
            Color color, float size = 17f)
        {
            return Text(parent, font, value, size, color, x, y, width, size + 8f, TextAlignmentOptions.TopLeft, FontStyles.Bold, 14f);
        }

        /// <summary>A palette button (frame + inset fill + centred label) placed by its top-left corner.</summary>
        public static PaletteButton Button(Transform parent, TMP_FontAsset font, string label, PaletteButton.Style style,
            float x, float y, float width, float height, UnityEngine.Events.UnityAction action, float fontSize = 22f)
        {
            var rect = Place(parent, label.Replace(" ", "") + "Button", x, y, width, height);
            var frame = rect.gameObject.AddComponent<Image>();
            var fillRect = NewRect("Fill", rect);
            Stretch(fillRect, 1.5f);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.raycastTarget = false;

            var textRect = NewRect("Label", rect);
            Stretch(textRect);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            var button = rect.gameObject.AddComponent<PaletteButton>();
            button.Init(style, frame, fill);
            if (action != null) button.onClick.AddListener(action);
            return button;
        }
    }
}
