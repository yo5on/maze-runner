using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Bloody Alchemist 1: a cautious ranged enemy with a weapon in each hand - a potion in the right hand
    /// (the near arm; it is always holding one, and reaches for the next after a throw) and the knife in
    /// the left, where the rig's own art has it. It notices the player from 9 units (and a little above or below), keeps 3.5-6.5
    /// units away - backing off when approached, stepping closer when the player is far - and stands its
    /// ground with its back to a ledge.
    /// Signature: Potion Throw (right arm). Wind-up 0.5 s: the warning shows and the right arm swings the
    /// potion up and back behind the head while the body leans back; then the arm whips
    /// over and the potion leaves the hand at the top of the swing, from where the hand is, tumbling along a
    /// lobbed arc aimed where the player stands. A direct hit deals 1 damage; where it lands it shatters and
    /// leaves a spilled-potion pool (a hazard strip) for 2.5 s. At most two potions in the air and one pool
    /// per alchemist; cooldown 2.8 s. The potion is slow and arcs, so it can be outrun or jumped over.
    /// Knife Stab (left arm): when the player gets within reach on its floor - also while the potion is
    /// cooling down - it draws the knife back (0.3 s, warning shown), thrusts it straight at the player and
    /// pulls it back. It hits once, for 1, at full extension; cooldown 1.6 s.
    /// The arms are posed on the rig's bones after the Animator each frame, so the body keeps animating.
    /// </summary>
    public class AlchemistBehaviour : EnemyBehaviour
    {
        [SerializeField] protected float noticeRange = 9f;
        [SerializeField] private float tooClose = 3.5f;
        [SerializeField] private float tooFar = 6.5f;
        [SerializeField] private float retreatSpeed = 2f;
        [SerializeField] private float approachSpeed = 1.4f;
        [SerializeField] private float throwWindUp = 0.5f;
        [SerializeField] protected float throwCooldown = 2.8f;
        [Tooltip("Potions per throw. Extra potions land spread around the player's spot, so the ground fills with pools to step between.")]
        [SerializeField] private int volley = 1;
        [SerializeField] private float volleySpread = 1.8f;
        [SerializeField] private int maxFlasksInAir = 2;
        [SerializeField] private int maxPools = 1;
        [SerializeField] private float flightTime = 0.85f;
        [SerializeField] private float poolLifetime = 2.5f;
        [SerializeField] private float stabReach = 1.6f;
        [SerializeField] private float stabWindUp = 0.3f;
        [SerializeField] private float stabCooldown = 1.6f;
        [Tooltip("Potion art (Assets/Sprites/Weapons/Potions).")]
        [SerializeField] private PotionVisual potion = new PotionVisual();

        // Arm angles, in degrees added to the animated pose (0 = hanging at the side, as animated; negative
        // swings the hand backwards first, so the throw goes back, up behind the head and over the top).
        private const float ThrowCocked = -145f;  // throwing arm drawn back and up behind the head
        private const float ThrowRelease = -234f; // the arm comes over the top to up-front: the potion leaves the hand
        private const float ThrowFollow = -319f;  // follow-through, down in front
        private const float ThrowRest = -360f;    // the same as hanging at the side (0), reached going forward
        private const float WhipTime = 0.12f;
        private const float StabDrawn = -35f;     // knife arm pulled back
        private const float StabExtended = 100f;  // knife arm straight out in front
        private const float ThrustTime = 0.07f;
        private const float BodyLean = 8f;        // positive leans back, away from the player
        private const float TumbleSpeed = 300f;   // degrees per second, end over end in the throw direction (about one turn per flight)
        private const float BottleLean = 25f;     // the bottle in hand leans back on the wind-up, forward on the whip

        protected bool engaged;
        protected float nextThrow;
        private float nextStab;
        private int flasksInAir;
        private readonly System.Collections.Generic.List<GameObject> pools = new System.Collections.Generic.List<GameObject>();

        /// <summary>Potions per throw right now (a boss adds more in its second phase).</summary>
        protected virtual int Volley => volley;
        protected virtual float ThrowCooldown => throwCooldown;

        private Transform throwHand;
        private GameObject heldPotion;
        private bool throwing;
        private BoneOffset throwArm, stabArm, torso;
        private GripTurn knifePose;
        private Vector3 throwPalm;              // the right hand's palm, in the hand's own space
        private const float BottleGrip = 0.2f;  // the fist holds the bottle this far below its middle (of its height)
        private bool rigReady;

        protected override void Start()
        {
            base.Start();
            SetUpRig();
        }

        /// <summary>
        /// Finds the arm bones: the right arm throws (its hand holds the potion), the left arm stabs with the
        /// knife the rig's art already puts in the left hand.
        /// </summary>
        private void SetUpRig()
        {
            throwHand = FindPart("Right Hand");
            Transform knifeHand = FindPart("Left Hand");
            Transform knife = FindPart("Sword");
            if (throwHand == null || knifeHand == null || throwHand.parent == null || knifeHand.parent == null) return;

            throwArm = new BoneOffset(throwHand.parent.parent); // bone_000, the right upper arm
            stabArm = new BoneOffset(knifeHand.parent.parent);  // bone_002, the left upper arm
            torso = new BoneOffset(throwHand.parent.parent != null ? throwHand.parent.parent.parent : null);
            if (knife != null) knifePose = new GripTurn(knife, Palm(knifeHand, knife.parent));
            var throwArt = throwHand.GetComponent<SpriteRenderer>();
            throwPalm = throwArt != null && throwArt.sprite != null ? throwArt.sprite.bounds.center : Vector3.zero;
            heldPotion = HoldPotion();
            rigReady = true;
        }

        // After the Animator has posed the rig for this frame.
        private void LateUpdate()
        {
            if (!rigReady) return;
            if (!Busy)
            {
                // Back to the animated pose (also after a throw or stab is cut short).
                throwArm.Angle = Mathf.MoveTowardsAngle(throwArm.Angle, 0f, 900f * Time.deltaTime);
                stabArm.Angle = Mathf.MoveTowardsAngle(stabArm.Angle, 0f, 900f * Time.deltaTime);
                torso.Angle = Mathf.MoveTowards(torso.Angle, 0f, 120f * Time.deltaTime);
                throwing = false;
                // A fresh potion in hand (also after a throw that was cut short).
                if (heldPotion != null && !heldPotion.activeSelf && !Enemy.IsDead) heldPotion.SetActive(true);
            }
            // The blade keeps pointing at the target as the arm swings, turning about the fist that holds it.
            if (knifePose != null) knifePose.Angle = -stabArm.Angle;
            throwArm.Apply();
            stabArm.Apply();
            torso.Apply();
            knifePose?.Apply();
            if (!throwing) Lean(heldPotion, 0f); // carried upright
            PinPotionToHand();
        }

        public override void Think()
        {
            if (Busy) return;
            if (!engaged && Notices(noticeRange, 3f)) engaged = true;
            else if (engaged && (!HasPlayer || Mathf.Abs(DX) > noticeRange + 3f || Mathf.Abs(DY) > 4f)) engaged = false;

            if (HasPlayer && Time.time >= nextStab && Mathf.Abs(DX) <= stabReach && SameLevel())
            {
                RunSpecial(Stab());
            }
            else if (engaged && Time.time >= nextThrow && flasksInAir + Volley <= Mathf.Max(maxFlasksInAir, Volley) && Mathf.Abs(DX) >= 1.5f)
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
            nextThrow = Time.time + ThrowCooldown;
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Enemy.ReturnToLocomotion();
            Warn(throwWindUp);
            throwing = true;
            GameObject held = heldPotion != null && heldPotion.activeSelf ? heldPotion : null;

            // Wind-up: the right arm swings the potion back and up behind the head, the body leans back.
            for (float t = 0f; t < throwWindUp; t += Time.deltaTime)
            {
                float k = Ease(t / throwWindUp);
                SetThrowPose(ThrowCocked * k, BodyLean * k);
                Lean(held, BottleLean * k);
                yield return null;
            }
            SetThrowPose(ThrowCocked, BodyLean);

            // Whip: the arm comes over fast; the potion leaves the hand as it passes up-front.
            float released = -1f;
            Vector2 from = Vector2.zero;
            float angle = 0f;
            for (float t = 0f; t < WhipTime; t += Time.deltaTime)
            {
                float k = t / WhipTime;
                float arm = Mathf.Lerp(ThrowCocked, ThrowFollow, k);
                SetThrowPose(arm, Mathf.Lerp(BodyLean, -BodyLean, k));
                Lean(held, Mathf.Lerp(BottleLean, -BottleLean, k));
                if (released < 0f && arm <= ThrowRelease) { released = t; break; }
                yield return null;
            }
            if (held != null)
            {
                from = held.transform.position;
                angle = held.transform.eulerAngles.z;
                held.SetActive(false); // gone this frame, so it is never drawn next to the thrown one
            }
            else
            {
                Bounds b = Enemy.BodyCollider.bounds;
                from = new Vector2(b.center.x + Enemy.Direction * b.extents.x, b.max.y - 0.3f);
            }

            if (HasPlayer)
            {
                Vector2 target = new Vector2(PlayerTransform.position.x, PlayerTransform.position.y - 0.5f);
                float g = -Physics2D.gravity.y;
                Vector2 velocity = new Vector2((target.x - from.x) / flightTime,
                    (target.y - from.y + 0.5f * g * flightTime * flightTime) / flightTime);
                AlchemistFlask.Launch(this, from, velocity, potion, Square, angle, -Enemy.Direction * TumbleSpeed);
                Trace("throw");
                flasksInAir++;
                // A volley: the rest of the potions go short and long of the player, alternating.
                for (int i = 1; i < Volley; i++)
                {
                    float offset = ((i + 1) / 2) * volleySpread * (i % 2 == 1 ? -1f : 1f);
                    float x = target.x + offset;
                    var spread = new Vector2((x - from.x) / flightTime, velocity.y);
                    AlchemistFlask.Launch(this, from, spread, potion, Square, angle, -Enemy.Direction * TumbleSpeed);
                    flasksInAir++;
                }
                if (Volley > 1) Trace("volley " + Volley);
            }

            // Follow-through and recovery.
            for (float t = 0f; t < 0.1f; t += Time.deltaTime)
            {
                SetThrowPose(Mathf.Lerp(ThrowRelease, ThrowFollow, t / 0.1f), -BodyLean);
                yield return null;
            }
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float k = Ease(t / 0.25f);
                SetThrowPose(Mathf.Lerp(ThrowFollow, ThrowRest, k), Mathf.Lerp(-BodyLean, 0f, k));
                yield return null;
            }
            SetThrowPose(0f, 0f);
        }

        private IEnumerator Stab()
        {
            nextStab = Time.time + stabCooldown;
            nextThrow = Mathf.Max(nextThrow, Time.time + 0.6f); // no potion straight out of a stab
            Enemy.Halt();
            Enemy.Face(DirToPlayer);
            Enemy.ReturnToLocomotion();
            Warn(stabWindUp);
            Trace("stab windup");

            // Draw the knife back.
            for (float t = 0f; t < stabWindUp; t += Time.deltaTime)
            {
                float k = Ease(t / stabWindUp);
                SetStabPose(StabDrawn * k, BodyLean * 0.5f * k);
                yield return null;
            }

            // Thrust: straight out at the player; the hit lands once, at full extension.
            for (float t = 0f; t < ThrustTime; t += Time.deltaTime)
            {
                float k = t / ThrustTime;
                SetStabPose(Mathf.Lerp(StabDrawn, StabExtended, k), Mathf.Lerp(BodyLean * 0.5f, -BodyLean, k));
                yield return null;
            }
            SetStabPose(StabExtended, -BodyLean);
            if (HitInFront(1.1f, 1.6f, 1, new Vector2(Enemy.Direction * 4f, 3f))) Trace("stab hit player for 1");
            else Trace("stab");
            yield return new WaitForSeconds(0.12f);

            // Pull the knife back.
            for (float t = 0f; t < 0.22f; t += Time.deltaTime)
            {
                float k = Ease(t / 0.22f);
                SetStabPose(Mathf.Lerp(StabExtended, 0f, k), Mathf.Lerp(-BodyLean, 0f, k));
                yield return null;
            }
            SetStabPose(0f, 0f);
        }

        /// <summary>
        /// Keeps the potion in the right fist, whatever the arm and the bottle's lean: the palm sits a little
        /// below the bottle's middle, and the bottle is drawn just behind the fist (the fingers close over it)
        /// and in front of the body and head.
        /// </summary>
        private void PinPotionToHand()
        {
            if (heldPotion == null || !heldPotion.activeSelf || throwHand == null) return;
            Vector3 palm = throwHand.TransformPoint(new Vector3(throwPalm.x, throwPalm.y, 0f));
            float up = AlchemistFlask.BottleHeight * BottleGrip;
            Vector3 lift = heldPotion.transform.rotation * new Vector3(0f, up, 0f);
            heldPotion.transform.position = new Vector3(palm.x + lift.x, palm.y + lift.y, throwHand.position.z + 0.0005f);
        }

        /// <summary>The middle of a hand's art, in the space of <paramref name="space"/> (where the weapon lives).</summary>
        private static Vector3 Palm(Transform hand, Transform space)
        {
            var art = hand.GetComponent<SpriteRenderer>();
            Vector3 local = art != null && art.sprite != null ? art.sprite.bounds.center : Vector3.zero;
            return space.InverseTransformPoint(hand.TransformPoint(local));
        }

        private void SetThrowPose(float arm, float lean)
        {
            if (!rigReady) return;
            throwArm.Angle = arm;
            torso.Angle = lean;
        }

        private void SetStabPose(float arm, float lean)
        {
            if (!rigReady) return;
            stabArm.Angle = arm;
            torso.Angle = lean;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The potion held in the right hand: it moves with the arm, is hidden from the release until the throw is over, then a fresh one is in hand.</summary>
        private GameObject HoldPotion()
        {
            if (potion == null || potion.bottle == null) return null;
            Transform hand = throwHand != null ? throwHand : transform;
            var go = new GameObject("HeldPotion");
            go.transform.SetParent(hand, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = potion.bottle;
            var handRenderer = hand.GetComponent<SpriteRenderer>();
            if (handRenderer != null)
            {
                // Same order as the rig; PinPotionToHand puts it just behind the fist by depth.
                sr.sortingLayerID = handRenderer.sortingLayerID;
                sr.sortingOrder = handRenderer.sortingOrder;
            }
            else sr.sortingOrder = 20;
            // Same size as in flight, so nothing jumps at the release.
            float world = AlchemistFlask.BottleHeight / Mathf.Max(0.01f, potion.bottle.bounds.size.y);
            Vector3 parent = hand.lossyScale;
            go.transform.localScale = new Vector3(world / Mathf.Max(0.01f, Mathf.Abs(parent.x)), world / Mathf.Max(0.01f, Mathf.Abs(parent.y)), 1f);
            Vector3 palm = handRenderer != null && handRenderer.sprite != null ? handRenderer.sprite.bounds.center : Vector3.zero;
            go.transform.localPosition = new Vector3(palm.x, palm.y, -0.0005f);
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>
        /// Keeps the bottle in hand upright apart from a lean (positive = back, away from the player), so it
        /// reads as a bottle being thrown rather than turning over with the arm.
        /// </summary>
        private void Lean(GameObject held, float degrees)
        {
            if (held != null) held.transform.rotation = Quaternion.Euler(0f, 0f, degrees * Enemy.Direction);
        }

        private Transform FindPart(string partName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == partName) return t;
            return null;
        }

        /// <summary>Called by a potion that struck the player.</summary>
        public void FlaskHitPlayer() => Trace("potion hit player for 1");

        /// <summary>Called by a flask when it is gone; lands an acid pool when it hit the ground.</summary>
        public void FlaskFinished(Vector2? groundHit, Collider2D ground)
        {
            flasksInAir = Mathf.Max(0, flasksInAir - 1);
            if (!groundHit.HasValue || Enemy.IsDead || ground == null) return;
            pools.RemoveAll(p => p == null);
            while (pools.Count >= Mathf.Max(1, maxPools)) { Destroy(pools[0]); pools.RemoveAt(0); }
            pools.Add(AlchemistFlask.MakePool(groundHit.Value.x, ground, poolLifetime, potion));
            Trace("potion pool at " + groundHit.Value.x.ToString("0.0"));
        }

        public override void OnDeath()
        {
            base.OnDeath();
            if (rigReady)
            {
                // The death animation takes the arms from here (this behaviour stops updating).
                throwArm.Angle = stabArm.Angle = torso.Angle = 0f;
                if (knifePose != null) knifePose.Angle = 0f;
                throwArm.Apply(); stabArm.Apply(); torso.Apply(); knifePose?.Apply();
            }
            ClearPools();
        }

        public override void OnEncounterReset()
        {
            base.OnEncounterReset();
            engaged = false;
            ClearPools();
        }

        private void ClearPools()
        {
            foreach (GameObject p in pools) if (p != null) Destroy(p);
            pools.Clear();
        }

        /// <summary>
        /// Turns a weapon about the point where it is held (not about its own pivot), so the handle stays in the
        /// fist however far it turns. The weapon is not animated by the clips (only faded), so its rest pose is
        /// fixed.
        /// </summary>
        private sealed class GripTurn
        {
            private readonly Transform weapon;
            private readonly Vector3 grip, restPosition;
            private readonly Quaternion restRotation;
            public float Angle;

            public GripTurn(Transform weapon, Vector3 grip)
            {
                this.weapon = weapon;
                this.grip = grip;
                restPosition = weapon.localPosition;
                restRotation = weapon.localRotation;
            }

            public void Apply()
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, Angle);
                Vector3 p = grip + turn * (restPosition - grip);
                weapon.localPosition = new Vector3(p.x, p.y, restPosition.z);
                weapon.localRotation = turn * restRotation;
            }
        }

        /// <summary>
        /// A rotation added to a bone on top of the Animator's pose. If the current clip leaves the bone
        /// alone, the last animated pose is kept as the base, so the offset is never applied twice.
        /// </summary>
        private sealed class BoneOffset
        {
            private readonly Transform bone;
            private Quaternion basePose;
            private Quaternion written;
            public float Angle;

            public BoneOffset(Transform bone)
            {
                this.bone = bone;
                if (bone != null) basePose = written = bone.localRotation;
            }

            public void Apply()
            {
                if (bone == null) return;
                if (bone.localRotation != written) basePose = bone.localRotation;
                bone.localRotation = basePose * Quaternion.Euler(0f, 0f, Angle);
                written = bone.localRotation;
            }
        }
    }
}
