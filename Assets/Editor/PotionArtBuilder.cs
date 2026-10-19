using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Paints the potion sprites used by enemy potion attacks, recreated from the potion reference sheet
    /// (cartoon glass bottles: pale blue glass with a light blue outline, a cork, bright liquid with a
    /// lighter surface band, bubbles and a white highlight). Each design is its own transparent PNG in
    /// Assets/Sprites/Weapons/Potions, together with the pieces of the impact effect (droplet, glass
    /// shard) and a puddle. Running it again overwrites only these files.
    ///
    /// Menu: Maze Runner > Build Potion Sprites
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.PotionArtBuilder.RunFromCommandLine
    /// </summary>
    public static class PotionArtBuilder
    {
        public const string Folder = "Assets/Sprites/Weapons/Potions";
        private const int Size = 256;
        private const int PixelsPerUnit = 256;

        // Colours sampled by eye from the reference sheet.
        private static readonly Color Outline = Hex(0x5fb9e6);
        private static readonly Color Glass = Hex(0xd9f1fb, 0.92f);
        private static readonly Color Cork = Hex(0xc28a52);
        private static readonly Color CorkDark = Hex(0x8a5a2e);
        private static readonly Color Highlight = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color FacetOutline = Hex(0x3a9edc);
        private static readonly Color FacetGlass = Hex(0xa8dcf5, 0.92f);
        private static readonly Color FacetLip = Hex(0x7cc6ee);

        public static readonly (string name, Color liquid)[] Liquids =
        {
            ("pink", Hex(0xe84a7f)),
            ("green", Hex(0x9cc93b)),
            ("blue", Hex(0x2394da)),
            ("purple", Hex(0x6b3b9c)),
            ("orange", Hex(0xf39433)),
            ("yellow", Hex(0xf4c51c)),
        };

        private enum Shape { Round, Cone, Pentagon, Square, TallRound, Tube, Faceted }

        // The designs the game uses: one per bottle shape, each in a reference colour.
        private static readonly (string file, Shape shape, int liquid)[] Designs =
        {
            ("potion_faceted_green", Shape.Faceted, 1), // the reference's green faceted flask (thrown in game)
            ("potion_round_pink", Shape.Round, 0),
            ("potion_cone_green", Shape.Cone, 1),
            ("potion_pentagon_purple", Shape.Pentagon, 3),
            ("potion_square_blue", Shape.Square, 2),
            ("potion_tallround_orange", Shape.TallRound, 4),
            ("potion_tube_yellow", Shape.Tube, 5),
        };

        [MenuItem("Maze Runner/Build Potion Sprites")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            foreach (var (file, shape, liquid) in Designs) Save(file, PaintBottle(shape, Liquids[liquid].liquid));
            Save("fx_droplet", PaintDroplet());
            Save("fx_glass_shard", PaintShard());
            Save("fx_puddle", PaintPuddle());
            AssetDatabase.Refresh();

            foreach (var (file, _, _) in Designs) Configure(file, Vector4.zero);
            Configure("fx_droplet", Vector4.zero);
            Configure("fx_glass_shard", Vector4.zero);
            // The puddle is drawn sliced so its rounded ends keep their shape at any width.
            Configure("fx_puddle", new Vector4(48, 0, 48, 0));
            AssetDatabase.SaveAssets();
            Debug.Log("[PotionArt] built " + Designs.Length + " potions and 3 effect sprites in " + Folder);
        }

        // ------------------------------------------------------------------ bottles

        private static Color[] PaintBottle(Shape shape, Color liquid)
        {
            var px = new Color[Size * Size];
            Color surface = Color.Lerp(liquid, Color.white, 0.35f);
            Color deep = Color.Lerp(liquid, Color.black, 0.12f);
            bool faceted = shape == Shape.Faceted;
            Color rim = faceted ? FacetOutline : Outline;
            Color glass = faceted ? FacetGlass : Glass;
            float outline = faceted ? 0.026f : 0.032f;
            float level = LiquidLevel(shape);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    Color c = Color.clear;
                    float d = Body(shape, p);

                    // Glass: light blue rim around a pale fill.
                    c = Over(c, rim, Cover(d));
                    c = Over(c, glass, Cover(d + outline));
                    if (faceted)
                    {
                        // Lip band: a deeper blue collar at the top of the neck.
                        c = Over(c, rim, Cover(RoundBox(p, new Vector2(0.5f, 0.775f), new Vector2(0.075f, 0.03f), 0.012f)));
                        c = Over(c, FacetLip, Cover(RoundBox(p, new Vector2(0.5f, 0.775f), new Vector2(0.055f, 0.014f), 0.008f)));
                        // Soft facet edges in the glass.
                        float facets = Mathf.Min(Capsule(p, new Vector2(0.43f, 0.11f), new Vector2(0.475f, 0.57f), 0.005f),
                                                 Capsule(p, new Vector2(0.59f, 0.11f), new Vector2(0.53f, 0.57f), 0.005f));
                        c = Over(c, new Color(1f, 1f, 1f, 0.35f), Cover(d + outline) * Cover(facets));
                    }

                    // Liquid in the lower part of the inside, with a lighter surface band.
                    float inside = Cover(d + outline * 1.6f);
                    if (inside > 0f)
                    {
                        float below = Mathf.Clamp01((level - p.y) * Size + 0.5f);
                        Color body = Color.Lerp(liquid, deep, Mathf.Clamp01((level - p.y) * 2f));
                        c = Over(c, body, inside * below);
                        float band = Mathf.Clamp01(1f - Mathf.Abs(p.y - (level - 0.018f)) * Size / 5f);
                        c = Over(c, surface, inside * below * band);
                        // Bubbles.
                        foreach (Vector3 b in Bubbles(shape, level))
                        {
                            c = Over(c, new Color(1f, 1f, 1f, 0.55f), inside * below * Cover(Vector2.Distance(p, b) - b.z));
                        }
                    }

                    // Highlight: a soft white stroke on the left of the glass.
                    c = Over(c, Highlight, Cover(d + outline * 1.6f) * Cover(HighlightShape(shape, p)));

                    // Cork with a darker rim, above the neck.
                    float cork = CorkShape(shape, p);
                    c = Over(c, CorkDark, Cover(cork));
                    c = Over(c, Cork, Cover(cork + 0.014f));

                    px[y * Size + x] = c;
                }
            }
            return px;
        }

        private static float LiquidLevel(Shape shape)
        {
            switch (shape)
            {
                case Shape.Faceted: return 0.4f;
                case Shape.Tube: return 0.5f;
                case Shape.Cone: return 0.38f;
                case Shape.Square: return 0.36f;
                case Shape.TallRound: return 0.4f;
                default: return 0.36f;
            }
        }

        // Signed distance (negative inside) of the bottle's glass, neck and lip included.
        private static float Body(Shape shape, Vector2 p)
        {
            switch (shape)
            {
                case Shape.Faceted:
                    // Shoulders slope out from a narrow neck to the widest point low on the body, then
                    // facets turn in to a flat base.
                    return Union(RoundPoly(p, 0.02f, new Vector2(0.46f, 0.58f), new Vector2(0.545f, 0.58f), new Vector2(0.705f, 0.29f),
                                           new Vector2(0.64f, 0.09f), new Vector2(0.36f, 0.09f), new Vector2(0.29f, 0.29f)),
                                 RoundBox(p, new Vector2(0.5f, 0.69f), new Vector2(0.045f, 0.13f), 0.012f));
                case Shape.Round:
                    return Union(Circle(p, new Vector2(0.5f, 0.33f), 0.28f), Neck(p, 0.08f, 0.55f, 0.84f));
                case Shape.TallRound:
                    return Union(Ellipse(p, new Vector2(0.5f, 0.38f), new Vector2(0.27f, 0.31f)), Neck(p, 0.075f, 0.62f, 0.86f));
                case Shape.Square:
                    return Union(RoundBox(p, new Vector2(0.5f, 0.33f), new Vector2(0.27f, 0.25f), 0.08f), Neck(p, 0.09f, 0.55f, 0.8f));
                case Shape.Cone:
                    return Union(RoundPoly(p, 0.035f, new Vector2(0.4f, 0.66f), new Vector2(0.6f, 0.66f), new Vector2(0.78f, 0.12f), new Vector2(0.22f, 0.12f)),
                                 Neck(p, 0.07f, 0.6f, 0.86f));
                case Shape.Pentagon:
                    return Union(RoundPoly(p, 0.04f, new Vector2(0.32f, 0.62f), new Vector2(0.68f, 0.62f), new Vector2(0.8f, 0.36f),
                                           new Vector2(0.7f, 0.12f), new Vector2(0.3f, 0.12f), new Vector2(0.2f, 0.36f)),
                                 Neck(p, 0.08f, 0.58f, 0.84f));
                default: // Tube
                    return Union(RoundBox(p, new Vector2(0.5f, 0.45f), new Vector2(0.085f, 0.37f), 0.085f),
                                 RoundBox(p, new Vector2(0.5f, 0.82f), new Vector2(0.12f, 0.025f), 0.02f));
            }
        }

        // A neck of half-width w from y0 to y1 with a slightly wider lip at the top.
        private static float Neck(Vector2 p, float w, float y0, float y1)
        {
            float neck = RoundBox(p, new Vector2(0.5f, (y0 + y1) / 2f), new Vector2(w, (y1 - y0) / 2f), 0.015f);
            float lip = RoundBox(p, new Vector2(0.5f, y1 - 0.015f), new Vector2(w + 0.035f, 0.025f), 0.015f);
            return Union(neck, lip);
        }

        private static float CorkShape(Shape shape, Vector2 p)
        {
            if (shape == Shape.Faceted) return RoundBox(p, new Vector2(0.5f, 0.845f), new Vector2(0.042f, 0.042f), 0.014f);
            float top = shape == Shape.Tube ? 0.84f : shape == Shape.TallRound ? 0.86f : shape == Shape.Cone ? 0.86f : shape == Shape.Square ? 0.8f : 0.84f;
            float w = shape == Shape.Tube ? 0.075f : shape == Shape.Square ? 0.075f : 0.06f;
            return RoundBox(p, new Vector2(0.5f, top + 0.055f), new Vector2(w, 0.06f), 0.02f);
        }

        private static float HighlightShape(Shape shape, Vector2 p)
        {
            switch (shape)
            {
                case Shape.Faceted: return Capsule(p, new Vector2(0.36f, 0.17f), new Vector2(0.43f, 0.4f), 0.012f);
                case Shape.Tube: return Capsule(p, new Vector2(0.455f, 0.2f), new Vector2(0.455f, 0.7f), 0.014f);
                case Shape.Cone: return Capsule(p, new Vector2(0.32f, 0.2f), new Vector2(0.42f, 0.55f), 0.016f);
                case Shape.Pentagon: return Capsule(p, new Vector2(0.27f, 0.36f), new Vector2(0.34f, 0.55f), 0.017f);
                case Shape.Square: return Capsule(p, new Vector2(0.3f, 0.22f), new Vector2(0.3f, 0.48f), 0.018f);
                default: return Capsule(p, new Vector2(0.29f, 0.3f), new Vector2(0.37f, 0.5f), 0.018f);
            }
        }

        private static Vector3[] Bubbles(Shape shape, float level)
        {
            float cx = shape == Shape.Tube || shape == Shape.Faceted ? 0.52f : 0.56f;
            return new[]
            {
                new Vector3(cx - 0.06f, level - 0.12f, 0.022f),
                new Vector3(cx + 0.07f, level - 0.07f, 0.014f),
                new Vector3(cx + 0.02f, level - 0.2f, 0.016f),
            };
        }

        // ------------------------------------------------------------------ effect pieces

        private static Color[] PaintDroplet()
        {
            // White, tinted at runtime with the potion's liquid colour.
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float d = Union(Circle(p, new Vector2(0.5f, 0.4f), 0.26f),
                                    RoundPoly(p, 0.0f, new Vector2(0.5f, 0.88f), new Vector2(0.72f, 0.5f), new Vector2(0.28f, 0.5f)));
                    Color c = Over(Color.clear, new Color(0.82f, 0.82f, 0.82f, 1f), Cover(d));
                    c = Over(c, Color.white, Cover(d + 0.035f));
                    c = Over(c, new Color(1f, 1f, 1f, 1f), Cover(Circle(p, new Vector2(0.42f, 0.44f), 0.06f)));
                    px[y * Size + x] = c;
                }
            return px;
        }

        private static Color[] PaintShard()
        {
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float d = RoundPoly(p, 0.01f, new Vector2(0.3f, 0.2f), new Vector2(0.78f, 0.42f), new Vector2(0.46f, 0.82f));
                    Color c = Over(Color.clear, Outline, Cover(d));
                    c = Over(c, Glass, Cover(d + 0.04f));
                    px[y * Size + x] = c;
                }
            return px;
        }

        private static Color[] PaintPuddle()
        {
            // White, tinted at runtime. A flat pool with a lighter top lip; 256 x 256 with the pool in the
            // lower band so the sprite slices cleanly.
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float pool = RoundBox(p, new Vector2(0.5f, 0.5f), new Vector2(0.46f, 0.3f), 0.18f);
                    Color c = Over(Color.clear, new Color(0.78f, 0.78f, 0.78f, 0.95f), Cover(pool));
                    float lip = RoundBox(p, new Vector2(0.5f, 0.68f), new Vector2(0.36f, 0.05f), 0.05f);
                    c = Over(c, new Color(1f, 1f, 1f, 0.95f), Cover(pool) * Cover(lip));
                    px[y * Size + x] = c;
                }
            return px;
        }

        // ------------------------------------------------------------------ shapes

        private static float Circle(Vector2 p, Vector2 c, float r) => Vector2.Distance(p, c) - r;

        private static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
        {
            Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
            return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        }

        private static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(radius, radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t) - r;
        }

        /// <summary>Convex polygon (counter-clockwise or clockwise), corners rounded by <paramref name="radius"/>.</summary>
        private static float RoundPoly(Vector2 p, float radius, params Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d) - radius;
        }

        private static float Union(float a, float b) => Mathf.Min(a, b);

        /// <summary>Anti-aliased coverage of a signed distance (in unit space) at this resolution.</summary>
        private static float Cover(float d) => Mathf.Clamp01(0.5f - d * Size);

        private static Color Over(Color under, Color over, float amount)
        {
            float a = over.a * Mathf.Clamp01(amount);
            if (a <= 0f) return under;
            float outA = a + under.a * (1f - a);
            if (outA <= 0f) return Color.clear;
            Color rgb = (over * a + under * under.a * (1f - a)) / outA;
            rgb.a = outA;
            return rgb;
        }

        private static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        // ------------------------------------------------------------------ files

        private static void Save(string file, Color[] pixels)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, file + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void Configure(string file, Vector4 border)
        {
            string path = Folder + "/" + file + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Not imported: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = border == Vector4.zero ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try { Build(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
