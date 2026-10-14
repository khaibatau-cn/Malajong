using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Transition between screens: a lacquer sheet with a column of tiles on its edge sweeps off to the
/// right, revealing the new screen behind it.
///
/// The state change has already happened by the time this plays, so it is a reveal rather than a
/// cover-and-reveal. That keeps it free — no input is held up waiting for the transition.
/// </summary>
public class ScreenWipe : MonoBehaviour
{
    public const float Duration = 0.42f;
    /// <summary>Tiles in the edge column overlap vertically by this much, like a stacked wall.</summary>
    private const float TileOverlap = 10f;
    private const float EdgeWidth = 6f;

    private RectTransform sheet;

    /// <summary>Plays one wipe over everything on the canvas. Fire and forget.</summary>
    public static void Play(Transform canvasRoot, Sprite[] tileSprites)
    {
        if (canvasRoot == null) return;

        GameObject host = new GameObject("ScreenWipe", typeof(RectTransform));
        host.transform.SetParent(canvasRoot, false);
        host.transform.SetAsLastSibling();
        Stretch((RectTransform)host.transform);

        host.AddComponent<ScreenWipe>().Build(tileSprites);
    }

    private void Build(Sprite[] tileSprites)
    {
        sheet = new GameObject("Sheet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        sheet.SetParent(transform, false);
        Stretch(sheet);

        Image sheetImage = sheet.GetComponent<Image>();
        sheetImage.color = MalajongTheme.VermilionDeep;
        sheetImage.raycastTarget = false;

        // Gold trim along the leading edge.
        RectTransform edge = new GameObject("Edge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        edge.SetParent(sheet, false);
        edge.anchorMin = new Vector2(0f, 0f);
        edge.anchorMax = new Vector2(0f, 1f);
        edge.pivot = new Vector2(1f, 0.5f);
        edge.sizeDelta = new Vector2(EdgeWidth, 0f);
        edge.anchoredPosition = Vector2.zero;
        Image edgeImage = edge.GetComponent<Image>();
        edgeImage.color = MalajongTheme.Gold;
        edgeImage.raycastTarget = false;

        if (tileSprites == null || tileSprites.Length == 0) return;

        Vector2 tileSize = MalajongTheme.TileSize;
        float step = tileSize.y - TileOverlap;
        float height = ((RectTransform)transform).rect.height;
        int count = Mathf.CeilToInt(height / step) + 1;

        for (int i = 0; i < count; i++)
        {
            RectTransform tile = new GameObject($"EdgeTile_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            tile.SetParent(sheet, false);
            tile.anchorMin = tile.anchorMax = new Vector2(0f, 1f);
            tile.pivot = new Vector2(0.5f, 1f);
            tile.sizeDelta = tileSize;
            tile.anchoredPosition = new Vector2(0f, -i * step);
            tile.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 4f : -4f);

            Image tileImage = tile.GetComponent<Image>();
            tileImage.sprite = tileSprites[(i * 7 + 3) % tileSprites.Length];
            tileImage.raycastTarget = false;
        }
    }

    private IEnumerator Start()
    {
        // Wait one frame so the canvas has laid out and the width is real.
        yield return null;

        float travel = ((RectTransform)transform).rect.width + MalajongTheme.TileSize.x;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            // Ease in: the sheet hangs for a beat, then whips away.
            sheet.anchoredPosition = new Vector2(t * t * travel, 0f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
