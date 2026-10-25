using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Ogre Warlord (Level 1 boss): a bigger, tougher Ogre that teaches boss fights with two slow,
    /// clearly marked attacks and long openings after each.
    /// - Ground Slam (the Ogre's own): close up, 0.9 s wind-up with the ground in front marked.
    /// - Bull Charge: when the player keeps 3.5-9 units away on its floor, it plants its feet, marks the
    ///   whole run on the ground and shows the warning (0.9 s), then charges to where the player stood.
    ///   Running into it costs 1 heart; jumping over it or stepping out of the lane avoids it. It stops
    ///   at the platform edge, never past it, and stands dazed for 1.5 s - the best moment to stomp it.
    /// - Defensive footing: it sometimes reads a stomp coming (slow: 0.3 s to react, 30 %, plus what it
    ///   learns from repeated stomps) and lumbers a step clear of the landing, then answers with its
    ///   marked slam if the player lands in reach. Never during its own recoveries - those stay open.
    /// - A player who keeps coming from the same side finds it already turned that way.
    /// At half health it presses harder (shorter cooldowns), but its wind-ups never get shorter.
    /// It never flinches, and it resists knockback while charging or slamming.
    /// </summary>
    public class OgreWarlordBehaviour : OgreBehaviour
    {
        [Header("Reading stomps")]
        [SerializeField] private float sidestepReaction = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float sidestepChance = 0.3f;
        [SerializeField] private float sidestepCooldown = 4f;
        [SerializeField] private float sidestepSpeed = 3.5f;
        [SerializeField] private float sidestepDistance = 1.4f;

        [SerializeField] private float chargeMin = 3.5f;
        [SerializeField] private float chargeMax = 9f;
        [SerializeField] private float chargeWindUp = 0.9f;
        [SerializeField] private float chargeSpeed = 7f;
        [SerializeField] private float chargeCooldown = 5f;
        [SerializeField] private float chargeRecovery = 1.5f;
        [SerializeField] private int chargeDamage = 1;
        [Range(0f, 1f)] [SerializeField] private float enrageAt = 0.5f;
        [Tooltip("Cooldowns are multiplied by this below enrageAt health.")]
        [SerializeField] private float enragedCooldownScale = 0.7f;

        private float nextCharge;

        private bool Enraged => Enemy.CurrentHealth <= Enemy.MaxHealth * enrageAt;
        protected override float SlamCooldown => Enraged ? cooldown * enragedCooldownScale : cooldown;
        public override bool ResistsKnockback => Busy;

        public override void Think()
        {
            // Not gated on engagement: it drops while the player is high overhead, mid-stomp.
            if (!Busy && HasPlayer && Mathf.Abs(DX) < noticeRange && Anticipates(StompIncoming(0.7f, out _), sidestepReaction, sidestepChance, sidestepCooldown))
            {
                int away = AwayFromLanding;
                if (RoomAhead(away) < 1f) away = -away;
                if (RoomAhead(away) >= 1f) { RunSpecial(Sidestep(away)); return; }
            }
            base.Think(); // engagement and the slam
            if (Busy || !engaged || Time.time < nextCharge) return;
            float d = Mathf.Abs(DX);
            if (d >= chargeMin && d <= chargeMax && SameLevel(2f) && RoomAhead(DirToPlayer) > 1.5f)
            {
                RunSpecial(Charge());
            }
        }

        /// <summary>Stuck: it answers with its marked slam up close or its charge from range; otherwise it walks to open ground.</summary>
        protected override void RecoverFromStuck(StuckKind kind)
        {
            if (kind != StuckKind.Perched && HasPlayer && SameLevel(2f))
            {
                engaged = true;
                float d = Mathf.Abs(DX);
                if (d <= slamRange) { RunSpecial(Slam()); return; }
                if (d >= chargeMin && d <= chargeMax && RoomAhead(DirToPlayer) > 1.5f) { RunSpecial(Charge()); return; }
            }
            base.RecoverFromStuck(kind);
        }

        public override void Act()
        {
            // Waiting for the player: turned toward the side it keeps coming from.
            int expected = ExpectedApproachSide;
            if (!Busy && !engaged && expected != 0 && !Enemy.IsAttackLocked)
            {
                Enemy.Halt();
                Enemy.Face(expected);
                return;
            }
            base.Act();
        }

        private IEnumerator Sidestep(int dir)
        {
            Trace("sidestep");
            yield return Travel(dir, sidestepSpeed, sidestepDistance);
            Enemy.Halt();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            yield return new WaitForSeconds(0.15f);
            // The stomp came down beside it: the marked slam answers if the player landed in reach.
            if (HasPlayer && Time.time >= nextSlam && Mathf.Abs(DX) <= slamRange && SameLevel(2f)) yield return Slam();
        }

        private IEnumerator Charge()
        {
            nextCharge = Time.time + (Enraged ? chargeCooldown * enragedCooldownScale : chargeCooldown);
            nextSlam = Mathf.Max(nextSlam, Time.time + 1f);
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);

            // The lane: from its front to just past where the player stands, cut at the platform edge.
            Bounds b = Enemy.BodyCollider.bounds;
            float front = b.center.x + dir * b.extents.x;
            float stop = PlayerTransform.position.x + dir * 0.8f;
            if (Enemy.TryGetPlatformLimits(out float min, out float max)) stop = Mathf.Clamp(stop, min, max);
            DangerZone(front, stop + dir * b.extents.x, 0.3f);
            Warn(chargeWindUp);
            Trace("charge windup");
            Enemy.PlayState("Idle Blinking");
            yield return new WaitForSeconds(chargeWindUp);

            Trace("charge");
            Enemy.PlayState("Running");
            bool hit = false;
            float limit = Mathf.Abs(stop - transform.position.x) / chargeSpeed + 0.5f;
            for (float t = 0f; t < limit && (stop - transform.position.x) * dir > 0.05f; t += Time.fixedDeltaTime)
            {
                if (!Enemy.MoveHorizontal(dir, chargeSpeed, true)) break; // the edge or a wall
                if (!hit) hit = HitInFront(0.5f, 2.2f, chargeDamage, new Vector2(dir * 8f, 6f));
                yield return new WaitForFixedUpdate();
            }
            Enemy.Halt();
            ClearTelegraphs();

            // Dazed: the opening.
            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Trace("dazed");
            yield return new WaitForSeconds(chargeRecovery);
            Enemy.ReturnToLocomotion();
            if (HasPlayer) Enemy.Face(DirToPlayer);
        }
    }
}
