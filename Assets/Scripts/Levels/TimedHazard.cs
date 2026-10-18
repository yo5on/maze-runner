using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Switches a spike trap (<see cref="Hazard"/>) on and off on a fixed rhythm. While retracted
    /// it is harmless and dimmed; shortly before it extends again it flickers as a warning.
    /// Traps with different phases can be chained so the safe window travels along a path.
    /// </summary>
    [RequireComponent(typeof(Hazard))]
    public class TimedHazard : MonoBehaviour
    {
        [SerializeField] private float activeTime = 1.2f;
        [SerializeField] private float inactiveTime = 1.2f;
        [Tooltip("Offset into the cycle in seconds.")]
        [SerializeField] private float phase;
        [SerializeField] private float warningTime = 0.35f;
        [SerializeField] private Color activeColor = new Color(0.82f, 0.07f, 0.1f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.82f, 0.07f, 0.1f, 0.18f);

        private Hazard hazard;
        private Collider2D trigger;
        private SpriteRenderer sprite;
        private Vector3 activeScale;

        public void Configure(float active, float inactive, float offset)
        {
            activeTime = active;
            inactiveTime = inactive;
            phase = offset;
        }

        private void Awake()
        {
            hazard = GetComponent<Hazard>();
            trigger = GetComponent<Collider2D>();
            sprite = GetComponent<SpriteRenderer>();
            activeScale = transform.localScale;
        }

        private void Update()
        {
            float cycle = activeTime + inactiveTime;
            float t = Mathf.Repeat(Time.timeSinceLevelLoad + phase, cycle);
            bool active = t < activeTime;
            hazard.enabled = active;
            trigger.enabled = active;

            if (sprite == null) return;
            bool warning = !active && t > cycle - warningTime;
            sprite.color = active ? activeColor
                : warning ? (Mathf.Repeat(t * 12f, 1f) < 0.5f ? activeColor : inactiveColor)
                : inactiveColor;
            // Retracted spikes sink to a low strip.
            transform.localScale = active ? activeScale : new Vector3(activeScale.x, activeScale.y * 0.35f, activeScale.z);
        }
    }
}
