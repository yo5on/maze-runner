using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>The raised shield kicks forward and settles back (about 0.2 s) when it blocks a stomp.</summary>
    public class ShieldBlockPulse : MonoBehaviour
    {
        private const float Duration = 0.2f;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private float t = -1f;

        public void Play()
        {
            if (t < 0f)
            {
                baseScale = transform.localScale;
                basePosition = transform.localPosition;
            }
            t = 0f;
        }

        private void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Duration);
            float kick = Mathf.Sin(k * Mathf.PI); // out and back
            transform.localScale = baseScale * (1f + 0.22f * kick);
            transform.localPosition = basePosition + new Vector3(0.12f * kick / Mathf.Max(0.01f, Mathf.Abs(transform.parent != null ? transform.parent.lossyScale.x : 1f)), 0f, 0f);
            if (k >= 1f)
            {
                transform.localScale = baseScale;
                transform.localPosition = basePosition;
                t = -1f;
            }
        }
    }
}
