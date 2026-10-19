using System.Collections;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Bloody Alchemist 1: a cautious ranged enemy with a weapon in each hand - potions in the left, the
    /// knife in the right. It notices the player from 9 units (and a little above or below), keeps 3.5-6.5
    /// units away - backing off when approached, stepping closer when the player is far - and stands its
    /// ground with its back to a ledge.
    /// Signature: Potion Throw (left arm). Wind-up 0.5 s: the warning shows, a potion appears in the left
    /// hand and the arm swings up and back behind the head while the body leans back; then the arm whips
    /// over and the potion leaves the hand at the top of the swing, from where the hand is, tumbling along a
    /// lobbed arc aimed where the player stands. A direct hit deals 1 damage; where it lands it shatters and
    /// leaves a spilled-potion pool (a hazard strip) for 2.5 s. At most two potions in the air and one pool
    /// per alchemist; cooldown 2.8 s. The potion is slow and arcs, so it can be outrun or jumped over.
    /// Knife Stab (right arm): when the player gets within reach on its floor - also while the potion is
    /// cooling down - it draws the knife back (0.3 s, warning shown), thrusts it straight at the player and
    /// pulls it back. It hits once, for 1, at full extension; cooldown 1.6 s.
    /// The arms are posed on the rig's bones after the Animator each frame, so the body keeps animating.
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
        [SerializeField] private float stabReach = 1.6f;
        [SerializeField] private float stabWindUp = 0.3f;
        [SerializeField] private float stabCooldown = 1.6f;
        [Tooltip("Potion art (Assets/Sprites/Weapons/Potions).")]
        [SerializeField] private PotionVisual potion = new PotionVisual();

        // Arm angles, in degrees added to the animated pose (0 = hanging at the side, as animated; negative
        // swings the hand backwards first, so the throw goes back, up behind the head and over the top).
        private const float ThrowCocked = -145f;  // left arm drawn back and up behind the head
        private const float ThrowRelease = -234f; // the arm comes over the top to up-front: the potion leaves the hand
        private const float ThrowFollow = -319f;  // follow-through, down in front
        private const float ThrowRest = -360f;    // the same as hanging at the side (0), reached going forward
        private const float WhipTime = 0.12f;
        private const float StabDrawn = -35f;     // right arm pulled back
        private const float StabExtended = 100f;  // right arm straight out in front
        private const float ThrustTime = 0.07f;
        private const float BodyLean = 8f;        // positive leans back, away from the player
        private const float TumbleSpeed = 480f;   // degrees per second, end over end in the throw direction
        private const float BottleLean = 25f;     // the bottle in hand leans back on the wind-up, forward on the whip

        private bool engaged;
        private float nextThrow;
        private float nextStab;
        private int flasksInAir;
        private GameObject pool;

        private Transform leftHand;
        private BoneOffset leftArm, rightArm, knifePose, torso;
        private bool rigReady;

        protected override void Start()
        {
            base.Start();
            SetUpRig();
        }

        /// <summary>
        /// Finds the arm bones and moves the knife from the left hand (the potion hand, whose arm the throw
        /// swings) to the right hand, keeping its grip.
        /// </summary>
        private void SetUpRig()
        {
            leftHand = FindPart("Left Hand");
            Transform rightHand = FindPart("Right Hand");
            Transform knife = FindPart("Sword");
            if (leftHand == null || rightHand == null || leftHand.parent == null || rightHand.parent == null) return;

            leftArm = new BoneOffset(leftHand.parent.parent);   // bone_002, the left upper arm
            rightArm = new BoneOffset(rightHand.parent.parent); // bone_000, the right upper arm
            torso = new BoneOffset(leftHand.parent.parent != null ? leftHand.parent.parent.parent : null);

            if (knife != null && knife.parent == leftHand.parent)
            {
                // The knife's grip relative to the left hand, carried over to the right hand (in bone space,
                // so it does not matter which way the alchemist faces).
                Quaternion turn = Quaternion.Inverse(leftHand.localRotation) * knife.localRotation;
                Vector3 grip = Quaternion.Inverse(leftHand.localRotation) * (knife.localPosition - leftHand.localPosition);
                knife.SetParent(rightHand.parent, false);
                knife.localRotation = rightHand.localRotation * turn;
                Vector3 local = rightHand.localPosition + rightHand.localRotation * grip;
                // Just behind the right hand (the fist closes over the handle), in front of the body.
                knife.localPosition = new Vector3(local.x, local.y, rightHand.localPosition.z + 0.0005f);
            }
            if (knife != null) knifePose = new BoneOffset(knife);
            rigReady = true;
        }

        // After the Animator has posed the rig for this frame.
        private void LateUpdate()
        {
            if (!rigReady) return;
            if (!Busy)
            {
                // Back to the animated pose (also after a throw or stab is cut short).
                leftArm.Angle = Mathf.MoveTowardsAngle(leftArm.Angle, 0f, 900f * Time.deltaTime);
                rightArm.Angle = Mathf.MoveTowardsAngle(rightArm.Angle, 0f, 900f * Time.deltaTime);
                torso.Angle = Mathf.MoveTowards(torso.Angle, 0f, 120f * Time.deltaTime);
            }
            knifePose.Angle = -rightArm.Angle; // the blade keeps pointing at the target as the arm swings
            leftArm.Apply();
            rightArm.Apply();
            torso.Apply();
            knifePose.Apply();
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
            else if (engaged && Time.time >= nextThrow && flasksInAir < 2 && Mathf.Abs(DX) >= 1.5f)
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
            Enemy.ReturnToLocomotion();
            Warn(throwWindUp);
            GameObject held = HoldPotion();

            // Wind-up: the left arm swings back and up behind the head, the body leans back.
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
                Destroy(held);
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

        private void SetThrowPose(float arm, float lean)
        {
            if (!rigReady) return;
            leftArm.Angle = arm;
            torso.Angle = lean;
        }

        private void SetStabPose(float arm, float lean)
        {
            if (!rigReady) return;
            rightArm.Angle = arm;
            torso.Angle = lean;
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The potion in the left hand during the wind-up; it moves with the arm and is removed if the throw is cancelled.</summary>
        private GameObject HoldPotion()
        {
            if (potion == null || potion.bottle == null) return null;
            Transform hand = leftHand != null ? leftHand : transform;
            var go = new GameObject("HeldPotion");
            go.transform.SetParent(hand, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = potion.bottle;
            var handRenderer = hand.GetComponent<SpriteRenderer>();
            if (handRenderer != null)
            {
                // Drawn over the rig: the left arm is the far arm, and drawn back behind the head the bottle
                // would otherwise vanish behind it for the whole wind-up.
                sr.sortingLayerID = handRenderer.sortingLayerID;
                sr.sortingOrder = handRenderer.sortingOrder + 1;
            }
            else sr.sortingOrder = 20;
            // Same size as in flight, so nothing jumps at the release.
            float world = AlchemistFlask.BottleHeight / Mathf.Max(0.01f, potion.bottle.bounds.size.y);
            Vector3 parent = hand.lossyScale;
            go.transform.localScale = new Vector3(world / Mathf.Max(0.01f, Mathf.Abs(parent.x)), world / Mathf.Max(0.01f, Mathf.Abs(parent.y)), 1f);
            Vector3 palm = handRenderer != null && handRenderer.sprite != null ? handRenderer.sprite.bounds.center : Vector3.zero;
            go.transform.localPosition = new Vector3(palm.x, palm.y, -0.0005f);
            go.transform.localRotation = Quaternion.identity;
            TrackTelegraph(go);
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
            if (pool != null) Destroy(pool);
            pool = AlchemistFlask.MakePool(groundHit.Value.x, ground, poolLifetime, potion);
            Trace("potion pool at " + groundHit.Value.x.ToString("0.0"));
        }

        public override void OnDeath()
        {
            base.OnDeath();
            if (rigReady)
            {
                // The death animation takes the arms from here (this behaviour stops updating).
                leftArm.Angle = rightArm.Angle = torso.Angle = knifePose.Angle = 0f;
                leftArm.Apply(); rightArm.Apply(); torso.Apply(); knifePose.Apply();
            }
            if (pool != null) Destroy(pool);
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
