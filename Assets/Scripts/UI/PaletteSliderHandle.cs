using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>Slider handle in the shared theme: white at rest and light green while active.</summary>
    public class PaletteSliderHandle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Image handle;
        private bool hover, down, selected;

        public void Init(Image handleImage)
        {
            handle = handleImage;
            Apply();
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; Apply(); }
        public void OnPointerExit(PointerEventData e) { hover = false; Apply(); }
        public void OnPointerDown(PointerEventData e) { down = true; Apply(); }
        public void OnPointerUp(PointerEventData e) { down = false; Apply(); }
        public void OnSelect(BaseEventData e) { selected = true; Apply(); }
        public void OnDeselect(BaseEventData e) { selected = false; Apply(); }

        private void Apply()
        {
            if (handle != null) handle.color = hover || down || selected ? PaletteButton.GreenHover : Color.white;
        }
    }
}
