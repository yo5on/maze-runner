using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Villager Zombie 1: a slow, persistent pursuer. It wanders aimlessly (short shuffles and long
    /// pauses) until the player comes near on its floor, then follows at a slow, steady pace and never
    /// gives up while the player stays on its platform.
    /// Signature: Heavy Bite. Wind-up 0.55 s (it stops, a warning shows), then a short reach that hurts
    /// and slows the player to half speed for 1.5 s. Cooldown 2.6 s, recovery 0.8 s. Avoid it by jumping
    /// away or over the zombie during the wind-up.
    /// </summary>
    public class ZombieShamblerBehaviour : EnemyBehaviour
    {
        [SerializeField] private float wanderSpeed = 0.7f;
        [SerializeField] private float chaseSpeed = 1.3f;
        [SerializeField] private float noticeRange = 5.5f;
        [SerializeField] private float giveUpRange = 9f;
        [SerializeField] private float biteRange = 1.4f;
        [SerializeField] private float biteWindUp = 0.55f;
        [SerializeField] private float biteRecovery = 0.8f;
        [SerializeField] private float biteCooldown = 2.6f;
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float slowDuration = 1.5f;

        private bool hunting;
        private float wanderTimer;
        private int wanderDirection = 1;
        private bool wanderPaused;
        private float nextBite;

        public override void Think()
        {
            if (Busy) return;
            if (!hunting && Notices(noticeRange)) hunting = true;
            else if (hunting && (!HasPlayer || Mathf.Abs(DX) > giveUpRange || Mathf.Abs(DY) > 3f)) hunting = false;

            if (hunting && Time.time >= nextBite && Mathf.Abs(DX) <= biteRange && SameLevel())
            {
                RunSpecial(Bite());
            }
        }

        public override void Act()
        {
            if (Busy) return; // the bite moves the zombie itself
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }

            if (hunting)
            {
                if (Mathf.Abs(DX) > biteRange * 0.8f) Enemy.MoveHorizontal(DirToPlayer, chaseSpeed);
                else { Enemy.Face(DirToPlayer); Enemy.Halt(); }
                return;
            }

            // Aimless wandering: shuffle, stop, maybe turn around.
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f)
            {
                wanderPaused = !wanderPaused;
                wanderTimer = wanderPaused ? Random.Range(0.8f, 2f) : Random.Range(1.2f, 2.8f);
                if (!wanderPaused && Random.value < 0.5f) wanderDirection = -wanderDirection;
            }
            if (wanderPaused) Enemy.Halt();
            else if (!Enemy.MoveHorizontal(wanderDirection, wanderSpeed)) wanderDirection = -wanderDirection;
        }

        private IEnumerator Bite()
        {
            nextBite = Time.time + biteCooldown;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Idle Blinking");
            Warn(biteWindUp);
            yield return new WaitForSeconds(biteWindUp);

            Enemy.PlayState("Slashing");
            Trace("bite");
            Enemy.Body.linearVelocity = new Vector2(Enemy.Direction * 2.5f, Enemy.Body.linearVelocity.y);
            yield return new WaitForSeconds(0.15f);
            if (HitInFront(1.2f, 1.6f, 1, Vector2.zero) && PlayerController != null)
            {
                PlayerController.ApplySlow(slowMultiplier, slowDuration);
            }
            Enemy.Halt();
            Trace("recover");
            yield return new WaitForSeconds(biteRecovery);
            Enemy.ReturnToLocomotion();
        }
    }
}
