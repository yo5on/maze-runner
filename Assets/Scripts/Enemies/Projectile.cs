using UnityEngine;

namespace MazeRunner.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private Rigidbody2D rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
        }
        
        public void Initialize(Vector2 dir, float spd, int dmg)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            rb.linearVelocity = direction * speed;
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Destroy(gameObject, 5f);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var playerHealth = other.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
            else if (other.CompareTag("Ground"))
            {
                Destroy(gameObject);
            }
        }
    }
}