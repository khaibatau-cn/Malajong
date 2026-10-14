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
    public SuitAffinityMeter BambooAffinityMeter;
    public SuitAffinityMeter CharactersAffinityMeter;
    public SuitAffinityMeter DotsAffinityMeter;
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
    public TextMeshProUGUI OptionsTitleText;
    public Button ToggleAudioButton;
    public TextMeshProUGUI ToggleAudioText;
    public Button ForfeitRunButton;
    public Button CloseOptionsButton;

    public GameObject RedrawModal;
    public TextMeshProUGUI RedrawText;
    public Button RedrawButton;

    public GameObject AbandonConfirmModal;
    public TextMeshProUGUI AbandonConfirmText;
    public Button ConfirmAbandonButton;
    public Button CancelAbandonButton;
    
    private List<TileUI> spawnedTileUIs = new List<TileUI>();
    private List<TileUI> selectedTileUIs = new List<TileUI>();
    private bool isScoringSequenceActive = false;

    // Null until the first state is shown, so the opening screen does not play a transition.
    private GameManager.GameState? lastShownState;
    // Spirits already shown on the rack, so only a newly bought one pops in.
    private int shownSpiritCount;

    void Start()
    {
        UIJuice.EnsureExists();
        AttachButtonJuice();
        if (HandContainer != null) MatAmbience.Ensure(HandContainer.parent);

        if (gameManager != null)
        {
            gameManager.OnHandUpdated += RefreshHandDisplay;
            gameManager.OnStateChanged += HandleStateChanged;
            gameManager.OnRedrawRequired += ShowRedrawPrompt;

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
            gameManager.OnRedrawRequired -= ShowRedrawPrompt;
        }
    }

    public void HandleStateChanged()
    {
        if (gameManager == null) return;

        bool screenChanged = lastShownState.HasValue && lastShownState.Value != gameManager.State;
        lastShownState = gameManager.State;

        SetPanelActive(StartMenuPanel, gameManager.State == GameManager.GameState.StartMenu);
        SetPanelActive(PlayingPanel, gameManager.State == GameManager.GameState.Playing);
        SetPanelActive(ShopPanel, gameManager.State == GameManager.GameState.Shop);
        SetPanelActive(GameOverPanel, gameManager.State == GameManager.GameState.GameOver);
        SetPanelActive(VictoryPanel, gameManager.State == GameManager.GameState.Victory);

        UpdateLeftBlindPanel();
        UpdateCenterSpiritRack();
        UpdateCenterAffinityHUD();
        UpdateRightScoreEngine();
        UpdateShopText();
        UpdateSummaryTexts();
        UpdateSelectionPreview();

        if (screenChanged)
        {
            Canvas canvas = FindUICanvas();
            if (canvas != null) ScreenWipe.Play(canvas.transform, TileSprites());
        }
    }

    private Canvas FindUICanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && PlayingPanel != null) canvas = PlayingPanel.GetComponentInParent<Canvas>();
        if (canvas == null && StartMenuPanel != null) canvas = StartMenuPanel.GetComponentInParent<Canvas>();
        return canvas;
    }

    private Sprite[] TileSprites()
    {
        if (gameManager == null || gameManager.AllTileTypes == null) return null;
        return gameManager.AllTileTypes.Where(t => t != null && t.TileSprite != null).Select(t => t.TileSprite).ToArray();
    }

    /// <summary>
    /// Shows or hides a panel, popping it in when it goes from hidden to visible. Panels that
    /// are already on screen are left alone, so the HUD doesn't re-pop on every state refresh.
    /// </summary>
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel == null) return;

        bool wasActive = panel.activeSelf;
        panel.SetActive(active);

        if (active && !wasActive) UIJuice.PopIn(panel.transform);
    }

    /// <summary>
    /// Adds press/hover spring feedback to every Button under the canvas. Done at runtime so
    /// buttons built by SceneSetupTool pick it up without the scene being rebuilt. Tiles are
    /// skipped — TileUI already drives its own hover and selection juice.
    /// </summary>
    private void AttachButtonJuice()
    {
        Canvas canvas = FindUICanvas();
        if (canvas == null) return;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInParent<TileUI>() != null) continue;
            if (button.GetComponent<ButtonJuice>() == null) button.gameObject.AddComponent<ButtonJuice>();
        }
    }

    // --- Left Panel: Blind & Stakes Updates ---

    private void UpdateLeftBlindPanel()
    {
        if (gameManager == null) return;

        string[] winds = { "East Wind", "South Wind", "West Wind", "North Wind", "Dragon Boss" };
        int windIndex = Mathf.Clamp(gameManager.CurrentRound - 1, 0, winds.Length - 1);

        if (BlindTitleText != null)
        {
            BlindTitleText.text = $"<color={MalajongTheme.HexOnGold}><b>{winds[windIndex].ToUpper()}</b></color>";
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
            TargetScoreText.text = $"Score at least\n<size=130%><color=#D8402E><b>{gameManager.CurrentTargetScore}</b></color></size>";
        }

        if (RewardText != null)
        {
            RewardText.text = "CLEAR REWARD\n<color=#D9A93A>¥4 + ¥1 per unused hand</color>";
        }

        if (YuanText != null)
        {
            YuanText.text = $"<color=#D9A93A><b>¥{gameManager.Yuan}</b></color>";
        }

        if (AnteText != null)
        {
            AnteText.text = $"Wall\n<color=#D9A93A><b>{gameManager.Deck?.Remaining ?? 136}</b></color>";
        }

        if (RoundText != null)
        {
            RoundText.text = $"Round\n<color=#D9A93A><b>{gameManager.CurrentRound}/{gameManager.MaxRounds}</b></color>";
        }
    }

    // --- Center Stage: Spirit Rack & Affinity Updates ---

    private void UpdateCenterSpiritRack()
    {
        if (SpiritRackContainer == null || gameManager == null) return;

        int maxSlots = gameManager.MaxSpirits;
        int equippedCount = gameManager.EquippedSpirits.Count;
        for (int i = 0; i < maxSlots; i++)
        {
            Transform slot = SpiritRackContainer.Find($"SpiritSlot_{i}");
            if (slot == null) continue;

            TextMeshProUGUI label = slot.GetComponentInChildren<TextMeshProUGUI>(true);
            Image bg = slot.GetComponent<Image>();

            Transform iconTransform = slot.Find("Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            Transform badgeTransform = slot.Find("Badge");
            Image badge = badgeTransform != null ? badgeTransform.GetComponent<Image>() : null;

            if (i < equippedCount && gameManager.EquippedSpirits[i] != null)
            {
                SpiritData spirit = gameManager.EquippedSpirits[i];
                Color accent = MalajongTheme.ForSpirit(spirit);
                if (label != null) label.text = $"<b><color={MalajongTheme.HexOf(accent)}>{spirit.SpiritName}</color></b>";
                if (bg != null) bg.color = MalajongTheme.MalachiteDeep;

                // The scroll icons all look alike, so the spirit's accent backs the icon.
                if (badge != null)
                {
                    badge.color = Color.Lerp(accent, MalajongTheme.Ink, 0.45f);
                    badge.gameObject.SetActive(true);
                }

                // An equipped spirit with no icon assigned still shows its name — better a nameplate
                // than an empty white box where art was expected.
                if (icon != null)
                {
                    icon.sprite = spirit.Icon;
                    icon.gameObject.SetActive(spirit.Icon != null);
                }

                if (i >= shownSpiritCount) UIJuice.PopIn(slot, (i - shownSpiritCount) * 0.06f);
            }
            else
            {
                // A ghost slot: a faint mark rather than a loud "Empty" label.
                if (label != null) label.text = $"<size=150%><color={MalajongTheme.HexSlotGhostMark}>+</color></size>";
                if (bg != null) bg.color = MalajongTheme.SlotGhost;
                if (icon != null) icon.gameObject.SetActive(false);
                if (badge != null) badge.gameObject.SetActive(false);
            }
        }

        shownSpiritCount = equippedCount;
    }

    private void UpdateCenterAffinityHUD()
    {
        if (gameManager == null || gameManager.Affinity == null) return;

        UpdateAffinityMeter(BambooAffinityMeter, TileSuit.Bamboo);
        UpdateAffinityMeter(CharactersAffinityMeter, TileSuit.Characters);
        UpdateAffinityMeter(DotsAffinityMeter, TileSuit.Dots);
    }

    private void UpdateAffinityMeter(SuitAffinityMeter meter, TileSuit suit)
    {
        if (meter == null) return;

        meter.SetLevel(gameManager.Affinity.GetLevel(suit), gameManager.Affinity.GetMultiplier(suit));
    }

    // --- Right Panel: Score Engine Updates ---

    private void UpdateRightScoreEngine()
    {
        if (gameManager == null) return;

        if (HandsRemainingText != null)
        {
            HandsRemainingText.text = $"Hands\n<color=#6FB8EE><b>{gameManager.HandsRemaining}</b></color>";
        }

        if (DiscardsRemainingText != null)
        {
            // Red at zero: losing the safety net should be visible before it costs anything.
            string discardColor = gameManager.DiscardsRemaining > 0 ? MalajongTheme.HexGold : MalajongTheme.HexVermilion;
            DiscardsRemainingText.text = $"Discards\n<color={discardColor}><b>{gameManager.DiscardsRemaining}</b></color>";
        }

        if (RoundScoreText != null)
        {
            RoundScoreText.text = RoundScoreLabel(gameManager.CurrentScore);
        }

        if (ScoreProgressBar != null && gameManager.CurrentTargetScore > 0)
        {
            SetScoreProgress((float)gameManager.CurrentScore / gameManager.CurrentTargetScore);
        }

        UpdatePlayableCombosList();
    }

    /// <summary>
    /// Round score readout. The number renders at 2x the 24px body size — 48, a whole multiple of
    /// the pixel font's grid — so the figure the player watches climb stays crisp.
    /// </summary>
    private string RoundScoreLabel(int score)
    {
        int toClear = Mathf.Max(0, gameManager.CurrentTargetScore - score);
        string status = toClear > 0
            ? $"<color={MalajongTheme.HexBoneDim}>{toClear:N0} to clear</color>"
            : $"<color={MalajongTheme.HexGold}><b>QUOTA CLEARED</b></color>";
        return $"ROUND SCORE\n<size=200%><b>{score:N0}</b></size>\n{status}";
    }

    private void UpdatePlayableCombosList()
    {
        if (PlayableCombosText == null || gameManager == null || gameManager.Hand == null) return;

        var playable = ScoreEngine.FindPlayableCombos(gameManager.Hand.Tiles);
        if (playable.Count > 0)
        {
            // Each line is tinted by the suit its tiles feed, the same colour the affinity meter uses.
            var lines = playable.Select(p => (preview: ScoreEngine.PreviewScore(p.tiles, gameManager.Hand.Tiles,
                    gameManager.Affinity, gameManager.EquippedSpirits, gameManager),
                    color: MalajongTheme.HexOf(MalajongTheme.ForTiles(p.tiles))))
                .OrderByDescending(p => p.preview.ProjectedScore)
                .Select(p => $"<b><color={p.color}>{p.preview.DetailText}</color></b>\n<color={MalajongTheme.HexGold}>{p.preview.ProjectedScore:N0} points</color>")
                .Distinct().Take(4);
            PlayableCombosText.text = "<b>AVAILABLE PLAYS</b>\n\n" + string.Join("\n\n", lines);
        }
        else if (gameManager.DiscardsRemaining > 0)
        {
            // A dead hand with discards left is not an aside, it is an instruction — discarding is
            // now the only legal move, so this reads as a warning rather than grey filler.
            PlayableCombosText.text =
                $"<b><color={MalajongTheme.HexVermilion}>DEAD HAND</color></b>\n" +
                $"<color={MalajongTheme.HexBoneDim}>No combo can be formed. You must discard — " +
                $"<b>{gameManager.DiscardsRemaining}</b> left.</color>";
        }
        else
        {
            PlayableCombosText.text =
                $"<b><color={MalajongTheme.HexVermilion}>DEAD HAND — NO DISCARDS</color></b>\n" +
                $"<color={MalajongTheme.HexBoneDim}>Only the cat can help you now.</color>";
        }
    }

    private void SetScoreProgress(float fraction)
    {
        if (ScoreProgressBar == null) return;
        // A sprite-less Image ignores fillAmount. Size the fill against its track instead.
        ScoreProgressBar.type = Image.Type.Simple;
        var rect = ScoreProgressBar.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        // A full bar turns gold: the quota is met, and the bar says so without a label.
        ScoreProgressBar.color = fraction >= 1f ? MalajongTheme.Gold : MalajongTheme.MalachiteBright;
    }

    // --- Hand Sorting Actions ---

    public void SortHandBySuit()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortBySuit();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 110, 0), "SORTED BY SUIT", MalajongTheme.MalachiteBright);
    }

    public void SortHandByRank()
    {
        if (isScoringSequenceActive || gameManager == null || gameManager.Hand == null) return;

        gameManager.Hand.SortByRank();
        RefreshHandDisplay();

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 110, 0), "SORTED BY RANK", MalajongTheme.FuText);
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

    public void ReturnToMainMenu()
    {
        if (gameManager != null) gameManager.ReturnToMenu();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void BuyShopItem(int catalogIndex)
    {
        if (gameManager == null || ShopCatalog == null || catalogIndex < 0 || catalogIndex >= ShopCatalog.Count) return;
        
        SpiritData targetSpirit = ShopCatalog[catalogIndex];
        bool success = gameManager.BuySpirit(targetSpirit, 5);
        if (success)
        {
            MalajongAudio.Instance?.PlayCashChime();
            FloatingBadge.Spawn(ShopPanel.transform, GetMousePosition(), $"BOUGHT {targetSpirit.SpiritName}!", MalajongTheme.Gold);
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
                PreviewComboNameText.text = "<color=#96826F><b>SELECT TILES</b></color>";
            }
            else
            {
                // A valid combo is named in the colour of the suit it feeds; an invalid one in red.
                string colorHex = preview.IsValid
                    ? MalajongTheme.HexOf(MalajongTheme.ForTiles(selectedTiles))
                    : MalajongTheme.HexVermilion;
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
            PreviewTotalScoreText.text = preview.IsValid
                ? $"<color={MalajongTheme.HexMalachite}><b>= {preview.ProjectedScore:N0} PTS</b></color>"
                : $"<color={MalajongTheme.HexSmoke}>--</color>";
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

        var comboTiles = playable.OrderByDescending(p => ScoreEngine.PreviewScore(p.tiles, gameManager.Hand.Tiles,
            gameManager.Affinity, gameManager.EquippedSpirits, gameManager).ProjectedScore).First().tiles;
        var fullPreview = ScoreEngine.PreviewScore(gameManager.Hand.Tiles, gameManager.Hand.Tiles,
            gameManager.Affinity, gameManager.EquippedSpirits, gameManager);
        var bestPreview = ScoreEngine.PreviewScore(comboTiles, gameManager.Hand.Tiles,
            gameManager.Affinity, gameManager.EquippedSpirits, gameManager);
        if (fullPreview.IsValid && fullPreview.ProjectedScore > bestPreview.ProjectedScore)
            comboTiles = gameManager.Hand.Tiles.ToList();
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

        yield return new WaitForSeconds(0.1f);

        // 1. Step 1: Per-Tile Fu Tally.
        // Each tile scores in turn — pops, throws its own number, and plays a note one
        // step higher than the last. Scoring the whole group at once loses the build-up.
        int targetFu = preview.TotalFu;
        int tileCount = Mathf.Max(1, playedUIs.Count);
        int fuPerTile = targetFu / tileCount;
        int fuRemainder = targetFu % tileCount;

        int runningFu = 0;
        for (int i = 0; i < playedUIs.Count; i++)
        {
            // Spread the remainder over the first tiles so the tally lands exactly on TotalFu.
            int tileFu = fuPerTile + (i < fuRemainder ? 1 : 0);
            runningFu += tileFu;

            TileUI tileUI = playedUIs[i];
            if (tileUI != null)
            {
                tileUI.TriggerScoreBounce();
                // Each tile's number comes off in its own suit colour; the Fu total below stays blue.
                Color badgeColor = tileUI.BoundTile != null && tileUI.BoundTile.Data != null
                    ? MalajongTheme.ForSuit(tileUI.BoundTile.Suit)
                    : MalajongTheme.FuText;
                FloatingBadge.Spawn(PlayingPanel.transform, tileUI.transform.position + new Vector3(0f, 80f, 0f),
                    $"+{tileFu}", badgeColor, MalajongTheme.TextCrisp2x);
            }

            // Sweep the full pitch range regardless of how many tiles were played, so a
            // 3-tile Pong still arpeggiates instead of clustering on the low notes.
            int pitchIndex = tileCount > 1 ? Mathf.RoundToInt((float)i / (tileCount - 1) * 6f) : 0;
            MalajongAudio.Instance?.PlayTileSelect(pitchIndex);

            if (PreviewFuBoxText != null)
            {
                PreviewFuBoxText.text = $"<b>{runningFu}</b>";
                UIJuice.Punch(PreviewFuBoxText.transform.parent, 1.14f);
            }

            yield return new WaitForSeconds(0.09f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(-120, 90, 0), $"+{targetFu} FU", MalajongTheme.FuText);
        yield return new WaitForSeconds(0.12f);

        // 3. Step 2: Fan Tally
        float currentTallyFan = 0f;
        float targetFan = preview.TotalFan;

        while (currentTallyFan < targetFan)
        {
            currentTallyFan = Mathf.MoveTowards(currentTallyFan, targetFan, targetFan * 0.25f);
            if (PreviewFanBoxText != null)
            {
                PreviewFanBoxText.text = $"<b>{currentTallyFan:F1}</b>";
                UIJuice.Punch(PreviewFanBoxText.transform.parent, 1.14f);
            }
            MalajongAudio.Instance?.PlayMultPop();
            yield return new WaitForSeconds(0.05f);
        }

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(120, 90, 0), $"{targetFan:F1}X FAN", MalajongTheme.FanText);
        yield return new WaitForSeconds(0.18f);

        // 4. Step 3: Big Multiplication Crunch / Slam
        int calculatedScore = Mathf.RoundToInt(targetFu * targetFan);
        MalajongAudio.Instance?.PlayScoreCrunchSlam();

        // Impact scales with how much of the round quota this one hand just covered, so a
        // Pair barely registers while a dora-loaded Kong freezes and rattles the screen.
        float impact = gameManager.CurrentTargetScore > 0
            ? Mathf.Clamp01((float)calculatedScore / gameManager.CurrentTargetScore)
            : 0.5f;

        UIJuice.Hitstop(0.05f + impact * 0.06f);
        UIJuice.Shake(PlayingPanel != null ? PlayingPanel.transform as RectTransform : null, impact);
        if (ComboPreviewBox != null) UIJuice.Punch(ComboPreviewBox.transform, 1.06f + impact * 0.08f, 0.22f);

        FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 140, 0), $"+{calculatedScore:N0} PTS!", MalajongTheme.Gold, MalajongTheme.TextCrisp3x);
        yield return new WaitForSeconds(0.35f);

        // 5. Step 4: Smooth Score Bar Roll-Up
        int startScore = gameManager.CurrentScore;
        int endScore = startScore + calculatedScore;
        float rollDuration = 0.45f;
        float rollElapsed = 0f;
        // Crossing the quota is the moment the round is won, so it gets its own beat mid roll-up.
        bool quotaAlreadyMet = gameManager.CurrentTargetScore > 0 && startScore >= gameManager.CurrentTargetScore;
        Transform roundScoreBox = RoundScoreText != null ? RoundScoreText.transform.parent : null;

        while (rollElapsed < rollDuration)
        {
            rollElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rollElapsed / rollDuration);
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, t));
            if (RoundScoreText != null) RoundScoreText.text = RoundScoreLabel(displayScore);
            if (ScoreProgressBar != null && gameManager.CurrentTargetScore > 0)
            {
                SetScoreProgress((float)displayScore / gameManager.CurrentTargetScore);
            }

            if (!quotaAlreadyMet && gameManager.CurrentTargetScore > 0 && displayScore >= gameManager.CurrentTargetScore)
            {
                quotaAlreadyMet = true;
                UIJuice.Punch(roundScoreBox, 1.15f, 0.25f);
                UIJuice.Shake(PlayingPanel != null ? PlayingPanel.transform as RectTransform : null, 0.6f, 0.35f);
                FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 220, 0),
                    "QUOTA CLEARED!", MalajongTheme.Gold, MalajongTheme.TextCrisp3x);
                MalajongAudio.Instance?.PlayCashChime();
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

        int index = 0;
        foreach (var tile in gameManager.Hand.Tiles)
        {
            GameObject newTileObj = Instantiate(TilePrefab, HandContainer);
            TileUI tileUI = newTileObj.GetComponent<TileUI>();
            if (tileUI != null)
            {
                tileUI.Initialize(tile, this);
                spawnedTileUIs.Add(tileUI);

                // Deal tiles in one at a time rather than the whole hand appearing at once.
                tileUI.PlayDealIn(index * 0.035f);

                Button btn = newTileObj.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(tileUI.OnTileClicked);
                }
            }
            index++;
        }

        // Apply Balatro Fan Curve hand layout if component is attached
        var handLayout = HandContainer != null ? HandContainer.GetComponent<BalatroHandLayout>() : null;
        if (handLayout != null)
        {
            handLayout.ArrangeHand(spawnedTileUIs);
        }

        UpdateRightScoreEngine();
        UpdateSelectionPreview();
    }

    private void UpdateShopText()
    {
        if (gameManager == null) return;

        if (ShopStatusText != null)
        {
            ShopStatusText.text = $"<b>SPIRIT SHOP</b>   |   Your Yuan: <b><color=#D9A93A>¥{gameManager.Yuan}</color></b>   |   " +
                                  $"Spirits Owned: <b>{gameManager.EquippedSpirits.Count}/{gameManager.MaxSpirits}</b>\n" +
                                  $"Next: {gameManager.GetTargetScoreForRound(gameManager.CurrentRound + 1):N0} points  •  Choose a spirit to shape your next round.";
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
                cardText.text = $"<b><color={MalajongTheme.HexOf(MalajongTheme.ForSpirit(spirit))}>{spirit.SpiritName}</color></b>\n{spirit.Description}";
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
                    if (buyText != null) buyText.text = gameManager.EquippedSpirits.Count >= gameManager.MaxSpirits
                        ? "SPIRIT SLOTS FULL" : gameManager.Yuan < 5 ? "NEED ¥5" : "BUY (¥5)";
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
            // Losing to a dead hand and losing to the quota feel completely different, so the
            // screen names which one happened instead of reporting a bare score.
            string reason = string.IsNullOrEmpty(gameManager.GameOverReason)
                ? $"Final Score: {gameManager.CurrentScore} / {gameManager.CurrentTargetScore}"
                : gameManager.GameOverReason;

            GameOverSummaryText.text =
                $"<b>GAME OVER</b>\n\nFailed at Round {gameManager.CurrentRound}\n\n" +
                $"<color={MalajongTheme.HexBoneDim}>{reason}</color>{CatSaveLine()}";
        }

        if (VictorySummaryText != null)
        {
            VictorySummaryText.text =
                $"<b>VICTORY!</b>\n\nYou successfully completed all {gameManager.MaxRounds} rounds of Malajong!\n" +
                $"Final Yuan: ¥{gameManager.Yuan} | Spirits Equipped: {gameManager.EquippedSpirits.Count}{CatSaveLine()}";
        }
    }

    /// <summary>
    /// End-of-run line for the cat counter. Returns nothing at zero — a run that never deadlocked
    /// should not be told it was rescued no times.
    /// </summary>
    private string CatSaveLine()
    {
        if (gameManager == null || gameManager.CatSaves <= 0) return "";

        string times = gameManager.CatSaves == 1 ? "once" : $"{gameManager.CatSaves} times";
        return $"\n\n<color={MalajongTheme.HexGold}>The lucky cat saved you {times} this run.</color>";
    }

    // --- Modal / Popup Handlers ---

    public void OpenRunInfoModal()
    {
        if (RunInfoModal == null) return;
        UpdateRunInfoContent();
        RunInfoModal.SetActive(true);
        UIJuice.PopIn(RunInfoModal.transform);
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
        sb.AppendLine("<size=115%><b><color=#D9A93A>YAKU & COMBO SCORING GUIDE</color></b></size>");
        Combo[] examples = { new Pair(new List<Tile>()), new Chow(new List<Tile>()), new Pong(new List<Tile>()),
            new Kong(new List<Tile>()), new ConcealedKong(new List<Tile>()) };
        foreach (var combo in examples)
            sb.AppendLine($"• <b>{combo.Name}</b>: {combo.BaseFu} Fu × {combo.BaseFan:0.0} Fan");
        sb.AppendLine("Tile ranks add Fu. Suit affinity multiplies Fan after the play's boost.");
        sb.AppendLine("14 tiles of one suit: +150 Fu, ×10 Fan. All Honors: +180 Fu, ×12 Fan.");
        sb.AppendLine("Full Mahjong (4 triples + pair): 800 points; uses one hand and all 14 tiles.");
        sb.AppendLine();

        sb.AppendLine("<size=115%><b><color=#D9A93A>RUN MULTIPLIERS & SPIRITS</color></b></size>");
        if (gameManager.Affinity != null)
        {
            sb.AppendLine($"<color={MalajongTheme.HexOf(MalajongTheme.SuitBamboo)}>Bamboo: {gameManager.Affinity.GetMultiplier(TileSuit.Bamboo):F1}x</color>  |  " +
                          $"<color={MalajongTheme.HexOf(MalajongTheme.SuitCharacters)}>Characters: {gameManager.Affinity.GetMultiplier(TileSuit.Characters):F1}x</color>  |  " +
                          $"<color={MalajongTheme.HexOf(MalajongTheme.SuitDots)}>Dots: {gameManager.Affinity.GetMultiplier(TileSuit.Dots):F1}x</color>");
        }

        sb.AppendLine();
        sb.AppendLine($"<b>Equipped Spirits ({gameManager.EquippedSpirits.Count}/{gameManager.MaxSpirits}):</b>");
        if (gameManager.EquippedSpirits.Count == 0)
        {
            sb.AppendLine("<color=#96826F><i>No spirits equipped. Visit the Shop between rounds!</i></color>");
        }
        else
        {
            foreach (var s in gameManager.EquippedSpirits)
            {
                if (s != null)
                {
                    sb.AppendLine($"• <color=#D9A93A><b>{s.SpiritName}</b></color>: {s.Description}");
                }
            }
        }

        if (gameManager.Deck != null)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>Tiles Remaining in Wall:</b> {gameManager.Deck.Remaining}");
        }

        if (gameManager.CatSaves > 0)
        {
            sb.AppendLine($"<b>Rescued by the Lucky Cat:</b> <color={MalajongTheme.HexGold}>{gameManager.CatSaves}x</color>");
        }

        RunInfoContentText.text = sb.ToString();
    }

    public void OpenOptionsModal()
    {
        if (OptionsModal == null) return;

        // The same modal serves two jobs: HOW TO PLAY from the title screen, and pause/options
        // mid-run. Abandoning only means anything when there is a run to abandon, so outside
        // Playing the button is removed rather than shown disabled — an "abandon run" control on
        // the main menu reads as a mistake even greyed out.
        bool inRun = gameManager != null && gameManager.State == GameManager.GameState.Playing;

        if (ForfeitRunButton != null) ForfeitRunButton.gameObject.SetActive(inRun);

        if (OptionsTitleText != null)
        {
            OptionsTitleText.text = inRun ? "<b>GAME OPTIONS & SETTINGS</b>" : "<b>HOW TO PLAY</b>";
        }

        if (CloseOptionsButton != null)
        {
            // With Abandon gone the close button takes the whole row, so the footer is not left
            // visibly half-empty.
            RectTransform closeRect = CloseOptionsButton.transform as RectTransform;
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(inRun ? 0.52f : 0.30f, 0.12f);
                closeRect.anchorMax = new Vector2(inRun ? 0.88f : 0.70f, 0.24f);
                closeRect.offsetMin = Vector2.zero;
                closeRect.offsetMax = Vector2.zero;
            }

            TextMeshProUGUI closeLabel = CloseOptionsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null) closeLabel.text = inRun ? "RESUME [X]" : "BACK";
        }

        UpdateOptionsUI();
        OptionsModal.SetActive(true);
        UIJuice.PopIn(OptionsModal.transform);
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
            PlayerPrefs.SetInt("Malajong.SfxMuted", isCurrentlyMuted ? 0 : 1);
            PlayerPrefs.Save();
        }
        UpdateOptionsUI();
        MalajongAudio.Instance?.PlayTileSelect(0);
    }

    private void UpdateOptionsUI()
    {
        if (ToggleAudioText != null)
        {
            bool isMuted = MalajongAudio.Instance != null && MalajongAudio.Instance.MasterVolume <= 0.001f;
            ToggleAudioText.text = isMuted ? "SFX: <color=#D8402E>MUTED</color>" : "SFX: <color=#43B87A>ENABLED</color>";
        }
    }

    /// <summary>
    /// Abandoning throws away a whole run, so it asks first. Kept under the original name because
    /// scenes built before the confirm modal existed still have this bound to their forfeit button.
    /// </summary>
    public void ForfeitRun()
    {
        OpenAbandonConfirm();
    }

    // --- Dead hand / redraw ------------------------------------------------

    /// <summary>
    /// The deadlock prompt. Deliberately has no dismiss button — a redraw is the only action left
    /// on the board, so offering a way to close the window would just strand the player.
    /// </summary>
    public void ShowRedrawPrompt()
    {
        if (RedrawModal == null)
        {
            // No modal in this scene: take the redraw automatically rather than freeze the board.
            gameManager?.RedrawHand();
            return;
        }

        if (RedrawText != null)
        {
            // A repeat visitor gets a different line — the cat keeping count is funnier than the
            // same warning twice, and it quietly tells the player this is a run stat.
            string history = "";
            if (gameManager != null && gameManager.CatSaves > 0)
            {
                string times = gameManager.CatSaves == 1 ? "once" : $"{gameManager.CatSaves} times";
                history = $"\n<size=75%><color={MalajongTheme.HexGold}>The cat has already bailed you out {times} this run. It is keeping count.</color></size>";
            }

            RedrawText.text =
                $"<b><color={MalajongTheme.HexVermilion}>DEAD HAND</color></b>\n\n" +
                $"No combo to play. No discards left.\n" +
                $"The lucky cat is now your entire strategy.\n\n" +
                $"<size=80%><color={MalajongTheme.HexBoneDim}>Redraw your hand. Find a combo and the cat grants you " +
                $"<color={MalajongTheme.HexGold}>+1 discard</color> — find nothing, and the run ends here.</color></size>" +
                history;
        }

        RedrawModal.SetActive(true);
        UIJuice.PopIn(RedrawModal.transform);
        MalajongAudio.Instance?.PlayGameOver();
    }

    public void TakeRedraw()
    {
        if (RedrawModal != null) RedrawModal.SetActive(false);
        if (gameManager == null) return;

        gameManager.RedrawHand();

        // The badge only lands on a survival — the game over panel speaks for the other outcome.
        if (gameManager.State == GameManager.GameState.Playing && PlayingPanel != null)
        {
            string badge = gameManager.CatSaves > 1
                ? $"CAT SAVE #{gameManager.CatSaves}! +1 DISCARD"
                : "THE CAT SMILES! +1 DISCARD";

            MalajongAudio.Instance?.PlayCashChime();
            FloatingBadge.Spawn(PlayingPanel.transform, HandContainer.position + new Vector3(0, 120, 0),
                badge, MalajongTheme.Gold, 30f);
        }
    }

    public void OpenAbandonConfirm()
    {
        if (AbandonConfirmModal == null)
        {
            // No modal in this scene — abandon outright rather than leaving the button dead.
            CloseOptionsModal();
            ReturnToMainMenu();
            return;
        }

        if (AbandonConfirmText != null && gameManager != null)
        {
            AbandonConfirmText.text =
                $"<b>ABANDON THIS RUN?</b>\n\n" +
                $"<color={MalajongTheme.HexBoneDim}>You are on round <b>{gameManager.CurrentRound}/{gameManager.MaxRounds}</b> " +
                $"with <b>{gameManager.CurrentScore}/{gameManager.CurrentTargetScore}</b> points and " +
                $"<b>{gameManager.EquippedSpirits.Count}</b> spirit(s) equipped.</color>\n\n" +
                $"<color={MalajongTheme.HexVermilion}>All run progress will be lost.</color>";
        }

        AbandonConfirmModal.SetActive(true);
        UIJuice.PopIn(AbandonConfirmModal.transform);
        MalajongAudio.Instance?.PlayTileSelect(0);
    }

    public void CancelAbandon()
    {
        if (AbandonConfirmModal == null) return;
        AbandonConfirmModal.SetActive(false);
        MalajongAudio.Instance?.PlayTileDeselect();
    }

    public void ConfirmAbandonRun()
    {
        CancelAbandon();
        CloseOptionsModal();
        CloseRunInfoModal();
        ReturnToMainMenu();
    }

    private Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
#endif
        try
        {
            return Input.mousePosition;
        }
        catch
        {
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
    }
}
