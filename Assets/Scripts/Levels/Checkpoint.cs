using UnityEngine;

namespace MazeRunner.Levels
{
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private bool activated = false;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color inactiveColor = Color.gray;
        [SerializeField] private Color activeColor = Color.green;
        
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            UpdateVisual();
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated) return;
            
            if (other.CompareTag("Player"))
            {
                Activate(other.gameObject);
            }
        }
        
        private void Activate(GameObject player)
        {
            activated = true;
            
            var health = player.GetComponent<Player.PlayerHealth>();
            if (health != null)
            {
                health.SetCheckpoint(transform.position);
            }
            
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayCheckpoint();
            }
            
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = activated ? activeColor : inactiveColor;
            }
        }
    }
}
