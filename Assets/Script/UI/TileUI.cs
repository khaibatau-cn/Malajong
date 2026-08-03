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
    public Transform CardVisual;        // Child transform that gets lifted vertically when selected
    public Image BackgroundImage;
    public Image TileSpriteImage;      // Displays the pixel art sprite from sheet.png
    public Image SelectionGlow;        // Optional highlight border
    public TextMeshProUGUI TileText;   // Fallback text or badge

    [Header("Juice & Animation Settings")]
    public float LiftHeight = 28f;
    public float HoverScale = 1.08f;
    public float SmoothSpeed = 20f;

    private bool isHovered = false;
    private Vector3 targetLocalPos = Vector3.zero;
    private Vector3 targetScale = Vector3.one;

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
                spriteRect.offsetMin = new Vector2(4, 4);
                spriteRect.offsetMax = new Vector2(-4, -4);

                TileSpriteImage = spriteObj.GetComponent<Image>();
                TileSpriteImage.preserveAspect = true;
                TileSpriteImage.raycastTarget = false;
            }
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
        
        UpdateVisuals();
    }

    void Update()
    {
        // Smoothly interpolate position and scale for juicy game feel
        if (CardVisual != null)
        {
            CardVisual.localPosition = Vector3.Lerp(CardVisual.localPosition, targetLocalPos, Time.deltaTime * SmoothSpeed);
            CardVisual.localScale = Vector3.Lerp(CardVisual.localScale, targetScale, Time.deltaTime * SmoothSpeed);
        }
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
            CardVisual.localScale = Vector3.one * 1.25f;
        }

        uiManager?.OnTileSelectionChanged(this, isSelected);
        UpdateVisuals();
    }
    
    public void ForceDeselect()
    {
        isSelected = false;
        UpdateVisuals();
    }

    public void TriggerScoreBounce()
    {
        if (CardVisual != null)
        {
            CardVisual.localScale = Vector3.one * 1.35f;
            targetLocalPos = new Vector3(0, LiftHeight * 1.5f, 0);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = Vector3.one * HoverScale;
        MalajongAudio.Instance?.PlayTileHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = Vector3.one;
    }

    private void UpdateVisuals()
    {
        // Lift child visual smoothly when selected
        targetLocalPos = isSelected ? new Vector3(0, LiftHeight, 0) : Vector3.zero;

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
                TileSpriteImage.color = isSelected ? new Color(1f, 1f, 0.75f, 1f) : Color.white;
                TileSpriteImage.preserveAspect = true;
            }

            // Hide fallback text since pixel sprite is actively rendered
            if (TileText != null)
            {
                TileText.gameObject.SetActive(false);
            }

            // Make outer card background transparent so the pixel tile doesn't have an awkward outer box
            if (BackgroundImage != null)
            {
                BackgroundImage.color = isSelected ? new Color(0.18f, 0.85f, 0.35f, 0.4f) : Color.clear;
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
                string suitColor = BoundTile.Suit switch
                {
                    TileSuit.Bamboo => "#2ECC71",     // Green
                    TileSuit.Characters => "#E74C3C", // Red
                    TileSuit.Dots => "#3498DB",       // Blue
                    TileSuit.Honor => "#F1C40F",      // Gold
                    _ => "#FFFFFF"
                };

                TileText.text = $"<size=70%><color={suitColor}><b>{BoundTile.Suit}</b></color></size>\n<size=120%><b>{BoundTile.Rank}</b></size>";
            }

            if (BackgroundImage != null)
            {
                BackgroundImage.color = isSelected ? new Color(0.2f, 0.95f, 0.35f, 1f) : new Color(0.96f, 0.96f, 0.96f, 1f);
            }
        }

        // Selection glow highlight
        if (SelectionGlow != null)
        {
            SelectionGlow.gameObject.SetActive(isSelected);
        }
    }
}
