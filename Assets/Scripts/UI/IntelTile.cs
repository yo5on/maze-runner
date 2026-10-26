using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// One portrait in the enemy intel roster. Drawn only in the palette, like <see cref="PaletteButton"/>:
    /// - idle: mist hairline frame on navy, mist number;
    /// - hover / keyboard focus: green hairline;
    /// - chosen (<see cref="IsOn"/>): a thick green frame, green number and a green bar under the tile.
    /// </summary>
    public class IntelTile : Button
    {
        private Image frame;
        private RectTransform surface;
        private Image underline;
        private TMP_Text number;
        private bool isOn;
        private bool lastKeyboardFocus;

        /// <summary>Keyboard focus reached this tile (arrow keys / WASD): the panel shows its enemy.</summary>
        public System.Action Focused;

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Focused?.Invoke();
        }

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

        private void LateUpdate()
        {
            bool keyboardFocus = MenuNavigator.KeyboardFocusVisible;
            if (lastKeyboardFocus == keyboardFocus) return;
            lastKeyboardFocus = keyboardFocus;
            Apply(currentSelectionState);
        }

        private void Apply(SelectionState state)
        {
            if (frame == null) return;
            bool hot = state == SelectionState.Highlighted || (state == SelectionState.Selected && MenuNavigator.KeyboardFocusVisible)
                || state == SelectionState.Pressed;
            frame.color = isOn || hot ? PaletteButton.Green : PaletteButton.Mist;
            float inset = isOn ? 4f : 1.5f;
            surface.offsetMin = new Vector2(inset, inset);
            surface.offsetMax = new Vector2(-inset, -inset);
            underline.enabled = isOn;
            number.color = isOn ? PaletteButton.Green : PaletteButton.Mist;
        }
    }
}
