using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Hurts the player on contact (spikes). Standing in it keeps hurting, once per
    /// <see cref="repeatInterval"/>, and every hit bounces the player up so it can be escaped.
    /// A <see cref="TimedHazard"/> on the same object switches it on and off.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private bool instantKill = false;
        [Tooltip("Seconds between hits while the player stays inside.")]
        [SerializeField] private float repeatInterval = 0.8f;
        [Tooltip("Upward speed given to the player on each hit (0 = none).")]
        [SerializeField] private float bounceSpeed = 9f;

        private float nextHitTime;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHurt(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHurt(other);
        }

        private void TryHurt(Collider2D other)
        {
            if (!enabled || !other.CompareTag("Player") || Time.time < nextHitTime) return;

            var health = other.GetComponent<Player.PlayerHealth>();
            if (health == null) return;

            nextHitTime = Time.time + repeatInterval;
            if (instantKill)
            {
                health.Die();
                return;
            }

            health.TakeDamage(damage);
            if (bounceSpeed > 0f && other.attachedRigidbody != null)
            {
                Vector2 v = other.attachedRigidbody.linearVelocity;
                other.attachedRigidbody.linearVelocity = new Vector2(v.x, Mathf.Max(v.y, bounceSpeed));
            }
        }
    }
}
