using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// One portrait in the enemy intel roster. Drawn only in the palette, like <see cref="PaletteButton"/>:
    /// - idle: stone hairline frame on navy, stone number;
    /// - hover / keyboard focus: crimson hairline;
    /// - chosen (<see cref="IsOn"/>): a thick crimson frame, crimson number and a crimson bar under the tile.
    /// </summary>
    public class IntelTile : Button
    {
        private Image frame;
        private RectTransform surface;
        private Image underline;
        private TMP_Text number;
        private bool isOn;

        public bool IsOn
        {
            get => isOn;
            set { isOn = value; Apply(currentSelectionState); }
        }

        public void Init(Image frameImage, RectTransform surfaceRect, Image underlineImage, TMP_Text numberText)
        {
            frame = frameImage;
            surface = surfaceRect;
            underline = underlineImage;
            number = numberText;
            targetGraphic = frameImage;
            transition = Transition.None;
            Apply(currentSelectionState);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            Apply(state);
        }

        private void Apply(SelectionState state)
        {
            if (frame == null) return;
            bool hot = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
            frame.color = isOn || hot ? PaletteButton.Crimson : PaletteButton.Stone;
            float inset = isOn ? 4f : 1.5f;
            surface.offsetMin = new Vector2(inset, inset);
            surface.offsetMax = new Vector2(-inset, -inset);
            underline.enabled = isOn;
            number.color = isOn ? PaletteButton.Crimson : PaletteButton.Stone;
        }
    }
}
