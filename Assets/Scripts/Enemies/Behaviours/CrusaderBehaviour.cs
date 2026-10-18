using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Skeleton Crusader 1: a slow, heavily armoured guardian that holds a post (where it was placed).
    /// While it holds its ground its shield is up (stone bar): stomps glance off. It never strays far
    /// and walks back to its post afterwards.
    /// Signature: Shield Charge. When the player is 2.2-6 units away on its floor: a 0.8 s wind-up with a
    /// warning and the charge lane marked on the ground, then a charge of up to 4 units (stops at
    /// ledges) that hits for 1 and knocks the player back. The shield is down during the charge and the
    /// 1.4 s recovery after it - the window to stomp. Cooldown 4 s. Not interrupted by stomps.
    /// </summary>
    public class CrusaderBehaviour : EnemyBehaviour
    {
        [SerializeField] private float walkSpeed = 1f;
        [SerializeField] private float watchRange = 7f;
        [SerializeField] private float chargeMin = 2.2f;
        [SerializeField] private float chargeMax = 6f;
        [SerializeField] private float chargeWindUp = 0.8f;
        [SerializeField] private float chargeSpeed = 6.5f;
        [SerializeField] private float chargeDistance = 4f;
        [SerializeField] private float recovery = 1.4f;
        [SerializeField] private float cooldown = 4f;
        [SerializeField] private float leash = 2.5f;

        private float post;
        private float nextCharge;
        private bool shieldUp = true;
        private GameObject shield;

        protected override bool HasSuperArmor => true;

        protected override void Start()
        {
            base.Start();
            post = transform.position.x;
            SetShield(true);
        }

        public override bool TryBlock(int damage)
        {
            if (shieldUp) Trace("blocked stomp");
            return shieldUp;
        }

        public override void Think()
        {
            if (Busy) return;
            float distance = Mathf.Abs(DX);
            if (HasPlayer && Time.time >= nextCharge && distance >= chargeMin && distance <= chargeMax && SameLevel() && RoomAhead(DirToPlayer) > 1f)
            {
                RunSpecial(Charge());
            }
            else if (HasPlayer && distance <= Enemy.AttackRange && SameLevel() && Enemy.CanBasicAttack)
            {
                Enemy.Face(DirToPlayer);
                Enemy.BasicAttack();
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }

            float fromPost = transform.position.x - post;
            bool watching = Notices(watchRange, 2.5f);
            if (Mathf.Abs(fromPost) > 0.3f && (!watching || Mathf.Abs(fromPost) > leash))
            {
                Enemy.MoveHorizontal(fromPost > 0f ? -1 : 1, walkSpeed); // back to the post
            }
            else
            {
                Enemy.Halt();
                if (watching) Enemy.Face(DirToPlayer);
            }
        }

        private IEnumerator Charge()
        {
            nextCharge = Time.time + cooldown;
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            float lane = Mathf.Min(chargeDistance, RoomAhead(dir));
            float front = Enemy.BodyCollider.bounds.center.x;
            Warn(chargeWindUp);
            DangerZone(front, front + dir * (lane + Enemy.BodyCollider.bounds.extents.x), 0.25f);
            Enemy.PlayState("Idle Blinking");
            yield return new WaitForSeconds(chargeWindUp);

            SetShield(false);
            Enemy.PlayState("Run Slashing");
            Trace("charge");
            float travelled = 0f;
            bool hit = false;
            while (travelled < lane && !Enemy.IsDead)
            {
                if (!Enemy.MoveHorizontal(dir, chargeSpeed, true)) break;
                travelled += chargeSpeed * Time.fixedDeltaTime;
                if (!hit && HitInFront(0.5f, 1.8f, 1, new Vector2(dir * 8f, 5f))) hit = true;
                yield return new WaitForFixedUpdate();
            }
            Enemy.Halt();
            ClearTelegraphs();
            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Trace("recover");
            yield return new WaitForSeconds(recovery);
            Enemy.ReturnToLocomotion();
            SetShield(true);
        }

        private void SetShield(bool up)
        {
            shieldUp = up;
            if (up && shield == null) shield = ShieldMarker();
            else if (!up && shield != null) { Destroy(shield); shield = null; }
        }

        public override void OnDeath()
        {
            SetShield(false);
            base.OnDeath();
        }
    }
}
