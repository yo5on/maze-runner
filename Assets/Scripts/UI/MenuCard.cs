using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.UI
{
    /// <summary>
    /// Draws a rounded card behind a menu's buttons and titles, sized to fit them
    /// whenever the menu is shown.
    /// </summary>
    public class MenuCard : MonoBehaviour
    {
        private const float Padding = 36f;
        private RectTransform card;

        public void Init(Sprite sprite, Color color)
        {
            var go = new GameObject("MenuCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card = (RectTransform)go.transform;
            card.SetParent(transform, false);
            card.SetAsFirstSibling();
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            go.GetComponent<LayoutElement>().ignoreLayout = true;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;

            if (isActiveAndEnabled) Fit();
        }

        private void OnEnable()
        {
            if (card != null) Fit();
        }

        private void Fit()
        {
            var panel = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            bool any = false;
            Vector2 min = Vector2.zero, max = Vector2.zero;
            var corners = new Vector3[4];
            foreach (RectTransform child in panel)
            {
                if (child == card || !child.gameObject.activeSelf) continue;
                child.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector2 p = panel.InverseTransformPoint(corner);
                    if (!any) { min = max = p; any = true; }
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            }
            if (!any) { card.gameObject.SetActive(false); return; }

            card.gameObject.SetActive(true);
            card.sizeDelta = (max - min) + new Vector2(Padding * 2f, Padding * 2f);
            card.anchoredPosition = (min + max) * 0.5f - panel.rect.center;
        }
    }
}
