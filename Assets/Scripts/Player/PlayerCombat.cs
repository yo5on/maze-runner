using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 10f;
        [SerializeField] private LayerMask enemyLayer;
        
        private PlayerHealth playerHealth;
        private Rigidbody2D rb;
        
        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            rb = GetComponent<Rigidbody2D>();

            if (playerHealth == null)
            {
                Debug.LogError("PlayerCombat: PlayerHealth component not found!", this);
            }
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;
            
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            GoblinEnemy goblinEnemy = collision.gameObject.GetComponent<GoblinEnemy>();
            
            if (enemy == null && goblinEnemy == null) return;
            
            Vector2 contactNormal = collision.contacts[0].normal;
            float playerVelocityY = rb.linearVelocity.y;
            
            bool isStomping = playerVelocityY < -0.1f && contactNormal.y > 0.5f;
            
            if (isStomping)
            {
                if (enemy != null)
                {
                    enemy.Defeat();
                }
                else if (goblinEnemy != null)
                {
                    goblinEnemy.TakeDamage(1);
                }
                
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlayEnemyDefeat();
                }
            }
            // Body collision does not damage player - only sword attacks via Animation Events
        }
    }
}
