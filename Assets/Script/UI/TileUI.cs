using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TileUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Tile BoundTile { get; private set; }
    private UIManager uiManager;
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    [Header("UI References")]
    public Transform CardVisual;        // Child transform that gets lifted vertically
    public Image BackgroundImage;
    public Image TileSpriteImage;      // Displays the pixel art sprite from sheet.png
    public Image SelectionGlow;        // Optional highlight border
    public TextMeshProUGUI TileText;   // Fallback text or badge
    public BalatroCardVisual BalatroVisual { get; private set; }
    public BalatroTileTooltip Tooltip { get; private set; }

    [Header("Juice & Animation Settings")]
    public float LiftHeight = 36f;
    public float HoverLift = 14f;
    public float SmoothSpeed = 24f;

    private bool isHovered = false;
    private Vector3 targetLocalPos = Vector3.zero;
    private Vector3 targetScale = Vector3.one;
    private bool isWaitingToDeal = false;

    // Shadow behind the art and a suit border over it, built at runtime so older prefabs pick them up too.
    private Image shadowImage;
    private GameObject suitBorder;
    private Image[] suitBorderEdges;
    /// <summary>Suit border thickness: two art pixels at the 2x tile scale.</summary>
    private const float SuitBorderWidth = 4f;

    public void Initialize(Tile tile, UIManager manager)
    {
        BoundTile = tile;
        uiManager = manager;
        isSelected = false;
        isHovered = false;
        
        // Auto-find CardVisual child if unassigned
        if (CardVisual == null)
        {
            Transform visualChild = transform.Find("TileFace");
            CardVisual = visualChild != null ? visualChild : transform;
        }

        // Auto-attach BalatroCardVisual for iconic 3D tilt, spring animations & edition shaders
        if (CardVisual != null)
        {
            BalatroVisual = CardVisual.GetComponent<BalatroCardVisual>();
            if (BalatroVisual == null)
            {
                BalatroVisual = CardVisual.gameObject.AddComponent<BalatroCardVisual>();
            }
        }

        // Auto-find or attach BalatroTileTooltip
        Tooltip = GetComponentInChildren<BalatroTileTooltip>();
        if (Tooltip == null && CardVisual != null)
        {
            Tooltip = CardVisual.GetComponentInChildren<BalatroTileTooltip>();
        }

        if (BackgroundImage == null && CardVisual != null) BackgroundImage = CardVisual.GetComponent<Image>();
        
        if (TileSpriteImage == null && CardVisual != null)
        {
            Transform iconTransform = CardVisual.Find("TileSpriteImage");
            if (iconTransform != null)
            {
                TileSpriteImage = iconTransform.GetComponent<Image>();
            }
            else
            {
                // Auto-create TileSpriteImage child so it renders on ANY prefab instance!
                GameObject spriteObj = new GameObject("TileSpriteImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                spriteObj.transform.SetParent(CardVisual, false);
                
                RectTransform spriteRect = spriteObj.GetComponent<RectTransform>();
                spriteRect.anchorMin = Vector2.zero;
                spriteRect.anchorMax = Vector2.one;
                spriteRect.offsetMin = Vector2.zero;
                spriteRect.offsetMax = Vector2.zero;

                TileSpriteImage = spriteObj.GetComponent<Image>();
                TileSpriteImage.preserveAspect = false;
                TileSpriteImage.raycastTarget = false;
            }
        }
        
        if (TileSpriteImage != null)
        {
            TileSpriteImage.preserveAspect = false;
        }

        if (TileText == null && CardVisual != null) TileText = CardVisual.GetComponentInChildren<TextMeshProUGUI>();
        
        Button btn = GetComponent<Button>();
        if (btn == null) btn = GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnTileClicked);
        }

        targetLocalPos = Vector3.zero;
        targetScale = Vector3.one;
        if (CardVisual != null)
        {
            CardVisual.localPosition = Vector3.zero;
            CardVisual.localScale = Vector3.one;
        }

        EnsureDepthLayers();
        UpdateVisuals();
    }

    /// <summary>
    /// Adds a cast shadow behind the tile art and a suit-coloured border over it.
    ///
    /// The shadow is the tile's own silhouette, so its corners match the art. It is named "Shadow"
    /// because BalatroCardVisual already looks for a child of that name and pushes it further out as
    /// the tile lifts, which is what sells the height.
    ///
    /// The border is drawn on top of the art, inside the tile's edge. Drawn behind and outside, the
    /// next tile in the rack would cover its right side and leave a lopsided outline.
    /// </summary>
    private void EnsureDepthLayers()
    {
        if (CardVisual == null) return;

        Sprite art = BoundTile != null && BoundTile.Data != null ? BoundTile.Data.TileSprite : null;

        Transform existingShadow = CardVisual.Find("Shadow");
        GameObject shadow = existingShadow != null
            ? existingShadow.gameObject
            : new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadow.transform.SetParent(CardVisual, false);
        shadow.transform.SetAsFirstSibling();
        Stretch(shadow.GetComponent<RectTransform>());

        shadowImage = shadow.GetComponent<Image>();
        shadowImage.raycastTarget = false;
        shadowImage.sprite = art;
        shadowImage.color = MalajongTheme.TileShadow;
        shadowImage.enabled = art != null;

        Transform existingBorder = CardVisual.Find("SuitBorder");
        if (existingBorder != null)
        {
            suitBorder = existingBorder.gameObject;
            suitBorderEdges = suitBorder.GetComponentsInChildren<Image>(true);
        }
        else
        {
            suitBorder = new GameObject("SuitBorder", typeof(RectTransform));
            suitBorder.transform.SetParent(CardVisual, false);
            Stretch(suitBorder.GetComponent<RectTransform>());

            // Left, right, bottom, top: anchored to each edge, SuitBorderWidth thick.
            (Vector2 min, Vector2 max)[] edges =
            {
                (new Vector2(0f, 0f), new Vector2(0f, 1f)),
                (new Vector2(1f, 0f), new Vector2(1f, 1f)),
                (new Vector2(0f, 0f), new Vector2(1f, 0f)),
                (new Vector2(0f, 1f), new Vector2(1f, 1f)),
            };
            suitBorderEdges = new Image[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                GameObject edge = new GameObject($"Edge_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                edge.transform.SetParent(suitBorder.transform, false);
                RectTransform rect = edge.GetComponent<RectTransform>();
                rect.anchorMin = edges[i].min;
                rect.anchorMax = edges[i].max;
                bool vertical = i < 2;
                rect.pivot = new Vector2(i == 1 ? 1f : 0f, i == 3 ? 1f : 0f);
                rect.sizeDelta = vertical ? new Vector2(SuitBorderWidth, 0f) : new Vector2(0f, SuitBorderWidth);
                rect.anchoredPosition = Vector2.zero;

                Image image = edge.GetComponent<Image>();
                image.raycastTarget = false;
                suitBorderEdges[i] = image;
            }
        }

        // Last sibling, so the border draws over the art rather than under it.
        suitBorder.transform.SetAsLastSibling();
        suitBorder.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        // Held at zero scale until this tile's turn in the deal stagger.
        if (isWaitingToDeal) return;

        // Smoothly interpolate position and scale for juicy game feel
        if (CardVisual != null)
        {
            CardVisual.localPosition = Vector3.Lerp(CardVisual.localPosition, targetLocalPos, Time.deltaTime * SmoothSpeed);
            CardVisual.localScale = Vector3.Lerp(CardVisual.localScale, targetScale, Time.deltaTime * SmoothSpeed);
        }
    }

    /// <summary>
    /// Deals this tile in after a delay. Rather than running its own tween, it parks the tile
    /// at zero scale and then hands control back to Update's existing lerp, which springs it
    /// to full size — so the deal never fights the hover/select animation.
    /// </summary>
    public void PlayDealIn(float delay)
    {
        if (CardVisual == null) return;

        // The hand is refreshed after the round-end state change, so tiles can be spawned
        // under an already-hidden PlayingPanel. Unity can't run a coroutine on an inactive
        // object, and there's nothing to watch anyway — settle at full size instead. The
        // next round's RefreshHandDisplay deals them in properly.
        if (!isActiveAndEnabled)
        {
            CardVisual.localScale = Vector3.one;
            isWaitingToDeal = false;
            return;
        }

        CardVisual.localScale = Vector3.zero;
        isWaitingToDeal = true;
        StartCoroutine(DealInRoutine(delay));
    }

    private IEnumerator DealInRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        isWaitingToDeal = false;
        MalajongAudio.Instance?.PlayTileHover();
    }

    private void OnEnable()
    {
        // Deactivating an object kills its coroutines, so a tile hidden mid-deal would come
        // back with isWaitingToDeal stuck true and stay invisible. Clear it on re-enable.
        if (!isWaitingToDeal) return;

        isWaitingToDeal = false;
        if (CardVisual != null) CardVisual.localScale = Vector3.one;
    }

    public void OnTileClicked()
    {
        SetSelected(!isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Punch scale bounce on click
        if (CardVisual != null)
        {
            CardVisual.localScale = Vector3.one * 1.15f;
        }

        BalatroVisual?.TriggerSelectPunch();
        uiManager?.OnTileSelectionChanged(this, isSelected);
        UpdateVisuals();
    }
    
    public void ForceDeselect()
    {
        isSelected = false;
        isHovered = false;
        UpdateVisuals();
    }

    public void TriggerScoreBounce()
    {
        if (CardVisual != null)
        {
            CardVisual.localScale = Vector3.one * 1.25f;
            targetLocalPos = new Vector3(0, LiftHeight * 1.35f, 0);
        }
        BalatroVisual?.TriggerScorePunch();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = Vector3.one * 1.04f;
        BalatroVisual?.TriggerHoverPunch();
        Tooltip?.Show(BoundTile, BalatroVisual != null ? BalatroVisual.Edition : BalatroCardVisual.CardEdition.Regular);
        UpdateVisuals();
        MalajongAudio.Instance?.PlayTileHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = Vector3.one;
        Tooltip?.Hide();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Determine vertical lift smoothly: Selected = LiftHeight (36px), Hovered = HoverLift (14px), Idle = 0px
        float targetY = 0f;
        if (isSelected)
        {
            targetY = LiftHeight;
        }
        else if (isHovered)
        {
            targetY = HoverLift;
        }
        targetLocalPos = new Vector3(0, targetY, 0);

        if (TileSpriteImage == null && CardVisual != null)
        {
            Transform iconTransform = CardVisual.Find("TileSpriteImage");
            if (iconTransform != null) TileSpriteImage = iconTransform.GetComponent<Image>();
        }

        bool hasSprite = BoundTile != null && BoundTile.Data != null && BoundTile.Data.TileSprite != null;

        // Render pixel art TileSprite if available
        if (hasSprite)
        {
            if (TileSpriteImage != null)
            {
                TileSpriteImage.gameObject.SetActive(true);
                TileSpriteImage.sprite = BoundTile.Data.TileSprite;
                TileSpriteImage.color = isSelected ? MalajongTheme.TileSelectTint : Color.white;
                TileSpriteImage.preserveAspect = false; // Stretch to fill full rectangular card!
            }

            // Hide fallback text since pixel sprite is actively rendered
            if (TileText != null)
            {
                TileText.gameObject.SetActive(false);
            }

            if (BackgroundImage != null)
            {
                BackgroundImage.color = isSelected ? MalajongTheme.TileSelectGlow : Color.clear;
            }
        }
        else
        {
            // Fallback formatted text rendering
            if (TileSpriteImage != null)
            {
                TileSpriteImage.gameObject.SetActive(false);
            }

            if (TileText != null && BoundTile != null)
            {
                TileText.gameObject.SetActive(true);
                string suitColor = MalajongTheme.HexOf(MalajongTheme.ForSuit(BoundTile.Suit));

                TileText.text = $"<size=70%><color={suitColor}><b>{BoundTile.Suit}</b></color></size>\n<size=120%><b>{BoundTile.Rank}</b></size>";
            }

            if (BackgroundImage != null)
            {
                BackgroundImage.color = isSelected ? MalajongTheme.Gold : MalajongTheme.Bone;
            }
        }

        // Selection glow highlight
        if (SelectionGlow != null)
        {
            SelectionGlow.gameObject.SetActive(isSelected);
        }

        // Border in the tile's suit colour, so a selection shows which suit it will feed.
        if (suitBorder != null)
        {
            bool show = isSelected && BoundTile != null && BoundTile.Data != null;
            suitBorder.SetActive(show);
            if (show)
            {
                Color suitColor = MalajongTheme.ForSuit(BoundTile.Suit);
                foreach (Image edge in suitBorderEdges) edge.color = suitColor;
            }
        }
    }
}
