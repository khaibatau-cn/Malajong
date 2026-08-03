using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    
    [Header("State Panels")]
    public GameObject StartMenuPanel;
    public GameObject PlayingPanel;
    public GameObject ShopPanel;
    public GameObject GameOverPanel;
    public GameObject VictoryPanel;

    [Header("Gameplay UI References")]
    public Transform HandContainer; // A Horizontal Layout Group
    public GameObject TilePrefab;   // A UI Button with TileUI script attached
    public TextMeshProUGUI StatusText;         // Shows score, hands, discards, round, coins
    public Image ScoreProgressBar;            // Visual score gauge towards target
    public TextMeshProUGUI DebugHintText;      // Shows playable hands/combos for debugging
    public TextMeshProUGUI AffinityHUDText;   // Shows suit affinity multipliers
    public TextMeshProUGUI SpiritsHUDText;    // Shows equipped spirits

    [Header("Hand Management & Action Buttons")]
    public Button PlayButton;
    public Button DiscardButton;
    public Button SortSuitButton;
    public Button SortRankButton;
    public Button AutoComboButton;

    [Header("Balatro Combo Preview HUD")]
    public GameObject ComboPreviewBox;
    public TextMeshProUGUI PreviewComboNameText;
    public TextMeshProUGUI PreviewChipsText;
    public TextMeshProUGUI PreviewMultText;
    public TextMeshProUGUI PreviewTotalScoreText;

    [Header("Score Tally & Juice Animation")]
    public GameObject ScoreTallyBanner;
    public TextMeshProUGUI TallyChipsText;
    public TextMeshProUGUI TallyMultText;
    public TextMeshProUGUI TallyResultText;

    [Header("Shop UI References")]
    public TextMeshProUGUI ShopStatusText;
    public Transform ShopCatalogContainer;
    public List<SpiritData> ShopCatalog = new List<SpiritData>();

    [Header("Game Over & Victory UI")]
    public TextMeshProUGUI GameOverSummaryText;
    public TextMeshProUGUI VictorySummaryText;
    
    private List<TileUI> spawnedTileUIs = new List<TileUI>();
    private List<TileUI> selectedTileUIs = new List<TileUI>();
    private bool isScoringSequenceActive = false;

    void Start()
    {
        if (gameManager != null)
        {
            gameManager.OnHandUpdated += RefreshHandDisplay;
            gameManager.OnStateChanged += HandleStateChanged;
            
            RefreshHandDisplay();
            HandleStateChanged();
        }

        UpdateSelectionPreview();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnHandUpdated -= RefreshHandDisplay;
            gameManager.OnStateChanged -= HandleStateChanged;
        }
    }

    public void HandleStateChanged()
    {
        if (gameManager == null) return;

        if (StartMenuPanel != null) StartMenuPanel.SetActive(gameManager.State == GameManager.GameState.StartMenu);
        if (PlayingPanel != null) PlayingPanel.SetActive(gameManager.State == GameManager.GameState.Playing);
        if (ShopPanel != null) ShopPanel.SetActive(gameManager.State == GameManager.GameState.Shop);
        if (GameOverPanel != null) GameOverPanel.SetActive(gameManager.State == GameManager.GameState.GameOver);
        if (VictoryPanel != null) VictoryPanel.SetActive(gameManager.State == GameManager.GameState.Victory);

        if (ScoreTallyBanner != null) ScoreTallyBanner.SetActive(false);

        UpdateStatusText();
        UpdateAffinityHUD();
        UpdateSpiritsHUD();
        UpdateShopText();
        UpdateSummaryTexts();
        UpdateSelectionPreview();
    }

    // --- Hand Sorting Actions ---

    public void SortHandBySuit()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortBySuit();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 100, 0), "SORTED BY SUIT", new Color(0.18f, 0.85f, 0.35f));
    }

    public void SortHandByRank()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortByRank();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 100, 0), "SORTED BY RANK", new Color(0.2f, 0.7f, 1f));
    }

    // --- UI Button Actions ---

    public void StartRun()
    {
        if (gameManager != null) gameManager.StartGame();
    }

    public void NextRound()
    {
        if (gameManager != null) gameManager.NextRound();
    }

    public void RestartRun()
    {
        if (gameManager != null) gameManager.StartGame();
    }

    public void BuyShopItem(int catalogIndex)
    {
        if (gameManager == null || ShopCatalog == null || catalogIndex < 0 || catalogIndex >= ShopCatalog.Count) return;
        
        SpiritData targetSpirit = ShopCatalog[catalogIndex];
        bool success = gameManager.BuySpirit(targetSpirit, 5);
        if (success)
        {
            MalajongAudio.Instance?.PlayCashChime();
            FloatingBadge.Spawn(ShopPanel.transform, Input.mousePosition, $"BOUGHT {targetSpirit.SpiritName}!", new Color(0.95f, 0.8f, 0.1f));
        }
        else
        {
            Debug.Log($"[Shop UI] Could not purchase {targetSpirit.SpiritName}. Check coins or spirit slots.");
        }
    }

    public void OnTileSelectionChanged(TileUI tileUI, bool isSelected)
    {
        if (isScoringSequenceActive) return;

        if (isSelected)
        {
            if (!selectedTileUIs.Contains(tileUI))
            {
                selectedTileUIs.Add(tileUI);
                MalajongAudio.Instance?.PlayTileSelect(selectedTileUIs.Count);
            }
        }
        else
        {
            if (selectedTileUIs.Remove(tileUI))
            {
                MalajongAudio.Instance?.PlayTileDeselect();
            }
        }

        UpdateSelectionPreview();
    }

    private void UpdateSelectionPreview()
    {
        if (gameManager == null || gameManager.Hand == null) return;

        List<Tile> selectedTiles = selectedTileUIs.Select(t => t.BoundTile).Where(t => t != null).ToList();
        ScorePreview preview = ScoreEngine.PreviewScore(selectedTiles, gameManager.Hand.Tiles, gameManager.Affinity, gameManager.EquippedSpirits, gameManager);

        if (ComboPreviewBox != null)
        {
            ComboPreviewBox.SetActive(selectedTiles.Count > 0);
        }

        if (PreviewComboNameText != null)
        {
            string colorHex = preview.IsValid ? "#F1C40F" : "#E74C3C";
            PreviewComboNameText.text = $"<color={colorHex}><b>{preview.ComboName.ToUpper()}</b></color>";
        }

        if (PreviewChipsText != null)
        {
            PreviewChipsText.text = $"<color=#3498DB><b>{preview.TotalChips}</b></color> Chips";
        }

        if (PreviewMultText != null)
        {
            PreviewMultText.text = $"<color=#E74C3C><b>{preview.TotalMult:F1}X</b></color> Mult";
        }

        if (PreviewTotalScoreText != null)
        {
            PreviewTotalScoreText.text = preview.IsValid ? $"<color=#2ECC71><b>≈ {preview.ProjectedScore} PTS</b></color>" : "<color=#7F8C8D>--</color>";
        }

        if (PlayButton != null)
        {
            PlayButton.interactable = preview.IsValid && !isScoringSequenceActive;
        }

        if (DiscardButton != null)
        {
            DiscardButton.interactable = selectedTiles.Count > 0 && selectedTiles.Count <= 5 && gameManager.DiscardsRemaining > 0 && !isScoringSequenceActive;
        }
    }

    // Link this to "Play Combo" UI Button
    public void PlaySelected()
    {
        if (isScoringSequenceActive || selectedTileUIs.Count == 0 || gameManager == null) return;
        
        List<Tile> tilesToPlay = new List<Tile>();
        List<TileUI> uisToPlay = new List<TileUI>(selectedTileUIs);
        foreach (var t in selectedTileUIs) tilesToPlay.Add(t.BoundTile);

        ScorePreview preview = ScoreEngine.PreviewScore(tilesToPlay, gameManager.Hand.Tiles, gameManager.Affinity, gameManager.EquippedSpirits, gameManager);
        if (!preview.IsValid) return;

        StartCoroutine(AnimateBalatroScoringSequence(uisToPlay, tilesToPlay, preview));
    }

    // Link this to "Discard" UI Button
    public void DiscardSelected()
    {
        if (isScoringSequenceActive || selectedTileUIs.Count == 0) return;
        
        List<Tile> tilesToDiscard = new List<Tile>();
        foreach (var t in selectedTileUIs) tilesToDiscard.Add(t.BoundTile);
        
        gameManager.DiscardSelectedTiles(tilesToDiscard);
        ClearSelection();
    }

    // Auto-Select Best Playable Combo
    public void AutoSelectBestCombo()
    {
        if (isScoringSequenceActive) return;
        ClearSelection();

        if (gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count == 0)
        {
            Debug.Log("[Debug] No playable combo in current hand!");
            return;
        }

        var (combo, comboTiles) = playable[0];
        List<TileUI> remainingUIs = new List<TileUI>(spawnedTileUIs);

        int soundPitchCounter = 1;
        foreach (var targetTile in comboTiles)
        {
            var uiMatch = remainingUIs.FirstOrDefault(u => u.BoundTile == targetTile);
            if (uiMatch != null)
            {
                uiMatch.SetSelected(true);
                remainingUIs.Remove(uiMatch);
                soundPitchCounter++;
            }
        }

        UpdateSelectionPreview();
    }

    // --- Balatro-Style Animated Scoring Sequence ---

    private IEnumerator AnimateBalatroScoringSequence(List<TileUI> playedUIs, List<Tile> playedTiles, ScorePreview preview)
    {
        isScoringSequenceActive = true;
        SetControlsInteractable(false);

        // 1. Highlight and lift played tiles with visual bounce
        foreach (var tileUI in playedUIs)
        {
            if (tileUI != null) tileUI.TriggerScoreBounce();
        }

        // 2. Show score tally banner
        if (ScoreTallyBanner != null)
        {
            ScoreTallyBanner.SetActive(true);
            if (TallyChipsText != null) TallyChipsText.text = $"<color=#3498DB>0</color>";
            if (TallyMultText != null) TallyMultText.text = $"<color=#E74C3C>0.0X</color>";
            if (TallyResultText != null) TallyResultText.text = $"<b>{preview.ComboName}</b>";
        }

        yield return new WaitForSeconds(0.18f);

        // 3. Step 1: Base Chips + Tile Chips Tally
        int currentTallyChips = 0;
        int targetChips = preview.TotalChips;
        int stepChips = Mathf.Max(1, targetChips / 8);

        while (currentTallyChips < targetChips)
        {
            currentTallyChips = Mathf.Min(currentTallyChips + stepChips, targetChips);
            if (TallyChipsText != null) TallyChipsText.text = $"<color=#3498DB><b>{currentTallyChips}</b></color>";
            MalajongAudio.Instance?.PlayScoreChipTick();
            yield return new WaitForSeconds(0.04f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(-80, 80, 0), $"+{targetChips} CHIPS", new Color(0.2f, 0.6f, 1f));
        yield return new WaitForSeconds(0.15f);

        // 4. Step 2: Multiplier Tally
        float currentTallyMult = 0f;
        float targetMult = preview.TotalMult;

        while (currentTallyMult < targetMult)
        {
            currentTallyMult = Mathf.MoveTowards(currentTallyMult, targetMult, targetMult * 0.25f);
            if (TallyMultText != null) TallyMultText.text = $"<color=#E74C3C><b>{currentTallyMult:F1}X</b></color>";
            MalajongAudio.Instance?.PlayMultPop();
            yield return new WaitForSeconds(0.05f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(80, 80, 0), $"{targetMult:F1}X MULT", new Color(0.95f, 0.3f, 0.25f));
        yield return new WaitForSeconds(0.2f);

        // 5. Step 3: Big Multiplication Crunch / Slam
        int calculatedScore = Mathf.RoundToInt(targetChips * targetMult);
        MalajongAudio.Instance?.PlayScoreCrunchSlam();

        if (TallyResultText != null)
        {
            TallyResultText.text = $"<b><color=#F1C40F>{preview.ComboName}: +{calculatedScore} PTS!</color></b>";
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 140, 0), $"+{calculatedScore} PTS!", new Color(1f, 0.85f, 0.1f), 34f);
        yield return new WaitForSeconds(0.4f);

        // 6. Step 4: Smooth Score Bar Roll-Up
        int startScore = gameManager.CurrentScore;
        int endScore = startScore + calculatedScore;
        float rollDuration = 0.45f;
        float rollElapsed = 0f;

        while (rollElapsed < rollDuration)
        {
            rollElapsed += Time.deltaTime;
            float t = rollElapsed / rollDuration;
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
            UpdateLiveScoreHUD(displayScore);
            yield return null;
        }

        // 7. Commit play in GameManager
        gameManager.PlaySelectedTiles(playedTiles);

        if (ScoreTallyBanner != null) ScoreTallyBanner.SetActive(false);
        ClearSelection();
        isScoringSequenceActive = false;
        SetControlsInteractable(true);

        // Check if round was won or lost
        if (gameManager.State == GameManager.GameState.Shop || gameManager.State == GameManager.GameState.Victory)
        {
            MalajongAudio.Instance?.PlayCashChime();
            MalajongAudio.Instance?.PlayRoundWin();
        }
        else if (gameManager.State == GameManager.GameState.GameOver)
        {
            MalajongAudio.Instance?.PlayGameOver();
        }
    }

    private void SetControlsInteractable(bool interactable)
    {
        if (PlayButton != null) PlayButton.interactable = interactable;
        if (DiscardButton != null) DiscardButton.interactable = interactable;
        if (SortSuitButton != null) SortSuitButton.interactable = interactable;
        if (SortRankButton != null) SortRankButton.interactable = interactable;
        if (AutoComboButton != null) AutoComboButton.interactable = interactable;
    }

    private void ClearSelection()
    {
        foreach (var tileUI in spawnedTileUIs)
        {
            if (tileUI != null) tileUI.ForceDeselect();
        }
        selectedTileUIs.Clear();
        UpdateSelectionPreview();
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
                
                Button btn = newTileObj.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(tileUI.OnTileClicked);
                }
            }
        }

        UpdateDebugHints();
        UpdateSelectionPreview();
    }

    private void UpdateLiveScoreHUD(int score)
    {
        if (StatusText != null && gameManager != null)
        {
            StatusText.text = $"<size=120%><b>Round {gameManager.CurrentRound} / {gameManager.MaxRounds}</b></size>\n" +
                              $"Score: <b>{score}</b> / {gameManager.CurrentTargetScore}   |   " +
                              $"Hands: <b>{gameManager.HandsRemaining}</b>   |   Discards: <b>{gameManager.DiscardsRemaining}</b>   |   " +
                              $"Coins: <b><color=#F1C40F>${gameManager.Coins}</color></b>";
        }

        if (ScoreProgressBar != null && gameManager != null && gameManager.CurrentTargetScore > 0)
        {
            ScoreProgressBar.fillAmount = Mathf.Clamp01((float)score / gameManager.CurrentTargetScore);
        }
    }

    private void UpdateStatusText()
    {
        if (gameManager == null) return;
        UpdateLiveScoreHUD(gameManager.CurrentScore);
        UpdateDebugHints();
    }

    private void UpdateAffinityHUD()
    {
        if (AffinityHUDText == null || gameManager == null || gameManager.Affinity == null) return;

        float bambooMult = gameManager.Affinity.GetMultiplier(TileSuit.Bamboo);
        float charMult = gameManager.Affinity.GetMultiplier(TileSuit.Characters);
        float dotMult = gameManager.Affinity.GetMultiplier(TileSuit.Dots);

        AffinityHUDText.text = $"<b>SUIT AFFINITY MULTIPLIERS</b>\n" +
                               $"<color=#2ECC71>Bamboo:</color> {bambooMult:F2}x  |  " +
                               $"<color=#E74C3C>Characters:</color> {charMult:F2}x  |  " +
                               $"<color=#3498DB>Dots:</color> {dotMult:F2}x";
    }

    private void UpdateSpiritsHUD()
    {
        if (SpiritsHUDText == null || gameManager == null) return;

        if (gameManager.EquippedSpirits.Count == 0)
        {
            SpiritsHUDText.text = "<b>EQUIPPED SPIRITS (0/5):</b> <i>None</i>";
        }
        else
        {
            var names = gameManager.EquippedSpirits.Select(s => s != null ? s.SpiritName : "Empty");
            SpiritsHUDText.text = $"<b>EQUIPPED SPIRITS ({gameManager.EquippedSpirits.Count}/5):</b>\n<color=#F1C40F>{string.Join("  •  ", names)}</color>";
        }
    }

    private void UpdateShopText()
    {
        if (gameManager == null) return;

        if (ShopStatusText != null)
        {
            ShopStatusText.text = $"<b>SPIRIT SHOP</b>   |   Your Coins: <b><color=#F1C40F>${gameManager.Coins}</color></b>   |   " +
                                  $"Spirits Owned: <b>{gameManager.EquippedSpirits.Count}/{gameManager.MaxSpirits}</b>\n" +
                                  $"<size=85%>Purchase powerful spirits ($5 each) to augment your mahjong hand combos!</size>";
        }

        RefreshShopCards();
    }

    private void RefreshShopCards()
    {
        if (ShopCatalogContainer == null || ShopCatalog == null || gameManager == null) return;

        for (int i = 0; i < ShopCatalog.Count; i++)
        {
            int index = i;
            SpiritData spirit = ShopCatalog[i];
            Transform card = ShopCatalogContainer.Find($"ShopItem_{index}");
            if (card == null) continue;

            Button buyBtn = card.Find("BuyButton")?.GetComponent<Button>();
            TextMeshProUGUI buyText = buyBtn != null ? buyBtn.GetComponentInChildren<TextMeshProUGUI>() : null;
            TextMeshProUGUI cardText = card.Find("CardText")?.GetComponent<TextMeshProUGUI>();

            if (cardText != null && spirit != null)
            {
                cardText.text = $"<b><color=#F1C40F>{spirit.SpiritName}</color></b>\n\n<size=80%>{spirit.Description}</size>";
            }

            if (buyBtn != null && spirit != null)
            {
                bool owned = gameManager.EquippedSpirits.Contains(spirit);
                bool canAfford = gameManager.Coins >= 5 && gameManager.EquippedSpirits.Count < gameManager.MaxSpirits;

                buyBtn.onClick.RemoveAllListeners();
                
                if (owned)
                {
                    if (buyText != null) buyText.text = "OWNED";
                    buyBtn.interactable = false;
                }
                else
                {
                    if (buyText != null) buyText.text = "BUY ($5)";
                    buyBtn.interactable = canAfford;
                    buyBtn.onClick.AddListener(() => BuyShopItem(index));
                }
            }
        }
    }

    private void UpdateSummaryTexts()
    {
        if (gameManager == null) return;

        if (GameOverSummaryText != null)
        {
            GameOverSummaryText.text = $"<b>GAME OVER</b>\n\nFailed at Round {gameManager.CurrentRound}\nFinal Score: {gameManager.CurrentScore} / {gameManager.CurrentTargetScore}";
        }

        if (VictorySummaryText != null)
        {
            VictorySummaryText.text = $"<b>VICTORY!</b>\n\nYou successfully completed all {gameManager.MaxRounds} rounds of Malajong!\nFinal Coins: ${gameManager.Coins} | Spirits Equipped: {gameManager.EquippedSpirits.Count}";
        }
    }

    private void UpdateDebugHints()
    {
        if (DebugHintText == null || gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count > 0)
        {
            var comboDescriptions = playable.Select(p => $"{p.combo.Name} ({string.Join("-", p.tiles.Select(t => $"{t.Suit} {t.Rank}"))})");
            DebugHintText.text = $"<b><color=#2ECC71>PLAYABLE COMBOS IN HAND:</color></b>\n" + string.Join("  |  ", comboDescriptions);
        }
        else
        {
            DebugHintText.text = "<b><color=#E74C3C>HINT:</color></b> No complete combo in current hand. Try discarding up to 5 tiles!";
        }
    }
}
