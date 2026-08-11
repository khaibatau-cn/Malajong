using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A drifting field of mahjong tiles, used as living wallpaper behind the title screen.
///
/// Tiles are spawned once at random positions, then drift, spin and wrap around the edges forever.
/// Size drives both speed and opacity — larger tiles move faster and sit brighter, smaller ones
/// hang back dim and slow — which reads as depth without needing a second camera or any real
/// parallax rig.
///
/// Everything is deliberately low-contrast: this is texture behind the menu, not something to look
/// at. If a tile ever competes with the START RUN button, lower <see cref="AlphaRange"/> rather
/// than removing tiles.
/// </summary>
public class FloatingTileField : MonoBehaviour
{
    [Header("Source Art")]
    /// <summary>Sprites to draw from. Assigned by SceneSetupTool from the TileData assets.</summary>
    public Sprite[] TilePool;

    [Header("Field")]
    public int Count = 18;
    /// <summary>Tile height in reference pixels, min to max. Width follows the sprite's aspect.</summary>
    public Vector2 SizeRange = new Vector2(52f, 104f);
    /// <summary>How far outside the screen a tile travels before wrapping, so nothing pops in mid-view.</summary>
    public float WrapMargin = 90f;

    [Header("Motion")]
    /// <summary>Drift speed in pixels per second, mapped from the size range.</summary>
    public Vector2 DriftSpeedRange = new Vector2(7f, 22f);
    /// <summary>Maximum horizontal drift as a fraction of vertical speed. Keeps the field mostly rising.</summary>
    public float HorizontalDrift = 0.35f;
    /// <summary>Maximum spin in degrees per second, either direction.</summary>
    public float MaxSpin = 9f;

    [Header("Appearance")]
    /// <summary>Opacity, mapped from the size range. Low by design — this is wallpaper.</summary>
    public Vector2 AlphaRange = new Vector2(0.05f, 0.15f);
    /// <summary>Maximum tilt a tile can be spawned at, degrees either way.</summary>
    public float MaxSpawnTilt = 25f;

    private RectTransform fieldRect;
    private RectTransform[] tiles;
    private Vector2[] velocities;
    private float[] spins;

    private void Start()
    {
        fieldRect = transform as RectTransform;

        if (fieldRect == null || TilePool == null || TilePool.Length == 0)
        {
            enabled = false;
            return;
        }

        Spawn();
    }

    private void Spawn()
    {
        Vector2 extents = fieldRect.rect.size * 0.5f;

        tiles = new RectTransform[Count];
        velocities = new Vector2[Count];
        spins = new float[Count];

        for (int i = 0; i < Count; i++)
        {
            GameObject tileObj = new GameObject($"FloatingTile_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tileObj.transform.SetParent(transform, false);

            RectTransform rect = tileObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            // depth: 0 is the far plane, 1 the near one. Everything else is derived from it, so a
            // tile is never large-but-slow or tiny-but-bright.
            float depth = Random.value;
            float height = Mathf.Lerp(SizeRange.x, SizeRange.y, depth);

            rect.sizeDelta = new Vector2(height * 0.74f, height);
            rect.anchoredPosition = new Vector2(
                Random.Range(-extents.x, extents.x),
                Random.Range(-extents.y, extents.y));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-MaxSpawnTilt, MaxSpawnTilt));

            Image image = tileObj.GetComponent<Image>();
            image.sprite = TilePool[Random.Range(0, TilePool.Length)];
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Lerp(AlphaRange.x, AlphaRange.y, depth));

            float speed = Mathf.Lerp(DriftSpeedRange.x, DriftSpeedRange.y, depth);
            velocities[i] = new Vector2(Random.Range(-HorizontalDrift, HorizontalDrift) * speed, speed);
            spins[i] = Random.Range(-MaxSpin, MaxSpin);
            tiles[i] = rect;
        }
    }

    private void Update()
    {
        if (tiles == null) return;

        // Read every frame rather than caching, so the field still fills the screen after a
        // resolution change or an aspect-ratio switch in the Game view.
        Vector2 extents = fieldRect.rect.size * 0.5f;

        for (int i = 0; i < tiles.Length; i++)
        {
            RectTransform tile = tiles[i];
            if (tile == null) continue;

            tile.anchoredPosition += velocities[i] * Time.unscaledDeltaTime;
            tile.Rotate(0f, 0f, spins[i] * Time.unscaledDeltaTime);

            tiles[i].anchoredPosition = Wrap(tile.anchoredPosition, extents);
        }
    }

    /// <summary>
    /// Teleports a tile to the opposite edge once it is fully out of view. The crossing axis keeps
    /// its position, so a tile rising off the top returns at the bottom in the same column rather
    /// than jumping sideways.
    /// </summary>
    private Vector2 Wrap(Vector2 position, Vector2 extents)
    {
        float limitX = extents.x + WrapMargin;
        float limitY = extents.y + WrapMargin;

        if (position.y > limitY) position.y = -limitY;
        else if (position.y < -limitY) position.y = limitY;

        if (position.x > limitX) position.x = -limitX;
        else if (position.x < -limitX) position.x = limitX;

        return position;
    }
}
