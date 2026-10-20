using UnityEngine;
using MazeRunner.Levels;
using MazeRunner.Player;

namespace MazeRunner.Enemies
{
    /// <summary>The art of a thrown potion: the bottle, its liquid colour and the pieces of its splash.</summary>
    [System.Serializable]
    public class PotionVisual
    {
        [Tooltip("The bottle that is held, thrown and tumbles through the air.")]
        public Sprite bottle;
        [Tooltip("Liquid drop for the splash (white; tinted with the liquid colour).")]
        public Sprite droplet;
        [Tooltip("Broken glass for the splash.")]
        public Sprite shard;
        [Tooltip("The spilled potion left on the ground (white, 9-sliced; tinted with the liquid colour).")]
        public Sprite puddle;
        public Color liquid = new Color32(0xe8, 0x4a, 0x7f, 0xff);
    }

    /// <summary>
    /// The Bloody Alchemist's thrown potion: a bottle on a ballistic arc, tumbling end over end at a steady
    /// rate from the angle it left the hand at. It hurts the player
    /// once on a direct hit, and when it reaches the ground the alchemist leaves a spilled-potion pool
    /// there (a hazard strip with the level's Hazard rules, removed after a few seconds). Either way it
    /// shatters in a splash of liquid and glass (<see cref="PotionSplash"/>).
    /// </summary>
    public class AlchemistFlask : MonoBehaviour
    {
        private const float MaxLifetime = 3f;
        private const int GroundLayer = 3;
        /// <summary>World height of the bottle sprite (its canvas), held and in flight.</summary>
        public const float BottleHeight = 0.8f; // the bottle fills ~91 % of its cut-out, so it shows about 0.73 units tall
        private const float HitRadius = 0.192f;    // world units (unchanged from the first version)

        private AlchemistBehaviour owner;
        private PotionVisual visual;
        private bool finished;
        private float age;

        public static void Launch(AlchemistBehaviour owner, Vector2 position, Vector2 velocity, PotionVisual visual, Sprite fallback,
            float angle = 0f, float spin = 360f)
        {
            var go = new GameObject("Potion");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
            float scale;
            if (visual != null && visual.bottle != null)
            {
                sr.sprite = visual.bottle;
                go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                scale = BottleHeight / Mathf.Max(0.01f, visual.bottle.bounds.size.y);
            }
            else
            {
                // No potion art assigned: the original crimson diamond.
                sr.sprite = fallback;
                sr.color = new Color(0.82f, 0.07f, 0.1f, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                scale = 0.32f;
            }
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.linearVelocity = velocity;
            body.angularVelocity = spin; // a steady tumble (the trigger collider never adds torque)
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = HitRadius / scale;
            var flask = go.AddComponent<AlchemistFlask>();
            flask.owner = owner;
            flask.visual = visual;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age > MaxLifetime || transform.position.y < -12f) Finish(null, null, false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (finished) return;
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponentInParent<PlayerHealth>();
                if (health != null && owner != null && owner.enabled)
                {
                    health.TakeDamage(1);
                    owner.FlaskHitPlayer();
                }
                Finish(null, null, true);
            }
            else if (other.gameObject.layer == GroundLayer && !other.isTrigger)
            {
                Finish(new Vector2(transform.position.x, other.bounds.max.y), other, true);
            }
        }

        private void Finish(Vector2? groundHit, Collider2D ground, bool shatter)
        {
            if (finished) return;
            finished = true;
            if (shatter) PotionSplash.Spawn(transform.position, visual, transform.rotation, transform.localScale.x);
            if (owner != null) owner.FlaskFinished(groundHit, ground);
            Destroy(gameObject);
        }

        /// <summary>A spilled-potion pool on top of a platform: a short hazard strip that hurts the player.</summary>
        public static GameObject MakePool(float x, Collider2D ground, float lifetime, PotionVisual visual)
        {
            const float width = 1.6f;
            const float height = 0.24f;
            Bounds b = ground.bounds;
            float left = Mathf.Max(b.min.x, x - width / 2f);
            float right = Mathf.Min(b.max.x, x + width / 2f);
            if (right - left < 0.4f) return null;
            float w = right - left;
            var go = new GameObject("PotionPool");
            go.transform.position = new Vector3((left + right) / 2f, b.max.y + height / 2f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (visual != null && visual.puddle != null)
            {
                sr.sprite = visual.puddle;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(w, height * 1.6f);
                Color c = visual.liquid;
                sr.color = new Color(c.r, c.g, c.b, 0.9f);
            }
            else
            {
                sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                sr.color = new Color(0.82f, 0.07f, 0.1f, 0.75f);
                go.transform.localScale = new Vector3(w, height, 1f);
            }
            // Same hurt area as before in world units: the strip's width, from the platform top up to
            // the player's feet (0.48 high).
            Vector3 s = go.transform.localScale;
            col.size = new Vector2(w / s.x, 0.48f / s.y);
            col.offset = new Vector2(0f, 0.12f / s.y);
            go.AddComponent<Hazard>();
            Destroy(go, lifetime);
            return go;
        }
    }
}
