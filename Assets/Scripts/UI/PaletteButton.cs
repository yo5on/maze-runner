using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>Applies the shared steel-blue, navy and green UI theme to generated buttons.</summary>
    public class PaletteButton : Button
    {
        public enum Style { Quiet, Strong, Option }

        public static readonly Color SteelBlue = new Color32(0x26, 0x3f, 0x60, 0xff);
        public static readonly Color Navy = new Color32(0x1b, 0x2b, 0x4a, 0xff);
        public static readonly Color Green = new Color32(0x4d, 0xa8, 0x61, 0xff);
        public static readonly Color GreenHover = new Color32(0x6d, 0xb8, 0x7d, 0xff);
        public static readonly Color GreenPressed = new Color32(0x39, 0x7b, 0x4b, 0xff);
        public static readonly Color SecondaryHover = new Color32(0x40, 0x49, 0x60, 0xff);
        public static readonly Color Stone = new Color32(0xaf, 0xae, 0xa2, 0xff);
        public static readonly Color Disabled = new Color32(0x59, 0x64, 0x77, 0xff);

        [SerializeField] private Style style = Style.Quiet;
        [SerializeField] private bool isOn;

        private Image fill;
        private Image border;
        private TMP_Text[] labels;

        public bool IsOn
        {
            get => isOn;
            set { isOn = value; Refresh(); }
        }

        public void Init(Style buttonStyle, Image frame, Image fillImage)
        {
            style = buttonStyle;
            fill = fillImage;
            border = frame;
            targetGraphic = frame;
            transition = Transition.None;
            labels = GetComponentsInChildren<TMP_Text>(true);
            Refresh();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            Apply(state);
        }

        private void Refresh() => Apply(currentSelectionState);

        private void Apply(SelectionState state)
        {
            if (fill == null) return;
            if (labels == null) labels = GetComponentsInChildren<TMP_Text>(true);
            bool hot = state == SelectionState.Highlighted || state == SelectionState.Selected;
            bool pressed = state == SelectionState.Pressed;

            Color fillColor, borderColor, textColor;
            switch (style)
            {
                case Style.Strong:
                    fillColor = pressed ? GreenPressed : hot ? GreenHover : Green;
                    borderColor = fillColor;
                    textColor = Color.white;
                    break;
                default:
                    bool on = style == Style.Option && isOn;
                    fillColor = pressed ? (on ? GreenPressed : new Color32(0x11, 0x1a, 0x2c, 0xff)) : on ? Green : hot ? SecondaryHover : Navy;
                    borderColor = on ? Green : Stone;
                    textColor = Color.white;
                    break;
            }
            if (state == SelectionState.Disabled) { fillColor = WithAlpha(Disabled, 0.55f); borderColor = Disabled; textColor = WithAlpha(Color.white, 0.55f); }

            fill.color = fillColor;
            if (border != null) border.color = borderColor;
            foreach (TMP_Text label in labels)
            {
                label.color = textColor;
                label.fontStyle = style == Style.Option && isOn ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
