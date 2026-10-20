using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using MazeRunner.Enemies;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Gives the enemies their weapon art and checks it is in place:
    /// - Shields: imports the two shields used in game from the craftpix shield pack kept (unchanged) at
    ///   weapons/shield.zip - PNG/Shield_5/1.png (dark iron-banded round shield) for Skeleton Warrior 2 and
    ///   PNG/Shield_1/5.png (riveted steel round shield) for Skeleton Crusader 1 - into
    ///   Assets/Sprites/Weapons/Shields, unchanged. The pack's other designs are left in the archive.
    /// - Potions: Bloody Alchemist 1 throws the original green potion artwork,
    ///   Assets/Sprites/Weapons/Potions/potion_green_original.png - the bottle cut out of the supplied
    ///   reference (Potions/Source/potion_reference_original.png, kept unchanged) with only the background
    ///   removed and a transparent crop; the bottle's pixels are untouched apart from its one-pixel rim,
    ///   which is un-blended from the grey background (colour-to-alpha) so the edge is smooth. The splash pieces and puddle come from
    ///   <see cref="PotionArtBuilder"/>.
    /// - Enemy Intel: points Resources/EnemyIntelArt at the potion and shields so the panel shows them.
    /// Then reports every assigned sprite and fails if one is missing.
    ///
    /// Menu: Maze Runner > Set Up Enemy Weapons
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.EnemyWeaponSetup.RunFromCommandLine
    /// </summary>
    public static class EnemyWeaponSetup
    {
        private const string ShieldArchive = "weapons/shield.zip";
        private const string ShieldFolder = "Assets/Sprites/Weapons/Shields";
        private const string IntelArtPath = "Assets/Resources/EnemyIntelArt.asset";

        private const string Alchemist = "Assets/Enemies/Bloody_Alchemist/Bloody_Alchemist_1.prefab";
        private const string Warrior = "Assets/Enemies/Skeleton_Warrior/Skeleton_Warrior_2.prefab";
        private const string Crusader = "Assets/Enemies/Skeleton_Crusader/Skeleton_Crusader_1.prefab";

        private static readonly (string entry, string target)[] Shields =
        {
            ("PNG/Shield_5/1.png", "Shield_5_1.png"), // Skeleton Warrior 2
            ("PNG/Shield_1/5.png", "Shield_1_5.png"), // Skeleton Crusader 1
        };

        private static string Potion(string file) => PotionArtBuilder.Folder + "/" + file + ".png";

        private const string WarriorShield = ShieldFolder + "/Shield_5_1.png";
        private const string CrusaderShield = ShieldFolder + "/Shield_1_5.png";

        [MenuItem("Maze Runner/Set Up Enemy Weapons")]
        public static bool Run()
        {
            ImportShields();
            PotionArtBuilder.Build();

            Sprite bottle = ImportOriginalPotion();
            Sprite droplet = Load(Potion("fx_droplet"));
            Sprite shard = Load(Potion("fx_glass_shard"));
            Sprite puddle = Load(Potion("fx_puddle"));
            Sprite warriorShield = Load(WarriorShield);
            Sprite crusaderShield = Load(CrusaderShield);
            Color green = PotionArtBuilder.Liquids[1].liquid;

            EditPrefab(Alchemist, typeof(AlchemistBehaviour), so =>
            {
                so.FindProperty("potion.bottle").objectReferenceValue = bottle;
                so.FindProperty("potion.droplet").objectReferenceValue = droplet;
                so.FindProperty("potion.shard").objectReferenceValue = shard;
                so.FindProperty("potion.puddle").objectReferenceValue = puddle;
                so.FindProperty("potion.liquid").colorValue = green;
            });
            EditPrefab(Warrior, typeof(ShieldWarriorBehaviour), so => so.FindProperty("shieldSprite").objectReferenceValue = warriorShield);
            EditPrefab(Crusader, typeof(CrusaderBehaviour), so => so.FindProperty("shieldSprite").objectReferenceValue = crusaderShield);

            var art = AssetDatabase.LoadAssetAtPath<EnemyIntelArt>(IntelArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<EnemyIntelArt>();
                AssetDatabase.CreateAsset(art, IntelArtPath);
            }
            art.icons = new[]
            {
                new EnemyIntelArt.Icon { id = "bloody_alchemist_1", sprite = bottle },
                new EnemyIntelArt.Icon { id = "skeleton_warrior_2", sprite = warriorShield },
                new EnemyIntelArt.Icon { id = "skeleton_crusader_1", sprite = crusaderShield },
            };
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return Verify();
        }

        /// <summary>
        /// Only the shields: imports them, gives each shield enemy its shield and points the Enemy Intel icons
        /// at them. Potions and every other weapon are left as they are.
        /// Menu: Maze Runner > Set Up Enemy Shields
        /// </summary>
        [MenuItem("Maze Runner/Set Up Enemy Shields")]
        public static bool RunShields()
        {
            ImportShields();
            Sprite warriorShield = Load(WarriorShield);
            Sprite crusaderShield = Load(CrusaderShield);
            EditPrefab(Warrior, typeof(ShieldWarriorBehaviour), so => so.FindProperty("shieldSprite").objectReferenceValue = warriorShield);
            EditPrefab(Crusader, typeof(CrusaderBehaviour), so => so.FindProperty("shieldSprite").objectReferenceValue = crusaderShield);
            var art = AssetDatabase.LoadAssetAtPath<EnemyIntelArt>(IntelArtPath);
            if (art != null && art.icons != null)
            {
                for (int i = 0; i < art.icons.Length; i++)
                {
                    if (art.icons[i].id == "skeleton_warrior_2") art.icons[i].sprite = warriorShield;
                    else if (art.icons[i].id == "skeleton_crusader_1") art.icons[i].sprite = crusaderShield;
                }
                EditorUtility.SetDirty(art);
            }
            AssetDatabase.SaveAssets();
            return Verify();
        }

        private const string OriginalPotion = PotionArtBuilder.Folder + "/potion_green_original.png";

        /// <summary>
        /// Imports the original potion cut-out as a sprite exactly as it is (no compression, no resizing, no
        /// mipmaps) and returns it.
        /// </summary>
        private static Sprite ImportOriginalPotion()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(OriginalPotion);
            if (importer == null) throw new Exception("Missing " + OriginalPotion);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; // in-game size is set by AlchemistFlask.BottleHeight
            importer.alphaIsTransparency = true;
            // Shown at about half its pixel size in game: mipmaps + trilinear keep it smooth instead of
            // shimmering and jagged when scaled down; no compression, no resizing.
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.borderMipmap = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return Load(OriginalPotion);
        }

        /// <summary>
        /// Only the potion bottle: gives Bloody Alchemist 1 (and its Enemy Intel icon) the original potion
        /// artwork. Shields, splash pieces and everything else are left as they are.
        /// Menu: Maze Runner > Set Up Original Potion
        /// </summary>
        [MenuItem("Maze Runner/Set Up Original Potion")]
        public static bool RunPotion()
        {
            Sprite bottle = ImportOriginalPotion();
            EditPrefab(Alchemist, typeof(AlchemistBehaviour), so => so.FindProperty("potion.bottle").objectReferenceValue = bottle);
            var art = AssetDatabase.LoadAssetAtPath<EnemyIntelArt>(IntelArtPath);
            if (art != null && art.icons != null)
            {
                for (int i = 0; i < art.icons.Length; i++)
                    if (art.icons[i].id == "bloody_alchemist_1") art.icons[i].sprite = bottle;
                EditorUtility.SetDirty(art);
            }
            AssetDatabase.SaveAssets();
            return Verify();
        }

        public static void RunPotionFromCommandLine()
        {
            bool ok;
            try { ok = RunPotion(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void RunShieldsFromCommandLine()
        {
            bool ok;
            try { ok = RunShields(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void ImportShields()
        {
            if (!File.Exists(ShieldArchive)) throw new Exception("Shield pack not found at " + Path.GetFullPath(ShieldArchive));
            Directory.CreateDirectory(ShieldFolder);
            using (ZipArchive zip = ZipFile.OpenRead(ShieldArchive))
            {
                foreach (var (entryName, target) in Shields)
                {
                    ZipArchiveEntry entry = zip.GetEntry(entryName);
                    if (entry == null) throw new Exception(ShieldArchive + " has no " + entryName);
                    string path = ShieldFolder + "/" + target;
                    using (Stream source = entry.Open())
                    using (FileStream file = File.Create(path)) source.CopyTo(file);
                }
            }
            AssetDatabase.Refresh();
            foreach (var (_, target) in Shields)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(ShieldFolder + "/" + target);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 160; // the pack's 160 px art = 1 unit before in-game sizing
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Debug.Log("[EnemyWeapons] imported " + Shields.Length + " shields from " + ShieldArchive + " (archive unchanged)");
        }

        private static void EditPrefab(string path, Type behaviour, Action<SerializedObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Component component = root.GetComponent(behaviour);
                if (component == null) throw new Exception(path + " has no " + behaviour.Name);
                var so = new SerializedObject(component);
                edit(so);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new Exception("Missing sprite " + path);
            return sprite;
        }

        [MenuItem("Maze Runner/Verify Enemy Weapons")]
        public static bool Verify()
        {
            int problems = 0;
            void Check(string prefab, Type type, params string[] properties)
            {
                var component = AssetDatabase.LoadAssetAtPath<GameObject>(prefab)?.GetComponent(type);
                if (component == null) { Debug.LogError("[EnemyWeapons] " + prefab + ": no " + type.Name); problems++; return; }
                var so = new SerializedObject(component);
                foreach (string property in properties)
                {
                    UnityEngine.Object value = so.FindProperty(property)?.objectReferenceValue;
                    if (value == null) { Debug.LogError($"[EnemyWeapons] {Path.GetFileName(prefab)}: {property} is not set"); problems++; }
                    else Debug.Log($"[EnemyWeapons] {Path.GetFileName(prefab)}: {property} = {AssetDatabase.GetAssetPath(value)}");
                }
            }
            Check(Alchemist, typeof(AlchemistBehaviour), "potion.bottle", "potion.droplet", "potion.shard", "potion.puddle");
            Check(Warrior, typeof(ShieldWarriorBehaviour), "shieldSprite");
            Check(Crusader, typeof(CrusaderBehaviour), "shieldSprite");
            var art = AssetDatabase.LoadAssetAtPath<EnemyIntelArt>(IntelArtPath);
            if (art == null || art.icons.Length != 3) { Debug.LogError("[EnemyWeapons] EnemyIntelArt missing or incomplete"); problems++; }
            else foreach (var icon in art.icons) if (icon.sprite == null) { Debug.LogError("[EnemyWeapons] intel icon missing for " + icon.id); problems++; }
            Debug.Log(problems == 0 ? "[EnemyWeapons] VERIFY OK" : $"[EnemyWeapons] VERIFY FAILED: {problems} problem(s)");
            return problems == 0;
        }

        public static void RunFromCommandLine()
        {
            bool ok;
            try { ok = Run() && EnemyIntelTools.Verify(); }
            catch (Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
