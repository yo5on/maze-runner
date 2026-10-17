using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeRunner.Customization
{
    /// <summary>
    /// Shows the selected playable character on the Player. The Player's rig, animations, physics, collider,
    /// health and combat stay exactly as they are; only the sprites of the built-in character are replaced,
    /// all at once, with the selected character's matching sprites. Remapping runs after the Animator each
    /// frame, so animation-driven sprite swaps (such as face changes) also show the selected character.
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

            if (scene.name.StartsWith("Level")) EnsureCharacterRig(player);

            if (player.GetComponent<PlayerCharacterSkin>() == null)
            {
                player.AddComponent<PlayerCharacterSkin>();
            }
        }

        /// <summary>
        /// Level 1's Player carries the Fallen Angels rig and a PlayerAnimationController; Levels 2 and 3 were
        /// saved without them, so their Player had no character to show. Give such a Player the same rig,
        /// placed exactly as in Level 1, so every level shows the selected character with the same animations.
        /// </summary>
        private static void EnsureCharacterRig(GameObject player)
        {
            if (player.transform.Find(PlayerRigSettings.RigName) != null) return;

            var settings = Resources.Load<PlayerRigSettings>(PlayerRigSettings.ResourcePath);
            if (settings == null || settings.RigPrefab == null) return;

            GameObject rig = Instantiate(settings.RigPrefab, player.transform, false);
            rig.name = PlayerRigSettings.RigName;
            rig.transform.localPosition = settings.LocalPosition;
            rig.transform.localRotation = Quaternion.identity;
            rig.transform.localScale = settings.LocalScale;

            // Its Awake finds the rig by name, so add it after the rig exists.
            if (player.GetComponent<Player.PlayerAnimationController>() == null)
            {
                player.AddComponent<Player.PlayerAnimationController>();
            }
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Apply(PlayableCharacters.Selected);
        }

        /// <summary>Switches the Player to a whole character. Unknown or incomplete characters fall back to the built-in one.</summary>
        public void Apply(PlayableCharacter character)
        {
            if (character == null) character = PlayableCharacters.Default;

            if (PlayableCharacters.TryLoadParts(character, out Dictionary<string, Sprite> parts))
            {
                targetParts = parts;
                foreach (Sprite sprite in parts.Values) variantSprites.Add(sprite);
            }
            else
            {
                targetParts = null;
                if (!character.IsBuiltIn) character = PlayableCharacters.Default;
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
