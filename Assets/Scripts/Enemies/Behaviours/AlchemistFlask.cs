using UnityEngine;
using MazeRunner.Levels;
using MazeRunner.Player;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// The Bloody Alchemist's thrown flask: a small crimson diamond on a ballistic arc. It hurts the
    /// player once on a direct hit, and when it reaches the ground the alchemist leaves an acid pool
    /// there (a spike strip with the level's Hazard rules, removed after a few seconds).
    /// </summary>
    public class AlchemistFlask : MonoBehaviour
    {
        private const float MaxLifetime = 3f;
        private const int GroundLayer = 3;

        private AlchemistBehaviour owner;
        private bool finished;
        private float age;

        public static void Launch(AlchemistBehaviour owner, Vector2 position, Vector2 velocity, Sprite sprite)
        {
            var go = new GameObject("AcidFlask");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            go.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.82f, 0.07f, 0.1f, 1f);
            sr.sortingOrder = 20;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.linearVelocity = velocity;
            body.angularVelocity = 360f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            go.AddComponent<AlchemistFlask>().owner = owner;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age > MaxLifetime || transform.position.y < -12f) Finish(null, null);
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
                Finish(null, null);
            }
            else if (other.gameObject.layer == GroundLayer && !other.isTrigger)
            {
                Finish(new Vector2(transform.position.x, other.bounds.max.y), other);
            }
        }

        private void Finish(Vector2? groundHit, Collider2D ground)
        {
            if (finished) return;
            finished = true;
            if (owner != null) owner.FlaskFinished(groundHit, ground);
            Destroy(gameObject);
        }

        /// <summary>An acid pool on top of a platform: a short spike strip that hurts the player.</summary>
        public static GameObject MakePool(float x, Collider2D ground, float lifetime)
        {
            const float width = 1.6f;
            Bounds b = ground.bounds;
            float left = Mathf.Max(b.min.x, x - width / 2f);
            float right = Mathf.Min(b.max.x, x + width / 2f);
            if (right - left < 0.4f) return null;

            var go = new GameObject("AcidPool");
            go.transform.position = new Vector3((left + right) / 2f, b.max.y + 0.12f, 0f);
            go.transform.localScale = new Vector3(right - left, 0.24f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            sr.color = new Color(0.82f, 0.07f, 0.1f, 0.75f);
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f); // reaches up to the player's feet
            col.offset = new Vector2(0f, 0.5f);
            go.AddComponent<Hazard>();
            Destroy(go, lifetime);
            return go;
        }
    }
}
