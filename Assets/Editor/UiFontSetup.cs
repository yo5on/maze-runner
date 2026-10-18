using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MazeRunner.EditorTools
{
    /// <summary>
    /// Makes the TextMesh Pro font asset for the portfolio's display face, Instrument Serif Italic
    /// (SIL Open Font License, Assets/Fonts/OFL-InstrumentSerif.txt). The Settings menu loads it
    /// from Resources for its heading.
    ///
    /// Menu: Maze Runner > Build UI Fonts
    /// CLI:  Unity -batchmode -quit -projectPath . -executeMethod MazeRunner.EditorTools.UiFontSetup.RunFromCommandLine
    /// </summary>
    public static class UiFontSetup
    {
        private const string SourceFont = "Assets/Fonts/InstrumentSerif-Italic.ttf";
        private const string OutputAsset = "Assets/Resources/Fonts/InstrumentSerif-Italic SDF.asset";
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%&-+/'\"()";

        [MenuItem("Maze Runner/Build UI Fonts")]
        public static void Run()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFont);
            if (font == null) throw new System.Exception("Missing " + SourceFont);

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAsset) != null) AssetDatabase.DeleteAsset(OutputAsset);
            Directory.CreateDirectory(Path.GetDirectoryName(OutputAsset));

            // Static atlas with the characters the UI needs: the web build does not have to rasterise
            // glyphs at run time.
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            asset.name = Path.GetFileNameWithoutExtension(OutputAsset);
            asset.TryAddCharacters(Characters, out string missing);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(asset, OutputAsset);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = asset.name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UiFontSetup] {OutputAsset}: {asset.characterTable.Count} characters" + (string.IsNullOrEmpty(missing) ? "" : ", missing: " + missing));
        }

        public static void RunFromCommandLine()
        {
            bool ok = true;
            try { Run(); }
            catch (System.Exception e) { Debug.LogException(e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
