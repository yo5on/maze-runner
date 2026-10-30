using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// The level exit. Hidden (no sprite, no trigger) while any enemy of the level lives, boss included; it fades
    /// in once <see cref="EnemyTracker"/> has none left, and then completes the level when the player enters.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelGoal : MonoBehaviour
    {
        [SerializeField] private bool completeOnlyOnce = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = Color.white;
        [SerializeField] private Color completedColor = Color.green;
        [SerializeField] private float revealTime = 0.6f;

        private bool completed;
        private Collider2D trigger;
        private float revealedAt = -1f;

        /// <summary>True once every enemy is down and the exit can be entered.</summary>
        public bool IsRevealed => revealedAt >= 0f;

        private void Awake()
        {
            trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            SetRevealed(false);
        }

        // Checked from Update, after every scene enemy's Start has registered it, so a full level never reads as empty.
        private void Update()
        {
            bool clear = EnemyTracker.Instance != null && EnemyTracker.Instance.RemainingEnemyCount == 0;
            if (clear != IsRevealed) SetRevealed(clear);
            if (IsRevealed && spriteRenderer != null && !completed)
            {
                float k = revealTime > 0f ? Mathf.Clamp01((Time.time - revealedAt) / revealTime) : 1f;
                Color c = inactiveColor;
                spriteRenderer.color = new Color(c.r, c.g, c.b, c.a * k);
            }
        }

        private void SetRevealed(bool revealed)
        {
            revealedAt = revealed ? Time.time : -1f;
            trigger.enabled = revealed;
            if (spriteRenderer != null) spriteRenderer.enabled = revealed;
            UpdateVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || (completeOnlyOnce && completed) || !IsRevealed)
            {
                return;
            }

            CompleteLevel();
        }

        private void CompleteLevel()
        {
            if (LevelProgressionManager.Instance == null)
            {
                Debug.LogWarning("No LevelProgressionManager is available.", this);
                return;
            }

            completed = true;
            UpdateVisual();
            LevelProgressionManager.Instance.CompleteCurrentLevel();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = completed ? completedColor : inactiveColor;
            }
        }
    }
}
