using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Which sprite plays which structural role in the UI, and the import settings those sprites need
/// to survive as pixel art.
///
/// This is the shape half of <see cref="MalajongTheme"/>: colour lives there, frame art lives here.
/// Re-skinning the entire UI is the three role constants below.
///
/// Editor-only on purpose. <c>SceneSetupTool</c> bakes the sprite references straight into the
/// scene when it builds the UI, so nothing has to resolve them at runtime.
///
/// Art is Kenney "Fantasy UI Borders" (CC0 — see FantasyBorders/License.txt). Its line work is
/// pure white, which is the whole reason MalajongTheme can tint it; a pre-coloured frame could not
/// be recoloured this way without going muddy.
/// </summary>
public static class MalajongSkin
{
    public const string Root = "Assets/Sprites/UI/FantasyBorders";

    /// <summary>
    /// Spirit icons, named after their SpiritData asset so the two stay wired by convention rather
    /// than by a mapping table someone has to remember to update.
    /// </summary>
    public const string SpiritIconRoot = "Assets/Sprites/UI/Icons/Spirits";

    // --- Role assignment. These three lines re-skin everything. ---
    /// <summary>Full-height cabinets: the three main columns.</summary>
    private const string PanelFrameName = "panel-border-013";
    /// <summary>Value boxes sitting inside a cabinet. Lighter, so it does not compete.</summary>
    private const string BoxFrameName = "panel-border-009";
    /// <summary>Plain rectangle — a button needs to read as pressable, not as ornament.</summary>
    private const string ButtonFrameName = "panel-border-015";

    /// <summary>
    /// 9-slice inset in source pixels. The art is 48x48 and every usable frame's corner motif fits
    /// inside 16px, leaving a 16px stretchable middle. Measured across all 32 frames, not guessed.
    /// </summary>
    public const int SliceBorder = 16;

    /// <summary>
    /// One source pixel to one UI pixel, given <see cref="ImportPixelsPerUnit"/> matches the
    /// canvas reference PPU. Lower this to draw the frame heavier.
    /// </summary>
    public const float PixelsPerUnitMultiplier = 1f;

    private const float ImportPixelsPerUnit = 100f;

    /// <summary>
    /// These two put decoration at the midpoint of their edges, so the middle band is not uniform
    /// and stretching it would smear the motif. Usable at fixed size only — imported unsliced so
    /// the mistake is impossible rather than merely discouraged.
    /// </summary>
    private static readonly HashSet<string> NotSliceable = new HashSet<string>
    {
        "panel-border-020", "panel-border-025"
    };

    public static Sprite PanelFrame => LoadBorder(PanelFrameName);
    public static Sprite BoxFrame => LoadBorder(BoxFrameName);
    public static Sprite ButtonFrame => LoadBorder(ButtonFrameName);

    private static Sprite LoadBorder(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Border/{name}.png");
    }

    /// <param name="spiritAssetName">The SpiritData asset's file name, e.g. "ImperialScholar".</param>
    public static Sprite SpiritIcon(string spiritAssetName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpiritIconRoot}/{spiritAssetName}.png");
    }

    [MenuItem("Malajong/Import UI Skin Sprites")]
    public static void ConfigureAllMenu()
    {
        int changed = ConfigureAll();
        int wired = WireSpiritIcons();
        Debug.Log($"[MalajongSkin] UI skin sprites configured ({changed} changed), {wired} spirit icons wired.");
    }

    /// <summary>
    /// Points every SpiritData at the icon named after its asset file. Convention over wiring: a
    /// new spirit needs only a matching PNG dropped into the icons folder, never an inspector drag.
    /// Spirits without a matching file keep whatever icon they already had.
    /// </summary>
    /// <returns>How many spirits had their icon changed.</returns>
    public static int WireSpiritIcons()
    {
        int wired = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:SpiritData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpiritData spirit = AssetDatabase.LoadAssetAtPath<SpiritData>(path);
            if (spirit == null) continue;

            Sprite icon = SpiritIcon(Path.GetFileNameWithoutExtension(path));
            if (icon == null || spirit.Icon == icon) continue;

            spirit.Icon = icon;
            EditorUtility.SetDirty(spirit);
            wired++;
        }

        if (wired > 0) AssetDatabase.SaveAssets();
        return wired;
    }

    /// <summary>
    /// Applies pixel-art import settings to every sprite under <see cref="Root"/>. Cheap to call
    /// repeatedly — sprites already configured are skipped without a reimport.
    /// </summary>
    /// <returns>How many assets actually had to be reimported.</returns>
    public static int ConfigureAll()
    {
        int changed = 0;

        // Frames are 9-sliced so they can stretch; icons are drawn at their natural size and must
        // not be, or the artwork would smear.
        changed += ConfigureFolder(Root, sliced: true);
        changed += ConfigureFolder(SpiritIconRoot, sliced: false);

        if (changed > 0) AssetDatabase.SaveAssets();
        return changed;
    }

    private static int ConfigureFolder(string folder, bool sliced)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[MalajongSkin] No sprites at '{folder}'. That part of the UI falls back to its unskinned form.");
            return 0;
        }

        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            if (Configure(AssetDatabase.GUIDToAssetPath(guid), sliced)) changed++;
        }
        return changed;
    }

    /// <returns>True if the importer had to change, meaning the asset was reimported.</returns>
    public static bool Configure(string path, bool sliced)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        int slice = (!sliced || NotSliceable.Contains(Path.GetFileNameWithoutExtension(path))) ? 0 : SliceBorder;

        // Clamp per-axis: the dividers are as short as 96x10, and a 16px border on a 10px-tall
        // sprite leaves no middle band to stretch. Half the dimension is the hard ceiling.
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int horizontal = texture != null ? Mathf.Min(slice, (texture.width - 1) / 2) : slice;
        int vertical = texture != null ? Mathf.Min(slice, (texture.height - 1) / 2) : slice;

        // spriteBorder is (left, bottom, right, top).
        Vector4 border = new Vector4(horizontal, vertical, horizontal, vertical);

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        bool alreadyCorrect =
            settings.textureType == TextureImporterType.Sprite &&
            settings.spriteMode == (int)SpriteImportMode.Single &&
            settings.spriteMeshType == SpriteMeshType.FullRect &&
            settings.spriteBorder == border &&
            settings.spritePixelsPerUnit == ImportPixelsPerUnit &&
            settings.filterMode == FilterMode.Point &&
            settings.mipmapEnabled == false &&
            importer.textureCompression == TextureImporterCompression.Uncompressed;

        if (alreadyCorrect) return false;

        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        // Sliced rendering requires FullRect; the default Tight mesh silently ignores the border.
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = border;
        settings.spritePixelsPerUnit = ImportPixelsPerUnit;
        settings.alphaIsTransparency = true;
        settings.filterMode = FilterMode.Point;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.mipmapEnabled = false;
        importer.SetTextureSettings(settings);

        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return true;
    }
}
