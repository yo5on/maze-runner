using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// A potion shattering: drops of its liquid and a few glass shards burst out, fall and fade, then the
    /// whole effect removes itself (about half a second). Purely visual - no colliders. Built from the
    /// potion sprites (fx_droplet, fx_glass_shard); with no art it shows nothing.
    /// </summary>
    public class PotionSplash : MonoBehaviour
    {
        private const float Lifetime = 0.5f;
        private const float Gravity = 14f;

        private Transform[] pieces;
        private Vector2[] velocities;
        private float[] spins;
        private SpriteRenderer[] renderers;
        private Color[] colors;
        private float age;

        public static void Spawn(Vector2 position, PotionVisual visual)
        {
            if (visual == null || (visual.droplet == null && visual.shard == null)) return;
            var root = new GameObject("PotionSplash");
            root.transform.position = position;
            var splash = root.AddComponent<PotionSplash>();
            splash.Build(visual);
            Destroy(root, Lifetime + 0.1f); // never outlives its animation
        }

        private void Build(PotionVisual visual)
        {
            int drops = visual.droplet != null ? 8 : 0;
            int shards = visual.shard != null ? 4 : 0;
            int n = drops + shards;
            pieces = new Transform[n];
            velocities = new Vector2[n];
            spins = new float[n];
            renderers = new SpriteRenderer[n];
            colors = new Color[n];
            for (int i = 0; i < n; i++)
            {
                bool drop = i < drops;
                Sprite sprite = drop ? visual.droplet : visual.shard;
                var go = new GameObject(drop ? "Drop" : "Shard");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 21;
                Color c = drop ? visual.liquid : Color.white;
                sr.color = c;
                float size = (drop ? Random.Range(0.12f, 0.2f) : Random.Range(0.12f, 0.17f)) / Mathf.Max(0.01f, sprite.bounds.size.y);
                go.transform.localScale = new Vector3(size, size, 1f);

                // Upward fan, wider for drops.
                float angle = Random.Range(drop ? 20f : 35f, drop ? 160f : 145f) * Mathf.Deg2Rad;
                float speed = Random.Range(drop ? 2.2f : 1.6f, drop ? 4.2f : 3.2f);
                velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spins[i] = drop ? 0f : Random.Range(-720f, 720f);
                if (drop) go.transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
                pieces[i] = go.transform;
                renderers[i] = sr;
                colors[i] = c;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            float fade = Mathf.Clamp01(1f - (age - Lifetime * 0.5f) / (Lifetime * 0.5f));
            for (int i = 0; i < pieces.Length; i++)
            {
                velocities[i].y -= Gravity * Time.deltaTime;
                pieces[i].position += (Vector3)(velocities[i] * Time.deltaTime);
                if (spins[i] != 0f) pieces[i].Rotate(0f, 0f, spins[i] * Time.deltaTime);
                Color c = colors[i];
                renderers[i].color = new Color(c.r, c.g, c.b, c.a * fade);
            }
        }
    }
}
