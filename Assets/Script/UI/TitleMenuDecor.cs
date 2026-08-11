using UnityEngine;

/// <summary>
/// Idle motion for the start menu: the logo breathes, the decorative tile fan sways.
///
/// Purely cosmetic and self-contained — it touches nothing but the transforms it was handed, so
/// the menu still works exactly the same if this component is missing. Runs on unscaled time so
/// the menu keeps moving even while a hitstop has scaled time pinned at zero.
/// </summary>
public class TitleMenuDecor : MonoBehaviour
{
    [Header("Logo")]
    public RectTransform Logo;
    /// <summary>Vertical travel in reference pixels. Small on purpose — this should read as breathing, not floating.</summary>
    public float LogoBobHeight = 10f;
    public float LogoBobSpeed = 1.1f;
    /// <summary>Scale swell at the top of the bob. 0.02 is roughly the largest value that still reads as one steady logo.</summary>
    public float LogoBreathAmount = 0.02f;

    [Header("Tile Fan")]
    public RectTransform[] Tiles;
    public float TileSwayDegrees = 2.5f;
    public float TileSwayHeight = 6f;
    public float TileSwaySpeed = 0.8f;
    /// <summary>Phase offset per tile, so the fan ripples along its length instead of moving as one slab.</summary>
    public float TilePhaseStep = 0.45f;

    private Vector2 logoOrigin;
    private Vector2[] tileOrigins;
    private float[] tileBaseAngles;

    private void Awake()
    {
        if (Logo != null) logoOrigin = Logo.anchoredPosition;

        if (Tiles != null)
        {
            tileOrigins = new Vector2[Tiles.Length];
            tileBaseAngles = new float[Tiles.Length];

            for (int i = 0; i < Tiles.Length; i++)
            {
                if (Tiles[i] == null) continue;
                tileOrigins[i] = Tiles[i].anchoredPosition;
                // The fan's resting rotation is baked in by the scene builder, so sway has to be
                // measured from wherever each tile already sits rather than from zero.
                tileBaseAngles[i] = Tiles[i].localEulerAngles.z;
            }
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (Logo != null)
        {
            float wave = Mathf.Sin(time * LogoBobSpeed);
            Logo.anchoredPosition = logoOrigin + new Vector2(0f, wave * LogoBobHeight);
            Logo.localScale = Vector3.one * (1f + wave * LogoBreathAmount);
        }

        if (Tiles == null) return;

        for (int i = 0; i < Tiles.Length; i++)
        {
            RectTransform tile = Tiles[i];
            if (tile == null) continue;

            float phase = time * TileSwaySpeed + i * TilePhaseStep;
            tile.anchoredPosition = tileOrigins[i] + new Vector2(0f, Mathf.Sin(phase) * TileSwayHeight);
            tile.localRotation = Quaternion.Euler(0f, 0f, tileBaseAngles[i] + Mathf.Cos(phase) * TileSwayDegrees);
        }
    }
}
