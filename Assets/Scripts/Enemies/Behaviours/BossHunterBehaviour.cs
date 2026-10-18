using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Boss Goblin: a persistent hunter. Movement is the original boss AI: idle until the player comes
    /// near, then a real chase with the Running animation, stopping at ledges and to fight.
    /// Signature: Hunter's Combo. In reach it strikes twice (slash, then a lunging run-slash; 1 damage
    /// each) and every third combo ends with a Heavy Cleave: 0.7 s wind-up with a warning and a slowed
    /// swing, then 2 damage and a big knock-back. After each combo it stands winded for 1 s - the time to
    /// stomp it. Below half health it gets angrier: shorter recovery and cooldown.
    /// </summary>
    public class BossHunterBehaviour : EnemyBehaviour
    {
        [SerializeField] private float comboCooldown = 1.6f;
        [SerializeField] private float recovery = 1f;
        [SerializeField] private float enragedCooldown = 1.1f;
        [SerializeField] private float enragedRecovery = 0.6f;
        [SerializeField] private float heavyWindUp = 0.7f;

        private float nextCombo;
        private int comboCount;

        public override bool ControlsMovement => Busy;
        public override bool ControlsAttack => true;
        protected override bool HasSuperArmor => true;

        // While a move runs, the coroutine moves the enemy.
        public override void Act() { }

        private bool Enraged => Enemy.CurrentHealth * 2 <= Enemy.MaxHealth;

        public override void Think()
        {
            if (Busy || !HasPlayer) return;
            if (Enemy.IsBossAlerted && Time.time >= nextCombo && Mathf.Abs(DX) <= Enemy.AttackRange && Mathf.Abs(DY) <= 2f)
            {
                RunSpecial(Combo());
            }
        }

        private IEnumerator Combo()
        {
            comboCount++;
            bool heavy = comboCount % 3 == 0;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);

            Enemy.PlayState("Slashing");
            Trace(heavy ? "combo (ends heavy)" : "combo");
            yield return new WaitForSeconds(0.2f);
            HitInFront(1.6f, 2.4f, 1, new Vector2(Enemy.Direction * 4f, 4f));
            yield return new WaitForSeconds(0.3f);

            Enemy.Face(DirToPlayer);
            Enemy.PlayState("Run Slashing");
            if (RoomAhead(Enemy.Direction) > 0.8f) Enemy.Body.linearVelocity = new Vector2(Enemy.Direction * 5f, Enemy.Body.linearVelocity.y);
            yield return new WaitForSeconds(0.15f);
            Enemy.Halt();
            HitInFront(1.7f, 2.4f, 1, new Vector2(Enemy.Direction * 6f, 5f));

            if (heavy)
            {
                yield return new WaitForSeconds(0.2f);
                Enemy.Face(DirToPlayer);
                Warn(heavyWindUp);
                Enemy.PlayState("Slashing");
                Enemy.Anim.speed = 0.4f;
                yield return new WaitForSeconds(heavyWindUp);
                Enemy.Anim.speed = 1.4f;
                Trace("heavy cleave");
                HitInFront(2.2f, 2.6f, 2, new Vector2(Enemy.Direction * 9f, 7f));
                yield return new WaitForSeconds(0.2f);
                Enemy.Anim.speed = 1f;
            }

            Enemy.ReturnToLocomotion();
            Enemy.PlayState("Idle Blinking");
            Enemy.LockMovement(Enraged ? enragedRecovery : recovery);
            nextCombo = Time.time + (Enraged ? enragedCooldown : comboCooldown) + (Enraged ? enragedRecovery : recovery);
            Trace(Enraged ? "recover (enraged)" : "recover");
            yield return new WaitForSeconds(Enraged ? enragedRecovery : recovery);
            Enemy.ReturnToLocomotion();
        }
    }
}
