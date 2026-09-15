using UnityEngine;

namespace MazeRunner.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class WraithEnemy : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float patrolDistance = 5f;
        
        [Header("Combat")]
        [SerializeField] private GameObject spellPrefab;
        [SerializeField] private Transform spellSpawnPoint;
        [SerializeField] private float attackRange = 8f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int spellDamage = 1;
        [SerializeField] private float spellSpeed = 5f;
        
        [Header("Health")]
        [SerializeField] private int maxHealth = 3;
        
        private Rigidbody2D rb;
        private Animator animator;
        private Vector3 startPosition;
        private int direction = 1;
        private float attackTimer;
        private int currentHealth;
        private bool isDead;
        private Transform player;
        
        private static readonly int IsFloatingHash = Animator.StringToHash("isFloating");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            startPosition = transform.position;
            currentHealth = maxHealth;
            rb.gravityScale = 0;
        }
        
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        private void Update()
        {
            if (isDead) return;
            
            attackTimer -= Time.deltaTime;
            
            if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange && attackTimer <= 0)
            {
                CastSpell();
            }
        }
        
        private void FixedUpdate()
        {
            if (isDead) return;
            Patrol();
        }
        
        private void Patrol()
        {
            float distanceFromStart = transform.position.x - startPosition.x;
            
            if (Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                direction *= -1;
            }
            
            rb.linearVelocity = new Vector2(direction * floatSpeed, rb.linearVelocity.y);
            transform.localScale = new Vector3(direction, 1, 1);
            animator.SetBool(IsFloatingHash, true);
        }
        
        private void CastSpell()
        {
            attackTimer = attackCooldown;
            animator.SetTrigger(AttackTriggerHash);
            
            if (spellPrefab != null && spellSpawnPoint != null)
            {
                Vector2 directionToPlayer = (player.position - spellSpawnPoint.position).normalized;
                GameObject spell = Instantiate(spellPrefab, spellSpawnPoint.position, Quaternion.identity);
                Projectile projectile = spell.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(directionToPlayer, spellSpeed, spellDamage);
                }
            }
        }
        
        public void TakeDamage(int damage)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            animator.SetTrigger(HurtTriggerHash);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            isDead = true;
            animator.SetTrigger(DeathTriggerHash);
            rb.linearVelocity = Vector2.zero;
            Destroy(gameObject, 1.5f);
        }
    }
}