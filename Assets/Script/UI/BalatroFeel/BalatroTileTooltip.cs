using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Recreates the iconic Balatro card tooltip popup showing tile rank, suit color,
/// base Fu score, and edition bonuses on hover.
/// </summary>
public class BalatroTileTooltip : MonoBehaviour
{
    // Box metrics, shared with SceneSetupTool so the built layout and the runtime resize agree.
    public const float Width = 220f;
    public const float BaseHeight = 124f;
    public const float EditionHeight = 28f;

    [Header("UI References")]
    [SerializeField] private GameObject tooltipContainer;
    [SerializeField] private TextMeshProUGUI headerTitleText;
    [SerializeField] private TextMeshProUGUI bodyScoreText;
    [SerializeField] private TextMeshProUGUI editionText;

    [Header("Animation Settings")]
    [SerializeField] private float smoothSpeed = 22f;

    // Not serialized: older prefabs stored 56, which overlaps the top of the 2x tile.
    private static readonly Vector3 hoverOffset = new Vector3(0f, MalajongTheme.TileArtHeight * MalajongTheme.TileScale / 2f + 10f, 0f);

    private CanvasGroup canvasGroup;
    private float popScale;
    private float targetPopScale;
    private bool isShowing = false;

    private void Awake()
    {
        if (tooltipContainer == null) tooltipContainer = gameObject;

        canvasGroup = tooltipContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = tooltipContainer.AddComponent<CanvasGroup>();

        AutoFindTextReferences();
        ApplyStyle();
        HideImmediate();
    }

    /// <summary>
    /// Makes the card readable whichever prefab it lives on. Prefabs built before the dark restyle
    /// have white header/body boxes sized for the old 160x72 card, and the bone and suit colours
    /// below wash out on white — so those boxes are recoloured and re-laid out here at runtime.
    /// Prefabs from the current SceneSetupTool have no box images and are left as built.
    /// </summary>
    private void ApplyStyle()
    {
        Canvas sortCanvas = GetComponent<Canvas>();
        if (sortCanvas == null) sortCanvas = gameObject.AddComponent<Canvas>();
        sortCanvas.overrideSorting = true;
        sortCanvas.sortingOrder = 20;

        Transform frame = transform.Find("Frame");
        if (frame == null) return;

        Image header = frame.Find("HeaderBox")?.GetComponent<Image>();
        Image body = frame.Find("BodyBox")?.GetComponent<Image>();
        if (header == null && body == null) return;

        // Old layout: the frame's own colour shows through as a gold border and divider.
        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null) frameImage.color = MalajongTheme.Gold;
        PinRow(header, 4f, 70f);
        PinRow(body, 76f, 44f);

        foreach (TextMeshProUGUI text in new[] { headerTitleText, bodyScoreText })
        {
            if (text == null) continue;
            text.enableAutoSizing = false;
            text.fontSize = MalajongTheme.TextCrisp2x;
            text.color = MalajongTheme.Bone;
        }
        if (editionText != null) editionText.fontSize = MalajongTheme.TextMin;
    }

    /// <summary>Dark row pinned <paramref name="top"/> pixels below the frame's top edge, 4px in from the sides.</summary>
    private static void PinRow(Image row, float top, float height)
    {
        if (row == null) return;
        row.color = MalajongTheme.Ink;
        RectTransform rect = row.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(4f, -top - height);
        rect.offsetMax = new Vector2(-4f, -top);
    }

    private void AutoFindTextReferences()
    {
        if (headerTitleText == null)
        {
            Transform headerTrans = transform.Find("Frame/HeaderBox/HeaderTitleText");
            if (headerTrans == null) headerTrans = transform.Find("HeaderBox/HeaderTitleText");
            if (headerTrans != null) headerTitleText = headerTrans.GetComponent<TextMeshProUGUI>();
        }

        if (bodyScoreText == null)
        {
            Transform bodyTrans = transform.Find("Frame/BodyBox/BodyScoreText");
            if (bodyTrans == null) bodyTrans = transform.Find("BodyBox/BodyScoreText");
            if (bodyTrans != null) bodyScoreText = bodyTrans.GetComponent<TextMeshProUGUI>();
        }

        if (editionText == null)
        {
            Transform editionTrans = transform.Find("Frame/BodyBox/EditionText");
            if (editionTrans == null) editionTrans = transform.Find("BodyBox/EditionText");
            if (editionTrans != null) editionText = editionTrans.GetComponent<TextMeshProUGUI>();
        }
    }

    private void LateUpdate()
    {
        // CRITICAL: Lock rotation upright to screen to eliminate 3D tilt slanting/blurriness!
        transform.rotation = Quaternion.identity;

        if (!isShowing && popScale <= 0.01f) return;

        // Smooth scale pop-in & fade. The tile under us scales when hovered or punched; dividing
        // that back out keeps the pixel font at exactly 2x once the pop settles, instead of blurry.
        popScale = Mathf.Lerp(popScale, targetPopScale, Time.deltaTime * smoothSpeed);
        if (isShowing && popScale > 0.995f) popScale = 1f;
        transform.localScale = Vector3.one * (popScale / ParentScaleOnCanvas());
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, isShowing ? 1f : 0f, Time.deltaTime * smoothSpeed);
        }
    }

    public void Show(Tile tile, BalatroCardVisual.CardEdition edition = BalatroCardVisual.CardEdition.Regular)
    {
        if (tile == null) return;
        AutoFindTextReferences();

        // 1. Name on two lines: rank in bone, suit in the same colour the rest of the HUD uses for it.
        //    Honors already say what they are ("Red Dragon"), so the suit line just reads "Honor".
        string suitColorHex = MalajongTheme.HexOf(MalajongTheme.ForSuit(tile.Suit));
        if (headerTitleText != null)
        {
            headerTitleText.text = $"<color={MalajongTheme.HexBone}>{GetRankDisplayName(tile)}</color>\n<color={suitColorHex}>{tile.Suit}</color>";
        }

        // 2. Base Fu, in the Fu colour from the score panel
        int baseFu = CalculateBaseTileFu(tile);
        if (bodyScoreText != null)
        {
            bodyScoreText.text = $"<color={MalajongTheme.HexFu}>+{baseFu} Fu</color>";
        }

        // 4. Format Holographic Edition Trait
        if (editionText != null)
        {
            switch (edition)
            {
                case BalatroCardVisual.CardEdition.Foil:
                    editionText.gameObject.SetActive(true);
                    editionText.text = $"<color={MalajongTheme.HexGold}>+50 Fu FOIL</color>";
                    break;
                case BalatroCardVisual.CardEdition.Polychrome:
                    editionText.gameObject.SetActive(true);
                    editionText.text = $"<color={MalajongTheme.HexOf(MalajongTheme.FanText)}>x1.5 Fan POLY</color>";
                    break;
                case BalatroCardVisual.CardEdition.Negative:
                    editionText.gameObject.SetActive(true);
                    editionText.text = $"<color={MalajongTheme.HexBoneDim}>+1 Hand NEGATIVE</color>";
                    break;
                default:
                    editionText.gameObject.SetActive(false);
                    break;
            }
        }

        // Room for the edition line, growing upward (the pivot is the bottom edge).
        bool hasEdition = editionText != null && editionText.gameObject.activeSelf;
        ((RectTransform)transform).sizeDelta = new Vector2(Width, BaseHeight + (hasEdition ? EditionHeight : 0f));

        // Position & Popup Pop
        transform.localPosition = hoverOffset;
        targetPopScale = 1f;
        isShowing = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isShowing = false;
        targetPopScale = 0f;
    }

    public void HideImmediate()
    {
        isShowing = false;
        targetPopScale = 0f;
        popScale = 0f;
        transform.localScale = Vector3.zero;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>How much the tile we hang off is scaled, relative to the canvas itself.</summary>
    private float ParentScaleOnCanvas()
    {
        Transform parent = transform.parent;
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (parent == null || canvas == null) return 1f;

        float canvasScale = canvas.rootCanvas.transform.lossyScale.x;
        float scale = canvasScale > 0f ? parent.lossyScale.x / canvasScale : 1f;
        return scale > 0.01f ? scale : 1f;
    }

    private static string GetRankDisplayName(Tile tile)
    {
        if (tile.Suit == TileSuit.Honor)
        {
            return tile.Rank switch
            {
                1 => "East Wind",
                2 => "South Wind",
                3 => "West Wind",
                4 => "North Wind",
                5 => "Red Dragon",
                6 => "Green Dragon",
                7 => "White Dragon",
                _ => $"Honor {tile.Rank}"
            };
        }

        return tile.Rank switch
        {
            1 => "Ace (1)",
            9 => "Terminal (9)",
            _ => $"Rank {tile.Rank}"
        };
    }

    private static int CalculateBaseTileFu(Tile tile)
    {
        if (tile == null) return 2;

        // Terminal (1, 9) and Honor tiles are worth higher base Fu
        if (tile.Suit == TileSuit.Honor || tile.Rank == 1 || tile.Rank == 9)
        {
            return 4;
        }
        return 2;
    }
}
