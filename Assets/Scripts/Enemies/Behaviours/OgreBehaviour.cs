using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Ogre: a slow, powerful brute that pressures the player with its size and reach. It lumbers
    /// toward the player and shrugs off stomps without flinching (it still takes the damage).
    /// Signature: Ground Slam. When the player is within 2.8 units on its floor: a long 0.9 s wind-up
    /// (slowed swing, warning, and the danger zone - 3 units in front - shown on the ground), then the
    /// slam. Anyone standing in the zone on the ground takes 2 damage and is thrown back; being in the
    /// air or outside the zone avoids it. A 1.3 s recovery follows (the opening). Cooldown 3.5 s.
    /// </summary>
    public class OgreBehaviour : EnemyBehaviour
    {
        [SerializeField] protected float walkSpeed = 1f;
        [SerializeField] protected float noticeRange = 7f;
        [SerializeField] protected float slamRange = 2.8f;
        [SerializeField] protected float slamReach = 3f;
        [SerializeField] protected float windUp = 0.9f;
        [SerializeField] protected float recovery = 1.3f;
        [SerializeField] protected float cooldown = 3.5f;
        [SerializeField] protected int slamDamage = 2;

        protected bool engaged;
        protected float nextSlam;

        protected override bool HasSuperArmor => true;

        public override void OnEncounterReset()
        {
            base.OnEncounterReset();
            engaged = false;
        }

        public override void Think()
        {
            if (Busy) return;
            if (!engaged && Notices(noticeRange)) engaged = true;
            else if (engaged && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 3f)) engaged = false;

            if (engaged && Time.time >= nextSlam && Mathf.Abs(DX) <= slamRange && SameLevel(2f))
            {
                RunSpecial(Slam());
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }
            if (!engaged) { Enemy.PatrolStep(); return; }
            if (Mathf.Abs(DX) > slamRange * 0.6f) Enemy.MoveHorizontal(DirToPlayer, walkSpeed);
            else { Enemy.Halt(); Enemy.Face(DirToPlayer); }
        }

        protected virtual float SlamCooldown => cooldown;

        protected IEnumerator Slam()
        {
            nextSlam = Time.time + SlamCooldown;
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            Bounds b = Enemy.BodyCollider.bounds;
            float near = b.center.x;
            float far = near + dir * (b.extents.x + slamReach);
            DangerZone(near, far, 0.3f);
            Warn(windUp);
            Enemy.PlayState("Slashing");
            Enemy.Anim.speed = 0.35f; // a slow, heavy swing
            yield return new WaitForSeconds(windUp);

            Enemy.Anim.speed = 1.6f;
            Trace("slam");
            float left = Mathf.Min(near, far), right = Mathf.Max(near, far);
            var center = new Vector2((left + right) / 2f, GroundY + 0.75f);
            HitBox(center, new Vector2(right - left, 1.5f), slamDamage, new Vector2(dir * 7f, 6f), groundedOnly: true);
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySwordHit();
            yield return new WaitForSeconds(0.2f);
            ClearTelegraphs();
            Enemy.Anim.speed = 1f;
            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Trace("recover");
            yield return new WaitForSeconds(recovery);
            Enemy.ReturnToLocomotion();
        }
    }
}
