using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Recreates the iconic Balatro card tooltip popup showing tile rank, suit color,
/// base Fu score, and edition bonuses on hover.
/// </summary>
public class BalatroTileTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tooltipContainer;
    [SerializeField] private TextMeshProUGUI headerTitleText;
    [SerializeField] private TextMeshProUGUI bodyScoreText;
    [SerializeField] private TextMeshProUGUI editionText;

    [Header("Animation Settings")]
    [SerializeField] private float smoothSpeed = 18f;
    [SerializeField] private Vector3 hoverOffset = new Vector3(0f, 62f, 0f);

    private CanvasGroup canvasGroup;
    private Vector3 targetScale = Vector3.zero;
    private bool isShowing = false;

    private void Awake()
    {
        if (tooltipContainer == null) tooltipContainer = gameObject;

        canvasGroup = tooltipContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = tooltipContainer.AddComponent<CanvasGroup>();

        AutoFindTextReferences();
        HideImmediate();
    }

    private void AutoFindTextReferences()
    {
        if (headerTitleText == null)
        {
            Transform headerTrans = transform.Find("HeaderBox/HeaderTitleText");
            if (headerTrans != null) headerTitleText = headerTrans.GetComponent<TextMeshProUGUI>();
        }

        if (bodyScoreText == null)
        {
            Transform bodyTrans = transform.Find("BodyBox/BodyScoreText");
            if (bodyTrans != null) bodyScoreText = bodyTrans.GetComponent<TextMeshProUGUI>();
        }

        if (editionText == null)
        {
            Transform editionTrans = transform.Find("BodyBox/EditionText");
            if (editionTrans != null) editionText = editionTrans.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (!isShowing && transform.localScale.x <= 0.01f) return;

        // Smooth scale pop-in & fade
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, isShowing ? 1f : 0f, Time.deltaTime * smoothSpeed);
        }
    }

    public void Show(Tile tile, BalatroCardVisual.CardEdition edition = BalatroCardVisual.CardEdition.Regular)
    {
        if (tile == null) return;
        AutoFindTextReferences();

        // 1. Determine Suit Hex Color & Display Name
        string suitColorHex = tile.Suit switch
        {
            TileSuit.Bamboo => "#2ECC71",     // Emerald Green
            TileSuit.Characters => "#E74C3C", // Crimson Red
            TileSuit.Dots => "#3498DB",       // Bright Blue
            TileSuit.Honor => "#F1C40F",      // Gold
            _ => "#FFFFFF"
        };

        string rankName = GetRankDisplayName(tile);
        string suitName = tile.Suit.ToString();

        // 2. Format Header Text (e.g. "Queen of Diamonds" -> "9 of Bamboo")
        if (headerTitleText != null)
        {
            headerTitleText.text = $"<color=#2C3E50><b>{rankName}</b></color> of <color={suitColorHex}><b>{suitName}</b></color>";
        }

        // 3. Format Base Fu Score
        int baseFu = CalculateBaseTileFu(tile);
        if (bodyScoreText != null)
        {
            bodyScoreText.text = $"<color=#3498DB><b>+{baseFu} Fu</b></color>";
        }

        // 4. Format Holographic Edition Trait
        if (editionText != null)
        {
            switch (edition)
            {
                case BalatroCardVisual.CardEdition.Foil:
                    editionText.gameObject.SetActive(true);
                    editionText.text = "<color=#F1C40F><b>+50 Fu</b> (FOIL)</color>";
                    break;
                case BalatroCardVisual.CardEdition.Polychrome:
                    editionText.gameObject.SetActive(true);
                    editionText.text = "<color=#E74C3C><b>X1.5 Fan</b> (POLYCHROME)</color>";
                    break;
                case BalatroCardVisual.CardEdition.Negative:
                    editionText.gameObject.SetActive(true);
                    editionText.text = "<color=#9B59B6><b>+1 Hand</b> (NEGATIVE)</color>";
                    break;
                default:
                    editionText.gameObject.SetActive(false);
                    break;
            }
        }

        // Position & Popup Pop
        transform.localPosition = hoverOffset;
        targetScale = Vector3.one;
        isShowing = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isShowing = false;
        targetScale = Vector3.zero;
    }

    public void HideImmediate()
    {
        isShowing = false;
        targetScale = Vector3.zero;
        transform.localScale = Vector3.zero;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
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
