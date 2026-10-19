using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Animates a wind-up warning (see <see cref="EnemyBehaviour"/>.Warn): the badge grows and, in the
    /// last part of the wind-up, blinks faster and faster; the timer bar under it fills with crimson
    /// and is full at the moment the move fires. Keeps the warning upright and un-mirrored whichever way
    /// the enemy faces, and in world units whatever the enemy's scale.
    /// </summary>
    public class TelegraphPulse : MonoBehaviour
    {
        private float from, to, duration, t;
        private Transform badge;
        private SpriteRenderer badgeRenderer;
        private Transform fill;
        private float barWidth;

        public void Init(Transform badgeTransform, float startSize, float endSize, Transform timerFill, float timerWidth, float seconds)
        {
            badge = badgeTransform;
            badgeRenderer = badge.GetComponent<SpriteRenderer>();
            fill = timerFill;
            barWidth = timerWidth;
            from = startSize;
            to = endSize;
            duration = Mathf.Max(0.05f, seconds);
            Apply();
        }

        private void LateUpdate()
        {
            t += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            // Cancel the parent's scale and mirroring so children are in plain world units.
            Vector3 parent = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(1f / NonZero(parent.x), 1f / NonZero(parent.y), 1f);

            float k = Mathf.Clamp01(t / duration);
            if (badge != null) badge.localScale = Vector3.one * Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
            // Steady first, then blinks faster as the move gets closer.
            if (badgeRenderer != null) badgeRenderer.enabled = k < 0.55f || Mathf.Repeat(t * (6f + 14f * k), 1f) < 0.65f;
            if (fill != null)
            {
                fill.localScale = new Vector3(barWidth * k, fill.localScale.y, 1f);
                fill.localPosition = new Vector3(-barWidth / 2f + barWidth * k / 2f, fill.localPosition.y, 0f);
            }
        }

        private static float NonZero(float v) => Mathf.Abs(v) < 0.0001f ? 1f : v;
    }
}
