using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Villager Zombie 3: aggressive and unpredictable. It prowls in stop-and-go bursts and, once it
    /// sees the player, rushes in with erratic speed and sudden short pauses.
    /// Signature: Lunge. From 2.2-4.5 units away it freezes in a crouch for 0.45 s (warning shown),
    /// then launches forward fast (up to 3.5 units, never past a ledge). Hit: 1 damage and a knock-back.
    /// It then needs 0.9 s to recover, which is the moment to stomp it. Cooldown 3 s. Up close it uses
    /// its normal slash. It relies on mobility, not toughness: it sometimes reads a stomp's arc (slowly -
    /// 0.3 s to react, 30 %, at most every 4 s) and springs back out from under it.
    /// </summary>
    public class ZombieRusherBehaviour : EnemyBehaviour
    {
        [SerializeField] private float prowlSpeed = 1.5f;
        [SerializeField] private float rushSpeed = 3f;
        [SerializeField] private float noticeRange = 7f;
        [SerializeField] private float lungeMin = 2.2f;
        [SerializeField] private float lungeMax = 4.5f;
        [SerializeField] private float lungeWindUp = 0.45f;
        [SerializeField] private float lungeSpeed = 9f;
        [SerializeField] private float lungeDistance = 3.5f;
        [SerializeField] private float lungeRecovery = 0.9f;
        [SerializeField] private float lungeCooldown = 3f;
        [SerializeField] private float springReaction = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float springChance = 0.3f;
        [SerializeField] private float springCooldown = 4f;

        private bool rushing;
        private float burstTimer;
        private bool burstPaused;
        private float nextLunge;
        private int prowlDirection = 1;

        public override void Think()
        {
            if (Busy) return;
            if (HasPlayer && Mathf.Abs(DX) < noticeRange && Anticipates(StompIncoming(0.6f, out _), springReaction, springChance, springCooldown))
            {
                int away = AwayFromLanding;
                if (RoomAhead(away) < 1f) away = -away;
                if (RoomAhead(away) >= 1f) { RunSpecial(SpringBack(away)); return; }
            }
            if (!rushing && Notices(noticeRange)) rushing = true;
            else if (rushing && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 3f)) rushing = false;
            if (!rushing) return;

            float distance = Mathf.Abs(DX);
            if (Time.time >= nextLunge && distance >= lungeMin && distance <= lungeMax && SameLevel() && RoomAhead(DirToPlayer) > 1f)
            {
                RunSpecial(Lunge());
            }
            else if (distance <= Enemy.AttackRange && SameLevel() && Enemy.CanBasicAttack)
            {
                Enemy.Face(DirToPlayer);
                Enemy.BasicAttack();
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }

            // Unpredictable rhythm: bursts of movement broken by short random stops.
            burstTimer -= Time.fixedDeltaTime;
            if (burstTimer <= 0f)
            {
                burstPaused = !burstPaused && Random.value < 0.45f;
                burstTimer = burstPaused ? Random.Range(0.15f, 0.4f) : Random.Range(0.4f, 1f);
            }
            if (burstPaused) { Enemy.Halt(); return; }

            if (rushing)
            {
                if (Mathf.Abs(DX) > Enemy.AttackRange * 0.8f) Enemy.MoveHorizontal(DirToPlayer, Randomized(rushSpeed, 0.25f), true);
                else { Enemy.Face(DirToPlayer); Enemy.Halt(); }
            }
            else if (!Enemy.MoveHorizontal(prowlDirection, prowlSpeed))
            {
                prowlDirection = -prowlDirection; // prowls its platform edge to edge
            }
        }

        private IEnumerator SpringBack(int dir)
        {
            Trace("spring back");
            yield return Travel(dir, 6f, 1.6f);
            Enemy.Halt();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Lunge()
        {
            nextLunge = Time.time + lungeCooldown;
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            Enemy.PlayState("Idle Blinking");
            Enemy.Anim.speed = 0f; // a frozen crouch reads as "about to pounce"
            Warn(lungeWindUp);
            yield return new WaitForSeconds(lungeWindUp);

            Enemy.Anim.speed = 1f;
            Enemy.PlayState("Run Slashing");
            Trace("lunge");
            float travelled = 0f;
            bool hit = false;
            float maxTravel = Mathf.Min(lungeDistance, RoomAhead(dir));
            while (travelled < maxTravel && !Enemy.IsDead)
            {
                if (!Enemy.MoveHorizontal(dir, lungeSpeed, true)) break;
                travelled += lungeSpeed * Time.fixedDeltaTime;
                if (!hit) hit = HitInFront(0.6f, 1.6f, 1, new Vector2(dir * 6f, 4f));
                yield return new WaitForFixedUpdate();
            }
            Enemy.Halt();
            Enemy.ReturnToLocomotion();
            Trace("recover");
            yield return new WaitForSeconds(lungeRecovery);
        }
    }
}
