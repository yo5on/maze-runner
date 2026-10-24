using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Wraith 3: elusive and supernatural. It drifts toward the player with an uneven, surging speed,
    /// hangs back at about two units, and darts away when the player is right above it (about to stomp).
    /// Signature: Phase Dash. When the player is 1.5-4 units away on its floor: a 0.4 s wind-up (warning;
    /// the Wraith fades), then it phases straight through the player for up to 3.5 units (never past a
    /// ledge), dealing 1 damage on the way through. While phased it cannot be hurt and does not collide
    /// with the player; collision and vulnerability return as soon as the dash ends. Recovery 0.7 s,
    /// cooldown 3.5 s.
    /// </summary>
    public class WraithPhaseBehaviour : EnemyBehaviour
    {
        public override Intelligence Tier => Enemy.IsBoss ? Intelligence.Boss : Intelligence.Elite;

        [SerializeField] private float noticeRange = 7f;
        [SerializeField] private float driftSpeed = 1.3f;
        [SerializeField] private float preferredDistance = 2f;
        [SerializeField] private float evadeSpeed = 3.2f;
        [SerializeField] private float dashMin = 1.5f;
        [SerializeField] private float dashMax = 4f;
        [SerializeField] private float dashWindUp = 0.4f;
        [SerializeField] private float dashSpeed = 10f;
        [SerializeField] private float dashDistance = 3.5f;
        [SerializeField] private float recovery = 0.7f;
        [SerializeField] private float cooldown = 3.5f;

        private bool engaged;
        private float surgeOffset;
        private bool phased;
        private float nextDash;
        private float evadeTimer;
        private Collider2D[] playerColliders;
        private SpriteRenderer[] renderers;

        protected override void Start()
        {
            base.Start();
            if (PlayerTransform != null) playerColliders = PlayerTransform.root.GetComponentsInChildren<Collider2D>(true);
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            surgeOffset = Random.Range(0f, 6.28f); // each wraith surges on its own rhythm
        }

        public override bool TryBlock(int damage)
        {
            if (phased) Trace("phased through stomp");
            return phased;
        }

        public override void Think()
        {
            if (Busy) return;
            if (!engaged && Notices(noticeRange)) engaged = true;
            else if (engaged && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 3f)) engaged = false;
            if (!engaged) return;

            // Threatened: the player is right above and coming down.
            if (evadeTimer <= 0f && Mathf.Abs(DX) < 1.6f && DY > 0.8f) evadeTimer = 0.5f;

            float distance = Mathf.Abs(DX);
            if (Time.time >= nextDash && distance >= dashMin && distance <= dashMax && SameLevel() && RoomAhead(DirToPlayer) > distance)
            {
                RunSpecial(PhaseDash());
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
            if (!engaged) { Enemy.PatrolStep(); return; }

            if (evadeTimer > 0f)
            {
                evadeTimer -= Time.fixedDeltaTime;
                int away = -DirToPlayer;
                if (!Enemy.MoveHorizontal(away, evadeSpeed, true)) Enemy.MoveHorizontal(-away, evadeSpeed, true);
                return;
            }

            // Surging drift: speed rises and falls so its approach is hard to time.
            float surge = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.1f + surgeOffset);
            float distance = Mathf.Abs(DX);
            if (distance > preferredDistance + 0.4f) Enemy.MoveHorizontal(DirToPlayer, driftSpeed * (0.4f + surge));
            else if (distance < preferredDistance - 0.6f) Enemy.MoveHorizontal(-DirToPlayer, driftSpeed * 0.8f);
            else { Enemy.Halt(); Enemy.Face(DirToPlayer); }
        }

        private IEnumerator PhaseDash()
        {
            nextDash = Time.time + cooldown;
            int dir = DirToPlayer;
            Enemy.Halt();
            Enemy.Face(dir);
            Warn(dashWindUp);
            SetAlpha(0.45f);
            yield return new WaitForSeconds(dashWindUp);

            float lane = Mathf.Min(dashDistance, RoomAhead(dir));
            SetPhased(true);
            Trace("dash");
            Enemy.PlayState("Run Slashing");
            float travelled = 0f;
            bool hit = false;
            while (travelled < lane && !Enemy.IsDead)
            {
                if (!Enemy.MoveHorizontal(dir, dashSpeed, true)) break;
                travelled += dashSpeed * Time.fixedDeltaTime;
                if (!hit) hit = HitBox(Enemy.BodyCollider.bounds.center, Enemy.BodyCollider.bounds.size + new Vector3(0.4f, 0f, 0f), 1, Vector2.zero);
                yield return new WaitForFixedUpdate();
            }
            Enemy.Halt();
            SetPhased(false);
            Enemy.ReturnToLocomotion();
            Enemy.Face(DirToPlayer);
            Trace("recover");
            yield return new WaitForSeconds(recovery);
        }

        private void SetPhased(bool on)
        {
            phased = on;
            if (playerColliders != null && Enemy.BodyCollider != null)
            {
                foreach (Collider2D c in playerColliders)
                {
                    if (c != null && !c.isTrigger) Physics2D.IgnoreCollision(Enemy.BodyCollider, c, on);
                }
            }
            SetAlpha(on ? 0.35f : 1f);
        }

        private Color[] originalColors;

        /// <summary>Fades the rig (alpha 1 restores each part's own colour; hidden parts stay hidden).</summary>
        private void SetAlpha(float alpha)
        {
            if (renderers == null) return;
            if (originalColors == null)
            {
                if (alpha >= 1f) return;
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = originalColors[i];
                renderers[i].color = alpha >= 1f ? c : new Color(c.r, c.g, c.b, c.a * alpha);
            }
            if (alpha >= 1f) originalColors = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetPhased(false);
        }

        public override void OnHurt()
        {
            base.OnHurt();
            SetPhased(false);
        }
    }
}
