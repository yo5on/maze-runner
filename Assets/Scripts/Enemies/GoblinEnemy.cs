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

        [Header("Platform Awareness")]
        [Tooltip("Layers that count as walkable ground and walls.")]
        [SerializeField] private LayerMask groundLayer = 1 << 3;
        [Tooltip("How far below the spawn point to look for the platform the goblin stands on.")]
        [SerializeField] private float platformProbeDistance = 5f;
        [Tooltip("Distance kept from a platform's real edge when patrolling it.")]
        [SerializeField] private float edgeMargin = 0.15f;
        [Tooltip("How far ahead of the feet to look for a ledge or wall.")]
        [SerializeField] private float lookAheadDistance = 0.2f;
        [Tooltip("Minimum time between two turns, so a goblin never flickers in place.")]
        [SerializeField] private float turnCooldown = 0.25f;
        [Tooltip("A patrolling goblin that makes no progress for this long (e.g. blocked by another goblin) turns around.")]
        [SerializeField] private float stallTurnTime = 0.2f;

        [Header("Boss Mode")]
        [SerializeField] private bool isBoss = false;
        [Tooltip("The boss notices the player inside this range and starts running.")]
        [SerializeField] private float bossChaseRange = 20f;
        [Tooltip("Extra distance the player must open up before the boss gives up the chase (prevents boundary jitter).")]
        [SerializeField] private float bossLoseRangeBuffer = 1.5f;
        [Tooltip("Chase speed = moveSpeed x this multiplier.")]
        [SerializeField] private float bossRunSpeedMultiplier = 1.75f;
        [Tooltip("Extra distance beyond attackRange before the boss starts running again after stopping to fight.")]
        [SerializeField] private float bossResumeChaseBuffer = 0.4f;
        [Tooltip("Horizontal distance under which the boss keeps its current facing (player directly above or below).")]
        [SerializeField] private float bossFacingDeadZone = 0.15f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 0.5f;
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("The goblin stands still for this long while its slash plays.")]
        [SerializeField] private float attackMoveLock = 0.4f;

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
        private Collider2D bodyCollider;
        private Vector3 startPosition;
        private int direction = 1;
        private float attackTimer;
        private float attackLockTimer;
        private float turnTimer;
        private float stallTimer;
        private float lastPatrolX;
        private int currentHealth;
        private bool isDead;
        private Transform player;
        private bool hasNoticedPlayer;
        private float noticeSpeedTimer;

        private float patrolMinX;
        private float patrolMaxX;
        private Collider2D platform;
        private bool patrolsWholePlatform;

        private bool bossAlerted;
        private bool bossHolding;
        private bool hasRunParameter;

        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");

        /// <summary>True once this goblin has taken over patrolling its whole platform.</summary>
        public bool PatrolsWholePlatform => patrolsWholePlatform;

        /// <summary>True while the boss is chasing the player.</summary>
        public bool IsBossAlerted => isBoss && bossAlerted;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            bodyCollider = GetComponent<Collider2D>();
            startPosition = transform.position;
            currentHealth = maxHealth;

            direction = startDirection >= 0 ? 1 : -1;
            patrolMinX = startPosition.x - patrolDistance;
            patrolMaxX = startPosition.x + patrolDistance;
            hasRunParameter = HasAnimatorParameter(IsRunningHash);
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;

            FindPlatform();
        }

        private void OnEnable()
        {
            // Re-join the platform group if this goblin was disabled and enabled again.
            if (!isDead && platform != null) GoblinPlatformGroup.Register(platform, this);
        }

        private void OnDisable()
        {
            LeavePlatformGroup();
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
            attackLockTimer -= Time.deltaTime;
            turnTimer -= Time.deltaTime;

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

        // ---------------------------------------------------------------- platform

        private void FindPlatform()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, platformProbeDistance, groundLayer);
            if (hit.collider == null) return;

            platform = hit.collider;

            // Never patrol past the platform's real ends, even if patrolDistance is larger than the platform.
            GetPlatformLimits(out float min, out float max);
            patrolMinX = Mathf.Max(patrolMinX, min);
            patrolMaxX = Mathf.Min(patrolMaxX, max);
            if (patrolMinX > patrolMaxX) patrolMinX = patrolMaxX = Mathf.Clamp(startPosition.x, min, max);

            if (!isBoss && isActiveAndEnabled) GoblinPlatformGroup.Register(platform, this);
        }

        private void GetPlatformLimits(out float min, out float max)
        {
            Bounds b = platform.bounds;
            float halfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0f;
            min = b.min.x + halfWidth + edgeMargin;
            max = b.max.x - halfWidth - edgeMargin;
            if (min > max) min = max = b.center.x;
        }

        /// <summary>
        /// Called once by <see cref="GoblinPlatformGroup"/> when the other goblin sharing this platform is gone.
        /// From then on this goblin walks the full length of the platform and turns at its real ends.
        /// </summary>
        internal void OnPlatformPartnerLost(Collider2D sharedPlatform)
        {
            if (isDead || patrolsWholePlatform || sharedPlatform == null || sharedPlatform != platform) return;

            patrolsWholePlatform = true;
            GetPlatformLimits(out patrolMinX, out patrolMaxX);
        }

        private void LeavePlatformGroup()
        {
            if (ReferenceEquals(platform, null)) return;
            GoblinPlatformGroup.Unregister(platform, this);
        }

        /// <summary>True when the next step in the current direction would walk off a ledge or into a wall.</summary>
        private bool IsBlockedAhead()
        {
            if (bodyCollider == null) return false;

            Bounds b = bodyCollider.bounds;
            float frontX = direction > 0 ? b.max.x : b.min.x;

            Vector2 footProbe = new Vector2(frontX + direction * lookAheadDistance, b.min.y + 0.1f);
            bool groundAhead = Physics2D.Raycast(footProbe, Vector2.down, 0.6f, groundLayer).collider != null;

            Vector2 wallOrigin = new Vector2(frontX, b.center.y);
            RaycastHit2D wall = Physics2D.Raycast(wallOrigin, new Vector2(direction, 0f), lookAheadDistance, groundLayer);
            bool wallAhead = wall.collider != null && wall.collider != platform;

            return !groundAhead || wallAhead;
        }

        // ---------------------------------------------------------------- boss

        private void BossChase()
        {
            if (player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Hysteresis: notice inside bossChaseRange, give up only once clearly outside it.
            if (!bossAlerted && distanceToPlayer <= bossChaseRange) bossAlerted = true;
            else if (bossAlerted && distanceToPlayer > bossChaseRange + bossLoseRangeBuffer) bossAlerted = false;

            bool running = false;
            if (bossAlerted)
            {
                float dx = player.position.x - transform.position.x;
                if (Mathf.Abs(dx) > bossFacingDeadZone) direction = dx > 0 ? 1 : -1;

                // Stop to fight inside attackRange; start running again only once the player is clearly out of it.
                if (bossHolding)
                {
                    if (distanceToPlayer > attackRange + bossResumeChaseBuffer) bossHolding = false;
                }
                else if (distanceToPlayer <= attackRange)
                {
                    bossHolding = true;
                }

                running = !bossHolding && attackLockTimer <= 0 && !IsBlockedAhead();
            }
            else
            {
                bossHolding = false;
            }

            float speed = running ? moveSpeed * bossRunSpeedMultiplier : 0f;
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

            ApplyFacing();
            SetLocomotion(false, running);
        }

        private bool IsPlayerInBossRange()
        {
            if (player == null) return false;

            float distance = Vector2.Distance(transform.position, player.position);
            return distance <= attackRange;
        }

        // ---------------------------------------------------------------- patrol

        private void Patrol()
        {
            if (attackLockTimer > 0)
            {
                // Stand still and keep facing the target while the slash plays.
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                ApplyFacing();
                SetLocomotion(false, false);
                stallTimer = 0f;
                lastPatrolX = rb.position.x;
                return;
            }

            float x = rb.position.x;

            // Walking but not getting anywhere: something solid (such as the other goblin) is in the way.
            stallTimer = Mathf.Abs(x - lastPatrolX) < 0.002f ? stallTimer + Time.fixedDeltaTime : 0f;
            lastPatrolX = x;

            // Turn only when moving outward past a bound, so a goblin standing on the bound cannot flip every step.
            if (direction > 0 && x >= patrolMaxX) Turn(-1);
            else if (direction < 0 && x <= patrolMinX) Turn(1);
            else if (turnTimer <= 0 && (IsBlockedAhead() || stallTimer >= stallTurnTime)) Turn(-direction);

            float currentSpeed = moveSpeed * (hasNoticedPlayer && noticeSpeedTimer > 0 ? noticeSpeedMultiplier : 1f);
            rb.linearVelocity = new Vector2(direction * currentSpeed, rb.linearVelocity.y);
            ApplyFacing();

            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            SetLocomotion(isMoving, false);
        }

        private void Turn(int newDirection)
        {
            if (newDirection == direction) return;
            direction = newDirection;
            turnTimer = turnCooldown;
            stallTimer = 0f;
        }

        private void ApplyFacing()
        {
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(direction * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }

        private void SetLocomotion(bool walking, bool running)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            animator.SetBool(IsWalkingHash, walking);
            if (hasRunParameter) animator.SetBool(IsRunningHash, running);
        }

        private bool HasAnimatorParameter(int hash)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == hash) return true;
            }
            return false;
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
            attackLockTimer = attackMoveLock;
            SetLocomotion(false, false);
            animator.SetTrigger(AttackTriggerHash);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
            SetLocomotion(false, false);
            animator.SetTrigger(DeathTriggerHash);

            // Tell a platform partner right away, not when the corpse is destroyed 1.5 s later.
            LeavePlatformGroup();

            if (Levels.EnemyTracker.Instance != null)
            {
                Levels.EnemyTracker.Instance.NotifyEnemyDefeated();
            }

            Destroy(gameObject, 1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && !isBoss)
            {
                Gizmos.color = patrolsWholePlatform ? Color.cyan : Color.green;
                float y = transform.position.y;
                Gizmos.DrawLine(new Vector3(patrolMinX, y), new Vector3(patrolMaxX, y));
            }

            if (attackPoint == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);

            if (isBoss)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, bossChaseRange);
                Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, bossChaseRange + bossLoseRangeBuffer);
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
