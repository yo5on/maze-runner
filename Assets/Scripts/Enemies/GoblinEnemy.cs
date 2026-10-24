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
        [Tooltip("Name shown on the boss health bar.")]
        [SerializeField] private string bossTitle = "Goblin Warchief";
        [Tooltip("The health bar shows while the player is this close (horizontally) to the boss.")]
        [SerializeField] private float bossArenaRange = 14f;

        [Header("Knockback & Edges")]
        [Tooltip("Share of knockback ignored (0 = full knockback). Also sets the body's mass, so the player's body shoves it less.")]
        [Range(0f, 0.95f)] [SerializeField] private float knockbackResistance = 0.5f;
        [Tooltip("Used instead of knockbackResistance when this enemy is a boss.")]
        [Range(0f, 0.95f)] [SerializeField] private float bossKnockbackResistance = 0.8f;
        [Tooltip("How long a light knocked-back slide lasts before the enemy's own movement takes over again. Strong hits stagger up to 2.5x longer.")]
        [SerializeField] private float knockbackStun = 0.2f;
        [Tooltip("How fast the player's body can shove this enemy along the ground (units per second): it gives way a little, but is never carried.")]
        [SerializeField] private float shoveGive = 0.6f;
        [Tooltip("Used instead of shoveGive when this enemy is a boss.")]
        [SerializeField] private float bossShoveGive = 0.25f;
        [Tooltip("Off by default: knockback and shoving never carry an enemy over a ledge. Turn on only for an enemy deliberately meant to be knocked into pits.")]
        [SerializeField] private bool canBeKnockedOffLedges = false;

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 0.5f;
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("The goblin stands still for this long while its slash plays.")]
        [SerializeField] private float attackMoveLock = 0.4f;
        [Tooltip("When the slash hits if the attack clip has no DealDamage animation event (rigs imported without events).")]
        [SerializeField] private float attackHitTime = 0.2f;

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
        private float pendingHitTimer;
        private bool hitDealtThisAttack = true;
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
        private BossHealthBar bossBar;
        private Player.PlayerHealth playerHealth;

        private float knockbackTimer;
        private float knockbackTime;    // length of the current slide
        private float knockbackSpeed;   // its starting sideways speed; it eases to 0
        private float lastHurtTime = -10f;
        private Collider2D playerCollider;
        // The motion this enemy chose for the coming physics step (see ResistShove).
        private Vector2 ownedPosition;
        private Vector2 ownedVelocity;
        private bool hasOwnedMotion;

        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");

        private EnemyBehaviour behaviour;

        /// <summary>True once this goblin has taken over patrolling its whole platform.</summary>
        public bool PatrolsWholePlatform => patrolsWholePlatform;

        // ------------------------------------------------ API for EnemyBehaviour personalities
        // GoblinEnemy is the shared body of every enemy: health, damage, death and the defeat count,
        // platform awareness, facing, locomotion animation, the basic patrol / slash and the boss chase.
        // An optional EnemyBehaviour on the same object is its personality: it decides how the enemy
        // moves and attacks, using the methods below.

        public bool IsDead => isDead;
        public bool IsBoss => isBoss;
        public Transform Player => player;
        public int Direction => direction;
        public float MoveSpeed => moveSpeed;
        public float AttackRange => attackRange;
        public Rigidbody2D Body => rb;
        public Animator Anim => animator;
        public Collider2D BodyCollider => bodyCollider;
        public LayerMask PlayerLayer => playerLayer;
        public Collider2D Platform => platform;
        /// <summary>True while a basic slash (or a behaviour's attack lock) holds the enemy still.</summary>
        public bool IsAttackLocked => attackLockTimer > 0f;
        /// <summary>True while a knockback is sliding the enemy (its own movement waits).</summary>
        public bool IsKnockedBack => knockbackTimer > 0f;
        public float LastHurtTime => lastHurtTime;
        public string BossTitle => bossTitle;
        private float Resistance => isBoss ? bossKnockbackResistance : knockbackResistance;

        /// <summary>The walkable span of the platform under the enemy (its real edges, minus a margin).</summary>
        public bool TryGetPlatformLimits(out float min, out float max)
        {
            min = max = transform.position.x;
            if (platform == null) return false;
            GetPlatformLimits(out min, out max);
            return true;
        }

        public void Face(int newDirection)
        {
            if (newDirection == 0) return;
            direction = newDirection > 0 ? 1 : -1;
            ApplyFacing();
        }

        /// <summary>
        /// Walks or runs in a direction, but never off a ledge or into a wall: returns false (and stands
        /// still) when the next step is blocked or would leave the platform. With
        /// <paramref name="faceMovement"/> false it backs away while still facing the way it faces.
        /// </summary>
        public bool MoveHorizontal(int moveDirection, float speed, bool running = false, bool faceMovement = true)
        {
            if (moveDirection == 0) return false;
            // A knockback in progress wins over the enemy's own steps, unless it is guarding.
            if (knockbackTimer > 0f && !(behaviour != null && behaviour.enabled && behaviour.ResistsKnockback)) return false;
            int dir = moveDirection > 0 ? 1 : -1;
            if (faceMovement) Face(dir);
            float x = rb.position.x;
            bool outOfBounds = platform != null && TryGetPlatformLimits(out float min, out float max) &&
                               ((dir > 0 && x >= max) || (dir < 0 && x <= min));
            if (outOfBounds || IsBlockedAhead(dir))
            {
                Halt();
                return false;
            }
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            SetLocomotion(!running, running);
            return true;
        }

        public void Halt()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetLocomotion(false, false);
        }

        /// <summary>The original patrol (bounded, turns at ledges, walls and other goblins).</summary>
        public void PatrolStep() => Patrol();

        /// <summary>The original boss chase (runs at the player, stops at ledges).</summary>
        public void BossChaseStep() => BossChase();

        /// <summary>True when the basic slash is off cooldown and the player is in front, in reach.</summary>
        public bool CanBasicAttack => !isDead && attackTimer <= 0f && player != null && (isBoss ? IsPlayerInBossRange() : IsPlayerInVision());

        /// <summary>The original slash (Attack trigger, hit at attackPoint).</summary>
        public void BasicAttack() => Attack();

        /// <summary>Holds the enemy still for a while (used by behaviours for wind-ups and recoveries).</summary>
        public void LockMovement(float seconds) => attackLockTimer = Mathf.Max(attackLockTimer, seconds);

        /// <summary>Delays the next basic slash.</summary>
        public void DelayBasicAttack(float seconds) => attackTimer = Mathf.Max(attackTimer, seconds);

        /// <summary>Plays an animator state directly (e.g. "Kicking", "Throwing", "Sliding") if this rig has it.</summary>
        public bool PlayState(string stateName, float fade = 0.05f)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return false;
            animator.CrossFadeInFixedTime(hash, fade, 0);
            return true;
        }

        /// <summary>Returns to the locomotion states after a special move.</summary>
        public void ReturnToLocomotion()
        {
            animator.speed = 1f;
            PlayState("Idle", 0.08f);
        }

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
            behaviour = GetComponent<EnemyBehaviour>();
            // Heavier bodies are shoved less by the player's (mass 1) body: 0.5 -> 2, 0.8 -> 5.
            rb.mass = 1f / (1f - Resistance);
        }

        /// <summary>
        /// Sets the patrol for an enemy spawned at runtime (call right after Instantiate, before Start;
        /// Start then clamps the patrol to the platform under the enemy).
        /// </summary>
        public void ConfigurePatrol(int initialDirection, float distance)
        {
            startDirection = initialDirection >= 0 ? 1 : -1;
            direction = startDirection;
            patrolDistance = Mathf.Max(0f, distance);
            startPosition = transform.position;
            patrolMinX = startPosition.x - patrolDistance;
            patrolMaxX = startPosition.x + patrolDistance;
        }
        
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = playerObj.GetComponent<Player.PlayerHealth>();
                playerCollider = playerObj.GetComponent<Collider2D>();
            }

            FindPlatform();

            if (isBoss)
            {
                bossBar = BossHealthBar.Create(bossTitle, maxHealth);
                if (playerHealth != null) playerHealth.OnPlayerDeath += ResetEncounter;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.OnPlayerDeath -= ResetEncounter;
            if (bossBar != null) Destroy(bossBar.gameObject);
        }

        /// <summary>
        /// The player died: a boss goes back to where it stood, at full health and calm, so the fight
        /// restarts cleanly when the player comes back from the checkpoint.
        /// </summary>
        private void ResetEncounter()
        {
            if (isDead) return;
            if (behaviour != null) behaviour.OnEncounterReset();
            currentHealth = maxHealth;
            rb.position = startPosition;
            transform.position = startPosition;
            rb.linearVelocity = Vector2.zero;
            bossAlerted = bossHolding = hasOwnedMotion = false;
            knockbackTimer = attackLockTimer = 0f;
            hitDealtThisAttack = true;
            animator.speed = 1f;
            SetLocomotion(false, false);
            if (bossBar != null) bossBar.SetHealth(currentHealth);
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

            if (bossBar != null)
            {
                // Shown while the player is in the boss's arena.
                bool near = player != null && Mathf.Abs(player.position.x - transform.position.x) <= bossArenaRange &&
                            Mathf.Abs(player.position.y - transform.position.y) <= 6f;
                bossBar.SetVisible(near);
            }

            // Clips without a DealDamage event still hit, once, at attackHitTime.
            if (!hitDealtThisAttack)
            {
                pendingHitTimer -= Time.deltaTime;
                if (pendingHitTimer <= 0f) DealDamage();
            }

            if (noticeSpeedTimer > 0)
            {
                noticeSpeedTimer -= Time.deltaTime;
                if (noticeSpeedTimer <= 0)
                {
                    hasNoticedPlayer = false;
                }
            }

            if (behaviour != null && behaviour.enabled)
            {
                behaviour.Perceive();
                behaviour.Think();
                if (behaviour.ControlsAttack) return;
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

            bool guarding = behaviour != null && behaviour.enabled && behaviour.ResistsKnockback;
            if (knockbackTimer > 0f && !guarding)
            {
                // Staggered: the hit slides the enemy and eases to a stop; its own movement waits.
                knockbackTimer -= Time.fixedDeltaTime;
                float ease = knockbackTime > 0f ? Mathf.Clamp01(knockbackTimer / knockbackTime) : 0f;
                rb.linearVelocity = new Vector2(knockbackSpeed * ease, rb.linearVelocity.y);
            }
            else
            {
                if (knockbackTimer > 0f) knockbackTimer -= Time.fixedDeltaTime; // a committed move shrugs it off
                if (behaviour != null && behaviour.enabled && behaviour.ControlsMovement) behaviour.Act();
                else if (isBoss) BossChase();
                else Patrol();
            }
            GuardEdge();

            // What it chose for this step; ResistShove compares the step's result against it.
            ownedPosition = rb.position;
            ownedVelocity = rb.linearVelocity;
            hasOwnedMotion = true;
        }

        // ---------------------------------------------------------------- knockback and shoving

        /// <summary>
        /// A hit's push: a stagger that slides the enemy along the ground and eases to a stop. Resistance
        /// (stronger on bosses, and while a behaviour guards) shrinks it; stronger hits slide faster and
        /// stagger longer (up to 2.5x). The slide is planned to end before the platform edge and never
        /// lifts the enemy, so a hit can stagger an enemy but not throw it off its platform - unless
        /// <see cref="canBeKnockedOffLedges"/> marks it as deliberately vulnerable to falls.
        /// </summary>
        public void ApplyKnockback(Vector2 velocity)
        {
            if (isDead) return;
            float scale = 1f - Resistance;
            if (behaviour != null && behaviour.enabled && behaviour.ResistsKnockback) scale *= 0.25f;
            float vx = velocity.x * scale;
            if (Mathf.Abs(vx) < 0.1f) return;
            knockbackTime = knockbackStun * Mathf.Clamp(Mathf.Abs(vx) / 3f, 1f, 2.5f);
            float vy = rb.linearVelocity.y;
            if (canBeKnockedOffLedges) vy = Mathf.Max(vy, velocity.y * scale);
            else if (TryGetPlatformLimits(out float min, out float max))
            {
                // An eased stop covers speed x time / 2: cap the speed so the slide ends inside the platform.
                float room = Mathf.Max(0f, vx > 0f ? max - rb.position.x : rb.position.x - min);
                vx = Mathf.Sign(vx) * Mathf.Min(Mathf.Abs(vx), 2f * room / knockbackTime);
            }
            knockbackSpeed = vx;
            knockbackTimer = knockbackTime;
            rb.linearVelocity = new Vector2(vx, vy);
        }

        private void OnCollisionEnter2D(Collision2D collision) => ResistShove(collision);
        private void OnCollisionStay2D(Collision2D collision) => ResistShove(collision);

        /// <summary>
        /// Runs right after the physics step, for contacts with the player's body: the player may nudge the
        /// enemy along at <see cref="shoveGive"/> units/s, but the rest of the push is taken back and the
        /// contact's extra speed (sideways and upward) removed, so walking into, standing on or jumping into
        /// an enemy never carries or lifts it. Only pushes away from the player are undone, and its own
        /// steps into the player are not replayed (the bodies are touching: it holds its ground instead),
        /// so the two bodies never fight each other step after step.
        /// </summary>
        private void ResistShove(Collision2D collision)
        {
            if (isDead || !hasOwnedMotion || playerCollider == null || collision.collider != playerCollider) return;
            float away = Mathf.Sign(rb.position.x - playerCollider.bounds.center.x);
            float ownVx = ownedVelocity.x * away < 0f ? 0f : ownedVelocity.x;
            float expected = ownedPosition.x + ownVx * Time.fixedDeltaTime;
            float push = (rb.position.x - expected) * away;
            float give = (isBoss ? bossShoveGive : shoveGive) * Time.fixedDeltaTime;
            if (push > give) rb.position = new Vector2(expected + away * give, rb.position.y);
            Vector2 v = rb.linearVelocity;
            float vx = v.x * away > 0f ? Mathf.Min(v.x * away, ownVx * away) * away : v.x;
            rb.linearVelocity = new Vector2(vx, Mathf.Min(v.y, Mathf.Max(ownedVelocity.y, 0f)));
        }

        /// <summary>
        /// Keeps the enemy on its platform. Applies whenever its feet are at or above the platform top and
        /// over it - standing, hopping, leaping or lifted - and works on the velocity before the physics
        /// step: the step's speed is capped so it arrives at the limit instead of crossing it, so there is
        /// nothing to snap back and nothing to jitter. The limit sits outside the span its own movement
        /// uses (<see cref="TryGetPlatformLimits"/>), so the guard never fights the enemy's own steps. No
        /// physics queries, only the platform bounds found at Start.
        /// </summary>
        private void GuardEdge()
        {
            if (platform == null || bodyCollider == null) return;
            if (canBeKnockedOffLedges && knockbackTimer > 0f) return; // the deliberate exception
            Bounds feet = bodyCollider.bounds;
            Bounds ground = platform.bounds;
            if (feet.min.y < ground.max.y - 0.3f || feet.max.x < ground.min.x || feet.min.x > ground.max.x) return;

            float halfWidth = feet.extents.x;
            float min = ground.min.x + halfWidth * 0.5f, max = ground.max.x - halfWidth * 0.5f;
            if (min > max) return;
            float dt = Time.fixedDeltaTime;
            float center = feet.center.x;
            Vector2 v = rb.linearVelocity;
            // Only something outside its control (a test teleport, a crushing contact) can leave it past the limit.
            float correction = center < min ? min - center : center > max ? max - center : 0f;
            if (correction != 0f)
            {
                rb.position += new Vector2(correction, 0f);
                center += correction;
            }
            if (v.x > 0f) v.x = Mathf.Min(v.x, (max - center) / dt);
            else if (v.x < 0f) v.x = Mathf.Max(v.x, (min - center) / dt);
            rb.linearVelocity = v;
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

        private bool IsBlockedAhead() => IsBlockedAhead(direction);

        /// <summary>True when the next step in a direction would walk off a ledge or into a wall.</summary>
        private bool IsBlockedAhead(int dir)
        {
            if (bodyCollider == null) return false;

            Bounds b = bodyCollider.bounds;
            float frontX = dir > 0 ? b.max.x : b.min.x;

            Vector2 footProbe = new Vector2(frontX + dir * lookAheadDistance, b.min.y + 0.1f);
            bool groundAhead = Physics2D.Raycast(footProbe, Vector2.down, 0.6f, groundLayer).collider != null;

            Vector2 wallOrigin = new Vector2(frontX, b.center.y);
            RaycastHit2D wall = Physics2D.Raycast(wallOrigin, new Vector2(dir, 0f), lookAheadDistance, groundLayer);
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
            hitDealtThisAttack = false;
            pendingHitTimer = attackHitTime;
            SetLocomotion(false, false);
            animator.SetTrigger(AttackTriggerHash);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        public void DealDamage()
        {
            hitDealtThisAttack = true;
            if (isDead) return; // a slash interrupted by death never lands
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

            // A guarding personality (shield, parry, phasing) can turn the hit away.
            if (behaviour != null && behaviour.enabled && behaviour.TryBlock(damage)) return;

            currentHealth -= damage;
            lastHurtTime = Time.time;
            if (bossBar != null) bossBar.SetHealth(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            animator.SetTrigger(HurtTriggerHash);
            if (behaviour != null && behaviour.enabled) behaviour.OnHurt();
        }

        /// <summary>Damage with a push (see <see cref="ApplyKnockback"/>); a blocked hit does not push.</summary>
        public void TakeDamage(int damage, Vector2 knockback)
        {
            int before = currentHealth;
            TakeDamage(damage);
            if (currentHealth < before) ApplyKnockback(knockback);
        }

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            hitDealtThisAttack = true;
            if (bossBar != null) bossBar.SetHealth(0);
            rb.linearVelocity = Vector2.zero;
            SetLocomotion(false, false);
            // Clear anything queued before the killing blow so nothing interrupts the death clip.
            animator.ResetTrigger(AttackTriggerHash);
            animator.ResetTrigger(HurtTriggerHash);
            animator.SetTrigger(DeathTriggerHash);
            animator.speed = 1f;
            // Death interrupts any special move: its coroutines, telegraphs and hit boxes stop now.
            if (behaviour != null) behaviour.OnDeath();

            // Tell a platform partner right away, not when the corpse is removed after its death clip.
            LeavePlatformGroup();

            if (Levels.EnemyTracker.Instance != null)
            {
                Levels.EnemyTracker.Instance.NotifyEnemyDefeated();
            }

            StartCoroutine(RemoveAfterDeathAnimation());
        }

        /// <summary>
        /// Waits for the Dying clip to play through once (it does not loop), holds the last frame
        /// briefly, then removes the enemy. Falls back to a time limit if the state never starts.
        /// </summary>
        private System.Collections.IEnumerator RemoveAfterDeathAnimation()
        {
            const float holdLastFrame = 0.5f;
            const float maxWait = 3f;
            float waited = 0f;
            bool started = false;
            while (waited < maxWait)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Dying"))
                {
                    started = true;
                    if (state.normalizedTime >= 1f && !animator.IsInTransition(0)) break;
                }
                else if (started || animator.runtimeAnimatorController == null)
                {
                    break;
                }
                waited += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(holdLastFrame);
            Destroy(gameObject);
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
