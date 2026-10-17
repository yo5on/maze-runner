using System.Collections.Generic;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>
    /// Tracks which goblins patrol the same platform collider. Goblins register once when they spawn and
    /// unregister when they die, are disabled or destroyed, so nothing is scanned per frame. When a shared
    /// platform drops to a single goblin, that survivor is told to patrol the whole platform.
    /// </summary>
    internal static class GoblinPlatformGroup
    {
        private static readonly Dictionary<Collider2D, List<GoblinEnemy>> Groups = new Dictionary<Collider2D, List<GoblinEnemy>>();

        // Static state survives scene loads (and play sessions with domain reload disabled), so start clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Groups.Clear();
        }

        public static void Register(Collider2D platform, GoblinEnemy goblin)
        {
            if (platform == null || goblin == null) return;

            if (!Groups.TryGetValue(platform, out List<GoblinEnemy> members))
            {
                members = new List<GoblinEnemy>(2);
                Groups.Add(platform, members);
            }

            if (!members.Contains(goblin)) members.Add(goblin);
        }

        public static void Unregister(Collider2D platform, GoblinEnemy goblin)
        {
            if (ReferenceEquals(platform, null) || !Groups.TryGetValue(platform, out List<GoblinEnemy> members)) return;

            bool wasShared = members.Count >= 2;
            if (!members.Remove(goblin)) return;

            // Drop entries for goblins that were destroyed without unregistering (defensive; OnDisable covers it).
            members.RemoveAll(m => m == null);

            if (members.Count == 0)
            {
                Groups.Remove(platform);
                return;
            }

            if (wasShared && members.Count == 1)
            {
                members[0].OnPlatformPartnerLost(platform);
            }
        }
    }
}
