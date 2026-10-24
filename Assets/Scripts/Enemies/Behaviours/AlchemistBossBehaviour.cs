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

        private bool enraged;

        protected override int Volley => base.Volley + (enraged ? enragedVolleyBonus : 0);
        protected override float ThrowCooldown => enraged ? throwCooldown * enragedCooldownScale : throwCooldown;

        public override void OnEncounterReset()
        {
            base.OnEncounterReset();
            enraged = false;
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
