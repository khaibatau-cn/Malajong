using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Felt grain and a soft vignette over the play mat, so the centre column reads as a table rather
/// than a flat green rectangle.
///
/// Both textures are generated in code — there is no asset to lose or re-import — and the layers
/// are slotted directly above the mat's Fill, under its frame and everything parented to it.
/// </summary>
public class MatAmbience : MonoBehaviour
{
    /// <summary>Grain texels are drawn at the tiles' own pixel scale, so the felt matches the art.</summary>
    private const int GrainTexels = 64;
    private const float GrainAlpha = 0.07f;
    private const float VignetteAlpha = 0.45f;

    private static Texture2D grainTexture;
    private static Texture2D vignetteTexture;

    private RawImage grain;

    /// <summary>Adds the ambience layers to a mat once. Safe to call every time the UI starts.</summary>
    public static void Ensure(Transform mat)
    {
        if (mat == null || mat.Find("FeltGrain") != null) return;

        Transform fill = mat.Find("Fill");
        int insertAt = fill != null ? fill.GetSiblingIndex() + 1 : 0;

        RawImage grainLayer = CreateLayer(mat, "FeltGrain", insertAt, GrainTexture());
        CreateLayer(mat, "Vignette", insertAt + 1, VignetteTexture());

        MatAmbience ambience = mat.gameObject.AddComponent<MatAmbience>();
        ambience.grain = grainLayer;
    }

    private void LateUpdate()
    {
        // Tile the grain at a constant on-screen texel size however large the mat is.
        if (grain == null) return;
        Rect rect = grain.rectTransform.rect;
        float texelSpan = GrainTexels * MalajongTheme.TileScale;
        grain.uvRect = new Rect(0f, 0f, rect.width / texelSpan, rect.height / texelSpan);
    }

    private static RawImage CreateLayer(Transform parent, string name, int siblingIndex, Texture texture)
    {
        GameObject layer = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        layer.transform.SetParent(parent, false);
        layer.transform.SetSiblingIndex(siblingIndex);

        RectTransform rect = layer.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = layer.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    private static Texture2D GrainTexture()
    {
        if (grainTexture != null) return grainTexture;

        grainTexture = new Texture2D(GrainTexels, GrainTexels, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            name = "MatGrain"
        };

        // Fixed seed: the felt should look the same every run, not shimmer between sessions.
        var random = new System.Random(1393);
        var pixels = new Color32[GrainTexels * GrainTexels];
        for (int i = 0; i < pixels.Length; i++)
        {
            double roll = random.NextDouble();
            byte alpha = (byte)(random.NextDouble() * GrainAlpha * 255);
            // Mostly dark flecks with the odd light fibre, like worn baize.
            pixels[i] = roll < 0.18 ? new Color32(255, 255, 255, alpha)
                      : roll < 0.62 ? new Color32(0, 0, 0, alpha)
                      : new Color32(0, 0, 0, 0);
        }
        grainTexture.SetPixels32(pixels);
        grainTexture.Apply(false, true);
        return grainTexture;
    }

    private static Texture2D VignetteTexture()
    {
        if (vignetteTexture != null) return vignetteTexture;

        const int width = 128, height = 72;
        vignetteTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "MatVignette"
        };

        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f) / width * 2f - 1f;
                float dy = (y + 0.5f) / height * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, distance)) * VignetteAlpha;
                pixels[y * width + x] = new Color32(0, 0, 0, (byte)(alpha * 255));
            }
        }
        vignetteTexture.SetPixels32(pixels);
        vignetteTexture.Apply(false, true);
        return vignetteTexture;
    }
}
