using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Boss Goblin (Level 3): the Goblin Warchief, a skirmisher. A state machine (see <see cref="State"/>)
    /// on top of the shared goblin body; every move goes through <see cref="GoblinEnemy.MoveHorizontal"/>,
    /// so it never runs off a ledge or into a wall.
    /// - Stalk: once the player is near, it circles at 2.6-4.2 units, drifting in and out while facing
    ///   the player, and picks a move every 0.5-1 s.
    /// - Dash + Hunter's Combo: a short crouch (warning) then a dash in and two slashes (1 + 1). Every
    ///   third combo ends in a Heavy Cleave: 0.7 s wind-up with the ground marked, 2 damage. After a
    ///   combo it stands winded (the opening to stomp it), then backs off to stalk again.
    /// - Feint: the same crouch and dash, but it stops short and hops back.
    /// - Vault: when the player crowds it or corners it against a ledge, it leaps over the player and
    ///   lands behind them (only where there is ground to land on).
    /// - Dodge and counter: it reads a stomp coming (the player's arc), and after a short reaction time
    ///   it sometimes slides clear of the predicted landing, then counters with a telegraphed slash. A
    ///   stomp it reads will land beside it is answered in the air: the counter's warning shows at once
    ///   and the slash comes 0.3 s after the landing (time to jump away). A missed stomp it did not read
    ///   is punished after the landing. Repeated stomps make all of this likelier. 2.4 s cooldown.
    /// - Phase 2 (half health): a roar (warning, it stands still), then shorter wind-ups and recoveries,
    ///   more feints and dodges, and the Pounce: it leaps onto the player's spot, the landing zone marked
    ///   on the ground from take-off, and the shockwave hits only someone standing in it.
    /// It takes stomps without flinching, and resists knockback while committed to a move.
    /// </summary>
    public class BossHunterBehaviour : EnemyBehaviour
    {
        private enum State { Idle, Stalk, Dash, Combo, Heavy, Feint, Retreat, Vault, Pounce, Dodge, Counter, Recover, Enrage }

        [Header("Stalking")]
        [SerializeField] private float noticeRange = 12f;
        [SerializeField] private float stalkMin = 2.6f;
        [SerializeField] private float stalkMax = 4.2f;
        [SerializeField] private float stalkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 3.6f;
        [SerializeField] private float decisionMin = 0.5f;
        [SerializeField] private float decisionMax = 1f;
        [Tooltip("Longest it stalks without attacking before it commits to a dash.")]
        [SerializeField] private float stalkPatience = 2.5f;

        [Header("Dash + combo")]
        [SerializeField] private float dashReach = 5.5f;
        [SerializeField] private float dashSpeed = 8f;
        [SerializeField] private float dashWindUp = 0.3f;
        [SerializeField] private float comboRecovery = 0.7f;
        [SerializeField] private int heavyEvery = 3;
        [SerializeField] private float heavyWindUp = 0.7f;
        [SerializeField] private float heavyRecovery = 1.1f;
        [SerializeField] private float retreatDistance = 2.2f;
        [SerializeField] private float retreatSpeed = 4f;

        [Header("Vault")]
        [SerializeField] private float vaultCooldown = 4f;

        [Header("Dodge and counter")]
        [SerializeField] private float dodgeReaction = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0.5f;
        [SerializeField] private float dodgeCooldown = 2.4f;
        [SerializeField] private float dodgeSpeed = 9f;
        [SerializeField] private float dodgeDistance = 2.4f;
        [SerializeField] private float counterWindUp = 0.4f;
        [Tooltip("Chance (per jump, plus what it learns from repeated stomps) to read a stomp that will land beside it and wait for the landing with a counter.")]
        [Range(0f, 1f)] [SerializeField] private float readMissChance = 0.45f;
        [Tooltip("Seconds between the player's landing and that counter's slash: the time to jump away again.")]
        [SerializeField] private float landingGrace = 0.3f;

        [Header("Phase 2 (half health)")]
        [Range(0f, 1f)] [SerializeField] private float enrageAt = 0.5f;
        [SerializeField] private float roarTime = 0.9f;
        [Tooltip("Wind-ups and recoveries are multiplied by this in phase 2 (never below 0.22 s for a wind-up).")]
        [SerializeField] private float enragedTiming = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float enragedDodgeChance = 0.65f;
        [SerializeField] private float pounceWindUp = 0.6f;
        [SerializeField] private float pounceRadius = 1.6f;
        [SerializeField] private float pounceCooldown = 5f;

        private State state = State.Idle;
        private bool enraged;
        private int comboCount;
        private float nextDecision;
        private float nextVault;
        private float nextPounce;
        private float nextPunish;
        private float lastAttack;
        private bool cornered;
        private float paceOffset;
        private int readMissJump = -1;

        public override bool ControlsMovement => true;
        public override bool ControlsAttack => true;
        protected override bool HasSuperArmor => true;

        public override bool ResistsKnockback =>
            state == State.Dash || state == State.Combo || state == State.Heavy || state == State.Vault ||
            state == State.Pounce || state == State.Enrage;

        private float Timing(float seconds) => enraged ? Mathf.Max(0.22f, seconds * enragedTiming) : seconds;

        protected override void Start()
        {
            base.Start();
            paceOffset = Random.value * 10f;
        }

        public override void OnEncounterReset()
        {
            base.OnEncounterReset();
            state = State.Idle;
            enraged = false;
            comboCount = 0;
            cornered = false;
        }

        public override void OnHurt()
        {
            nextDecision = Mathf.Min(nextDecision, Time.time + 0.2f); // a hit makes it rethink soon
        }

        // ---------------------------------------------------------------- decisions

        public override void Think()
        {
            if (Busy) return;
            if (!HasPlayer) { state = State.Idle; return; }

            if (state == State.Idle && Notices(noticeRange, 4f)) Enter(State.Stalk);
            // A jump overhead (a stomp coming) must not end the fight: only real distance does.
            else if (state != State.Idle && (Mathf.Abs(DX) > noticeRange + 4f || Mathf.Abs(DY) > 8f)) Enter(State.Idle);
            if (state == State.Idle) return;

            if (!enraged && Enemy.CurrentHealth <= Enemy.MaxHealth * enrageAt)
            {
                RunSpecial(Enrage());
                return;
            }

            // Defence first: a stomp coming down on it, or a stomp that just missed beside it.
            bool stompComing = StompIncoming(0.55f, out _);
            if (Anticipates(stompComing, dodgeReaction, enraged ? enragedDodgeChance : dodgeChance, dodgeCooldown))
            {
                RunSpecial(Dodge());
                return;
            }
            // A stomp it reads will land beside it: it readies the counter in the air, timed to the landing.
            StompForecast f = Forecast;
            if (Time.time >= nextPunish && f.Near && f.Time <= 0.45f &&
                RollForThisJump(ref readMissJump, readMissChance + (enraged ? 0.15f : 0f)))
            {
                nextPunish = Time.time + dodgeCooldown;
                Trace("read missed stomp, landing " + f.LandX.ToString("0.0"));
                RunSpecial(Counter(f.Time + landingGrace));
                return;
            }
            if (Time.time >= nextPunish && PlayerMissedNearby(2.4f))
            {
                nextPunish = Time.time + dodgeCooldown;
                Trace("punish missed stomp");
                RunSpecial(Counter());
                return;
            }

            if (Time.time >= nextDecision) Decide();
        }

        private void Decide()
        {
            nextDecision = Time.time + Random.Range(decisionMin, decisionMax) * (enraged ? 0.8f : 1f);
            float d = Mathf.Abs(DX);
            bool level = SameLevel(1.6f);
            bool impatient = Time.time - lastAttack > stalkPatience;

            float engage = level && d <= dashReach ? 3f : 0f;
            float feint = level && !impatient && d >= 2f && d <= dashReach ? (enraged ? 1.5f : 1f) : 0f;
            float vault = level && PlayerGrounded && d <= 3.2f && Time.time >= nextVault && VaultTarget(out _)
                ? (cornered ? 4f : enraged ? 1.2f : 0.6f) : 0f;
            float pounce = enraged && level && d >= 3f && d <= 7f && Time.time >= nextPounce ? 1.5f : 0f;
            if (impatient) engage += 4f;
            if (cornered && d < stalkMin) engage += 2f;

            float total = engage + feint + vault + pounce;
            if (total <= 0f) return;
            float roll = Random.value * total;
            if ((roll -= engage) < 0f) RunSpecial(DashCombo());
            else if ((roll -= feint) < 0f) RunSpecial(Feint());
            else if ((roll -= vault) < 0f) RunSpecial(Vault());
            else RunSpecial(Pounce());
        }

        private void Enter(State next)
        {
            if (state == next) return;
            state = next;
            Trace("state " + next);
        }

        // ---------------------------------------------------------------- stalking movement

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked || state == State.Idle || !HasPlayer)
            {
                Enemy.Halt();
                if (HasPlayer && state != State.Idle) Enemy.Face(DirToPlayer);
                return;
            }

            // Drift in and out of the stalking band so the pressure never stands still.
            float wave = 0.5f + 0.5f * Mathf.Sin((Time.time + paceOffset) * 1.3f);
            float want = Mathf.Lerp(stalkMin, stalkMax, wave);
            float d = Mathf.Abs(DX);
            Enemy.Face(DirToPlayer);
            if (d > stalkMax + 1.5f)
            {
                cornered = false;
                Enemy.MoveHorizontal(DirToPlayer, runSpeed, true);
            }
            else if (d > want + 0.3f)
            {
                cornered = false;
                Enemy.MoveHorizontal(DirToPlayer, stalkSpeed);
            }
            else if (d < want - 0.3f)
            {
                // Back off facing the player; a ledge or wall behind means it is cornered.
                cornered = !Enemy.MoveHorizontal(-DirToPlayer, stalkSpeed, false, false);
            }
            else
            {
                Enemy.Halt();
            }
        }

        // ---------------------------------------------------------------- moves

        private IEnumerator DashCombo()
        {
            Enter(State.Dash);
            lastAttack = Time.time;
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            float windUp = Timing(dashWindUp);
            Warn(windUp);
            yield return new WaitForSeconds(windUp);

            Enemy.PlayState("Running");
            float distance = Mathf.Max(0f, Mathf.Abs(DX) - Enemy.AttackRange * 0.6f);
            yield return Travel(dir, dashSpeed, Mathf.Min(distance, dashReach));
            Enemy.Halt();
            if (!HasPlayer || Mathf.Abs(DX) > Enemy.AttackRange + 0.3f || Mathf.Abs(DY) > 2f)
            {
                // The player got away: a short breather, then back to stalking.
                Enemy.ReturnToLocomotion();
                Enter(State.Recover);
                yield return new WaitForSeconds(Timing(0.35f));
                Enter(State.Stalk);
                yield break;
            }

            Enter(State.Combo);
            comboCount++;
            bool heavy = heavyEvery > 0 && comboCount % heavyEvery == 0;
            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Slashing");
            Trace(heavy ? "combo (ends heavy)" : "combo");
            yield return new WaitForSeconds(0.2f);
            HitInFront(1.6f, 2.4f, 1, new Vector2(Enemy.Direction * 4f, 4f));
            yield return new WaitForSeconds(0.3f);

            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Run Slashing");
            if (RoomAhead(Enemy.Direction) > 0.8f) Enemy.Body.linearVelocity = new Vector2(Enemy.Direction * 5f, Enemy.Body.linearVelocity.y);
            yield return new WaitForSeconds(0.15f);
            Enemy.Halt();
            HitInFront(1.7f, 2.4f, 1, new Vector2(Enemy.Direction * 6f, 5f));

            float recover = comboRecovery;
            if (heavy)
            {
                Enter(State.Heavy);
                yield return new WaitForSeconds(0.2f);
                Enemy.Face(DirToPlayer);
                Bounds b = Enemy.BodyCollider.bounds;
                DangerZone(b.center.x, b.center.x + Enemy.Direction * (b.extents.x + 2.2f), 0.3f);
                Warn(heavyWindUp); // the big one keeps its full wind-up in both phases
                Enemy.PlayState("Slashing");
                Enemy.Anim.speed = 0.4f;
                yield return new WaitForSeconds(heavyWindUp);
                Enemy.Anim.speed = 1.4f;
                Trace("heavy cleave");
                HitInFront(2.2f, 2.6f, 2, new Vector2(Enemy.Direction * 9f, 7f));
                yield return new WaitForSeconds(0.2f);
                Enemy.Anim.speed = 1f;
                ClearTelegraphs();
                recover = heavyRecovery;
            }

            // Winded: the opening.
            Enter(State.Recover);
            lastAttack = Time.time;
            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Trace(enraged ? "recover (enraged)" : "recover");
            yield return new WaitForSeconds(Timing(recover));
            Enemy.ReturnToLocomotion();
            yield return Retreat();
        }

        /// <summary>Backs off to stalking distance, unless a ledge is behind it.</summary>
        private IEnumerator Retreat()
        {
            int away = -DirToPlayer;
            if (HasPlayer && RoomAhead(away) > 1f)
            {
                Enter(State.Retreat);
                yield return Travel(away, retreatSpeed, retreatDistance);
                Enemy.Halt();
            }
            if (HasPlayer) Enemy.Face(DirToPlayer);
            Enter(State.Stalk);
            nextDecision = Time.time + Random.Range(decisionMin, decisionMax);
        }

        private IEnumerator Feint()
        {
            Enter(State.Feint);
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            float windUp = Timing(dashWindUp);
            Warn(windUp); // looks exactly like the dash
            yield return new WaitForSeconds(windUp);
            Trace("feint");
            Enemy.PlayState("Running");
            yield return Travel(dir, dashSpeed, Mathf.Max(0f, Mathf.Abs(DX) - 2.3f));
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            yield return new WaitForSeconds(0.15f);
            int away = -DirToPlayer;
            if (RoomAhead(away) > 1f)
            {
                Enemy.PlayState("Sliding");
                yield return Travel(away, dodgeSpeed * 0.8f, 1.6f);
                Enemy.Halt();
                Enemy.ReturnToLocomotion();
            }
            Enemy.Face(DirToPlayer);
            Enter(State.Stalk);
        }

        /// <summary>Where a vault over the player would land: behind the player, on this platform.</summary>
        private bool VaultTarget(out float x)
        {
            x = 0f;
            if (!HasPlayer || !Enemy.TryGetPlatformLimits(out float min, out float max)) return false;
            int dir = DirToPlayer;
            x = Mathf.Clamp(PlayerTransform.position.x + dir * 1.8f, min, max);
            return (x - PlayerTransform.position.x) * dir >= 1f; // really on the far side
        }

        private IEnumerator Vault()
        {
            if (!VaultTarget(out float x)) yield break;
            Enter(State.Vault);
            nextVault = Time.time + vaultCooldown;
            lastAttack = Time.time;
            Trace("vault");
            yield return Leap(x, 0.2f);
            Enemy.Face(DirToPlayer);
            yield return new WaitForSeconds(Timing(0.35f));
            Enter(State.Stalk);
            nextDecision = Time.time + 0.3f;
        }

        private IEnumerator Pounce()
        {
            if (!HasPlayer || !Enemy.TryGetPlatformLimits(out float min, out float max)) yield break;
            Enter(State.Pounce);
            nextPounce = Time.time + pounceCooldown;
            lastAttack = Time.time;
            float x = Mathf.Clamp(PlayerTransform.position.x, min, max);
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            DangerZone(x - pounceRadius, x + pounceRadius, 0.3f); // stays up until it lands
            Warn(pounceWindUp);
            Trace("pounce windup");
            yield return new WaitForSeconds(pounceWindUp);
            yield return Leap(x, 0f);
            Trace("pounce slam");
            var center = new Vector2(x, GroundY + 0.75f);
            int dir = DirToPlayer;
            HitBox(center, new Vector2(pounceRadius * 2f, 1.5f), 1, new Vector2(dir * 6f, 6f), groundedOnly: true);
            ClearTelegraphs();
            Enemy.PlayState("Idle Blinking");
            Enter(State.Recover);
            yield return new WaitForSeconds(Timing(0.8f)); // the opening after the landing
            Enemy.ReturnToLocomotion();
            yield return Retreat();
        }

        /// <summary>
        /// A ballistic jump onto <paramref name="targetX"/> on its own platform, high enough to clear the
        /// player's head (plus <paramref name="extraHeight"/>).
        /// </summary>
        private IEnumerator Leap(float targetX, float extraHeight)
        {
            Rigidbody2D body = Enemy.Body;
            float g = Mathf.Max(0.01f, -Physics2D.gravity.y * body.gravityScale);
            float feet = Enemy.BodyCollider.bounds.min.y;
            float clear = HasPlayer ? PlayerTransform.position.y + 1.4f - feet : 2.5f;
            float height = Mathf.Clamp(clear + extraHeight, 1.8f, 4f);
            float vy = Mathf.Sqrt(2f * g * height);
            float flight = 2f * vy / g;
            float dx = targetX - body.position.x;
            Enemy.Face(dx >= 0f ? 1 : -1);
            Enemy.PlayState("Jump Start");
            body.linearVelocity = new Vector2(dx / flight, vy);
            yield return new WaitForSeconds(0.1f);
            Enemy.PlayState("Jump Loop");
            float t = 0.1f;
            while (t < flight + 0.6f && !Enemy.IsDead)
            {
                if (t >= flight * 0.6f && body.linearVelocity.y > -0.05f) break; // landed
                // Keep the planned sideways speed (contacts may slow it), but stop once over the target.
                if (Mathf.Abs(targetX - body.position.x) > 0.1f) body.linearVelocity = new Vector2(dx / flight, body.linearVelocity.y);
                else body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
        }

        private IEnumerator Dodge()
        {
            Enter(State.Dodge);
            // Slide away from where it reads the stomp will land; under the player in phase 2 if there is room.
            int away = AwayFromLanding;
            if (enraged && RoomAhead(-away) > 3f && Random.value < 0.5f) away = -away;
            if (RoomAhead(away) < 1f) away = -away;
            if (RoomAhead(away) < 1f) { Enter(State.Stalk); yield break; }
            Trace("dodge slide");
            Enemy.PlayState("Sliding");
            yield return Travel(away, dodgeSpeed, dodgeDistance);
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            if (HasPlayer && Mathf.Abs(DX) <= 3f && Mathf.Abs(DY) <= 2.5f)
            {
                yield return Counter();
            }
            else
            {
                Enemy.Face(DirToPlayer);
                Enter(State.Stalk);
            }
        }

        /// <summary>
        /// A telegraphed punish slash; it steps in first when the player is just out of reach. A counter
        /// readied for a landing waits at least <paramref name="minWindUp"/> (the warning shows throughout).
        /// </summary>
        private IEnumerator Counter(float minWindUp = 0f)
        {
            Enter(State.Counter);
            lastAttack = Time.time;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            float windUp = Mathf.Max(Timing(counterWindUp), minWindUp);
            Warn(windUp);
            Enemy.PlayState("Slashing");
            Enemy.Anim.speed = 0.5f;
            yield return new WaitForSeconds(windUp);
            Enemy.Anim.speed = 1.3f;
            if (HasPlayer && Mathf.Abs(DX) > 1.6f && RoomAhead(Enemy.Direction) > 0.8f)
            {
                Enemy.Body.linearVelocity = new Vector2(Enemy.Direction * 6f, Enemy.Body.linearVelocity.y);
                yield return new WaitForSeconds(0.12f);
                Enemy.Halt();
            }
            Trace("counter slash");
            HitInFront(1.7f, 2.4f, 1, new Vector2(Enemy.Direction * 5f, 5f));
            yield return new WaitForSeconds(0.15f);
            Enemy.Anim.speed = 1f;
            Enemy.ReturnToLocomotion();
            Enter(State.Recover);
            yield return new WaitForSeconds(Timing(0.5f));
            Enter(State.Stalk);
            nextDecision = Time.time + Random.Range(decisionMin, decisionMax);
        }

        private IEnumerator Enrage()
        {
            Enter(State.Enrage);
            enraged = true;
            Enemy.Halt();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            Trace("phase 2");
            Warn(roarTime);
            Enemy.PlayState("Idle Blinking");
            yield return new WaitForSeconds(roarTime);
            Enemy.ReturnToLocomotion();
            Enter(State.Stalk);
            nextDecision = Time.time + 0.3f;
            nextPounce = Time.time + 1f;
        }
    }
}
