using System.Collections.Generic;
using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.UI
{
    /// <summary>
    /// Small line icons for the enemy intel panel, one per <see cref="AttackStyle"/>, drawn once in code
    /// as white anti-aliased shapes on transparency (tint them with Image.color, so they stay in the
    /// palette). Each is a 96 px square.
    /// </summary>
    public static class IntelIcons
    {
        private const int Size = 96;
        private const float Stroke = 0.055f; // half line width, in icon units (0..1)

        private static readonly Dictionary<AttackStyle, Sprite> cache = new Dictionary<AttackStyle, Sprite>();

        public static Sprite For(AttackStyle style)
        {
            if (cache.TryGetValue(style, out Sprite sprite) && sprite != null) return sprite;
            sprite = Paint(style);
            cache[style] = sprite;
            return sprite;
        }

        private static Sprite Paint(AttackStyle style)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float d = Distance(style, p);
                    // One pixel of soft edge.
                    float a = Mathf.Clamp01(0.5f - d * Size);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "IntelIcon_" + style };
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        // Signed distance (negative inside) from p to the icon's shape, in icon units.
        private static float Distance(AttackStyle style, Vector2 p)
        {
            switch (style)
            {
                case AttackStyle.Melee:
                    // One diagonal slash.
                    return Line(p, new Vector2(0.24f, 0.2f), new Vector2(0.76f, 0.8f));
                case AttackStyle.Combo:
                    // Two parallel slashes.
                    return Mathf.Min(Line(p, new Vector2(0.14f, 0.24f), new Vector2(0.56f, 0.82f)),
                                     Line(p, new Vector2(0.44f, 0.18f), new Vector2(0.86f, 0.76f)));
                case AttackStyle.Dash:
                {
                    // An arrow with two speed lines behind it.
                    float shaft = Line(p, new Vector2(0.4f, 0.5f), new Vector2(0.84f, 0.5f));
                    float head = Mathf.Min(Line(p, new Vector2(0.84f, 0.5f), new Vector2(0.66f, 0.68f)),
                                           Line(p, new Vector2(0.84f, 0.5f), new Vector2(0.66f, 0.32f)));
                    float trail = Mathf.Min(Line(p, new Vector2(0.14f, 0.64f), new Vector2(0.3f, 0.64f)),
                                            Line(p, new Vector2(0.14f, 0.36f), new Vector2(0.3f, 0.36f)));
                    return Mathf.Min(shaft, Mathf.Min(head, trail));
                }
                case AttackStyle.Charge:
                {
                    // A shield front with an arrow through it.
                    float shield = Line(p, new Vector2(0.62f, 0.2f), new Vector2(0.62f, 0.8f));
                    float shaft = Line(p, new Vector2(0.14f, 0.5f), new Vector2(0.44f, 0.5f));
                    float head = Mathf.Min(Line(p, new Vector2(0.44f, 0.5f), new Vector2(0.3f, 0.64f)),
                                           Line(p, new Vector2(0.44f, 0.5f), new Vector2(0.3f, 0.36f)));
                    float ground = Line(p, new Vector2(0.78f, 0.2f), new Vector2(0.86f, 0.2f));
                    return Mathf.Min(Mathf.Min(shield, shaft), Mathf.Min(head, ground));
                }
                case AttackStyle.Ranged:
                {
                    // A lobbed arc of dots ending in a flask.
                    float d = Circle(p, new Vector2(0.78f, 0.28f), 0.11f) - 0.0f;
                    d = Mathf.Min(d, Circle(p, new Vector2(0.18f, 0.32f), 0.045f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.3f, 0.6f), 0.045f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.5f, 0.74f), 0.045f));
                    d = Mathf.Min(d, Circle(p, new Vector2(0.68f, 0.62f), 0.045f));
                    return d;
                }
                case AttackStyle.Area:
                {
                    // Ground line with a shock wave rising from it.
                    float ground = Line(p, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.24f));
                    float mid = Line(p, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.8f));
                    float left = Line(p, new Vector2(0.28f, 0.38f), new Vector2(0.2f, 0.64f));
                    float right = Line(p, new Vector2(0.72f, 0.38f), new Vector2(0.8f, 0.64f));
                    return Mathf.Min(Mathf.Min(ground, mid), Mathf.Min(left, right));
                }
                default:
                {
                    // Counter: a shield outline.
                    Vector2[] shield =
                    {
                        new Vector2(0.22f, 0.8f), new Vector2(0.78f, 0.8f), new Vector2(0.78f, 0.5f),
                        new Vector2(0.5f, 0.16f), new Vector2(0.22f, 0.5f),
                    };
                    return Outline(p, shield);
                }
            }
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }

        private static float Line(Vector2 p, Vector2 a, Vector2 b) => Segment(p, a, b) - Stroke;

        private static float Circle(Vector2 p, Vector2 c, float r) => Vector2.Distance(p, c) - r;

        private static float Outline(Vector2 p, Vector2[] poly)
        {
            float d = float.MaxValue;
            for (int i = 0; i < poly.Length; i++) d = Mathf.Min(d, Segment(p, poly[i], poly[(i + 1) % poly.Length]));
            return d - Stroke;
        }
    }
}
