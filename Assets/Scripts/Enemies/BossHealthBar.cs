using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// The boss's health bar: its name over an iron-framed blood-red bar at the top of the screen,
    /// shown while the player is in the boss's arena. Built at runtime by <see cref="GoblinEnemy"/> for
    /// bosses, so no scene needs a reference to it. The bar drains smoothly; a lighter trail shows the
    /// last hit for a moment.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        private static readonly Color Iron = new Color(0.11f, 0.09f, 0.08f, 0.88f);
        private static readonly Color Gilt = new Color(0.72f, 0.56f, 0.26f, 1f);
        private static readonly Color Blood = new Color(0.62f, 0.09f, 0.06f, 1f);
        private static readonly Color Wound = new Color(0.95f, 0.78f, 0.55f, 1f);
        private static readonly Color Parchment = new Color(0.93f, 0.86f, 0.70f, 1f);

        private RectTransform fill;
        private RectTransform trail;
        private CanvasGroup group;
        private int max = 1;
        private float shown = 1f;   // displayed fraction of the fill
        private float trailShown = 1f;
        private float target = 1f;
        private float trailHold;
        private bool visible;

        public static BossHealthBar Create(string title, int maxHealth)
        {
            var root = new GameObject("BossHealthBar", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // above the HUD, below pause and game-over menus
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = root.AddComponent<BossHealthBar>();
            bar.group = root.AddComponent<CanvasGroup>();
            bar.group.alpha = 0f;
            bar.group.blocksRaycasts = false;
            bar.group.interactable = false;
            bar.max = Mathf.Max(1, maxHealth);

            // Panel: top centre, clear of the hearts on the left.
            RectTransform panel = Rect("Panel", root.transform, Gilt);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = new Vector2(760f, 30f);
            panel.anchoredPosition = new Vector2(0f, -96f);

            RectTransform inner = Rect("Inner", panel, Iron);
            Stretch(inner, 3f);
            bar.trail = Rect("Trail", inner, Wound);
            bar.fill = Rect("Fill", inner, Blood);
            bar.trail.pivot = bar.fill.pivot = new Vector2(0f, 0.5f); // drains towards the left
            Stretch(bar.trail, 2f);
            Stretch(bar.fill, 2f);

            var label = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(panel, false);
            label.text = title.ToUpperInvariant();
            label.fontSize = 30f;
            label.characterSpacing = 6f;
            label.alignment = TextAlignmentOptions.Bottom;
            label.color = Parchment;
            label.raycastTarget = false;
            var lr = (RectTransform)label.transform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(0f, 44f);
            lr.anchoredPosition = new Vector2(0f, 4f);
            return bar;
        }

        public void SetHealth(int health)
        {
            float next = Mathf.Clamp01(health / (float)max);
            if (next < target) trailHold = 0.45f;
            else trailShown = next; // healing (a fight restarted) refills the trail at once
            target = next;
        }

        public void SetVisible(bool on) => visible = on && target > 0f;

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt * 4f);
            shown = Mathf.MoveTowards(shown, target, dt * 2.5f);
            if (trailHold > 0f) trailHold -= dt;
            else trailShown = Mathf.MoveTowards(trailShown, shown, dt * 0.8f);
            fill.localScale = new Vector3(shown, 1f, 1f);
            trail.localScale = new Vector3(Mathf.Max(trailShown, shown), 1f, 1f);
        }

        private static RectTransform Rect(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
