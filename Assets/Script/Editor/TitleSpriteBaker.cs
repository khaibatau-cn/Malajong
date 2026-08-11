using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the raw title art into a tightly-cropped, transparent-backed sprite the menu can show at
/// full size.
///
/// The Aseprite source carries an opaque Background layer and a canvas wider than the artwork, so
/// dropping it straight into an Image produces a white slab with the logo floating small in the
/// middle. This bakes a cleaned PNG next to it instead of asking the artist to maintain a second
/// export by hand.
///
/// Two passes:
/// 1. <b>Knock out</b> — flood fill inward from the canvas edges, clearing every pixel that matches
///    the corner colour. Flood fill rather than a global colour match on purpose: the logo is full
///    of white (the cat, the tile faces), and those regions are sealed behind black outlines, so
///    the fill cannot reach them. A global "delete all white" would gut the artwork.
/// 2. <b>Trim</b> — crop to the bounding box of what survives.
///
/// Re-bakes only when the source is newer than the output, so it is cheap to call from scene setup.
/// </summary>
public static class TitleSpriteBaker
{
    public const string BakedPath = "Assets/Sprites/UI/Title_Baked.png";

    private static readonly string[] SourceCandidates =
    {
        "Assets/Sprites/UI/Title.aseprite",
        "Assets/Sprites/UI/Title.png",
        "Assets/Sprites/RawSource/Title.aseprite"
    };

    /// <summary>Per-channel match tolerance. Pixel art is flat, so this only needs to absorb colour-space rounding.</summary>
    private const float ColorTolerance = 0.04f;

    /// <summary>Transparent margin kept on each side, so the art never sits flush against the sprite edge.</summary>
    private const int Padding = 2;

    [MenuItem("Malajong/Bake Title Sprite")]
    public static void BakeMenu()
    {
        Sprite baked = Bake();
        if (baked != null)
        {
            Debug.Log($"[TitleSpriteBaker] Baked '{BakedPath}' at {baked.rect.width}x{baked.rect.height}.");
        }
    }

    /// <summary>Bakes only if the output is missing or older than its source. Safe to call every scene build.</summary>
    public static Sprite BakeIfStale()
    {
        string sourcePath = FindSource();
        if (sourcePath == null) return null;

        if (File.Exists(BakedPath) && File.GetLastWriteTimeUtc(BakedPath) >= File.GetLastWriteTimeUtc(sourcePath))
        {
            Sprite existing = FirstSpriteAt(BakedPath);
            if (existing != null) return existing;
        }

        return Bake();
    }

    public static Sprite Bake()
    {
        string sourcePath = FindSource();
        if (sourcePath == null)
        {
            Debug.LogWarning("[TitleSpriteBaker] No title art found. Expected one of: " + string.Join(", ", SourceCandidates));
            return null;
        }

        Sprite source = FirstSpriteAt(sourcePath);
        if (source == null)
        {
            Debug.LogWarning($"[TitleSpriteBaker] '{sourcePath}' imported without a sprite.");
            return null;
        }

        Color[] pixels = ReadSpritePixels(source, out int width, out int height);
        if (pixels == null) return null;

        KnockOutBackground(pixels, width, height);

        if (!TryGetContentBounds(pixels, width, height, out RectInt bounds))
        {
            Debug.LogWarning("[TitleSpriteBaker] Every pixel was treated as background — nothing left to bake. Check that the artwork is not the same colour as its canvas corners.");
            return null;
        }

        Texture2D output = Crop(pixels, width, height, bounds);
        File.WriteAllBytes(BakedPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);

        AssetDatabase.ImportAsset(BakedPath, ImportAssetOptions.ForceUpdate);
        MalajongSkin.Configure(BakedPath, sliced: false);

        return FirstSpriteAt(BakedPath);
    }

    private static string FindSource()
    {
        foreach (string path in SourceCandidates)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) return path;
        }
        return null;
    }

    /// <summary>
    /// Reads a sprite's own region out of its texture.
    ///
    /// Goes via a RenderTexture blit rather than <c>GetPixels</c> because importer-generated
    /// textures are usually not CPU-readable, and the Aseprite importer packs frames into an atlas —
    /// so the sprite's <see cref="Sprite.textureRect"/> is what matters, not the whole texture.
    /// </summary>
    private static Color[] ReadSpritePixels(Sprite sprite, out int width, out int height)
    {
        Rect region = sprite.textureRect;
        width = Mathf.RoundToInt(region.width);
        height = Mathf.RoundToInt(region.height);

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning("[TitleSpriteBaker] Sprite has an empty texture rect.");
            return null;
        }

        Texture source = sprite.texture;
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(region.x, region.y, width, height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Color[] pixels = readable.GetPixels();
        Object.DestroyImmediate(readable);

        return pixels;
    }

    /// <summary>
    /// Clears every pixel reachable from the canvas edge that matches the background colour.
    ///
    /// The background colour is sampled from the corners: whichever colour holds a majority of the
    /// four wins, so a logo bleeding into one corner cannot redefine what "background" means.
    ///
    /// <b>Leak-proofed.</b> A plain flood fill destroyed this artwork: the cat's body and the tile
    /// faces are painted the same white as the canvas, so a single-pixel gap in the black line work
    /// let the fill inside and it ate them from within. The fix is to erode before filling and
    /// dilate after:
    ///
    /// <list type="number">
    /// <item>Mark every background-coloured pixel.</item>
    /// <item><b>Erode</b> — a pixel only qualifies as fillable if all of its neighbours are also
    /// background. This closes any channel a pixel or two wide, so the fill physically cannot pass
    /// through a pinhole in an outline.</item>
    /// <item>Flood fill from the edges across what survived.</item>
    /// <item><b>Dilate</b> — give back the one-pixel skin erosion took, but only into pixels that
    /// were background to begin with, so the cut still lands flush against the art.</item>
    /// </list>
    ///
    /// Net effect: identical to a plain fill in open space, and inert against pinhole leaks.
    /// </summary>
    private static void KnockOutBackground(Color[] pixels, int width, int height)
    {
        Color background = SampleBackground(pixels, width, height);

        bool[] isBackground = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            isBackground[i] = Matches(pixels[i], background);
        }

        bool[] fillable = Erode(isBackground, width, height);
        bool[] cleared = FloodFromEdges(fillable, width, height);
        Dilate(cleared, isBackground, width, height);

        int clearedCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!cleared[i]) continue;

            pixels[i] = Color.clear;
            clearedCount++;
        }

        Debug.Log($"[TitleSpriteBaker] Knocked out {clearedCount} of {pixels.Length} pixels ({clearedCount * 100f / pixels.Length:F1}%).");
    }

    /// <summary>
    /// A pixel survives only if it and all eight neighbours are background. Out-of-bounds counts as
    /// background so the canvas edge itself is not eroded away.
    /// </summary>
    private static bool[] Erode(bool[] isBackground, int width, int height)
    {
        bool[] eroded = new bool[isBackground.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (!isBackground[index]) continue;

                bool solid = true;
                for (int dy = -1; dy <= 1 && solid; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                        if (!isBackground[ny * width + nx])
                        {
                            solid = false;
                            break;
                        }
                    }
                }

                eroded[index] = solid;
            }
        }

        return eroded;
    }

    private static bool[] FloodFromEdges(bool[] fillable, int width, int height)
    {
        bool[] cleared = new bool[fillable.Length];
        var queue = new Queue<int>();

        void Enqueue(int x, int y)
        {
            int index = y * width + x;
            if (cleared[index] || !fillable[index]) return;

            cleared[index] = true;
            queue.Enqueue(index);
        }

        for (int x = 0; x < width; x++)
        {
            Enqueue(x, 0);
            Enqueue(x, height - 1);
        }
        for (int y = 0; y < height; y++)
        {
            Enqueue(0, y);
            Enqueue(width - 1, y);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;

            if (x > 0) Enqueue(x - 1, y);
            if (x < width - 1) Enqueue(x + 1, y);
            if (y > 0) Enqueue(x, y - 1);
            if (y < height - 1) Enqueue(x, y + 1);
        }

        return cleared;
    }

    /// <summary>
    /// Grows the cleared region by one pixel, but only into pixels that were background anyway.
    /// This is the exact inverse of <see cref="Erode"/> in open space, so the cut lands flush
    /// against the artwork rather than leaving a one-pixel halo of canvas colour behind.
    /// </summary>
    private static void Dilate(bool[] cleared, bool[] isBackground, int width, int height)
    {
        bool[] source = (bool[])cleared.Clone();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (cleared[index] || !isBackground[index]) continue;

                for (int dy = -1; dy <= 1 && !cleared[index]; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                        if (source[ny * width + nx])
                        {
                            cleared[index] = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    private static Color SampleBackground(Color[] pixels, int width, int height)
    {
        Color[] corners =
        {
            pixels[0],
            pixels[width - 1],
            pixels[(height - 1) * width],
            pixels[height * width - 1]
        };

        Color best = corners[0];
        int bestCount = 0;

        foreach (Color candidate in corners)
        {
            int count = 0;
            foreach (Color other in corners)
            {
                if (Matches(candidate, other)) count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                best = candidate;
            }
        }

        return best;
    }

    private static bool Matches(Color a, Color b)
    {
        // Fully transparent pixels match each other regardless of their RGB, which importers are
        // free to leave as anything.
        if (a.a <= 0.004f && b.a <= 0.004f) return true;
        if (Mathf.Abs(a.a - b.a) > ColorTolerance) return false;

        return Mathf.Abs(a.r - b.r) <= ColorTolerance
            && Mathf.Abs(a.g - b.g) <= ColorTolerance
            && Mathf.Abs(a.b - b.b) <= ColorTolerance;
    }

    private static bool TryGetContentBounds(Color[] pixels, int width, int height, out RectInt bounds)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 0.004f) continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
        {
            bounds = default;
            return false;
        }

        minX = Mathf.Max(0, minX - Padding);
        minY = Mathf.Max(0, minY - Padding);
        maxX = Mathf.Min(width - 1, maxX + Padding);
        maxY = Mathf.Min(height - 1, maxY + Padding);

        bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    private static Texture2D Crop(Color[] pixels, int width, int height, RectInt bounds)
    {
        Color[] cropped = new Color[bounds.width * bounds.height];

        for (int y = 0; y < bounds.height; y++)
        {
            System.Array.Copy(pixels, (bounds.y + y) * width + bounds.x, cropped, y * bounds.width, bounds.width);
        }

        Texture2D texture = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
        texture.SetPixels(cropped);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// An .aseprite file's sprites are sub-assets, not the main asset, so a plain
    /// <c>LoadAssetAtPath&lt;Sprite&gt;</c> returns null on a file that imported perfectly well.
    /// </summary>
    public static Sprite FirstSpriteAt(string path)
    {
        if (AssetDatabase.LoadMainAssetAtPath(path) == null) return null;
        if (AssetDatabase.LoadMainAssetAtPath(path) is Sprite mainSprite) return mainSprite;

        foreach (Object representation in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (representation is Sprite sprite) return sprite;
        }

        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (sub is Sprite sprite) return sprite;
        }

        return null;
    }
}
