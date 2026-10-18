using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Bloody Alchemist 1: a cautious ranged enemy. It notices the player from 9 units (and a little
    /// above or below), keeps 3.5-6.5 units away - backing off when approached, stepping closer when
    /// the player is far - and stands its ground with its back to a ledge.
    /// Signature: Acid Flask. Wind-up 0.5 s (Throwing, warning shown), then a lobbed flask aimed where
    /// the player stands. A direct hit deals 1 damage; where it lands it leaves an acid pool (a spike
    /// strip) for 2.5 s. At most two flasks in the air and one pool per alchemist; cooldown 2.8 s.
    /// The flask is slow and arcs, so it can be outrun or jumped over.
    /// </summary>
    public class AlchemistBehaviour : EnemyBehaviour
    {
        [SerializeField] private float noticeRange = 9f;
        [SerializeField] private float tooClose = 3.5f;
        [SerializeField] private float tooFar = 6.5f;
        [SerializeField] private float retreatSpeed = 2f;
        [SerializeField] private float approachSpeed = 1.4f;
        [SerializeField] private float throwWindUp = 0.5f;
        [SerializeField] private float throwCooldown = 2.8f;
        [SerializeField] private float flightTime = 0.85f;
        [SerializeField] private float poolLifetime = 2.5f;

        private bool engaged;
        private float nextThrow;
        private int flasksInAir;
        private GameObject pool;

        public override void Think()
        {
            if (Busy) return;
            if (!engaged && Notices(noticeRange, 3f)) engaged = true;
            else if (engaged && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 4f)) engaged = false;

            if (engaged && Time.time >= nextThrow && flasksInAir < 2 && Mathf.Abs(DX) >= 1.5f)
            {
                RunSpecial(Throw());
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }
            if (!engaged) { Enemy.PatrolStep(); return; }

            float distance = Mathf.Abs(DX);
            if (distance < tooClose)
            {
                // Back away; with a ledge behind it, it holds its ground facing the player.
                if (!Enemy.MoveHorizontal(-DirToPlayer, retreatSpeed, true)) Enemy.Face(DirToPlayer);
            }
            else if (distance > tooFar)
            {
                Enemy.MoveHorizontal(DirToPlayer, approachSpeed);
            }
            else
            {
                Enemy.Halt();
                Enemy.Face(DirToPlayer);
            }
        }

        private IEnumerator Throw()
        {
            nextThrow = Time.time + throwCooldown;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Throwing");
            Warn(throwWindUp);
            yield return new WaitForSeconds(throwWindUp);
            if (!HasPlayer) { Enemy.ReturnToLocomotion(); yield break; }

            Bounds b = Enemy.BodyCollider.bounds;
            Vector2 from = new Vector2(b.center.x + Enemy.Direction * (b.extents.x + 0.2f), b.max.y - 0.3f);
            Vector2 target = new Vector2(PlayerTransform.position.x, PlayerTransform.position.y - 0.5f);
            float g = -Physics2D.gravity.y;
            Vector2 velocity = new Vector2((target.x - from.x) / flightTime,
                (target.y - from.y + 0.5f * g * flightTime * flightTime) / flightTime);

            AlchemistFlask.Launch(this, from, velocity, Square);
            Trace("throw");
            flasksInAir++;
            yield return new WaitForSeconds(0.35f);
            Enemy.ReturnToLocomotion();
        }

        /// <summary>Called by a flask that struck the player.</summary>
        public void FlaskHitPlayer() => Trace("flask hit player for 1");

        /// <summary>Called by a flask when it is gone; lands an acid pool when it hit the ground.</summary>
        public void FlaskFinished(Vector2? groundHit, Collider2D ground)
        {
            flasksInAir = Mathf.Max(0, flasksInAir - 1);
            if (!groundHit.HasValue || Enemy.IsDead || ground == null) return;
            if (pool != null) Destroy(pool);
            pool = AlchemistFlask.MakePool(groundHit.Value.x, ground, poolLifetime);
            Trace("acid pool at " + groundHit.Value.x.ToString("0.0"));
        }

        public override void OnDeath()
        {
            base.OnDeath();
            if (pool != null) Destroy(pool);
        }
    }
}
