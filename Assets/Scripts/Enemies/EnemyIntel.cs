using System;
using System.Globalization;
using UnityEngine;

namespace MazeRunner.Enemies
{
    /// <summary>How an enemy's signature move reaches the player. Drives the icon and tag in the intel panel.</summary>
    public enum AttackStyle { Melee, Combo, Dash, Charge, Ranged, Area, Counter }

    /// <summary>
    /// One figure shown for a signature move. <see cref="Fields"/> names the serialized fields it comes
    /// from (on the enemy's <see cref="EnemyBehaviour"/>, or "GoblinEnemy.x" on the shared body), so the
    /// editor check (Maze Runner > Verify Enemy Intel) can compare it with the prefabs. Values written
    /// into the behaviour code itself (most damage numbers) have no fields and are checked by reading.
    /// </summary>
    public readonly struct IntelStat
    {
        public readonly string Label;
        public readonly float Min;
        public readonly float Max;
        public readonly string Unit;
        public readonly string[] Fields;
        public readonly string Text;   // replaces the formatted number when set (e.g. "1 + 1")
        public readonly bool Severe;   // a 2-damage hit: drawn in crimson

        public IntelStat(string label, float min, float max, string unit, string[] fields, string text = null, bool severe = false)
        {
            Label = label; Min = min; Max = max; Unit = unit; Fields = fields ?? new string[0]; Text = text; Severe = severe;
        }

        public static IntelStat Seconds(string label, float value, string field) =>
            new IntelStat(label, value, value, "s", new[] { field });

        public static IntelStat SecondsRange(string label, float min, float max, string minField, string maxField) =>
            new IntelStat(label, min, max, "s", new[] { minField, maxField });

        public static IntelStat Distance(string label, float value, string field) =>
            new IntelStat(label, value, value, "", new[] { field });

        public static IntelStat DistanceRange(string label, float min, float max, string minField, string maxField) =>
            new IntelStat(label, min, max, "", new[] { minField, maxField });

        public static IntelStat Damage(int value) =>
            new IntelStat("DAMAGE", value, value, "", null, null, value >= 2);

        public static IntelStat DamageText(string text, bool severe) =>
            new IntelStat("DAMAGE", 0f, 0f, "", null, text, severe);

        public string Value
        {
            get
            {
                if (Text != null) return Text;
                string n = Number(Min);
                if (!Mathf.Approximately(Min, Max)) n += "-" + Number(Max);
                return Unit.Length > 0 ? n + " " + Unit : n;
            }
        }

        private static string Number(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>What the player can learn about one enemy type: its look, role and signature move.</summary>
    public sealed class EnemyIntelEntry
    {
        public string Id;            // portrait file in Resources/EnemyPortraits
        public string Prefab;        // the prefab the values come from (editor check)
        public Type Behaviour;       // matches live enemies to this entry
        public string Name;
        public string Role;
        public int Health;           // GoblinEnemy.maxHealth on the prefab
        public string Ability;
        public AttackStyle Style;
        public string Description;
        public IntelStat[] Stats;
        public string Counter;
    }

    /// <summary>
    /// The intel on every enemy type, written from the behaviours in Enemies/Behaviours and the values
    /// on their prefabs (EnemyPersonalitySetup lists which prefab has which behaviour). Distances are in
    /// world units; the player is about one unit wide. Keep this in step with the behaviours: the
    /// "Verify Enemy Intel" editor command reports any figure that no longer matches a prefab.
    /// </summary>
    public static class EnemyIntel
    {
        public static readonly EnemyIntelEntry[] All =
        {
            new EnemyIntelEntry
            {
                Id = "villager_zombie_1", Prefab = "Assets/Enemies/Zombie_Villager/Villager_Zombie_1.prefab",
                Behaviour = typeof(ZombieShamblerBehaviour),
                Name = "Villager Zombie 1", Role = "SLOW, RELENTLESS PURSUER", Health = 2,
                Ability = "Heavy Bite", Style = AttackStyle.Melee,
                Description = "Stops, then bites at close range. A bite deals 1 damage and slows you to half speed for 1.5 s.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.55f, "biteWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 2.6f, "biteCooldown"),
                    IntelStat.Distance("RANGE", 1.4f, "biteRange"),
                },
                Counter = "Jump away from it, or over it, while the warning shows.",
            },
            new EnemyIntelEntry
            {
                Id = "villager_zombie_3", Prefab = "Assets/Enemies/Zombie_Villager/Villager_Zombie_3.prefab",
                Behaviour = typeof(ZombieRusherBehaviour),
                Name = "Villager Zombie 3", Role = "ERRATIC RUSHER", Health = 2,
                Ability = "Lunge", Style = AttackStyle.Dash,
                Description = "Freezes in a crouch, then lunges up to 3.5 units. A hit deals 1 damage and knocks you back.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.45f, "lungeWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 3f, "lungeCooldown"),
                    IntelStat.DistanceRange("RANGE", 2.2f, 4.5f, "lungeMin", "lungeMax"),
                },
                Counter = "It needs 0.9 s to recover after a lunge. Stomp it then.",
            },
            new EnemyIntelEntry
            {
                Id = "skeleton_warrior_2", Prefab = "Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_2.prefab",
                Behaviour = typeof(ShieldWarriorBehaviour),
                Name = "Skeleton Warrior 2", Role = "DISCIPLINED GUARD", Health = 3,
                Ability = "Shield Guard", Style = AttackStyle.Counter,
                Description = "Raises its kite shield, then lowers it for a short opening. A stomp on its front is blocked and answered with a counter-slash. It is slow to turn while guarding.",
                Stats = new[]
                {
                    IntelStat.SecondsRange("GUARD", 1.8f, 2.6f, "guardMin", "guardMax"),
                    IntelStat.Seconds("OPENING", 1.2f, "openingTime"),
                    IntelStat.Seconds("COUNTER", 0.3f, "counterWindUp"),
                    IntelStat.Damage(1),
                },
                Counter = "Stomp in the opening, or land on its back before it turns.",
            },
            new EnemyIntelEntry
            {
                Id = "skeleton_warrior_3", Prefab = "Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_3.prefab",
                Behaviour = typeof(DuelistBehaviour),
                Name = "Skeleton Warrior 3", Role = "AGGRESSIVE DUELIST", Health = 3,
                Ability = "Two-Hit Combo", Style = AttackStyle.Combo,
                Description = "Advances at a run and strikes twice, stepping forward between the slashes. Each slash deals 1 damage.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.35f, "comboWarning"),
                    IntelStat.DamageText("1 + 1", false),
                    IntelStat.Seconds("COOLDOWN", 2.2f, "cooldown"),
                    IntelStat.Distance("RANGE", 1.5f, "comboRange"),
                },
                Counter = "It is winded for 1.2 s after the combo. Stomp it then.",
            },
            new EnemyIntelEntry
            {
                Id = "bloody_alchemist_1", Prefab = "Assets/Enemies/Bloody_Alchemist/Bloody_Alchemist_1.prefab",
                Behaviour = typeof(AlchemistBehaviour),
                Name = "Bloody Alchemist 1", Role = "CAUTIOUS RANGED THROWER", Health = 2,
                Ability = "Potion Throw", Style = AttackStyle.Ranged,
                Description = "Keeps its distance and lobs a potion at where you stand. A direct hit deals 1 damage; the spilled potion hurts for 2.5 s.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.5f, "throwWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 2.8f, "throwCooldown"),
                    IntelStat.Distance("SIGHT", 9f, "noticeRange"),
                },
                Counter = "The potion arcs slowly: outrun it or jump it, then close in.",
            },
            new EnemyIntelEntry
            {
                Id = "skeleton_crusader_1", Prefab = "Assets/Enemies/Skeleton_Crusader/Skeleton_Crusader_1.prefab",
                Behaviour = typeof(CrusaderBehaviour),
                Name = "Skeleton Crusader 1", Role = "ARMOURED GUARDIAN", Health = 3,
                Ability = "Shield Charge", Style = AttackStyle.Charge,
                Description = "Holds its post behind a tower shield that blocks stomps on its front, then marks a lane and charges up to 4 units: 1 damage and a knock-back.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.8f, "chargeWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 4f, "cooldown"),
                    IntelStat.DistanceRange("RANGE", 2.2f, 6f, "chargeMin", "chargeMax"),
                },
                Counter = "Its shield is down during the charge and for 1.4 s after. Or land on its back.",
            },
            new EnemyIntelEntry
            {
                Id = "wraith_3", Prefab = "Assets/Enemies/Wraith/Wraith_3.prefab",
                Behaviour = typeof(WraithPhaseBehaviour),
                Name = "Wraith 3", Role = "ELUSIVE PHANTOM", Health = 3,
                Ability = "Phase Dash", Style = AttackStyle.Dash,
                Description = "Fades, then phases straight through you for up to 3.5 units, dealing 1 damage. It cannot be hurt while phased.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.4f, "dashWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 3.5f, "cooldown"),
                    IntelStat.DistanceRange("RANGE", 1.5f, 4f, "dashMin", "dashMax"),
                },
                Counter = "It darts away from stomps. Strike in the 0.7 s after a dash.",
            },
            new EnemyIntelEntry
            {
                Id = "ogre", Prefab = "Assets/Enemies/Ogre/Ogre_Enemy.prefab",
                Behaviour = typeof(OgreBehaviour),
                Name = "Ogre", Role = "HEAVY BRUTE", Health = 4,
                Ability = "Ground Slam", Style = AttackStyle.Area,
                Description = "A slow, heavy swing that marks 3 units of ground in front of it. Anyone standing in the zone takes 2 damage and is thrown back.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.9f, "windUp"),
                    new IntelStat("DAMAGE", 2f, 2f, "", new[] { "slamDamage" }, null, true),
                    IntelStat.Seconds("COOLDOWN", 3.5f, "cooldown"),
                    IntelStat.Distance("RANGE", 2.8f, "slamRange"),
                },
                Counter = "Be in the air or outside the zone. Stomps never make it flinch.",
            },
            new EnemyIntelEntry
            {
                Id = "goblin", Prefab = "Assets/Enemies/Goblin/Goblin_Enemy.prefab",
                Behaviour = typeof(GoblinSkirmisherBehaviour),
                Name = "Regular Goblin", Role = "CUNNING SKIRMISHER", Health = 2,
                Ability = "Flanking Dash", Style = AttackStyle.Dash,
                Description = "Dashes in and slashes for 1 damage when you move away from it. When you drop on it, it may slide about 2 units clear.",
                Stats = new[]
                {
                    new IntelStat("WIND-UP", 0.2f, 0.2f, "s", null),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 3.5f, "dashCooldown"),
                    IntelStat.DistanceRange("RANGE", 2f, 4.5f, "dashMin", "dashMax"),
                },
                Counter = "Its slide has a 3 s cooldown, so a quick second stomp lands.",
            },
            new EnemyIntelEntry
            {
                Id = "ogre_warlord", Prefab = "Assets/Enemies/Ogre/Ogre_Warlord.prefab",
                Behaviour = typeof(OgreWarlordBehaviour),
                Name = "Ogre Warlord", Role = "BOSS  /  LEVEL 1", Health = 6,
                Ability = "Bull Charge", Style = AttackStyle.Charge,
                Description = "Marks its lane on the ground, then charges to where you stood for 1 damage. Up close it uses the Ogre's Ground Slam.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.9f, "chargeWindUp"),
                    IntelStat.DamageText("1 / 2", true),
                    IntelStat.Seconds("COOLDOWN", 5f, "chargeCooldown"),
                    IntelStat.DistanceRange("RANGE", 3.5f, 9f, "chargeMin", "chargeMax"),
                },
                Counter = "Jump over the charge; it stands dazed for 1.5 s. It may sidestep a stomp, never while dazed.",
            },
            new EnemyIntelEntry
            {
                Id = "plague_alchemist", Prefab = "Assets/Enemies/Bloody_Alchemist/Plague_Alchemist.prefab",
                Behaviour = typeof(AlchemistBossBehaviour),
                Name = "Plague Alchemist", Role = "BOSS  /  LEVEL 2", Health = 7,
                Ability = "Potion Volley", Style = AttackStyle.Ranged,
                Description = "Keeps its distance and lobs three potions at once - four below half health - leaving pools on and around you. Gets close and it stabs.",
                Stats = new[]
                {
                    IntelStat.Seconds("WIND-UP", 0.5f, "throwWindUp"),
                    IntelStat.Damage(1),
                    IntelStat.Seconds("COOLDOWN", 3f, "throwCooldown"),
                    IntelStat.DistanceRange("RANGE", 4f, 8f, "tooClose", "tooFar"),
                },
                Counter = "Step between the pools and close in. It slides from a stomp at most every 3 s.",
            },
            new EnemyIntelEntry
            {
                Id = "goblin_boss", Prefab = "Assets/Enemies/Goblin/Goblin_Boss.prefab",
                Behaviour = typeof(BossHunterBehaviour),
                Name = "Goblin Warchief", Role = "BOSS  /  LEVEL 3", Health = 9,
                Ability = "Skirmish", Style = AttackStyle.Combo,
                Description = "Circles you, feints, vaults over you and dashes in for a two-hit combo; every third ends in a 2-damage cleave. It also slides in low along the ground (jump it) and slides past you when cornered. Below half health it pounces on your spot.",
                Stats = new[]
                {
                    IntelStat.Seconds("DASH WIND-UP", 0.3f, "dashWindUp"),
                    IntelStat.DamageText("1 + 1 / 2", true),
                    IntelStat.Seconds("CLEAVE WIND-UP", 0.7f, "heavyWindUp"),
                    IntelStat.Distance("RANGE", 5.5f, "dashReach"),
                },
                Counter = "Stomp it while winded after a combo. It reads repeated jumps, but never dodges twice within 2.4 s.",
            },
        };

        /// <summary>The entry for a live enemy (by its behaviour), or null.</summary>
        public static EnemyIntelEntry For(EnemyBehaviour behaviour)
        {
            if (behaviour == null) return null;
            Type type = behaviour.GetType();
            foreach (EnemyIntelEntry entry in All) if (entry.Behaviour == type) return entry;
            return null;
        }

        public static string StyleLabel(AttackStyle style)
        {
            switch (style)
            {
                case AttackStyle.Melee: return "MELEE";
                case AttackStyle.Combo: return "COMBO";
                case AttackStyle.Dash: return "DASH";
                case AttackStyle.Charge: return "CHARGE";
                case AttackStyle.Ranged: return "RANGED";
                case AttackStyle.Area: return "AREA";
                default: return "COUNTER";
            }
        }
    }
}
