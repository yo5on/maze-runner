using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MazeRunner.Player;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// An enemy's personality: how it moves, notices the player, attacks and reacts.
    ///
    /// <see cref="GoblinEnemy"/> on the same object stays the shared body (health, damage, death and the
    /// defeat count, platform edges, facing, locomotion animation, basic slash, boss chase). A behaviour
    /// only decides: GoblinEnemy calls <see cref="Think"/> every frame and, while
    /// <see cref="ControlsMovement"/> is true, <see cref="Act"/> every physics step instead of its own
    /// patrol. One behaviour per enemy.
    ///
    /// Special moves run as coroutines started with <see cref="RunSpecial"/>; they always start with a
    /// visible wind-up (<see cref="Warn"/>, and <see cref="DangerZone"/> for area attacks), resolve their
    /// hit once, recover and respect a cooldown. Death (and, for light enemies, being stomped) stops a
    /// special at once, removes its telegraphs and keeps it from dealing damage.
    /// </summary>
    [RequireComponent(typeof(GoblinEnemy))]
    [DisallowMultipleComponent]
    public abstract class EnemyBehaviour : MonoBehaviour
    {
        protected GoblinEnemy Enemy { get; private set; }
        protected PlayerHealth PlayerHealth { get; private set; }
        protected PlayerController PlayerController { get; private set; }

        private Coroutine special;
        private readonly List<GameObject> telegraphs = new List<GameObject>();
        private static Sprite square;

        /// <summary>True while a special move (wind-up, action or recovery) is running.</summary>
        protected bool Busy => special != null;

        /// <summary>For tests and debugging: every wind-up, strike, hit, block and recovery is announced here.</summary>
        public static event System.Action<EnemyBehaviour, string> Traced;

        public bool IsBusy => Busy;

        protected void Trace(string what) => Traced?.Invoke(this, what);

        /// <summary>When true GoblinEnemy calls <see cref="Act"/> instead of its own patrol.</summary>
        public virtual bool ControlsMovement => true;

        /// <summary>When true GoblinEnemy leaves attacking to this behaviour (no automatic slash).</summary>
        public virtual bool ControlsAttack => true;

        /// <summary>Heavy enemies keep going when stomped (no flinch out of a wind-up).</summary>
        protected virtual bool HasSuperArmor => false;

        protected virtual void Awake()
        {
            Enemy = GetComponent<GoblinEnemy>();
        }

        protected virtual void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerHealth = player.GetComponent<PlayerHealth>();
                PlayerController = player.GetComponent<PlayerController>();
            }
        }

        /// <summary>Per frame: perception and decisions.</summary>
        public virtual void Think() { }

        /// <summary>Per physics step, while <see cref="ControlsMovement"/>: movement.</summary>
        public virtual void Act() { Enemy.PatrolStep(); }

        /// <summary>Return true to turn a hit away (shield, parry, phase). Called before damage.</summary>
        public virtual bool TryBlock(int damage) => false;

        /// <summary>The enemy was hurt but survived. Light enemies flinch out of their wind-ups.</summary>
        public virtual void OnHurt()
        {
            if (!HasSuperArmor && Busy) Interrupt(0.35f);
        }

        /// <summary>Death stops everything this behaviour was doing.</summary>
        public virtual void OnDeath()
        {
            CancelSpecial();
            enabled = false;
        }

        // ---------------------------------------------------------------- perception

        protected Transform PlayerTransform => Enemy.Player;
        protected bool HasPlayer => Enemy.Player != null && PlayerHealth != null && PlayerHealth.CurrentHealth > 0;

        /// <summary>Horizontal offset to the player (positive = player to the right).</summary>
        protected float DX => HasPlayer ? Enemy.Player.position.x - transform.position.x : float.MaxValue;

        /// <summary>Height of the player's feet above this enemy's feet.</summary>
        protected float DY => HasPlayer ? (Enemy.Player.position.y - 0.5f) - transform.position.y : float.MaxValue;

        protected int DirToPlayer => DX >= 0f ? 1 : -1;

        /// <summary>The player is on (roughly) the same floor, so a ground attack can reach it.</summary>
        protected bool SameLevel(float tolerance = 1.4f) => HasPlayer && Mathf.Abs(DY) <= tolerance;

        /// <summary>Within <paramref name="range"/> horizontally and on the same floor.</summary>
        protected bool Notices(float range, float heightTolerance = 1.6f) =>
            HasPlayer && Mathf.Abs(DX) <= range && Mathf.Abs(DY) <= heightTolerance;

        protected bool PlayerGrounded => PlayerController != null && PlayerController.IsGrounded;

        /// <summary>Distance the enemy can still move in a direction before its platform edge.</summary>
        protected float RoomAhead(int dir)
        {
            if (!Enemy.TryGetPlatformLimits(out float min, out float max)) return 0f;
            return dir > 0 ? max - transform.position.x : transform.position.x - min;
        }

        protected float GroundY => Enemy.Platform != null ? Enemy.Platform.bounds.max.y : transform.position.y;

        // ---------------------------------------------------------------- specials

        protected void RunSpecial(IEnumerator routine)
        {
            CancelSpecial();
            special = StartCoroutine(Wrap(routine));
        }

        private IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            special = null;
            ClearTelegraphs();
        }

        protected void CancelSpecial()
        {
            if (special != null) StopCoroutine(special);
            special = null;
            ClearTelegraphs();
            if (Enemy != null && Enemy.Anim != null) Enemy.Anim.speed = 1f;
        }

        /// <summary>Stops the current special and stands still for a moment (stunned).</summary>
        protected void Interrupt(float stun)
        {
            Trace("interrupted");
            CancelSpecial();
            Enemy.Halt();
            Enemy.LockMovement(stun);
        }

        // ---------------------------------------------------------------- telegraphs

        protected static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                    square.name = "EnemyTelegraphSquare";
                }
                return square;
            }
        }

        /// <summary>
        /// The wind-up warning above the head: a crimson "!" badge that grows and then blinks, over a
        /// stone timer bar that fills with crimson and is full when the move fires.
        /// </summary>
        protected void Warn(float duration)
        {
            Trace("windup " + duration.ToString("0.00"));
            var go = new GameObject("Warning");
            go.transform.SetParent(transform, false);
            float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            float top = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.max.y - transform.position.y : 2f;
            go.transform.localPosition = new Vector3(0f, (top + 0.8f) / scale, 0f);

            var badge = new GameObject("Badge");
            badge.transform.SetParent(go.transform, false);
            var badgeRenderer = badge.AddComponent<SpriteRenderer>();
            badgeRenderer.sprite = TelegraphArt.WarningBadge;
            badgeRenderer.sortingOrder = 52;

            const float barWidth = 0.7f, barHeight = 0.08f, barY = -0.5f;
            SpriteRenderer track = TelegraphPart(go.transform, "TimerTrack", TelegraphArt.Stone, 50);
            track.transform.localPosition = new Vector3(0f, barY, 0f);
            track.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            SpriteRenderer fill = TelegraphPart(go.transform, "TimerFill", TelegraphArt.Crimson, 51);
            fill.transform.localPosition = new Vector3(-barWidth / 2f, barY, 0f);
            fill.transform.localScale = new Vector3(0f, barHeight, 1f);

            go.AddComponent<TelegraphPulse>().Init(badge.transform, 0.5f, 0.72f, fill.transform, barWidth, duration);
            telegraphs.Add(go);
        }

        /// <summary>
        /// The ground an area attack or charge will cover: crimson hatching with a solid crimson top
        /// edge and end marks, so the reach reads at a glance without hiding the platform.
        /// </summary>
        protected GameObject DangerZone(float fromX, float toX, float height = 0.35f)
        {
            var go = new GameObject("DangerZone");
            float left = Mathf.Min(fromX, toX), right = Mathf.Max(fromX, toX);
            float width = Mathf.Max(0.1f, right - left);
            go.transform.position = new Vector3((left + right) / 2f, GroundY + height / 2f, 0f);

            var hatch = new GameObject("Hatching");
            hatch.transform.SetParent(go.transform, false);
            var sr = hatch.AddComponent<SpriteRenderer>();
            sr.sprite = TelegraphArt.Hatching;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, height);
            sr.sortingOrder = 4;

            const float line = 0.05f;
            SpriteRenderer edge = TelegraphPart(go.transform, "Edge", TelegraphArt.Crimson, 5);
            edge.transform.localPosition = new Vector3(0f, height / 2f - line / 2f, 0f);
            edge.transform.localScale = new Vector3(width, line, 1f);
            foreach (float side in new[] { -1f, 1f })
            {
                SpriteRenderer end = TelegraphPart(go.transform, "End", TelegraphArt.Crimson, 5);
                end.transform.localPosition = new Vector3(side * (width / 2f - line / 2f), 0f, 0f);
                end.transform.localScale = new Vector3(line, height, 1f);
            }
            telegraphs.Add(go);
            return go;
        }

        /// <summary>A rounded stone bar in front of the body while a shield is raised (stomps are blocked).</summary>
        protected GameObject ShieldMarker()
        {
            var go = new GameObject("Shield");
            go.transform.SetParent(transform, false);
            float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            float height = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.size.y : 2f;
            float halfWidth = Enemy.BodyCollider != null ? Enemy.BodyCollider.bounds.extents.x : 0.5f;
            go.transform.localPosition = new Vector3((halfWidth + 0.15f) / scale, height * 0.55f / scale, 0f);
            // The capsule sprite is a quarter unit wide and one unit tall.
            go.transform.localScale = new Vector3(0.14f * 4f / scale, height * 0.7f / scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TelegraphArt.Capsule;
            sr.color = new Color32(0xaf, 0xae, 0xa2, 0xe0);
            sr.sortingOrder = 40;
            return go;
        }

        private static SpriteRenderer TelegraphPart(Transform parent, string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TelegraphArt.Pixel;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        protected void ClearTelegraphs()
        {
            foreach (GameObject go in telegraphs) if (go != null) Destroy(go);
            telegraphs.Clear();
        }

        protected virtual void OnDisable()
        {
            CancelSpecial();
        }

        // ---------------------------------------------------------------- hits

        /// <summary>
        /// Damages the player if it is inside the box (world space). Returns true on a hit. Never hits
        /// when the enemy is dead.
        /// </summary>
        protected bool HitBox(Vector2 center, Vector2 size, int damage, Vector2 knockback, bool groundedOnly = false)
        {
            if (Enemy.IsDead || PlayerHealth == null) return false;
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, Enemy.PlayerLayer);
            if (hit == null) return false;
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health == null) return false;
            if (groundedOnly && !PlayerGrounded) { Trace("strike avoided (airborne)"); return false; }

            health.TakeDamage(damage);
            Rigidbody2D body = hit.attachedRigidbody;
            if (body != null && knockback != Vector2.zero) body.linearVelocity = knockback;
            if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlaySwordHit();
            Trace("hit player for " + damage);
            return true;
        }

        /// <summary>A box in front of the enemy: starts at its front edge and reaches <paramref name="reach"/>.</summary>
        protected bool HitInFront(float reach, float height, int damage, Vector2 knockback)
        {
            Bounds b = Enemy.BodyCollider.bounds;
            int dir = Enemy.Direction;
            float startX = dir > 0 ? b.center.x : b.center.x - reach - b.extents.x;
            var center = new Vector2(startX + (reach + b.extents.x) / 2f, b.min.y + height / 2f);
            return HitBox(center, new Vector2(reach + b.extents.x, height), damage, knockback);
        }

        protected static float Randomized(float value, float spread) => value * Random.Range(1f - spread, 1f + spread);
    }
}
