using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileUI : MonoBehaviour
{
    public Tile BoundTile { get; private set; }
    private UIManager uiManager;
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    [Header("UI References")]
    public Transform CardVisual;        // Child transform that gets lifted vertically when selected
    public Image BackgroundImage;
    public TextMeshProUGUI TileText;

    public void Initialize(Tile tile, UIManager manager)
    {
        BoundTile = tile;
        uiManager = manager;
        isSelected = false;
        
        // Auto-find CardVisual child if unassigned
        if (CardVisual == null)
        {
            Transform visualChild = transform.Find("TileFace");
            CardVisual = visualChild != null ? visualChild : transform;
        }

        if (BackgroundImage == null) BackgroundImage = GetComponentInChildren<Image>();
        if (TileText == null) TileText = GetComponentInChildren<TextMeshProUGUI>();
        
        Button btn = GetComponent<Button>();
        if (btn == null) btn = GetComponentInChildren<Button>();
        if (btn != null) btn.transition = Selectable.Transition.None;
        
        UpdateVisuals();
    }

    public void OnTileClicked()
    {
        SetSelected(!isSelected);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        uiManager?.OnTileSelectionChanged(this, isSelected);
        UpdateVisuals();
    }
    
    public void ForceDeselect()
    {
        isSelected = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Smoothly/instantly offset child CardVisual up by 25px without breaking HorizontalLayoutGroup!
        if (CardVisual != null && CardVisual != transform)
        {
            CardVisual.localPosition = isSelected ? new Vector3(0, 25f, 0) : Vector3.zero;
        }

        if (TileText != null && BoundTile != null)
        {
            string suitColor = BoundTile.Suit switch
            {
                TileSuit.Bamboo => "#2ECC71",     // Green
                TileSuit.Characters => "#E74C3C", // Red
                TileSuit.Dots => "#3498DB",       // Blue
                TileSuit.Honor => "#F1C40F",      // Gold
                _ => "#FFFFFF"
            };

            // Format suit name cleanly on top, rank bold on bottom
            TileText.text = $"<size=70%><color={suitColor}><b>{BoundTile.Suit}</b></color></size>\n<size=120%><b>{BoundTile.Rank}</b></size>";
        }

        if (BackgroundImage != null)
        {
            // Vibrant green when selected, off-white card face when unselected
            BackgroundImage.color = isSelected ? new Color(0.2f, 0.95f, 0.35f, 1f) : new Color(0.96f, 0.96f, 0.96f, 1f);
        }
    }
}
