using System.Collections.Generic;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Gives a skeletal (Spriter) rig another variant's look: every sprite on the rig whose name matches one
    /// of <see cref="parts"/> is replaced by that part, all at once. Used for enemy variants that share a
    /// skeleton and animations with another variant (e.g. Villager Zombie 3 on the Villager Zombie 1 rig),
    /// so no clips or controllers are duplicated. Runs after the Animator, so animated sprite swaps
    /// (such as face changes) keep the variant's look.
    /// </summary>
    [DisallowMultipleComponent]
    public class RigSpriteSkin : MonoBehaviour
    {
        [Tooltip("The variant's body-part sprites, named like the rig's parts (Body, Head, Face 01, ...).")]
        [SerializeField] private Sprite[] parts = new Sprite[0];

        private SpriteRenderer[] renderers;
        private readonly Dictionary<string, Sprite> partsByName = new Dictionary<string, Sprite>();
        private readonly Dictionary<Sprite, Sprite> resolved = new Dictionary<Sprite, Sprite>();

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (Sprite part in parts)
            {
                if (part != null) partsByName[part.name] = part;
            }
            Remap();
        }

        private void LateUpdate()
        {
            Remap();
        }

        private void Remap()
        {
            if (renderers == null || partsByName.Count == 0) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                if (spriteRenderer == null) continue;

                Sprite sprite = spriteRenderer.sprite;
                if (sprite == null) continue;

                if (!resolved.TryGetValue(sprite, out Sprite replacement))
                {
                    replacement = partsByName.TryGetValue(sprite.name, out Sprite part) ? part : sprite;
                    resolved[sprite] = replacement;
                    resolved[replacement] = replacement;
                }

                if (replacement != sprite) spriteRenderer.sprite = replacement;
            }
        }
    }
}
