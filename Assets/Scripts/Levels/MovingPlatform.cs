using System.Collections.Generic;
using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// A platform that moves back and forth between its start position and start + travel,
    /// easing at both ends. It is a kinematic body on the Ground layer, so the player's ground
    /// check works on it, and it carries whatever stands on top (the player, enemies) by moving
    /// their bodies along with it, so riders keep full control of their own movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 travel = new Vector2(4f, 0f);
        [Tooltip("Seconds for one full trip there and back.")]
        [SerializeField] private float period = 4f;
        [Tooltip("Where in the cycle the platform starts (0..1).")]
        [Range(0f, 1f)] [SerializeField] private float phase;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private Vector2 origin;
        private float time;
        private readonly Collider2D[] hits = new Collider2D[8];
        private readonly HashSet<Rigidbody2D> moved = new HashSet<Rigidbody2D>();
        private ContactFilter2D riderFilter;

        public void Configure(Vector2 newTravel, float newPeriod, float newPhase)
        {
            travel = newTravel;
            period = newPeriod;
            phase = newPhase;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = body.position;
            time = phase * period;
            riderFilter = new ContactFilter2D { useTriggers = false };
            body.position = PositionAt(time);
        }

        private Vector2 PositionAt(float t)
        {
            if (period <= 0f) return origin;
            // 0 -> 1 -> 0 over one period, eased (cosine) so riders are not jerked at the ends.
            float k = 0.5f - 0.5f * Mathf.Cos(t / period * Mathf.PI * 2f);
            return origin + travel * k;
        }

        private void FixedUpdate()
        {
            time += Time.fixedDeltaTime;
            Vector2 next = PositionAt(time);
            Vector2 delta = next - body.position;

            // Riders: bodies touching a thin strip along the top surface.
            Bounds b = box.bounds;
            Vector2 stripCenter = new Vector2(b.center.x, b.max.y + 0.08f);
            Vector2 stripSize = new Vector2(b.size.x - 0.05f, 0.16f);
            int count = Physics2D.OverlapBox(stripCenter, stripSize, 0f, riderFilter, hits);
            moved.Clear();
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D rider = hits[i].attachedRigidbody;
                if (rider == null || rider == body || rider.bodyType != RigidbodyType2D.Dynamic) continue;
                if (!moved.Add(rider)) continue;
                // Only bodies resting on (not jumping off) the platform.
                if (rider.linearVelocity.y - delta.y / Time.fixedDeltaTime > 0.5f) continue;
                rider.position += delta;
                // The platform moves the rider directly, so drop the vertical speed it picked up from
                // being pushed; otherwise a rider pops into the air when the platform slows at the top.
                if (rider.linearVelocity.y > 0f) rider.linearVelocity = new Vector2(rider.linearVelocity.x, 0f);
            }

            body.MovePosition(next);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, from + (Vector3)travel);
        }
    }
}
