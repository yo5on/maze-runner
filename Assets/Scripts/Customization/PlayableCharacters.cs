using System.Collections.Generic;
using UnityEngine;

namespace MazeRunner.Customization
{
    /// <summary>
    /// One complete playable character from the Fallen Angels pack. All three share the same skeleton and
    /// animations; each has its own full set of 13 body-part sprites, which are always applied together.
    /// </summary>
    public sealed class PlayableCharacter
    {
        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>Folder under Resources/ holding this character's parts and Preview sprite.</summary>
        public string ResourceFolder { get; }

        /// <summary>True for the character the Player prefab is built with (its parts live in Assets/Vector Parts).</summary>
        public bool IsBuiltIn { get; }

        public PlayableCharacter(string id, string displayName, bool isBuiltIn)
        {
            Id = id;
            DisplayName = displayName;
            ResourceFolder = "Characters/" + id;
            IsBuiltIn = isBuiltIn;
        }
    }

    /// <summary>
    /// The list of selectable characters and the saved selection (PlayerPrefs, like the rest of the game's saves).
    /// </summary>
    public static class PlayableCharacters
    {
        public const string SaveKey = "SelectedCharacter";

        /// <summary>Sprite names of one character's parts, exactly as the pack names them.</summary>
        public static readonly string[] PartNames =
        {
            "Body", "Head", "Face 01", "Face 02", "Face 03",
            "Left Arm", "Left Hand", "Left Leg", "Right Arm", "Right Hand", "Right Leg",
            "Sword", "SlashFX"
        };

        private static readonly PlayableCharacter[] Characters =
        {
            new PlayableCharacter("Fallen_Angels_1", "Silver Angel", false),
            new PlayableCharacter("Fallen_Angels_2", "Hooded Angel", true),
            new PlayableCharacter("Fallen_Angels_3", "Crowned Angel", false),
        };

        private const int DefaultIndex = 1;

        public static IReadOnlyList<PlayableCharacter> All => Characters;

        public static PlayableCharacter Default => Characters[DefaultIndex];

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Id == id) return i;
            }
            return -1;
        }

        /// <summary>The saved character, or the default one if nothing (or something unknown) is saved.</summary>
        public static PlayableCharacter Selected
        {
            get
            {
                int index = IndexOf(PlayerPrefs.GetString(SaveKey, string.Empty));
                return index >= 0 ? Characters[index] : Default;
            }
        }

        public static void SaveSelection(PlayableCharacter character)
        {
            if (character == null) return;
            PlayerPrefs.SetString(SaveKey, character.Id);
            PlayerPrefs.Save();
        }

        public static Sprite LoadPreview(PlayableCharacter character)
        {
            return character != null ? Resources.Load<Sprite>(character.ResourceFolder + "/Preview") : null;
        }

        /// <summary>
        /// Loads every part of a character. Returns false unless all parts are present, so a character with a
        /// missing file is never shown half-swapped; callers then fall back to the built-in character.
        /// </summary>
        public static bool TryLoadParts(PlayableCharacter character, out Dictionary<string, Sprite> parts)
        {
            parts = null;
            if (character == null || character.IsBuiltIn) return false;

            var loaded = new Dictionary<string, Sprite>(PartNames.Length);
            foreach (Sprite sprite in Resources.LoadAll<Sprite>(character.ResourceFolder))
            {
                if (sprite != null) loaded[sprite.name] = sprite;
            }

            foreach (string part in PartNames)
            {
                if (!loaded.ContainsKey(part))
                {
                    Debug.LogWarning($"Character '{character.Id}' is missing part '{part}'; using {Default.DisplayName} instead.");
                    return false;
                }
            }

            parts = loaded;
            return true;
        }
    }
}
