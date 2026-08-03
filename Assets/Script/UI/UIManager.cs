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

    [Header("Left Panel: Blind & Stakes")]
    public TextMeshProUGUI BlindTitleText;
    public Image BlindTileIcon;
    public TextMeshProUGUI TargetScoreText;
    public TextMeshProUGUI RewardText;
    public TextMeshProUGUI YuanText;
    public TextMeshProUGUI CoinsText { get => YuanText; set => YuanText = value; } // Backwards-compatible alias
    public TextMeshProUGUI AnteText;
    public TextMeshProUGUI RoundText;
    public Button RunInfoButton;
    public Button OptionsButton;

    [Header("Center Stage: Spirit Rack & Hand")]
    public Transform SpiritRackContainer;
    public TextMeshProUGUI SuitAffinityText;
    public Transform HandContainer;
    public GameObject TilePrefab;
    public Button SortSuitButton;
    public Button SortRankButton;
    public Button AutoComboButton;
    public Button PlayButton;
    public Button DiscardButton;

    [Header("Right Panel: Balatro Score Engine")]
    public TextMeshProUGUI HandsRemainingText;
    public TextMeshProUGUI DiscardsRemainingText;
    public TextMeshProUGUI RoundScoreText;
    public Image ScoreProgressBar;
    public GameObject ComboPreviewBox;
    public TextMeshProUGUI PreviewComboNameText;
    public TextMeshProUGUI PreviewFuBoxText;
    public TextMeshProUGUI PreviewChipsBoxText { get => PreviewFuBoxText; set => PreviewFuBoxText = value; } // Backwards-compatible alias
    public TextMeshProUGUI PreviewFanBoxText;
    public TextMeshProUGUI PreviewMultBoxText { get => PreviewFanBoxText; set => PreviewFanBoxText = value; } // Backwards-compatible alias
    public TextMeshProUGUI PreviewTotalScoreText;
    public TextMeshProUGUI PlayableCombosText;

    [Header("Shop UI References")]
    public TextMeshProUGUI ShopStatusText;
    public Transform ShopCatalogContainer;
    public List<SpiritData> ShopCatalog = new List<SpiritData>();

    [Header("Game Over & Victory UI")]
    public TextMeshProUGUI GameOverSummaryText;
    public TextMeshProUGUI VictorySummaryText;

    [Header("Modals & Overlays")]
    public GameObject RunInfoModal;
    public TextMeshProUGUI RunInfoContentText;
    public Button CloseRunInfoButton;

    public GameObject OptionsModal;
    public Button ToggleAudioButton;
    public TextMeshProUGUI ToggleAudioText;
    public Button ForfeitRunButton;
    public Button CloseOptionsButton;
    
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

        UpdateLeftBlindPanel();
        UpdateCenterSpiritRack();
        UpdateCenterAffinityHUD();
        UpdateRightScoreEngine();
        UpdateShopText();
        UpdateSummaryTexts();
        UpdateSelectionPreview();
    }

    // --- Left Panel: Blind & Stakes Updates ---

    private void UpdateLeftBlindPanel()
    {
        if (gameManager == null) return;

        string[] winds = { "East Wind", "South Wind", "West Wind", "North Wind", "Dragon Boss" };
        int windIndex = Mathf.Clamp(gameManager.CurrentRound - 1, 0, winds.Length - 1);

        if (BlindTitleText != null)
        {
            BlindTitleText.text = $"<b>{winds[windIndex].ToUpper()}</b>";
        }

        // Set Wind Tile Icon Sprite from loaded tiles
        if (BlindTileIcon != null && gameManager.AllTileTypes != null)
        {
            int targetRank = Mathf.Clamp(gameManager.CurrentRound, 1, 7);
            TileData windData = gameManager.AllTileTypes.FirstOrDefault(t => t.Suit == TileSuit.Honor && t.Rank == targetRank);
            if (windData != null && windData.TileSprite != null)
            {
                BlindTileIcon.sprite = windData.TileSprite;
                BlindTileIcon.color = Color.white;
            }
        }

        if (TargetScoreText != null)
        {
            TargetScoreText.text = $"Score at least\n<size=130%><color=#E74C3C><b>{gameManager.CurrentTargetScore}</b></color></size>";
        }

        if (RewardText != null)
        {
            RewardText.text = $"Reward: <color=#F1C40F><b>¥5</b></color>";
        }

        if (YuanText != null)
        {
            YuanText.text = $"<color=#F1C40F><b>¥{gameManager.Yuan}</b></color>";
        }

        if (AnteText != null)
        {
            AnteText.text = $"Ante\n<color=#F39C12><b>1/4</b></color>";
        }

        if (RoundText != null)
        {
            RoundText.text = $"Round\n<color=#F39C12><b>{gameManager.CurrentRound}/{gameManager.MaxRounds}</b></color>";
        }
    }

    // --- Center Stage: Spirit Rack & Affinity Updates ---

    private void UpdateCenterSpiritRack()
    {
        if (SpiritRackContainer == null || gameManager == null) return;

        int maxSlots = gameManager.MaxSpirits;
        for (int i = 0; i < maxSlots; i++)
        {
            Transform slot = SpiritRackContainer.Find($"SpiritSlot_{i}");
            if (slot == null) continue;

            TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>();
            Image bg = slot.GetComponent<Image>();

            if (i < gameManager.EquippedSpirits.Count && gameManager.EquippedSpirits[i] != null)
            {
                SpiritData spirit = gameManager.EquippedSpirits[i];
                if (label != null) label.text = $"<b>{spirit.SpiritName}</b>";
                if (bg != null) bg.color = new Color(0.18f, 0.45f, 0.32f, 0.95f);
            }
            else
            {
                if (label != null) label.text = "<color=#7F8C8D>Empty</color>";
                if (bg != null) bg.color = new Color(0.10f, 0.14f, 0.18f, 0.65f);
            }
        }
    }

    private void UpdateCenterAffinityHUD()
    {
        if (SuitAffinityText == null || gameManager == null || gameManager.Affinity == null) return;

        float bambooMult = gameManager.Affinity.GetMultiplier(TileSuit.Bamboo);
        float charMult = gameManager.Affinity.GetMultiplier(TileSuit.Characters);
        float dotMult = gameManager.Affinity.GetMultiplier(TileSuit.Dots);

        SuitAffinityText.text = $"<color=#2ECC71><b>Bamboo:</b> {bambooMult:F1}x</color>   |   " +
                                $"<color=#E74C3C><b>Chars:</b> {charMult:F1}x</color>   |   " +
                                $"<color=#3498DB><b>Dots:</b> {dotMult:F1}x</color>";
    }

    // --- Right Panel: Score Engine Updates ---

    private void UpdateRightScoreEngine()
    {
        if (gameManager == null) return;

        if (HandsRemainingText != null)
        {
            HandsRemainingText.text = $"Hands\n<color=#3498DB><b>{gameManager.HandsRemaining}</b></color>";
        }

        if (DiscardsRemainingText != null)
        {
            DiscardsRemainingText.text = $"Discards\n<color=#E67E22><b>{gameManager.DiscardsRemaining}</b></color>";
        }

        if (RoundScoreText != null)
        {
            RoundScoreText.text = $"Round score\n<b>{gameManager.CurrentScore}</b>";
        }

        if (ScoreProgressBar != null && gameManager.CurrentTargetScore > 0)
        {
            ScoreProgressBar.fillAmount = Mathf.Clamp01((float)gameManager.CurrentScore / gameManager.CurrentTargetScore);
        }

        UpdatePlayableCombosList();
    }

    private void UpdatePlayableCombosList()
    {
        if (PlayableCombosText == null || gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count > 0)
        {
            var lines = playable.Select(p => $"* <b>{p.combo.Name}</b>: <color=#3498DB>{p.combo.BaseFu} Fu</color> x <color=#E74C3C>{p.combo.BaseFan:F1} Fan</color>");
            PlayableCombosText.text = $"<b>PLAYABLE IN HAND:</b>\n" + string.Join("\n", lines);
        }
        else
        {
            PlayableCombosText.text = "<b>PLAYABLE IN HAND:</b>\n<color=#7F8C8D><i>No complete combo. Select tiles to discard or form Chow/Pong/Pair!</i></color>";
        }
    }

    // --- Hand Sorting Actions ---

    public void SortHandBySuit()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortBySuit();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 110, 0), "SORTED BY SUIT", new Color(0.18f, 0.85f, 0.35f));
    }

    public void SortHandByRank()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortByRank();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 110, 0), "SORTED BY RANK", new Color(0.2f, 0.7f, 1f));
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

        if (PreviewComboNameText != null)
        {
            if (selectedTiles.Count == 0)
            {
                PreviewComboNameText.text = "<color=#7F8C8D><b>SELECT TILES</b></color>";
            }
            else
            {
                string colorHex = preview.IsValid ? "#F1C40F" : "#E74C3C";
                PreviewComboNameText.text = $"<color={colorHex}><b>{preview.ComboName.ToUpper()}</b></color>";
            }
        }

        if (PreviewFuBoxText != null)
        {
            PreviewFuBoxText.text = $"<b>{preview.TotalFu}</b>";
        }

        if (PreviewFanBoxText != null)
        {
            PreviewFanBoxText.text = $"<b>{preview.TotalFan:F1}</b>";
        }

        if (PreviewTotalScoreText != null)
        {
            PreviewTotalScoreText.text = preview.IsValid ? $"<color=#2ECC71><b>= {preview.ProjectedScore} PTS</b></color>" : "<color=#7F8C8D>--</color>";
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

    public void DiscardSelected()
    {
        if (isScoringSequenceActive || selectedTileUIs.Count == 0) return;
        
        List<Tile> tilesToDiscard = new List<Tile>();
        foreach (var t in selectedTileUIs) tilesToDiscard.Add(t.BoundTile);
        
        gameManager.DiscardSelectedTiles(tilesToDiscard);
        ClearSelection();
    }

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

        foreach (var targetTile in comboTiles)
        {
            var uiMatch = remainingUIs.FirstOrDefault(u => u.BoundTile == targetTile);
            if (uiMatch != null)
            {
                uiMatch.SetSelected(true);
                remainingUIs.Remove(uiMatch);
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

        yield return new WaitForSeconds(0.15f);

        // 2. Step 1: Fu Count-Up Tally
        int currentTallyFu = 0;
        int targetFu = preview.TotalFu;
        int stepFu = Mathf.Max(1, targetFu / 8);

        while (currentTallyFu < targetFu)
        {
            currentTallyFu = Mathf.Min(currentTallyFu + stepFu, targetFu);
            if (PreviewFuBoxText != null) PreviewFuBoxText.text = $"<b>{currentTallyFu}</b>";
            MalajongAudio.Instance?.PlayScoreChipTick();
            yield return new WaitForSeconds(0.04f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(-120, 90, 0), $"+{targetFu} FU", new Color(0.2f, 0.6f, 1f));
        yield return new WaitForSeconds(0.12f);

        // 3. Step 2: Fan Tally
        float currentTallyFan = 0f;
        float targetFan = preview.TotalFan;

        while (currentTallyFan < targetFan)
        {
            currentTallyFan = Mathf.MoveTowards(currentTallyFan, targetFan, targetFan * 0.25f);
            if (PreviewFanBoxText != null) PreviewFanBoxText.text = $"<b>{currentTallyFan:F1}</b>";
            MalajongAudio.Instance?.PlayMultPop();
            yield return new WaitForSeconds(0.05f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(120, 90, 0), $"{targetFan:F1}X FAN", new Color(0.95f, 0.3f, 0.25f));
        yield return new WaitForSeconds(0.18f);

        // 4. Step 3: Big Multiplication Crunch / Slam
        int calculatedScore = Mathf.RoundToInt(targetFu * targetFan);
        MalajongAudio.Instance?.PlayScoreCrunchSlam();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 130, 0), $"+{calculatedScore} PTS!", new Color(1f, 0.85f, 0.1f), 34f);
        yield return new WaitForSeconds(0.35f);

        // 5. Step 4: Smooth Score Bar Roll-Up
        int startScore = gameManager.CurrentScore;
        int endScore = startScore + calculatedScore;
        float rollDuration = 0.45f;
        float rollElapsed = 0f;

        while (rollElapsed < rollDuration)
        {
            rollElapsed += Time.deltaTime;
            float t = rollElapsed / rollDuration;
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
            if (RoundScoreText != null) RoundScoreText.text = $"Round score\n<size=160%><b>{displayScore}</b></size>";
            if (ScoreProgressBar != null && gameManager.CurrentTargetScore > 0)
            {
                ScoreProgressBar.fillAmount = Mathf.Clamp01((float)displayScore / gameManager.CurrentTargetScore);
            }
            yield return null;
        }

        // 6. Commit play in GameManager
        gameManager.PlaySelectedTiles(playedTiles);

        ClearSelection();
        isScoringSequenceActive = false;
        SetControlsInteractable(true);

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

        UpdateRightScoreEngine();
        UpdateSelectionPreview();
    }

    private void UpdateShopText()
    {
        if (gameManager == null) return;

        if (ShopStatusText != null)
        {
            ShopStatusText.text = $"<b>SPIRIT SHOP</b>   |   Your Yuan: <b><color=#F1C40F>¥{gameManager.Yuan}</color></b>   |   " +
                                  $"Spirits Owned: <b>{gameManager.EquippedSpirits.Count}/{gameManager.MaxSpirits}</b>\n" +
                                  $"<size=85%>Purchase powerful spirits (¥5 each) to augment your mahjong hand combos!</size>";
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
                bool canAfford = gameManager.Yuan >= 5 && gameManager.EquippedSpirits.Count < gameManager.MaxSpirits;

                buyBtn.onClick.RemoveAllListeners();
                
                if (owned)
                {
                    if (buyText != null) buyText.text = "OWNED";
                    buyBtn.interactable = false;
                }
                else
                {
                    if (buyText != null) buyText.text = "BUY (¥5)";
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
            VictorySummaryText.text = $"<b>VICTORY!</b>\n\nYou successfully completed all {gameManager.MaxRounds} rounds of Malajong!\nFinal Yuan: ¥{gameManager.Yuan} | Spirits Equipped: {gameManager.EquippedSpirits.Count}";
        }
    }

    // --- Modal / Popup Handlers ---

    public void OpenRunInfoModal()
    {
        if (RunInfoModal == null) return;
        UpdateRunInfoContent();
        RunInfoModal.SetActive(true);
        MalajongAudio.Instance?.PlayTileSelect(0);
    }

    public void CloseRunInfoModal()
    {
        if (RunInfoModal == null) return;
        RunInfoModal.SetActive(false);
        MalajongAudio.Instance?.PlayTileDeselect();
    }

    public void UpdateRunInfoContent()
    {
        if (RunInfoContentText == null || gameManager == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<size=115%><b><color=#F1C40F>YAKU & COMBO SCORING GUIDE</color></b></size>");
        sb.AppendLine("• <b>Pair (Toitsu)</b>: 2 Identical Tiles  >>  <b><color=#3498DB>+10 Fu</color></b> | <b><color=#E74C3C>x1.0 Fan</color></b>");
        sb.AppendLine("• <b>Chow (Shuntsu)</b>: 3 Consecutive Suit Tiles  >>  <b><color=#3498DB>+20 Fu</color></b> | <b><color=#E74C3C>x1.5 Fan</color></b>");
        sb.AppendLine("• <b>Pong (Koutsu)</b>: 3 Identical Tiles  >>  <b><color=#3498DB>+30 Fu</color></b> | <b><color=#E74C3C>x2.0 Fan</color></b>");
        sb.AppendLine("• <b>Kong (Kantsu)</b>: 4 Identical Tiles  >>  <b><color=#3498DB>+60 Fu</color></b> | <b><color=#E74C3C>x3.0 Fan</color></b>");
        sb.AppendLine("• <b>Pure Hand (Flush)</b>: All tiles in single suit  >>  <b><color=#3498DB>+150 Fu</color></b> | <b><color=#E74C3C>+2.0 Fan</color></b>");
        sb.AppendLine();

        sb.AppendLine("<size=115%><b><color=#F1C40F>RUN MULTIPLIERS & SPIRITS</color></b></size>");
        if (gameManager.Affinity != null)
        {
            sb.AppendLine($"<color=#2ECC71>Bamboo: {gameManager.Affinity.GetMultiplier(TileSuit.Bamboo):F1}x</color>  |  " +
                          $"<color=#E67E22>Characters: {gameManager.Affinity.GetMultiplier(TileSuit.Characters):F1}x</color>  |  " +
                          $"<color=#3498DB>Dots: {gameManager.Affinity.GetMultiplier(TileSuit.Dots):F1}x</color>");
        }

        sb.AppendLine();
        sb.AppendLine($"<b>Equipped Spirits ({gameManager.EquippedSpirits.Count}/{gameManager.MaxSpirits}):</b>");
        if (gameManager.EquippedSpirits.Count == 0)
        {
            sb.AppendLine("<color=#7F8C8D><i>No spirits equipped. Visit the Shop between rounds!</i></color>");
        }
        else
        {
            foreach (var s in gameManager.EquippedSpirits)
            {
                if (s != null)
                {
                    sb.AppendLine($"• <color=#F1C40F><b>{s.SpiritName}</b></color>: {s.Description}");
                }
            }
        }

        if (gameManager.Deck != null)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>Tiles Remaining in Wall:</b> {gameManager.Deck.Remaining}");
        }

        RunInfoContentText.text = sb.ToString();
    }

    public void OpenOptionsModal()
    {
        if (OptionsModal == null) return;
        UpdateOptionsUI();
        OptionsModal.SetActive(true);
        MalajongAudio.Instance?.PlayTileSelect(0);
    }

    public void CloseOptionsModal()
    {
        if (OptionsModal == null) return;
        OptionsModal.SetActive(false);
        MalajongAudio.Instance?.PlayTileDeselect();
    }

    public void ToggleAudio()
    {
        if (MalajongAudio.Instance != null)
        {
            bool isCurrentlyMuted = MalajongAudio.Instance.MasterVolume <= 0.001f;
            MalajongAudio.Instance.MasterVolume = isCurrentlyMuted ? 0.8f : 0f;
        }
        UpdateOptionsUI();
        MalajongAudio.Instance?.PlayTileSelect(0);
    }

    private void UpdateOptionsUI()
    {
        if (ToggleAudioText != null)
        {
            bool isMuted = MalajongAudio.Instance != null && MalajongAudio.Instance.MasterVolume <= 0.001f;
            ToggleAudioText.text = isMuted ? "SFX: <color=#E74C3C>MUTED</color>" : "SFX: <color=#2ECC71>ENABLED</color>";
        }
    }

    public void ForfeitRun()
    {
        CloseOptionsModal();
        RestartRun();
    }
}
