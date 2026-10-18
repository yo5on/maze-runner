using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Regular Goblin: a cunning, mobile patroller. It keeps the original goblin patrol (paired platforms,
    /// turning at ledges) and the original slash, and adds two tricks:
    /// - Evasive Slide: when the player is right above it and falling (a stomp coming), it slides about
    ///   two units out of the way (Sliding). Cooldown 3 s, so a second, well-timed stomp lands.
    /// - Flanking Dash: when the player is 2-4.5 units away on its floor and moving away from it (an
    ///   exposed back), it dashes in and slashes. Cooldown 3.5 s.
    /// Both moves stop at ledges.
    /// </summary>
    public class GoblinSkirmisherBehaviour : EnemyBehaviour
    {
        [SerializeField] private float slideSpeed = 7f;
        [SerializeField] private float slideDistance = 2.2f;
        [SerializeField] private float slideCooldown = 3f;
        [SerializeField] private float dashSpeed = 6f;
        [SerializeField] private float dashMin = 2f;
        [SerializeField] private float dashMax = 4.5f;
        [SerializeField] private float dashCooldown = 3.5f;

        private float nextSlide;
        private float nextDash;

        // Its own patrol and slash stay in charge except while a trick is running.
        public override bool ControlsMovement => Busy;
        public override bool ControlsAttack => Busy;

        // While a move runs, the coroutine moves the enemy.
        public override void Act() { }

        public override void Think()
        {
            if (Busy || !HasPlayer || Enemy.IsAttackLocked) return;

            Rigidbody2D playerBody = PlayerController != null ? PlayerController.GetComponent<Rigidbody2D>() : null;
            float playerVx = playerBody != null ? playerBody.linearVelocity.x : 0f;
            float playerVy = playerBody != null ? playerBody.linearVelocity.y : 0f;

            if (Time.time >= nextSlide && Mathf.Abs(DX) < 1.2f && DY > 0.8f && DY < 4f && playerVy < -1f)
            {
                int away = DX >= 0f ? -1 : 1;
                if (RoomAhead(away) < 1f) away = -away;
                if (RoomAhead(away) >= 1f) RunSpecial(Slide(away));
                return;
            }

            float distance = Mathf.Abs(DX);
            bool movingAway = playerVx * DirToPlayer > 1f;
            if (Time.time >= nextDash && distance >= dashMin && distance <= dashMax && SameLevel() && movingAway && RoomAhead(DirToPlayer) > 1f)
            {
                RunSpecial(Dash(DirToPlayer));
            }
        }

        private IEnumerator Slide(int dir)
        {
            nextSlide = Time.time + slideCooldown;
            Trace("evasive slide");
            Enemy.PlayState("Sliding");
            yield return Travel(dir, slideSpeed, slideDistance);
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            Enemy.Face(DirToPlayer);
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator Dash(int dir)
        {
            nextDash = Time.time + dashCooldown;
            Trace("flank dash");
            Warn(0.2f);
            yield return new WaitForSeconds(0.2f);
            Enemy.PlayState("Running");
            float distance = Mathf.Max(0f, Mathf.Abs(DX) - Enemy.AttackRange * 0.7f);
            yield return Travel(dir, dashSpeed, distance);
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            if (HasPlayer && Mathf.Abs(DX) <= Enemy.AttackRange && SameLevel())
            {
                Enemy.Face(DirToPlayer);
                Enemy.BasicAttack();
            }
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Travel(int dir, float speed, float distance)
        {
            float travelled = 0f;
            while (travelled < distance && !Enemy.IsDead)
            {
                if (!Enemy.MoveHorizontal(dir, speed, true)) yield break;
                travelled += speed * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
