using UnityEngine;

namespace MazeRunner.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float patrolDistance = 5f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private bool faceRightInitially = true;
        
        private Rigidbody2D rb;
        private Vector3 startPosition;
        private int direction = 1;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            startPosition = transform.position;
            direction = faceRightInitially ? 1 : -1;
        }
        
        private void FixedUpdate()
        {
            Patrol();
        }
        
        private void Patrol()
        {
            float distanceFromStart = transform.position.x - startPosition.x;
            
            if (Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                direction *= -1;
            }
            
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            transform.localScale = new Vector3(direction, 1, 1);
        }
        
        public void Defeat()
        {
            Destroy(gameObject);
        }
    }
}
