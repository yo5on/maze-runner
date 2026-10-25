using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Skeleton Warrior 3: an aggressive duelist. It patrols briskly and, once the player is within
    /// 6 units on its floor, advances at a run.
    /// Signature: Two-Hit Combo. A short warning (0.35 s), a first slash, a step forward and a second
    /// slash (each hit 1 damage, each checked once). Afterwards it is winded for 1.2 s: it stands still
    /// and cannot attack, which is the window to stomp it. Cooldown 2.2 s from the start of the combo.
    /// Footwork: when it sees a stomp coming down on it (0.3 s to react) it sometimes steps back out of
    /// the way - about a third of the time, at most every 4 s.
    /// </summary>
    public class DuelistBehaviour : EnemyBehaviour
    {
        public override Intelligence Tier => Enemy.IsBoss ? Intelligence.Boss : Intelligence.Elite;

        [SerializeField] private float noticeRange = 6f;
        [SerializeField] private float advanceSpeed = 2.6f;
        [SerializeField] private float comboRange = 1.5f;
        [SerializeField] private float comboWarning = 0.35f;
        [SerializeField] private float betweenHits = 0.45f;
        [SerializeField] private float recovery = 1.2f;
        [SerializeField] private float cooldown = 2.2f;
        [SerializeField] private float stepReaction = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float stepChance = 0.35f;
        [SerializeField] private float stepCooldown = 4f;

        private bool engaged;
        private float nextCombo;

        public override void Think()
        {
            if (Busy) return;
            if (!engaged && Notices(noticeRange)) engaged = true;
            else if (engaged && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 3f)) engaged = false;

            // Not gated on engagement: it drops while the player is high overhead, mid-stomp.
            if (HasPlayer && Mathf.Abs(DX) < noticeRange && Anticipates(StompIncoming(0.7f, out _), stepReaction, stepChance, stepCooldown))
            {
                int away = AwayFromLanding;
                if (RoomAhead(away) < 1.2f) away = -away;
                if (RoomAhead(away) >= 1.2f) { RunSpecial(StepBack(away)); return; }
            }

            // Watching a stomp come down on it, it does not open a combo (that would always beat its read).
            if (engaged && !Forecast.OnTarget && Time.time >= nextCombo && Mathf.Abs(DX) <= comboRange && SameLevel())
            {
                RunSpecial(Combo());
            }
        }

        public override void Act()
        {
            if (Busy) return;
            if (Enemy.IsAttackLocked) { Enemy.Halt(); return; }
            if (!engaged) { Enemy.PatrolStep(); return; }
            if (Mathf.Abs(DX) > comboRange * 0.8f) Enemy.MoveHorizontal(DirToPlayer, advanceSpeed, true);
            else { Enemy.Face(DirToPlayer); Enemy.Halt(); }
        }

        private IEnumerator StepBack(int dir)
        {
            Trace("step back");
            yield return Travel(dir, 5f, 1.3f);
            Enemy.Halt();
            if (HasPlayer) Enemy.Face(DirToPlayer);
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator Combo()
        {
            nextCombo = Time.time + cooldown;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Warn(comboWarning);
            yield return new WaitForSeconds(comboWarning);

            Enemy.PlayState("Slashing");
            Trace("combo hit 1");
            yield return new WaitForSeconds(0.15f);
            HitInFront(1.3f, 1.8f, 1, new Vector2(Enemy.Direction * 3f, 3f));
            yield return new WaitForSeconds(betweenHits - 0.15f);

            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Run Slashing");
            Trace("combo hit 2");
            if (RoomAhead(Enemy.Direction) > 0.6f) Enemy.Body.linearVelocity = new Vector2(Enemy.Direction * 4f, Enemy.Body.linearVelocity.y);
            yield return new WaitForSeconds(0.15f);
            Enemy.Halt();
            HitInFront(1.4f, 1.8f, 1, new Vector2(Enemy.Direction * 5f, 4f));

            // Winded: the opening after the combo.
            yield return new WaitForSeconds(0.2f);
            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Trace("recover");
            yield return new WaitForSeconds(recovery);
            Enemy.ReturnToLocomotion();
        }
    }
}
