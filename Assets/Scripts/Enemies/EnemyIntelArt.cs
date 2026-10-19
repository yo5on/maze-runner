using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Artwork the Enemy Intel panel shows for a signature ability in place of its line icon - the
    /// potion an enemy throws or the shield it raises. Lives at Resources/EnemyIntelArt and is filled by
    /// Maze Runner > Set Up Enemy Weapons; entries are matched by <see cref="EnemyIntelEntry.Id"/>.
    /// </summary>
    public class EnemyIntelArt : ScriptableObject
    {
        [System.Serializable]
        public struct Icon
        {
            public string id;
            public Sprite sprite;
        }

        public Icon[] icons = new Icon[0];

        private static EnemyIntelArt loaded;
        private static bool tried;

        public static Sprite IconFor(string id)
        {
            if (!tried) { loaded = Resources.Load<EnemyIntelArt>("EnemyIntelArt"); tried = true; }
            if (loaded == null) return null;
            foreach (Icon icon in loaded.icons) if (icon.id == id) return icon.sprite;
            return null;
        }
    }
}
