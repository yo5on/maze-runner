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
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;
            
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy == null) return;
            
            Vector2 contactNormal = collision.contacts[0].normal;
            
            if (contactNormal.y < -0.5f)
            {
                enemy.Defeat();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
            }
            else
            {
                playerHealth.TakeDamage(1);
            }
        }
    }
}
