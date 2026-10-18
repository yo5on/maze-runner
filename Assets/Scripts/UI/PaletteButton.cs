using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// A button drawn only in the portfolio palette (navy #1b1e4a, stone #afaea2, crimson #d21319).
    /// Unity's colour-tint transition multiplies colours and would produce off-palette shades, so each
    /// state sets the fill, border and label colours explicitly:
    /// - Quiet (secondary): navy fill, thin stone border, stone label; hover/focus = crimson border.
    /// - Strong (primary): stone fill, navy label; hover/focus = crimson fill, stone label.
    /// - Option: like Quiet, and crimson fill while <see cref="IsOn"/> (the chosen option).
    /// Pressed is crimson for all of them. The border is a frame image behind an inset fill image.
    /// </summary>
    public class PaletteButton : Button
    {
        public enum Style { Quiet, Strong, Option }

        public static readonly Color Navy = new Color32(0x1b, 0x1e, 0x4a, 0xff);
        public static readonly Color Stone = new Color32(0xaf, 0xae, 0xa2, 0xff);
        public static readonly Color Crimson = new Color32(0xd2, 0x13, 0x19, 0xff);

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
                    fillColor = pressed || hot ? Crimson : Stone;
                    borderColor = pressed || hot ? Crimson : Stone;
                    textColor = pressed || hot ? Stone : Navy;
                    break;
                default:
                    bool on = style == Style.Option && isOn;
                    fillColor = pressed || on ? Crimson : Navy;
                    borderColor = pressed || on || hot ? Crimson : Stone;
                    textColor = Stone;
                    break;
            }
            if (state == SelectionState.Disabled) textColor = WithAlpha(textColor, 0.4f);

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
