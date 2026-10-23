using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// The moon in the night sky: a plain unlit sprite drawn behind everything (lowest sorting order, no
    /// collider), kept at the same spot of the screen - upper right, clear of the HUD buttons - and the
    /// same on-screen size whatever the camera position, zoom or aspect ratio (so it reads as infinitely
    /// far away). Optionally stretches a sky-gradient backdrop over the whole view so the moon sits in a
    /// deepening sky rather than on a flat clear colour. Runs after the camera has moved each frame.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkyMoon : MonoBehaviour
    {
        [Tooltip("Where the moon's centre sits on screen (0..1 viewport coordinates).")]
        [SerializeField] private Vector2 viewportPosition = new Vector2(0.84f, 0.74f);
        [Tooltip("Height of the moon sprite (disc + halo) as a fraction of the screen height.")]
        [SerializeField] private float screenHeightFraction = 0.24f;
        [Tooltip("On narrow (portrait) screens the size is taken from width x this instead, so the moon doesn't swamp the view.")]
        [SerializeField] private float narrowWidthFactor = 1.25f;
        [Tooltip("On narrow (portrait-ish) screens the moon keeps at least this far from the right edge, in moon widths.")]
        [SerializeField] private float edgeMargin = 0.65f;
        [Tooltip("Optional sky gradient stretched to fill the camera view behind the moon.")]
        [SerializeField] private SpriteRenderer backdrop;

        private SpriteRenderer sr;
        private UnityEngine.Camera cam;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic || sr.sprite == null) return;

            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * cam.aspect;
            float size = Mathf.Min(viewHeight, viewWidth * narrowWidthFactor) * screenHeightFraction;
            float scale = size / Mathf.Max(0.0001f, sr.sprite.bounds.size.y);
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 c = cam.transform.position;
            float x = c.x + (viewportPosition.x - 0.5f) * viewWidth;
            x = Mathf.Min(x, c.x + viewWidth / 2f - size * edgeMargin);
            float y = c.y + (viewportPosition.y - 0.5f) * viewHeight;
            transform.position = new Vector3(x, y, transform.position.z);

            if (backdrop != null && backdrop.sprite != null)
            {
                Vector3 b = backdrop.sprite.bounds.size;
                // Slight overscan so sub-pixel camera smoothing never shows the clear colour at the edges.
                backdrop.transform.localScale = new Vector3(viewWidth * 1.02f / b.x, viewHeight * 1.02f / b.y, 1f);
                backdrop.transform.position = new Vector3(c.x, c.y, backdrop.transform.position.z);
            }
        }
    }
}
