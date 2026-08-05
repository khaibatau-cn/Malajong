using UnityEngine;

/// <summary>
/// Malajong's visual identity in one place — "Vermilion &amp; Malachite".
///
/// Imperial red and malachite green, the two colours of a painted palace beam, with gold as
/// the only trim. Every value here was sampled from Assets/Sprites/Tilesets/Blueeyedrat, so the
/// UI and the tile art agree by construction rather than by tuning.
///
/// Nothing in the project should declare a raw <c>new Color(...)</c> for UI chrome. If a shade
/// is missing, add it here — the previous build accumulated ~30 near-identical hand-picked
/// navies precisely because there was nowhere central to put them.
/// </summary>
public static class MalajongTheme
{
    // --- Ground: aged lacquer, warm black with a red bias ---
    public static readonly Color Ink     = Hex("140B09");
    public static readonly Color InkSoft = Hex("1C100D");
    /// <summary>Inset fill for badges and value boxes sitting on a coloured cabinet.</summary>
    public static readonly Color BoxFill = Hex("140B09", 0.82f);

    // --- Vermilion: the side cabinets, imperial pillar red ---
    public static readonly Color VermilionDeep   = Hex("3E120F");
    public static readonly Color Vermilion       = Hex("5C1A15");
    public static readonly Color VermilionRaised = Hex("7A241C");
    /// <summary>The single accent. One button per screen earns this.</summary>
    public static readonly Color VermilionBright = Hex("D8402E");

    // --- Malachite: the play mat, imperial beam green ---
    public static readonly Color MalachiteDeep   = Hex("0E3120");
    public static readonly Color Malachite       = Hex("14402A");
    public static readonly Color MalachiteRaised = Hex("1D5A3A");
    public static readonly Color MalachiteBright = Hex("43B87A");

    // --- Metal and bone ---
    public static readonly Color Gold     = Hex("D9A93A");
    public static readonly Color GoldDim  = Hex("8A6A18");
    /// <summary>Border colour for every bevelled box. Structure, never fill.</summary>
    public static readonly Color GoldDark = Hex("4A3A0E");
    public static readonly Color Bone     = Hex("EDE6D6");
    public static readonly Color BoneDim  = Hex("B8AC97");
    public static readonly Color Smoke    = Hex("96826F");

    // --- Fu / Fan: Balatro's chip-vs-mult split is load-bearing, so the split stays ---
    public static readonly Color FuFill   = Hex("10314C");
    public static readonly Color FuBorder = Hex("21506F");
    public static readonly Color FanFill  = Hex("4E1512");
    /// <summary>Fu figures and the +Fu badges thrown by scoring tiles.</summary>
    public static readonly Color FuText   = Hex("6FB8EE");
    public static readonly Color FanText  = Hex("F5897E");

    // --- Tile states ---
    /// <summary>Warm tint over a selected tile's sprite. Gold-biased so it reads as lit, not washed.</summary>
    public static readonly Color TileSelectTint = Hex("FFF2C0");
    public static readonly Color TileSelectGlow = Hex("D9A93A", 0.35f);

    // --- Overlays ---
    public static readonly Color Scrim     = Hex("0A0503", 0.86f);
    public static readonly Color SlotEmpty = Hex("000000", 0.28f);

    // --- Rich-text hex, for TMP <color=#...> tags ---
    public const string HexGold      = "#D9A93A";
    public const string HexBone      = "#EDE6D6";
    public const string HexBoneDim   = "#B8AC97";
    public const string HexSmoke     = "#96826F";
    public const string HexVermilion = "#D8402E";
    public const string HexMalachite = "#43B87A";
    public const string HexFu        = "#6FB8EE";
    public const string HexFan       = "#F5897E";
    /// <summary>Text sitting on a gold header bar.</summary>
    public const string HexOnGold    = "#1A1006";

    // --- Metrics ---
    /// <summary>Border thickness in reference pixels. Thick and stepped, so it survives at pixel scale.</summary>
    public const int Border = 3;
    /// <summary>Hard offset shadow distance. Never blurred — blur is the giveaway of a modern vector UI.</summary>
    public const int ShadowOffset = 4;
    /// <summary>Segment count for blocked-out meters. Discrete blocks quantise like pixel art.</summary>
    public const int MeterSegments = 10;
    public const int SegmentGap = 2;

    /// <summary>Parses "RRGGBB" or "#RRGGBB". Falls back to magenta so a typo is visible, not silent.</summary>
    public static Color Hex(string hex, float alpha = 1f)
    {
        if (string.IsNullOrEmpty(hex)) return Color.magenta;
        if (hex[0] == '#') hex = hex.Substring(1);
        if (hex.Length != 6) return Color.magenta;

        int packed = System.Convert.ToInt32(hex, 16);
        return new Color(
            ((packed >> 16) & 0xFF) / 255f,
            ((packed >> 8) & 0xFF) / 255f,
            (packed & 0xFF) / 255f,
            alpha);
    }
}
