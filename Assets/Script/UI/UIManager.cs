using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    
    [Header("UI References")]
    public Transform HandContainer; // A Horizontal Layout Group
    public GameObject TilePrefab;   // A UI Button with TileUI script attached
    public TextMeshProUGUI StatusText;         // Shows score, hands, discards
    public TextMeshProUGUI DebugHintText;      // Shows playable hands/combos for debugging
    
    private List<TileUI> spawnedTileUIs = new List<TileUI>();
    private List<TileUI> selectedTileUIs = new List<TileUI>();

    void Start()
    {
        if (gameManager != null)
        {
            gameManager.OnHandUpdated += RefreshHandDisplay;
            gameManager.OnStateChanged += UpdateStatusText;
            
            RefreshHandDisplay();
            UpdateStatusText();
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnHandUpdated -= RefreshHandDisplay;
            gameManager.OnStateChanged -= UpdateStatusText;
        }
    }

    public void OnTileSelectionChanged(TileUI tileUI, bool isSelected)
    {
        if (isSelected)
        {
            if (!selectedTileUIs.Contains(tileUI)) selectedTileUIs.Add(tileUI);
        }
        else
        {
            selectedTileUIs.Remove(tileUI);
        }
    }

    // Link this to your "Play Combo" UI Button
    public void PlaySelected()
    {
        if (selectedTileUIs.Count == 0) return;
        
        List<Tile> tilesToPlay = new List<Tile>();
        foreach (var t in selectedTileUIs) tilesToPlay.Add(t.BoundTile);
        
        gameManager.PlaySelectedTiles(tilesToPlay);
        ClearSelection();
    }

    // Link this to your "Discard" UI Button
    public void DiscardSelected()
    {
        if (selectedTileUIs.Count == 0) return;
        
        List<Tile> tilesToDiscard = new List<Tile>();
        foreach (var t in selectedTileUIs) tilesToDiscard.Add(t.BoundTile);
        
        gameManager.DiscardSelectedTiles(tilesToDiscard);
        ClearSelection();
    }

    // Debug Button Handler: Auto-Select Best Playable Combo
    public void AutoSelectBestCombo()
    {
        ClearSelection();

        if (gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count == 0)
        {
            Debug.Log("[Debug] No playable combo in current hand!");
            return;
        }

        // Pick the first playable combo
        var (combo, comboTiles) = playable[0];
        List<TileUI> remainingUIs = new List<TileUI>(spawnedTileUIs);

        foreach (var targetTile in comboTiles)
        {
            var uiMatch = remainingUIs.FirstOrDefault(u => u.BoundTile == targetTile);
            if (uiMatch != null)
            {
                uiMatch.SetSelected(true);
                remainingUIs.Remove(uiMatch);
            }
        }
    }

    private void ClearSelection()
    {
        foreach (var tileUI in spawnedTileUIs)
        {
            tileUI.ForceDeselect();
        }
        selectedTileUIs.Clear();
    }

    private void RefreshHandDisplay()
    {
        foreach (var oldTile in spawnedTileUIs)
        {
            if (oldTile != null) Destroy(oldTile.gameObject);
        }
        spawnedTileUIs.Clear();
        selectedTileUIs.Clear();
        
        if (gameManager == null || gameManager.Hand == null) return;

        foreach (var tile in gameManager.Hand.Tiles)
        {
            GameObject newTileObj = Instantiate(TilePrefab, HandContainer);
            TileUI tileUI = newTileObj.GetComponent<TileUI>();
            if (tileUI != null)
            {
                tileUI.Initialize(tile, this);
                spawnedTileUIs.Add(tileUI);
                
                Button btn = newTileObj.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(tileUI.OnTileClicked);
            }
        }

        UpdateDebugHints();
    }

    private void UpdateStatusText()
    {
        if (StatusText != null && gameManager != null)
        {
            StatusText.text = $"Score: {gameManager.CurrentScore} / {gameManager.CurrentTargetScore}\n" +
                              $"Hands: {gameManager.HandsRemaining} | Discards: {gameManager.DiscardsRemaining}\n" +
                              $"State: {gameManager.State}";
        }
        UpdateDebugHints();
    }

    private void UpdateDebugHints()
    {
        if (DebugHintText == null || gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count > 0)
        {
            var comboDescriptions = playable.Select(p => $"{p.combo.Name} ({string.Join("-", p.tiles.Select(t => $"{t.Suit} {t.Rank}"))})");
            DebugHintText.text = $"<b><color=#2ECC71>DEBUG HINT - Playable Combos:</color></b>\n" + string.Join("\n", comboDescriptions);
        }
        else
        {
            DebugHintText.text = "<b><color=#E74C3C>DEBUG HINT:</color></b> No valid combo found in current hand. Try discarding tiles!";
        }
    }
}
