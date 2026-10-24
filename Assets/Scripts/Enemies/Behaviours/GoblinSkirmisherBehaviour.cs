using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Regular Goblin: a cunning, mobile patroller. It keeps the original goblin patrol (paired platforms,
    /// turning at ledges) and the original slash, and adds two tricks:
    /// - Evasive Slide: when the player's jump is about to come down on its head (a stomp coming) it may
    ///   slide about two units out of the way (Sliding): it needs 0.15 s to react, does it about half the
    ///   time, and then not again for 3 s, so a second, well-timed stomp lands.
    /// - Flanking Dash: when the player is 2-4.5 units away on its floor and moving away from it (an
    ///   exposed back), it dashes in and slashes. Cooldown 3.5 s.
    /// Both moves stop at ledges.
    /// </summary>
    public class GoblinSkirmisherBehaviour : EnemyBehaviour
    {
        [SerializeField] private float slideSpeed = 7f;
        [SerializeField] private float slideDistance = 2.2f;
        [SerializeField] private float slideCooldown = 3f;
        [SerializeField] private float slideReaction = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float slideChance = 0.5f;
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

            float playerVx = PlayerController != null ? PlayerController.Velocity.x : 0f;

            bool stompComing = Time.time >= nextSlide && StompIncoming(0.6f, out _);
            if (Anticipates(stompComing, slideReaction, slideChance, slideCooldown))
            {
                int away = AwayFromLanding;
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
    }
}
