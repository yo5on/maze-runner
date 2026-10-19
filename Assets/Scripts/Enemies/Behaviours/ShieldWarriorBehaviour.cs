using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Skeleton Warrior 2: a disciplined guard. It patrols; when the player comes within 5 units on its
    /// floor it stops, faces the player and alternates two stances:
    /// - Guard (1.8-2.6 s, its kite shield raised in front): stomps landing on its front are blocked
    ///   (the player bounces off unharmed) and answered with a quick counter-slash (0.3 s wind-up),
    ///   which leaves it in the opening. While guarding it is slow to turn (guardTurnDelay), so a
    ///   stomp on its back - the rear of the body - gets past the shield.
    /// - Opening (1.2 s, shield lowered): it can be stomped, and slashes if the player stands in reach.
    /// The player's only attack is the stomp, so "frontal attacks" are stomps landing on the guard;
    /// waiting for the opening, or getting behind it, is the counter-play.
    /// </summary>
    public class ShieldWarriorBehaviour : EnemyBehaviour
    {
        [SerializeField] private float engageRange = 5f;
        [SerializeField] private float disengageRange = 7.5f;
        [SerializeField] private float guardMin = 1.8f;
        [SerializeField] private float guardMax = 2.6f;
        [SerializeField] private float openingTime = 1.2f;
        [SerializeField] private float counterWindUp = 0.3f;
        [SerializeField] private float counterReach = 1.3f;
        [Tooltip("While guarding, how long the player must stay behind it before it turns around.")]
        [SerializeField] private float guardTurnDelay = 0.5f;
        [Tooltip("Shield art held up while guarding (Assets/Sprites/Weapons/Shields).")]
        [SerializeField] private Sprite shieldSprite;

        private enum Stance { Patrol, Guard, Opening }
        private Stance stance = Stance.Patrol;
        private float stanceTimer;
        private float lostTimer;
        private HeldShield shield;

        protected override void Start()
        {
            base.Start();
            shield = HoldShield(shieldSprite); // carried lowered while patrolling
        }

        public override bool TryBlock(int damage)
        {
            if (stance != Stance.Guard || Busy) return false;
            if (!PlayerInFront()) { Trace("stomp landed behind the shield"); return false; }
            Trace("blocked stomp");
            ShieldBlocked(shield != null ? shield.Visual : null);
            if (HasPlayer && Mathf.Abs(DX) <= counterReach + 0.8f) RunSpecial(Counter());
            return true;
        }

        public override void Think()
        {
            if (Busy) return;
            // Stays engaged through a stomp bounce or a short hop; only leaves once the player has been
            // clearly gone for a moment.
            bool inRange = HasPlayer && Mathf.Abs(DX) <= (stance == Stance.Patrol ? engageRange : disengageRange) && Mathf.Abs(DY) <= 5f;
            lostTimer = inRange ? 0f : lostTimer + Time.deltaTime;
            if (stance == Stance.Patrol && !inRange) return;
            if (stance != Stance.Patrol && lostTimer > 1.5f)
            {
                SetStance(Stance.Patrol);
                return;
            }

            stanceTimer -= Time.deltaTime;
            if (stance == Stance.Patrol || stanceTimer <= 0f)
            {
                SetStance(stance == Stance.Guard ? Stance.Opening : Stance.Guard);
            }

            if (stance == Stance.Opening && SameLevel() && Enemy.CanBasicAttack)
            {
                Enemy.Face(DirToPlayer);
                Enemy.BasicAttack();
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (stance == Stance.Patrol) { Enemy.PatrolStep(); return; }
            Enemy.Halt();
            if (Enemy.IsAttackLocked) return;
            if (stance == Stance.Guard) FacePlayerAfter(guardTurnDelay);
            else Enemy.Face(DirToPlayer);
        }

        private void SetStance(Stance next)
        {
            if (stance == next && stanceTimer > 0f) return;
            stance = next;
            Trace("stance " + next);
            stanceTimer = next == Stance.Guard ? Random.Range(guardMin, guardMax) : openingTime;
            bool raised = next == Stance.Guard;
            if (shield != null) shield.Raised = raised;
            if (next != Stance.Patrol) Enemy.PlayState(raised ? "Idle" : "Idle Blinking");
        }

        private IEnumerator Counter()
        {
            Enemy.Face(DirToPlayer);
            Warn(counterWindUp);
            yield return new WaitForSeconds(counterWindUp);
            Enemy.PlayState("Slashing");
            Trace("counter");
            yield return new WaitForSeconds(0.12f);
            HitInFront(counterReach, 1.8f, 1, new Vector2(Enemy.Direction * 5f, 4f));
            yield return new WaitForSeconds(0.4f);
            Enemy.ReturnToLocomotion();
            SetStance(Stance.Opening); // a counter leaves it open for a moment
        }

        public override void OnDeath()
        {
            if (shield != null) shield.Raised = false; // stays in hand through the death animation
            base.OnDeath();
        }
    }
}
