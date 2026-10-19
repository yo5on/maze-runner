using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Sprites for the in-world combat cues (<see cref="EnemyBehaviour"/> telegraphs), painted once in
    /// code in the portfolio palette: crimson #d21319 for danger, midnight navy #1b1e4a for contrast
    /// and warm stone #afaea2 for neutral parts.
    /// </summary>
    public static class TelegraphArt
    {
        public static readonly Color32 Crimson = new Color32(0xd2, 0x13, 0x19, 0xff);
        public static readonly Color32 Navy = new Color32(0x1b, 0x1e, 0x4a, 0xff);
        public static readonly Color32 Stone = new Color32(0xaf, 0xae, 0xa2, 0xff);

        private static Sprite badge, stripes, capsule, pixel;

        /// <summary>A crimson diamond with a navy outline and a navy "!" - "this enemy is about to strike". 1 world unit.</summary>
        public static Sprite WarningBadge => badge != null ? badge : badge = PaintBadge();

        /// <summary>Diagonal crimson hatching that tiles (use SpriteDrawMode.Tiled). Half a world unit per tile.</summary>
        public static Sprite Hatching => stripes != null ? stripes : stripes = PaintStripes();

        /// <summary>A white rounded bar, 1 x 1 world unit (tint it).</summary>
        public static Sprite Capsule => capsule != null ? capsule : capsule = PaintCapsule();

        /// <summary>A plain white square, 1 x 1 world unit (tint it).</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "TelegraphPixel" };
                    var white = new Color32[16];
                    for (int i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(white);
                    tex.Apply(false, true);
                    pixel = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return pixel;
            }
        }

        private static Sprite PaintBadge()
        {
            const int size = 128;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (x + 0.5f) / size - 0.5f, py = (y + 0.5f) / size - 0.5f;
                    // Diamond: |x| + |y| <= r, softly rounded by measuring in pixels.
                    float diamond = (Mathf.Abs(px) + Mathf.Abs(py) - 0.48f) * size * 0.7071f;
                    float outer = Mathf.Clamp01(0.5f - diamond);
                    float inner = Mathf.Clamp01(0.5f - (diamond + 7f)); // navy outline 7 px wide
                    // "!": a bar and a dot, in navy.
                    float bar = Box(px, py - 0.07f, 0.045f, 0.17f) * size;
                    float dot = (Mathf.Sqrt(px * px + (py + 0.2f) * (py + 0.2f)) - 0.055f) * size;
                    float glyph = Mathf.Clamp01(0.5f - Mathf.Min(bar, dot));

                    Color32 c = Color32.Lerp(Navy, Crimson, inner * (1f - glyph));
                    c.a = (byte)(outer * 255f);
                    pixels[y * size + x] = c;
                }
            }
            return Make("TelegraphBadge", size, size, pixels, size, TextureWrapMode.Clamp, SpriteMeshType.Tight);
        }

        private static Sprite PaintStripes()
        {
            const int size = 32;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 45 degree stripes, half filled; strong stripes over a faint wash.
                    float t = Mathf.Repeat((x + y) / (float)size, 0.5f) / 0.5f;
                    float stripe = Mathf.Clamp01(Mathf.Min(t, 1f - t) * size * 0.5f - 3f);
                    Color32 c = Crimson;
                    c.a = (byte)Mathf.Lerp(40f, 150f, stripe);
                    pixels[y * size + x] = c;
                }
            }
            return Make("TelegraphHatching", size, size, pixels, size * 2, TextureWrapMode.Repeat, SpriteMeshType.FullRect);
        }

        private static Sprite PaintCapsule()
        {
            const int w = 16, h = 64;
            var pixels = new Color32[w * h];
            float r = w / 2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cy = Mathf.Clamp(y + 0.5f, r, h - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, cy)) - r + 0.5f;
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
                }
            }
            // Pixels-per-unit by height: the sprite is 1 unit tall and a quarter unit wide; scale x by 4.
            return Make("TelegraphCapsule", w, h, pixels, h, TextureWrapMode.Clamp, SpriteMeshType.Tight);
        }

        private static float Box(float x, float y, float halfWidth, float halfHeight)
        {
            float dx = Mathf.Abs(x) - halfWidth, dy = Mathf.Abs(y) - halfHeight;
            return Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f);
        }

        private static Sprite Make(string name, int w, int h, Color32[] pixels, float ppu, TextureWrapMode wrap, SpriteMeshType mesh)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = wrap, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu, 0, mesh);
            sprite.name = name;
            return sprite;
        }
    }
}
