using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileUI : MonoBehaviour
{
    public Tile BoundTile { get; private set; }
    private UIManager uiManager;
    private bool isSelected = false;

    // References you'll hook up in the Editor Inspector
    public Image BackgroundImage;
    public TextMeshProUGUI TileText;

    public void Initialize(Tile tile, UIManager manager)
    {
        BoundTile = tile;
        uiManager = manager;
        isSelected = false;
        
        // Automatically find the components so you don't have to drag them in the Inspector!
        if (BackgroundImage == null) BackgroundImage = GetComponent<Image>();
        if (TileText == null) TileText = GetComponentInChildren<TextMeshProUGUI>();
        
        Button btn = GetComponent<Button>();
        if (btn != null) btn.transition = Selectable.Transition.None;
        
        UpdateVisuals();
    }

    public void OnTileClicked()
    {
        isSelected = !isSelected;
        uiManager.OnTileSelectionChanged(this, isSelected);
        UpdateVisuals();
    }
    
    public void ForceDeselect()
    {
        isSelected = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (TileText != null && BoundTile != null)
        {
            TileText.text = $"{BoundTile.Suit}\n{BoundTile.Rank}";
        }

        if (BackgroundImage != null)
        {
            // Simple visual cue: turns green when selected
            BackgroundImage.color = isSelected ? Color.green : Color.white;
        }
    }
}
