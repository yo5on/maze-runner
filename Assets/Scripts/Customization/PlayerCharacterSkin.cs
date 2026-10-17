using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Shows the selected playable character on the Player. In a level, the Player first gets the character's
    /// rig (its skeleton and animations; see <see cref="PlayerRigSettings"/>); the Player's controller,
    /// physics, collider, health and combat stay exactly as they are. Characters that are a part set for that
    /// rig then have all of the rig's sprites replaced at once with their own. Remapping runs after the Animator
    /// each frame, so animation-driven sprite swaps (such as face changes) also show the selected character.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerCharacterSkin : MonoBehaviour
    {
        private static readonly HashSet<string> PartNameSet = new HashSet<string>(PlayableCharacters.PartNames);

        private SpriteRenderer[] renderers;
        private Dictionary<string, Sprite> targetParts;                  // null = built-in character
        private readonly Dictionary<string, Sprite> builtInParts = new Dictionary<string, Sprite>();
        private readonly HashSet<Sprite> variantSprites = new HashSet<Sprite>();
        private readonly Dictionary<Sprite, Sprite> resolved = new Dictionary<Sprite, Sprite>();

        public PlayableCharacter Current { get; private set; }

        /// <summary>Id of the rig the Player currently wears.</summary>
        private string rigId = PlayerRigSettings.DefaultRigId;

        // Every scene that has a Player gets this component, so the selection applies in Levels 1-3 and
        // after any reload or restart without editing the scenes or the Player prefab.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The Player prefab's root carries PlayerController; its tag is only set in some scenes.
            var controller = FindAnyObjectByType<Player.PlayerController>();
            GameObject player = controller != null ? controller.gameObject : GameObject.FindGameObjectWithTag("Player");

            if (player == null) return;

            if (scene.name.StartsWith("Level")) EnsureCharacterRig(player, PlayableCharacters.Selected);

            if (player.GetComponent<PlayerCharacterSkin>() == null)
            {
                player.AddComponent<PlayerCharacterSkin>();
            }
        }

        /// <summary>
        /// Puts the selected character's rig on the Player. Level 1's Player is saved with the Fallen Angels rig
        /// and a PlayerAnimationController; Levels 2 and 3 were saved without them. A Player without a rig, or
        /// with a different one, gets the right rig placed exactly where Level 1's sits, and the existing
        /// PlayerAnimationController is pointed at it (PlayerHealth keeps its reference to that component).
        /// </summary>
        private static void EnsureCharacterRig(GameObject player, PlayableCharacter character)
        {
            var settings = Resources.Load<PlayerRigSettings>(PlayerRigSettings.ResourcePath);
            if (settings == null) return;

            Transform existing = player.transform.Find(PlayerRigSettings.RigName);
            string existingRigId = null;
            if (existing != null)
            {
                var marker = existing.GetComponent<PlayerRigMarker>();
                existingRigId = marker != null ? marker.RigId : PlayerRigSettings.DefaultRigId;
            }

            string wantedRigId = character != null ? character.RigId : PlayerRigSettings.DefaultRigId;
            if (existing != null && existingRigId == wantedRigId) return;

            if (!settings.TryGetRig(wantedRigId, out GameObject prefab, out RuntimeAnimatorController controller))
            {
                // Unknown rig: keep whatever the Player has, or fall back to the default rig.
                if (existing != null) return;
                wantedRigId = PlayerRigSettings.DefaultRigId;
                if (!settings.TryGetRig(wantedRigId, out prefab, out controller)) return;
            }

            Vector3 localPosition = existing != null ? existing.localPosition : settings.LocalPosition;
            Vector3 localScale = existing != null ? existing.localScale : settings.LocalScale;
            localScale.x = Mathf.Abs(localScale.x);
            if (existing != null) DestroyImmediate(existing.gameObject);

            GameObject rig = Instantiate(prefab, player.transform, false);
            rig.name = PlayerRigSettings.RigName;
            rig.transform.localPosition = localPosition;
            rig.transform.localRotation = Quaternion.identity;
            rig.transform.localScale = localScale;
            rig.AddComponent<PlayerRigMarker>().RigId = wantedRigId;

            var rigAnimator = rig.GetComponent<Animator>();
            if (rigAnimator != null && controller != null) rigAnimator.runtimeAnimatorController = controller;

            var animationController = player.GetComponent<Player.PlayerAnimationController>();
            if (animationController == null)
            {
                // Its Awake finds the rig by name, so add it after the rig exists.
                player.AddComponent<Player.PlayerAnimationController>();
            }
            else
            {
                animationController.UseRig(rigAnimator);
            }
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Transform rig = transform.Find(PlayerRigSettings.RigName);
            var marker = rig != null ? rig.GetComponent<PlayerRigMarker>() : null;
            rigId = marker != null ? marker.RigId : PlayerRigSettings.DefaultRigId;
            Apply(PlayableCharacters.Selected);
        }

        /// <summary>Switches the Player to a whole character. Unknown or incomplete characters fall back to the built-in one.</summary>
        public void Apply(PlayableCharacter character)
        {
            if (character == null) character = PlayableCharacters.Default;

            // Part sets only fit their own skeleton. A character for another rig (e.g. on the Customization
            // screen, where the Player keeps the rig it was saved with) leaves this rig's look unchanged.
            if (character.RigId != rigId)
            {
                character = PlayableCharacters.BuiltInFor(rigId);
            }

            if (PlayableCharacters.TryLoadParts(character, out Dictionary<string, Sprite> parts))
            {
                targetParts = parts;
                foreach (Sprite sprite in parts.Values) variantSprites.Add(sprite);
            }
            else
            {
                targetParts = null;
                if (!character.IsBuiltIn) character = PlayableCharacters.BuiltInFor(rigId);
            }

            Current = character;
            resolved.Clear();
            Remap();
        }

        private void LateUpdate()
        {
            Remap();
        }

        private void Remap()
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                if (spriteRenderer == null) continue;

                Sprite sprite = spriteRenderer.sprite;
                if (sprite == null) continue;

                if (!resolved.TryGetValue(sprite, out Sprite replacement))
                {
                    replacement = Resolve(sprite);
                    resolved[sprite] = replacement;
                }

                if (replacement != sprite) spriteRenderer.sprite = replacement;
            }
        }

        private Sprite Resolve(Sprite sprite)
        {
            string partName = sprite.name;
            if (!PartNameSet.Contains(partName)) return sprite;

            // Remember the built-in sprite for each part so the Player can be switched back to it.
            if (!variantSprites.Contains(sprite) && !builtInParts.ContainsKey(partName))
            {
                builtInParts[partName] = sprite;
            }

            if (targetParts != null && targetParts.TryGetValue(partName, out Sprite selected)) return selected;
            return builtInParts.TryGetValue(partName, out Sprite builtIn) ? builtIn : sprite;
        }
    }
}
