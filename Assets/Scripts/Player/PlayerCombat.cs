using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 10f;
        [Tooltip("Sideways push a stomp gives the enemy (before its knockback resistance). Small on purpose: a stomp never shoves an enemy off its platform.")]
        [SerializeField] private float stompKnockback = 3f;
        [SerializeField] private LayerMask enemyLayer;
        
        private const float StompInterval = 0.2f;
        
        private PlayerHealth playerHealth;
        private Rigidbody2D rb;
        private float lastStompTime = -1f;
        
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
            TryStomp(collision);
        }
        
        // Landing on an enemy's head and staying there (the contact can begin after the physics
        // step has already stopped the fall) still counts as a stomp.
        private void OnCollisionStay2D(Collision2D collision)
        {
            TryStomp(collision);
        }
        
        private void TryStomp(Collision2D collision)
        {
            if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;
            if (Time.time - lastStompTime < StompInterval) return;
            
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            GoblinEnemy goblinEnemy = collision.gameObject.GetComponent<GoblinEnemy>();
            
            if (enemy == null && goblinEnemy == null) return;
            
            // Stomp = touching the enemy from above while not moving up. Velocity is read after the
            // physics step, so a fall that the contact just stopped reads as ~0, not negative.
            bool fromAbove = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f) { fromAbove = true; break; }
            }
            bool isStomping = fromAbove && rb.linearVelocity.y <= 0.1f;
            
            if (isStomping)
            {
                lastStompTime = Time.time;
                if (enemy != null)
                {
                    enemy.Defeat();
                }
                else if (goblinEnemy != null)
                {
                    float side = Mathf.Sign(goblinEnemy.transform.position.x - transform.position.x);
                    goblinEnemy.TakeDamage(1, new Vector2(side * stompKnockback, 0f));
                }
                
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlayEnemyDefeat();
                }
            }
        }
    }
}
