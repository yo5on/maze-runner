using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// A shield held in the enemy's free hand, the one that does not carry the sword. It is parented to the
    /// rig, so it follows every animation (idle, walk, attack, hurt, death) and flips with the body.
    /// - If the rig has its own "Shield" part (Skeleton Crusader), the shield takes that part's place on its
    ///   animated bone, at the artist's grip and size, and the built-in part is hidden so only one shows.
    /// - Otherwise it is gripped by the free hand, its back edge over the fist.
    /// Raised, the shield arm is lifted forward (on top of whatever the animation does with it) and the
    /// shield stands upright in front of the chest; lowered, the arm hangs as animated and the shield tilts
    /// down about the grip.
    /// </summary>
    public class HeldShield : MonoBehaviour
    {
        private const float LoweredAngle = -40f;
        private const float ArmRaise = 65f;    // degrees the upper arm swings forward while guarding
        private const float BlendSpeed = 5f;   // raise/lower in about 0.2 s
        private const float BraceLift = 25f;   // extra lift while bracing against a blocked hit
        private const float BraceTime = 0.3f;

        private Transform visual;
        private Transform arm;
        private float raise;                   // 0 lowered .. 1 raised
        private float braceTimer;
        private Quaternion armBase;
        private Quaternion armWritten;

        public bool Raised { get; set; }

        /// <summary>The shield's sprite object (what a block kicks forward).</summary>
        public GameObject Visual => visual != null ? visual.gameObject : null;

        /// <param name="rig">The enemy root that holds the rig.</param>
        /// <param name="sprite">Shield art; null draws a stone bar instead.</param>
        /// <param name="height">World height for a hand-held shield (a rig's own shield keeps the artist's size).</param>
        public static HeldShield Attach(Transform rig, Sprite sprite, float height)
        {
            SpriteRenderer builtIn = null, sword = null, hand = null;
            foreach (SpriteRenderer sr in rig.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name == "Shield") builtIn = sr;
                else if (sr.name == "Sword") sword = sr;
            }
            foreach (SpriteRenderer sr in rig.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.name.EndsWith("Hand")) continue;
                // The free hand is the one not on the sword's bone.
                if (sword == null || sr.transform.parent != sword.transform.parent) { hand = sr; break; }
            }

            Transform anchor;
            Vector3 grip;
            float forward = 0f;
            SpriteRenderer reference = builtIn != null ? builtIn : hand;
            if (builtIn != null && builtIn.sprite != null)
            {
                anchor = builtIn.transform;
                grip = builtIn.sprite.bounds.center;
                // At the artist's size, but never smaller than a hand-held shield (the built-in one is small).
                height = Mathf.Max(height, builtIn.sprite.bounds.size.y * Mathf.Abs(anchor.lossyScale.y));
                builtIn.enabled = false;
            }
            else if (hand != null && hand.sprite != null)
            {
                anchor = hand.transform;
                grip = hand.sprite.bounds.center;
                forward = 0.32f; // of the shield's width: the fist holds the back of the shield
            }
            else
            {
                anchor = rig;
                grip = new Vector3(0.4f, height, 0f) / Mathf.Max(0.01f, Mathf.Abs(rig.lossyScale.y));
            }

            var gripGo = new GameObject("ShieldGrip");
            gripGo.transform.SetParent(anchor, false);
            gripGo.transform.localPosition = new Vector3(grip.x, grip.y, -0.002f);
            gripGo.transform.localRotation = Quaternion.identity;

            var visualGo = new GameObject("HeldShield");
            visualGo.transform.SetParent(gripGo.transform, false);
            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : TelegraphArt.Capsule;
            if (sprite == null) renderer.color = new Color32(0xaf, 0xae, 0xa2, 0xe0);
            if (reference != null)
            {
                renderer.sortingLayerID = reference.sortingLayerID;
                renderer.sortingOrder = reference.sortingOrder + 1; // in front of the arm that holds it
            }

            Bounds art = renderer.sprite.bounds;
            float size = height / Mathf.Max(0.0001f, art.size.y * Mathf.Abs(anchor.lossyScale.y));
            gripGo.transform.localScale = new Vector3(size, size, 1f);
            visualGo.transform.localPosition = new Vector3(forward * art.size.x - art.center.x, -art.center.y, 0f);

            var held = gripGo.AddComponent<HeldShield>();
            held.visual = visualGo.transform;
            held.arm = UpperArm(anchor, rig);
            if (held.arm != null) held.armBase = held.armWritten = held.arm.localRotation;
            held.Apply();
            return held;
        }

        /// <summary>The upper-arm bone above the grip: the bone on the torso bone (the rig root's child).</summary>
        private static Transform UpperArm(Transform anchor, Transform rig)
        {
            Transform t = anchor;
            while (t != null && t.parent != null && t.parent.parent != null && t.parent.parent != rig) t = t.parent;
            return t != null && t.parent != null && t.parent.parent == rig && t != anchor ? t : null;
        }

        /// <summary>A blocked hit: the arm drives the shield up and forward, then settles back into the guard.</summary>
        public void Brace() => braceTimer = BraceTime;

        // After the Animator has posed the rig for this frame.
        private void LateUpdate()
        {
            raise = Mathf.MoveTowards(raise, Raised ? 1f : 0f, BlendSpeed * Time.deltaTime);
            braceTimer = Mathf.Max(0f, braceTimer - Time.deltaTime);
            Apply();
        }

        private void Apply()
        {
            float lift = 0f;
            if (arm != null)
            {
                // The animation's pose for this frame, or the last one if this clip leaves the arm alone
                // (so the lift is never added twice).
                if (arm.localRotation != armWritten) armBase = arm.localRotation;
                float brace = Mathf.Sin(braceTimer / BraceTime * Mathf.PI); // out and back
                lift = ArmRaise * raise + BraceLift * brace;
                arm.localRotation = armBase * Quaternion.Euler(0f, 0f, lift);
                armWritten = arm.localRotation;
            }
            // Upright when raised (cancelling the arm's lift), tilted down when lowered.
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(LoweredAngle, 0f, raise) - lift);
        }
    }
}
