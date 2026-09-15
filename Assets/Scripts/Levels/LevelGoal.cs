using UnityEngine;

namespace MazeRunner.Levels
{
    /// <summary>
    /// Completes the active level when the player enters this trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelGoal : MonoBehaviour
    {
        [SerializeField] private bool completeOnlyOnce = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = Color.white;
        [SerializeField] private Color completedColor = Color.green;

        private bool completed;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            UpdateVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || (completeOnlyOnce && completed))
            {
                return;
            }

            if (EnemyTracker.Instance != null && EnemyTracker.Instance.RemainingEnemyCount > 0)
            {
                return;
            }
            
            if (EnemyTracker.Instance == null)
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