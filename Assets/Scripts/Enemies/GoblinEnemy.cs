using UnityEngine;

namespace MazeRunner.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class GoblinEnemy : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float patrolDistance = 5f;
        [SerializeField] private int startDirection = 1;
        
        [Header("Boss Mode")]
        [SerializeField] private bool isBoss = false;
        [SerializeField] private float bossChaseRange = 20f;
        
        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 0.5f;
        [SerializeField] private LayerMask playerLayer;
        
        [Header("Vision")]
        [SerializeField] private float visionRange = 3f;
        [SerializeField] private float visionWidth = 2f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float noticeSpeedMultiplier = 1.5f;
        [SerializeField] private float noticeSpeedDuration = 1.5f;
        [SerializeField] private float rearDetectionRange = 1.0f;
        
        [Header("Health")]
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private float fallDeathY = -10f;
        
        private Rigidbody2D rb;
        private Animator animator;
        private Vector3 startPosition;
        private int direction = 1;
        private float attackTimer;
        private int currentHealth;
        private bool isDead;
        private Transform player;
        private bool hasNoticedPlayer;
        private float noticeSpeedTimer;
        
        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            startPosition = transform.position;
            currentHealth = maxHealth;
            
            direction = startDirection >= 0 ? 1 : -1;
        }
        
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        private void Update()
        {
            if (isDead) return;
            
            if (transform.position.y < fallDeathY)
            {
                Die();
                return;
            }
            
            attackTimer -= Time.deltaTime;
            
            if (noticeSpeedTimer > 0)
            {
                noticeSpeedTimer -= Time.deltaTime;
                if (noticeSpeedTimer <= 0)
                {
                    hasNoticedPlayer = false;
                }
            }
            
            if (player != null)
            {
                if (!isBoss)
                {
                    CheckRearDetection();
                }
                
                bool canAttack = isBoss ? IsPlayerInBossRange() : IsPlayerInVision();
                
                if (canAttack)
                {
                    if (!hasNoticedPlayer && !isBoss)
                    {
                        hasNoticedPlayer = true;
                        noticeSpeedTimer = noticeSpeedDuration;
                    }
                    
                    if (attackTimer <= 0)
                    {
                        Attack();
                    }
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (isDead) return;
            
            if (isBoss)
            {
                BossChase();
            }
            else
            {
                Patrol();
            }
        }
        
        private void BossChase()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= bossChaseRange)
            {
                float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);
                direction = (int)directionToPlayer;
                
                if (distanceToPlayer > attackRange)
                {
                    rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(direction * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            if (animator != null && animator.runtimeAnimatorController != null && attackTimer <= 0)
            {
                animator.SetBool(IsWalkingHash, isMoving);
            }
        }
        
        private bool IsPlayerInBossRange()
        {
            if (player == null) return false;
            
            float distance = Vector2.Distance(transform.position, player.position);
            return distance <= attackRange;
        }
        
        private void Patrol()
        {
            float distanceFromStart = transform.position.x - startPosition.x;
            
            if (Mathf.Abs(distanceFromStart) >= patrolDistance)
            {
                direction *= -1;
            }
            
            float currentSpeed = moveSpeed * (hasNoticedPlayer && noticeSpeedTimer > 0 ? noticeSpeedMultiplier : 1f);
            rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(direction * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(IsWalkingHash, isMoving);
            }
        }
        
        private bool IsPlayerInVision()
        {
            if (player == null) return false;
            
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > visionRange) return false;
            
            float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);
            if (directionToPlayer != direction) return false;
            
            Vector2 origin = transform.position;
            Vector2 directionVector = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(origin, directionVector, distance, obstacleLayer);
            
            if (hit.collider != null) return false;
            
            return distance <= attackRange;
        }
        
        private void CheckRearDetection()
        {
            if (player == null) return;
            
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > rearDetectionRange) return;
            
            float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);
            if (directionToPlayer != direction)
            {
                direction = (int)directionToPlayer;
            }
        }
        
        private void Attack()
        {
            attackTimer = attackCooldown;
            animator.SetBool(IsWalkingHash, false);
            animator.SetTrigger(AttackTriggerHash);
            rb.linearVelocity = Vector2.zero;
        }
        
        public void DealDamage()
        {
            if (attackPoint == null) return;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
            foreach (Collider2D hit in hits)
            {
                var playerHealth = hit.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    
                    if (Audio.AudioManager.Instance != null)
                    {
                        Audio.AudioManager.Instance.PlaySwordHit();
                    }
                }
            }
        }
        
        public void TakeDamage(int damage)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            
            if (currentHealth <= 0)
            {
                Die();
                return;
            }
            
            animator.SetTrigger(HurtTriggerHash);
        }
        
        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            animator.SetTrigger(DeathTriggerHash);
            
            if (Levels.EnemyTracker.Instance != null)
            {
                Levels.EnemyTracker.Instance.NotifyEnemyDefeated();
            }
            
            Destroy(gameObject, 1.5f);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            
            if (isBoss)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, bossChaseRange);
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Vector3 pos = transform.position;
                Vector3 visionDir = new Vector3(direction, 0, 0);
                Vector3 visionEnd = pos + visionDir * visionRange;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, visionEnd);
                
                Vector3 topOffset = new Vector3(0, visionWidth / 2, 0);
                Vector3 bottomOffset = new Vector3(0, -visionWidth / 2, 0);
                Gizmos.DrawLine(pos + topOffset, visionEnd + topOffset);
                Gizmos.DrawLine(pos + bottomOffset, visionEnd + bottomOffset);
                Gizmos.DrawLine(visionEnd + topOffset, visionEnd + bottomOffset);
            }
        }
    }
}
