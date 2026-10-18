using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>Grows and blinks a telegraph marker over its lifetime.</summary>
    public class TelegraphPulse : MonoBehaviour
    {
        private float from, to, duration, t;
        private SpriteRenderer sr;

        public void Init(float startSize, float endSize, float seconds)
        {
            from = startSize;
            to = endSize;
            duration = Mathf.Max(0.05f, seconds);
            sr = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * from;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(from, to, k);
            // Blinks faster as the move gets closer.
            if (sr != null) sr.enabled = k < 0.5f || Mathf.Repeat(t * (6f + 14f * k), 1f) < 0.6f;
        }
    }
}
