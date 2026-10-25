using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// The Plague Alchemist (Level 2 boss): the Bloody Alchemist's kit, played tactically.
    /// - Keeps its distance like the regular alchemist and throws potions, but as a volley (3, then 4
    ///   below half health) landing on and around the player, so the arena fills with short-lived pools
    ///   (up to 4) to weave between. Each throw keeps the 0.5 s wind-up and warning.
    /// - Knife Stab when the player closes in (the alchemist's own), so rushing it is not free either.
    /// - Evasion: it reads a stomp coming (0.2 s to react) and about half the time (more against
    ///   repeated stomps) slides clear of the predicted landing, then throws sooner - at most every 3 s,
    ///   so pressing the attack pays off.
    /// - Hop clear: backed against the arena edge with the player within 2.6 units, it crouches (warning)
    ///   and leaps over the player to open ground, then throws sooner - at most every 5 s. It manages its
    ///   distance instead of cowering in a corner.
    /// - Phase 2 at half health: a pause with the warning shown, then one more potion per volley and
    ///   shorter cooldowns.
    /// </summary>
    public class AlchemistBossBehaviour : AlchemistBehaviour
    {
        [Header("Boss")]
        [SerializeField] private float evadeReaction = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float evadeChance = 0.5f;
        [SerializeField] private float evadeCooldown = 3f;
        [SerializeField] private float evadeSpeed = 7f;
        [SerializeField] private float evadeDistance = 2.2f;
        [Range(0f, 1f)] [SerializeField] private float enrageAt = 0.5f;
        [SerializeField] private float roarTime = 0.8f;
        [SerializeField] private int enragedVolleyBonus = 1;
        [SerializeField] private float enragedCooldownScale = 0.75f;
        [Header("Hop clear (cornered)")]
        [Tooltip("Backed against the arena edge with the player this close, it leaps over the player to open ground.")]
        [SerializeField] private float hopTrigger = 2.6f;
        [SerializeField] private float hopWindUp = 0.35f;
        [SerializeField] private float hopCooldown = 5f;

        private bool enraged;
        private float nextHop;

        protected override int Volley => base.Volley + (enraged ? enragedVolleyBonus : 0);
        protected override float ThrowCooldown => enraged ? throwCooldown * enragedCooldownScale : throwCooldown;

        public override void OnEncounterReset()
        {
            base.OnEncounterReset();
            enraged = false;
            nextHop = 0f;
        }

        /// <summary>Stuck: out of a corner over the player if it can, otherwise to open ground with a throw ready.</summary>
        protected override void RecoverFromStuck(StuckKind kind)
        {
            nextThrow = Mathf.Min(nextThrow, Time.time + 0.3f);
            if (kind == StuckKind.Cornered && HopTarget(out float x)) RunSpecial(HopClear(x));
            else base.RecoverFromStuck(kind);
        }

        /// <summary>Where a hop over the player lands: beyond them at its preferred distance, on this platform.</summary>
        private bool HopTarget(out float x)
        {
            x = transform.position.x;
            if (!HasPlayer || !Enemy.TryGetPlatformLimits(out float min, out float max)) return false;
            int dir = DirToPlayer;
            x = Mathf.Clamp(PlayerTransform.position.x + dir * tooClose, min, max);
            return (x - PlayerTransform.position.x) * dir >= 2.5f;
        }

        /// <summary>A marked crouch, then a leap over the player to open ground; the next throw comes sooner.</summary>
        private IEnumerator HopClear(float x)
        {
            nextHop = Time.time + (enraged ? hopCooldown * enragedCooldownScale : hopCooldown);
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Warn(hopWindUp);
            Enemy.PlayState("Idle Blinking");
            Trace("hop clear windup");
            yield return new WaitForSeconds(hopWindUp);
            Trace("hop clear");
            yield return Leap(x, 0.3f);
            if (HasPlayer) Enemy.Face(DirToPlayer);
            nextThrow = Mathf.Min(nextThrow, Time.time + 0.5f);
            yield return new WaitForSeconds(0.2f);
        }

        public override void Think()
        {
            if (Busy) return;
            if (engaged && !enraged && Enemy.CurrentHealth <= Enemy.MaxHealth * enrageAt)
            {
                RunSpecial(Enrage());
                return;
            }
            // Not gated on 'engaged': the regular alchemist's engagement drops while the player is high overhead.
            if (HasPlayer && Mathf.Abs(DX) < 4f && Anticipates(StompIncoming(0.55f, out _), evadeReaction, evadeChance, evadeCooldown))
            {
                int away = AwayFromLanding;
                if (RoomAhead(away) < 1f) away = -away;
                if (RoomAhead(away) >= 1f) { RunSpecial(Evade(away)); return; }
            }
            // Backed against the edge with the player closing in: instead of cowering there, it hops clear.
            if (engaged && Time.time >= nextHop && Mathf.Abs(DX) <= hopTrigger && SameLevel() &&
                RoomAhead(-DirToPlayer) < 1.2f && HopTarget(out float hopX))
            {
                RunSpecial(HopClear(hopX));
                return;
            }
            base.Think();
        }

        private IEnumerator Evade(int dir)
        {
            Trace("evade");
            Enemy.PlayState("Sliding");
            yield return Travel(dir, evadeSpeed, evadeDistance);
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            nextThrow = Mathf.Min(nextThrow, Time.time + 0.4f); // answers with a throw (wind-up still shown)
            yield return new WaitForSeconds(0.15f);
        }

        private IEnumerator Enrage()
        {
            enraged = true;
            Enemy.Halt();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            Trace("phase 2");
            Warn(roarTime);
            Enemy.PlayState("Idle Blinking");
            yield return new WaitForSeconds(roarTime);
            Enemy.ReturnToLocomotion();
            nextThrow = Time.time + 0.3f;
        }
    }
}
