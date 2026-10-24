using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeRunner.Player;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// An enemy's personality: how it moves, notices the player, attacks and reacts.
    ///
    /// <see cref="GoblinEnemy"/> on the same object stays the shared body (health, damage, death and the
    /// defeat count, platform edges, facing, locomotion animation, basic slash, boss chase). A behaviour
    /// only decides: GoblinEnemy calls <see cref="Think"/> every frame and, while
    /// <see cref="ControlsMovement"/> is true, <see cref="Act"/> every physics step instead of its own
    /// patrol. One behaviour per enemy.
    ///
    /// Special moves run as coroutines started with <see cref="RunSpecial"/>; they always start with a
    /// visible wind-up (<see cref="Warn"/>, and <see cref="DangerZone"/> for area attacks), resolve their
    /// hit once, recover and respect a cooldown. Death (and, for light enemies, being stomped) stops a
    /// special at once, removes its telegraphs and keeps it from dealing damage.
    /// </summary>
    [RequireComponent(typeof(GoblinEnemy))]
    [DisallowMultipleComponent]
    public abstract class EnemyBehaviour : MonoBehaviour
    {
        protected GoblinEnemy Enemy { get; private set; }
        protected PlayerHealth PlayerHealth { get; private set; }
        protected PlayerController PlayerController { get; private set; }

        private Coroutine special;
        private readonly List<GameObject> telegraphs = new List<GameObject>();
        private static Sprite square;

        /// <summary>True while a special move (wind-up, action or recovery) is running.</summary>
        protected bool Busy => special != null;

        /// <summary>For tests and debugging: every wind-up, strike, hit, block and recovery is announced here.</summary>
        public static event System.Action<EnemyBehaviour, string> Traced;

        public bool IsBusy => Busy;

        protected void Trace(string what) => Traced?.Invoke(this, what);

        /// <summary>When true GoblinEnemy calls <see cref="Act"/> instead of its own patrol.</summary>
        public virtual bool ControlsMovement => true;

        /// <summary>When true GoblinEnemy leaves attacking to this behaviour (no automatic slash).</summary>
        public virtual bool ControlsAttack => true;

        /// <summary>Heavy enemies keep going when stomped (no flinch out of a wind-up).</summary>
        protected virtual bool HasSuperArmor => false;

        /// <summary>While true (a guard, a committed attack) knockback on this enemy is cut to a quarter.</summary>
        public virtual bool ResistsKnockback => false;

        protected virtual void Awake()
        {
            Enemy = GetComponent<GoblinEnemy>();
            ForgetPlayer();
        }

        protected virtual void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerHealth = player.GetComponent<PlayerHealth>();
                PlayerController = player.GetComponent<PlayerController>();
                playerBody = player.GetComponent<Rigidbody2D>();
                playerCollider = player.GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// The player died and a boss fight restarts: stop any move and forget the fight. Behaviours with
        /// their own fight state (engaged, phase, counters) reset it here.
        /// </summary>
        public virtual void OnEncounterReset()
        {
            CancelSpecial();
            ForgetPlayer();
        }

        /// <summary>
        /// Per frame, before <see cref="Think"/>: tracks what the player is visibly doing - landings,
        /// take-offs (each jump gets a fresh misjudgement), stomp attempts on this enemy and approaches.
        /// </summary>
        public void Perceive()
        {
            bool grounded = PlayerGrounded;
            if (grounded && !playerWasGrounded) playerLandedAt = Time.time;
            if (!grounded && playerWasGrounded)
            {
                jumpId++;
                jumpError = Random.Range(-1f, 1f);
            }
            playerWasGrounded = grounded;
            if (!HasPlayer) return;

            if (Forecast.OnTarget && attemptJump != jumpId)
            {
                attemptJump = jumpId;
                attemptTimes[attemptIndex] = Time.time;
                attemptIndex = (attemptIndex + 1) % attemptTimes.Length;
                Trace("stomp attempt " + RecentStompAttempts);
            }

            // An approach: the player closing in on its floor from a distance.
            float d = Mathf.Abs(DX);
            if (d > 5f) approachArmed = true;
            else if (approachArmed && d < 3.5f && SameLevel())
            {
                approachArmed = false;
                approachSides[approachIndex] = DirToPlayer;
                approachIndex = (approachIndex + 1) % approachSides.Length;
            }
        }

        /// <summary>Per frame: perception and decisions.</summary>
        public virtual void Think() { }

        /// <summary>Per physics step, while <see cref="ControlsMovement"/>: movement.</summary>
        public virtual void Act() { Enemy.PatrolStep(); }

        /// <summary>Return true to turn a hit away (shield, parry, phase). Called before damage.</summary>
        public virtual bool TryBlock(int damage) => false;

        /// <summary>The enemy was hurt but survived. Light enemies flinch out of their wind-ups.</summary>
        public virtual void OnHurt()
        {
            if (!HasSuperArmor && Busy) Interrupt(0.35f);
        }

        /// <summary>Death stops everything this behaviour was doing.</summary>
        public virtual void OnDeath()
        {
            CancelSpecial();
            enabled = false;
        }

        // ---------------------------------------------------------------- perception

        protected Transform PlayerTransform => Enemy.Player;
        protected bool HasPlayer => Enemy.Player != null && PlayerHealth != null && PlayerHealth.CurrentHealth > 0;

        /// <summary>Horizontal offset to the player (positive = player to the right).</summary>
        protected float DX => HasPlayer ? Enemy.Player.position.x - transform.position.x : float.MaxValue;

        /// <summary>Height of the player's feet above this enemy's feet.</summary>
        protected float DY => HasPlayer ? (Enemy.Player.position.y - 0.5f) - transform.position.y : float.MaxValue;

        protected int DirToPlayer => DX >= 0f ? 1 : -1;

        /// <summary>The player is on (roughly) the same floor, so a ground attack can reach it.</summary>
        protected bool SameLevel(float tolerance = 1.4f) => HasPlayer && Mathf.Abs(DY) <= tolerance;

        /// <summary>Within <paramref name="range"/> horizontally and on the same floor.</summary>
        protected bool Notices(float range, float heightTolerance = 1.6f) =>
            HasPlayer && Mathf.Abs(DX) <= range && Mathf.Abs(DY) <= heightTolerance;

        protected bool PlayerGrounded => PlayerController != null && PlayerController.IsGrounded;

        // ---------------------------------------------------------------- anticipation
        // The player's only attack is the stomp, and its wind-up is plain to see: a jump whose arc comes
        // down on the enemy. Enemies read that arc - position, velocity and the jump's visible rise and
        // fall (it drops faster than it rises) - never the player's input. Every jump gets its own small
        // misjudgement of the landing spot (largest while the landing is far off, smaller for smarter
        // enemies); a reaction must wait out a reaction time, one roll per jump decides whether the enemy
        // reacts at all, then a cooldown follows - so no enemy dodges everything, and a stomp begun close
        // enough to land within the reaction time always gets through.
        //
        // Short-term memory: every stomp attempt on this enemy is remembered for 15 s, and repeated
        // attempts (a predictable player) make the next one likelier and quicker to read, up to a cap per
        // intelligence tier. The side the player keeps approaching from is remembered too.

        public enum Intelligence { Basic, Elite, Boss }

        /// <summary>How well this enemy reads the player. Bosses are always Boss; elites override this.</summary>
        public virtual Intelligence Tier => Enemy.IsBoss ? Intelligence.Boss : Intelligence.Basic;

        private struct Profile
        {
            public float ReactionScale, LandingError, LearnPerAttempt, MaxLearned, MaxChance;
            public Profile(float reaction, float error, float learn, float maxLearned, float maxChance)
            {
                ReactionScale = reaction; LandingError = error; LearnPerAttempt = learn; MaxLearned = maxLearned; MaxChance = maxChance;
            }
        }

        // Reaction-time scale, landing misjudgement (units, at its largest), chance learned per repeated
        // attempt, most it can learn, and the highest chance it ever reacts with.
        private static readonly Profile BasicProfile = new Profile(1.25f, 0.7f, 0.05f, 0.15f, 0.45f);
        private static readonly Profile EliteProfile = new Profile(1f, 0.35f, 0.08f, 0.2f, 0.6f);
        private static readonly Profile BossProfile = new Profile(0.9f, 0.18f, 0.08f, 0.25f, 0.7f);
        private Profile Mind => Tier == Intelligence.Boss ? BossProfile : Tier == Intelligence.Elite ? EliteProfile : BasicProfile;

        /// <summary>What the enemy reads from the player's jump.</summary>
        public struct StompForecast
        {
            /// <summary>The player is in the air at or above this enemy's head.</summary>
            public bool Airborne;
            /// <summary>Seconds until the arc comes down to head height.</summary>
            public float Time;
            /// <summary>Where it comes down, as this enemy judges it.</summary>
            public float LandX;
            /// <summary>Comes down on the head: a stomp.</summary>
            public bool OnTarget;
            /// <summary>Comes down beside it, within a step: a stomp that will miss.</summary>
            public bool Near;
        }

        private const float MemorySpan = 15f;
        private Rigidbody2D playerBody;
        private Collider2D playerCollider;
        private float threatSince = -1f;
        private int rolledJump = -1;
        private float nextReaction;
        private bool playerWasGrounded = true;
        private float playerLandedAt = -10f;
        private int jumpId;
        private float jumpError;
        private int attemptJump = -1;
        private readonly float[] attemptTimes = new float[6];
        private int attemptIndex;
        private readonly int[] approachSides = new int[4];
        private int approachIndex;
        private bool approachArmed = true;
        private StompForecast forecast;
        private int forecastFrame = -1;

        private void ForgetPlayer()
        {
            threatSince = -1f;
            nextReaction = 0f;
            for (int i = 0; i < attemptTimes.Length; i++) attemptTimes[i] = -MemorySpan * 2f;
            System.Array.Clear(approachSides, 0, approachSides.Length);
            approachArmed = true;
        }

        /// <summary>The player's current jump as this enemy reads it (computed once per frame).</summary>
        public StompForecast Forecast
        {
            get
            {
                if (forecastFrame != Time.frameCount)
                {
                    forecast = ReadJump();
                    forecastFrame = Time.frameCount;
                }
                return forecast;
            }
        }

        private StompForecast ReadJump()
        {
            var f = new StompForecast();
            if (!HasPlayer || PlayerGrounded || playerBody == null || Enemy.BodyCollider == null) return f;
            Bounds body = Enemy.BodyCollider.bounds;
            if (Mathf.Abs(playerBody.position.x - body.center.x) > 10f) return f;
            float feet = playerCollider != null ? playerCollider.bounds.min.y : playerBody.position.y - 0.5f;
            float height = feet - body.max.y;
            if (height < -0.3f) return f; // already level with or below the head
            float h = Mathf.Max(0f, height);
            Vector2 v = playerBody.linearVelocity;
            float g = Mathf.Max(0.01f, -Physics2D.gravity.y);
            float fall = g * Mathf.Max(0.01f, PlayerController != null ? PlayerController.FallGravityScale : playerBody.gravityScale);
            float t;
            if (v.y > 0f)
            {
                // Rising: up to the top of the arc, then the faster fall down to head height.
                float rise = g * Mathf.Max(0.01f, playerBody.gravityScale);
                t = v.y / rise + Mathf.Sqrt(2f * (h + v.y * v.y / (2f * rise)) / fall);
            }
            else
            {
                t = (v.y + Mathf.Sqrt(v.y * v.y + 2f * fall * h)) / fall;
            }
            float error = jumpError * Mind.LandingError * Mathf.Clamp01(t / 0.6f);
            f.Airborne = true;
            f.Time = t;
            f.LandX = playerBody.position.x + v.x * t + error;
            float off = Mathf.Abs(f.LandX - body.center.x);
            f.OnTarget = off <= body.extents.x + 0.5f;
            f.Near = !f.OnTarget && off <= body.extents.x + 2f;
            return f;
        }

        /// <summary>
        /// True when the player is in the air and its arc (as this enemy judges it) comes down on this
        /// enemy's head within <paramref name="horizon"/> seconds.
        /// </summary>
        protected bool StompIncoming(float horizon, out float timeToLand)
        {
            StompForecast f = Forecast;
            timeToLand = f.Time;
            return f.OnTarget && f.Time <= horizon;
        }

        /// <summary>The way to move to get out from under the predicted landing.</summary>
        protected int AwayFromLanding
        {
            get
            {
                float center = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.center.x : transform.position.x;
                float landX = Forecast.LandX;
                return landX > center ? -1 : landX < center ? 1 : (DX >= 0f ? -1 : 1);
            }
        }

        /// <summary>Stomp attempts on this enemy in the last 15 s.</summary>
        public int RecentStompAttempts
        {
            get
            {
                int n = 0;
                foreach (float t in attemptTimes) if (Time.time - t <= MemorySpan) n++;
                return n;
            }
        }

        /// <summary>Extra reaction chance learned from repeated attempts (the first attempt teaches nothing).</summary>
        private float Learned => Mathf.Min(Mind.MaxLearned, Mathf.Max(0, RecentStompAttempts - 1) * Mind.LearnPerAttempt);

        /// <summary>0..1: how predictable the player has been lately (repeated stomp attempts).</summary>
        public float PatternRead => Mind.MaxLearned > 0f ? Learned / Mind.MaxLearned : 0f;

        /// <summary>The side (+1 right, -1 left) the player came from in at least 3 of its last 4 approaches, else 0.</summary>
        public int ExpectedApproachSide
        {
            get
            {
                int sum = 0;
                foreach (int s in approachSides) sum += s;
                return sum >= 2 ? 1 : sum <= -2 ? -1 : 0;
            }
        }

        /// <summary>The reaction time actually used: scaled by the tier, a little quicker against a predictable player.</summary>
        public float ReactionTime(float baseTime) => Mathf.Max(0.1f, baseTime * Mind.ReactionScale * (1f - 0.25f * PatternRead));

        /// <summary>The reaction chance actually used: the base plus what it learned, capped by the tier.</summary>
        public float ReadChance(float baseChance) => Mathf.Min(Mind.MaxChance, baseChance + Learned);

        /// <summary>One roll per player jump (a second call during the same jump returns false).</summary>
        protected bool RollForThisJump(ref int lastJump, float chance)
        {
            if (lastJump == jumpId) return false;
            lastJump = jumpId;
            return Random.value < ReadChance(chance);
        }

        /// <summary>
        /// Turns a visible threat into a reaction: the threat must last the reaction time (seeing and
        /// deciding take time), one roll per jump against the chance, then <paramref name="cooldown"/>
        /// seconds before the next reaction. Tier and memory adjust the time and chance (see
        /// <see cref="ReactionTime"/>, <see cref="ReadChance"/>).
        /// </summary>
        protected bool Anticipates(bool threat, float reactionTime, float chance, float cooldown)
        {
            if (!threat) { threatSince = -1f; return false; }
            if (threatSince < 0f) threatSince = Time.time;
            if (rolledJump == jumpId || Time.time < nextReaction || Time.time - threatSince < ReactionTime(reactionTime)) return false;
            rolledJump = jumpId;
            if (Random.value > ReadChance(chance)) { Trace("let it come"); return false; }
            nextReaction = Time.time + cooldown;
            Trace("anticipated stomp, landing " + Forecast.LandX.ToString("0.0") + " in " + Forecast.Time.ToString("0.00") + " s");
            return true;
        }

        /// <summary>The player has just landed within <paramref name="range"/> without hurting this enemy: a missed stomp.</summary>
        protected bool PlayerMissedNearby(float range, float window = 0.35f) =>
            HasPlayer && PlayerGrounded && Time.time - playerLandedAt <= window && Mathf.Abs(DX) <= range &&
            Enemy.LastHurtTime < playerLandedAt - 0.3f;

        /// <summary>Distance the enemy can still move in a direction before its platform edge.</summary>
        protected float RoomAhead(int dir)
        {
            if (!Enemy.TryGetPlatformLimits(out float min, out float max)) return 0f;
            return dir > 0 ? max - transform.position.x : transform.position.x - min;
        }

        protected float GroundY => Enemy.Platform != null ? Enemy.Platform.bounds.max.y : transform.position.y;

        // ---------------------------------------------------------------- specials

        protected void RunSpecial(IEnumerator routine)
        {
            CancelSpecial();
            special = StartCoroutine(Wrap(routine));
        }

        private IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            special = null;
            ClearTelegraphs();
        }

        protected void CancelSpecial()
        {
            if (special != null) StopCoroutine(special);
            special = null;
            ClearTelegraphs();
            if (Enemy != null && Enemy.Anim != null) Enemy.Anim.speed = 1f;
        }

        /// <summary>Stops the current special and stands still for a moment (stunned).</summary>
        protected void Interrupt(float stun)
        {
            Trace("interrupted");
            CancelSpecial();
            Enemy.Halt();
            Enemy.LockMovement(stun);
        }

        // ---------------------------------------------------------------- telegraphs

        protected static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                    square.name = "EnemyTelegraphSquare";
                }
                return square;
            }
        }

        /// <summary>
        /// The wind-up warning above the head: a crimson "!" badge that grows and then blinks, over a
        /// stone timer bar that fills with crimson and is full when the move fires.
        /// </summary>
        protected void Warn(float duration)
        {
            Trace("windup " + duration.ToString("0.00"));
            var go = new GameObject("Warning");
            go.transform.SetParent(transform, false);
            float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            float top = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.max.y - transform.position.y : 2f;
            go.transform.localPosition = new Vector3(0f, (top + 0.8f) / scale, 0f);

            var badge = new GameObject("Badge");
            badge.transform.SetParent(go.transform, false);
            var badgeRenderer = badge.AddComponent<SpriteRenderer>();
            badgeRenderer.sprite = TelegraphArt.WarningBadge;
            badgeRenderer.sortingOrder = 52;

            const float barWidth = 0.7f, barHeight = 0.08f, barY = -0.5f;
            SpriteRenderer track = TelegraphPart(go.transform, "TimerTrack", TelegraphArt.Stone, 50);
            track.transform.localPosition = new Vector3(0f, barY, 0f);
            track.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            SpriteRenderer fill = TelegraphPart(go.transform, "TimerFill", TelegraphArt.Crimson, 51);
            fill.transform.localPosition = new Vector3(-barWidth / 2f, barY, 0f);
            fill.transform.localScale = new Vector3(0f, barHeight, 1f);

            go.AddComponent<TelegraphPulse>().Init(badge.transform, 0.5f, 0.72f, fill.transform, barWidth, duration);
            telegraphs.Add(go);
        }

        /// <summary>
        /// The ground an area attack or charge will cover: crimson hatching with a solid crimson top
        /// edge and end marks, so the reach reads at a glance without hiding the platform.
        /// </summary>
        protected GameObject DangerZone(float fromX, float toX, float height = 0.35f)
        {
            var go = new GameObject("DangerZone");
            float left = Mathf.Min(fromX, toX), right = Mathf.Max(fromX, toX);
            float width = Mathf.Max(0.1f, right - left);
            go.transform.position = new Vector3((left + right) / 2f, GroundY + height / 2f, 0f);

            var hatch = new GameObject("Hatching");
            hatch.transform.SetParent(go.transform, false);
            var sr = hatch.AddComponent<SpriteRenderer>();
            sr.sprite = TelegraphArt.Hatching;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = 4;

            const float line = 0.05f;
            SpriteRenderer edge = TelegraphPart(go.transform, "Edge", TelegraphArt.Crimson, 5);
            edge.transform.localPosition = new Vector3(0f, height / 2f - line / 2f, 0f);
            edge.transform.localScale = new Vector3(width, line, 1f);
            foreach (float side in new[] { -1f, 1f })
            {
                SpriteRenderer end = TelegraphPart(go.transform, "End", TelegraphArt.Crimson, 5);
                end.transform.localPosition = new Vector3(side * (width / 2f - line / 2f), 0f, 0f);
                end.transform.localScale = new Vector3(line, height, 1f);
            }
            telegraphs.Add(go);
            return go;
        }

        /// <summary>
        /// The enemy's shield, held in its free hand for the whole fight (see <see cref="HeldShield"/>); set
        /// <see cref="HeldShield.Raised"/> while stomps from the front are blocked. Uses the enemy's shield
        /// sprite (weapons pack) when it has one, otherwise a rounded stone bar.
        /// </summary>
        protected HeldShield HoldShield(Sprite sprite = null)
        {
            float height = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.size.y : 2f;
            return HeldShield.Attach(transform, sprite, height * 0.48f); // clear of the ground on the short-armed rigs
        }

        /// <summary>A short kick of the raised shield when it turns a stomp away.</summary>
        protected static void ShieldBlocked(GameObject shield)
        {
            if (shield == null) return;
            var held = shield.GetComponentInParent<HeldShield>();
            if (held != null) held.Brace();
            var pulse = shield.GetComponent<ShieldBlockPulse>();
            if (pulse == null) pulse = shield.AddComponent<ShieldBlockPulse>();
            pulse.Play();
        }

        /// <summary>
        /// True when the player is on the side the enemy faces (a raised shield covers it). The rear 35 %
        /// of the body counts as behind, so a stomp landing on the back gets past the shield.
        /// </summary>
        protected bool PlayerInFront()
        {
            if (!HasPlayer) return false;
            float halfWidth = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.extents.x : 0.5f;
            return DX * Enemy.Direction >= -halfWidth * 0.3f;
        }

        private float turnPendingSince = -1f;

        /// <summary>
        /// Turns to face the player only once the player has stayed on the other side for
        /// <paramref name="delay"/> seconds: a guarding enemy is slow to turn, which leaves its back open.
        /// </summary>
        protected void FacePlayerAfter(float delay)
        {
            if (!HasPlayer) return;
            if (DirToPlayer == Enemy.Direction) { turnPendingSince = -1f; return; }
            if (turnPendingSince < 0f) turnPendingSince = Time.time;
            if (Time.time - turnPendingSince >= delay)
            {
                Enemy.Face(DirToPlayer);
                turnPendingSince = -1f;
            }
        }

        /// <summary>Keeps a temporary object (a prop in hand, a marker) to be removed with the telegraphs.</summary>
        protected void TrackTelegraph(GameObject go)
        {
            if (go != null) telegraphs.Add(go);
        }

        private static SpriteRenderer TelegraphPart(Transform parent, string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TelegraphArt.Pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        protected void ClearTelegraphs()
        {
            foreach (GameObject go in telegraphs) if (go != null) Destroy(go);
            telegraphs.Clear();
        }

        /// <summary>Moves in a direction until <paramref name="distance"/> is covered or a ledge or wall stops it.</summary>
        protected IEnumerator Travel(int dir, float speed, float distance)
        {
            float travelled = 0f;
            while (travelled < distance && !Enemy.IsDead)
            {
                if (!Enemy.MoveHorizontal(dir, speed, true)) yield break;
                travelled += speed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        protected virtual void OnDisable()
        {
            CancelSpecial();
        }

        // ---------------------------------------------------------------- hits

        /// <summary>
        /// Damages the player if it is inside the box (world space). Returns true on a hit. Never hits
        /// when the enemy is dead.
        /// </summary>
        protected bool HitBox(Vector2 center, Vector2 size, int damage, Vector2 knockback, bool groundedOnly = false)
        {
            if (Enemy.IsDead || PlayerHealth == null) return false;
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, Enemy.PlayerLayer);
            if (hit == null) return false;
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health == null) return false;
            if (groundedOnly && !PlayerGrounded) { Trace("strike avoided (airborne)"); return false; }

            health.TakeDamage(damage);
            Rigidbody2D body = hit.attachedRigidbody;
            if (body != null && knockback != Vector2.zero) body.linearVelocity = knockback;
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySwordHit();
            Trace("hit player for " + damage);
            return true;
        }

        /// <summary>A box in front of the enemy: starts at its front edge and reaches <paramref name="reach"/>.</summary>
        protected bool HitInFront(float reach, float height, int damage, Vector2 knockback)
        {
            Bounds b = Enemy.BodyCollider.bounds;
            int dir = Enemy.Direction;
            float startX = dir > 0 ? b.center.x : b.center.x - reach - b.extents.x;
            var center = new Vector2(startX + (reach + b.extents.x) / 2f, b.min.y + height / 2f);
            return HitBox(center, new Vector2(reach + b.extents.x, height), damage, knockback);
        }

        protected static float Randomized(float value, float spread) => value * Random.Range(1f - spread, 1f + spread);
    }
}
