using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Skeleton Warrior 2: a disciplined guard. It patrols; when the player comes within 5 units on its
    /// floor it stops, faces the player and alternates two stances:
    /// - Guard (1.8-2.6 s, a stone shield bar shows in front): stomps are blocked (the player bounces
    ///   off unharmed). A blocked stomp is answered with a quick counter-slash (0.3 s wind-up).
    /// - Opening (1.2 s, shield lowered): it can be stomped, and slashes if the player stands in reach.
    /// The player's only attack is the stomp, so "frontal attacks" are stomps landing on the guard;
    /// waiting for the opening is the counter-play.
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

        private enum Stance { Patrol, Guard, Opening }
        private Stance stance = Stance.Patrol;
        private float stanceTimer;
        private float lostTimer;
        private GameObject shield;

        public override bool TryBlock(int damage)
        {
            if (stance != Stance.Guard || Busy) return false;
            Trace("blocked stomp");
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
            if (!Enemy.IsAttackLocked) Enemy.Face(DirToPlayer);
        }

        private void SetStance(Stance next)
        {
            if (stance == next && stanceTimer > 0f) return;
            stance = next;
            Trace("stance " + next);
            stanceTimer = next == Stance.Guard ? Random.Range(guardMin, guardMax) : openingTime;
            bool raised = next == Stance.Guard;
            if (raised && shield == null) shield = ShieldMarker();
            else if (!raised && shield != null) { Destroy(shield); shield = null; }
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
            if (shield != null) Destroy(shield);
            base.OnDeath();
        }
    }
}
