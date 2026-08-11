using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<TileData> AllTileTypes; // Assign in editor
    
    public int CurrentRound = 1;
    public int MaxRounds = 5;
    public int CurrentTargetScore = 150;
    public int HandsRemaining = 4;
    public int DiscardsRemaining = 3;
    public int CurrentScore = 0;
    public int Yuan = 5;
    public int Coins { get => Yuan; set => Yuan = value; } // Backwards-compatible alias
    public int MaxSpirits = 5;
    
    public TileBag Deck { get; private set; }
    public PlayerHand Hand { get; private set; }
    public SuitAffinity Affinity { get; private set; }
    public List<SpiritData> EquippedSpirits { get; private set; } = new List<SpiritData>();
    
    // UI Events
    public event System.Action OnHandUpdated;
    public event System.Action OnStateChanged;
    /// <summary>Raised when the hand deadlocks. The UI answers this by prompting for a redraw.</summary>
    public event System.Action OnRedrawRequired;
    
    public enum GameState { StartMenu, Playing, Shop, GameOver, Victory }
    public GameState State { get; private set; } = GameState.StartMenu;
    
    void Start()
    {
        if (AllTileTypes == null || AllTileTypes.Count == 0)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TileData");
            AllTileTypes = new List<TileData>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                TileData tile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileData>(path);
                if (tile != null) AllTileTypes.Add(tile);
            }
#endif
        }

        if (AllTileTypes == null || AllTileTypes.Count == 0)
        {
            Debug.LogWarning("[GameManager] No TileData assets found! Run 'Malajong -> Generate All Game Data' first.");
        }

        // Deliberately does NOT start a run. Entering Play used to drop the player straight into
        // round 1 because Start() called InitializeRun(), which flips State to Playing and hides
        // the menu before UIManager ever reads it. The run now begins only from START RUN.
        State = GameState.StartMenu;
        OnStateChanged?.Invoke();
    }
    
    public void InitializeRun()
    {
        CurrentRound = 1;
        Yuan = 5;
        CatSaves = 0;
        EquippedSpirits.Clear();
        Deck = new TileBag();
        Hand = new PlayerHand();
        Affinity = new SuitAffinity();
        
        StartRound(GetTargetScoreForRound(CurrentRound));
    }
    
    public int GetTargetScoreForRound(int round)
    {
        return round switch
        {
            1 => 150,
            2 => 350,
            3 => 800,
            4 => 1800,
            5 => 4000,
            _ => 4000 + (round - 5) * 3500
        };
    }
    
    public void StartGame()
    {
        InitializeRun();
    }

    /// <summary>Drops back to the title screen without touching run state — the run is rebuilt from
    /// scratch by <see cref="InitializeRun"/> whenever START RUN is pressed again.</summary>
    public void ReturnToMenu()
    {
        State = GameState.StartMenu;
        OnStateChanged?.Invoke();
    }
    
    public void StartRound(int targetScore)
    {
        CurrentTargetScore = targetScore;
        CurrentScore = 0;
        HandsRemaining = 4;
        DiscardsRemaining = 3;
        State = GameState.Playing;
        
        AwaitingRedraw = false;
        GameOverReason = "";

        DealOpeningHand();

        foreach (var spirit in EquippedSpirits)
        {
            if (spirit != null) spirit.OnRoundStart(this);
        }
        
        Debug.Log($"Round {CurrentRound} Started. Target: {targetScore}. Deck: {Deck.Remaining}. Hand: {Hand.Tiles.Count}");
        
        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }
    
    public void NextRound()
    {
        if (CurrentRound >= MaxRounds)
        {
            State = GameState.Victory;
            OnStateChanged?.Invoke();
            return;
        }

        CurrentRound++;
        StartRound(GetTargetScoreForRound(CurrentRound));
    }
    
    public bool BuySpirit(SpiritData spirit, int cost)
    {
        if (spirit == null) return false;
        if (Yuan < cost)
        {
            Debug.Log("[Shop] Not enough Yuan!");
            return false;
        }
        if (EquippedSpirits.Count >= MaxSpirits)
        {
            Debug.Log("[Shop] Spirit slots full!");
            return false;
        }

        Yuan -= cost;
        EquippedSpirits.Add(spirit);
        Debug.Log($"[Shop] Purchased Spirit: {spirit.SpiritName} for ¥{cost}. Yuan left: ¥{Yuan}");
        OnStateChanged?.Invoke();
        return true;
    }
    
    public void SellSpirit(SpiritData spirit, int refund = 2)
    {
        if (EquippedSpirits.Contains(spirit))
        {
            EquippedSpirits.Remove(spirit);
            Yuan += refund;
            Debug.Log($"[Shop] Sold Spirit: {spirit.SpiritName} for ¥{refund}. Yuan total: ¥{Yuan}");
            OnStateChanged?.Invoke();
        }
    }
    
    // Call this from UI when player selects tiles and clicks "Play Combo"
    public void PlaySelectedTiles(List<Tile> selectedTiles)
    {
        if (State != GameState.Playing) return;
        
        // 1. Try to play Full Hand first (instant win)
        if (selectedTiles.Count == 14)
        {
            var (bonusFu, bonusFan) = ScoreEngine.EvaluateFullHand(selectedTiles);
            if (bonusFu > 0)
            {
                int scoreGained = Mathf.RoundToInt(bonusFu * bonusFan);
                CurrentScore += scoreGained;
                Debug.Log($"FULL HAND ACHIEVED! Scored {scoreGained} bonus points.");
                CheckExitConditions();
                return;
            }
        }

        // 2. Try standard combo
        Combo combo = ScoreEngine.DetectCombo(selectedTiles);
        
        if (combo == null)
        {
            Debug.Log("Invalid combo. Play rejected!");
            return;
        }
        
        // 3. Valid combo
        var (fu, fan) = ScoreEngine.Calculate(combo, Hand.Tiles, Affinity, EquippedSpirits, this);
        int comboScore = Mathf.RoundToInt(fu * fan);
        
        CurrentScore += comboScore;
        HandsRemaining--;
        
        Debug.Log($"Played {combo.Name}. Scored {comboScore} (Fu: {fu}, Fan: {fan:F1}). Total: {CurrentScore}/{CurrentTargetScore}. Hands left: {HandsRemaining}");
        
        Hand.RemoveTiles(selectedTiles);
        RefillHand();
        CheckExitConditions();
        
        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }
    
    // Call this from UI when player selects tiles and clicks "Discard"
    public void DiscardSelectedTiles(List<Tile> selectedTiles)
    {
        if (State != GameState.Playing) return;
        
        if (DiscardsRemaining <= 0)
        {
            Debug.Log("No discards remaining!");
            return;
        }
        
        if (selectedTiles.Count > 5)
        {
            Debug.Log("Cannot discard more than 5 tiles at once!");
            return;
        }

        Debug.Log($"Discarded {selectedTiles.Count} tiles.");
        Hand.RemoveTiles(selectedTiles);
        DiscardsRemaining--;
        
        RefillHand();
        CheckExitConditions();
        
        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }
    
    // --- Dead hands ---------------------------------------------------------
    //
    // The smallest legal play is a Pair, so a hand holding no Pair, Chow, Pong or Kong cannot be
    // played at all. That is survivable while discards remain and terminal once they run out —
    // hence the redraw reprieve below.

    /// <summary>
    /// True when at least one combo can be formed from the current hand. Reads through
    /// <see cref="ScoreEngine.FindPlayableCombos"/>, the same call that feeds the PLAYABLE IN HAND
    /// panel, so what the player is told and what ends the run can never disagree.
    /// </summary>
    public bool HasPlayableCombo => Hand != null && ScoreEngine.FindPlayableCombos(Hand.Tiles).Count > 0;

    /// <summary>Dead hand with no discards left: nothing can be played, discarded, or passed.</summary>
    public bool IsDeadlocked => State == GameState.Playing && DiscardsRemaining <= 0 && !HasPlayableCombo;

    /// <summary>Set when a deadlock is hit. The round is frozen until <see cref="RedrawHand"/> resolves it.</summary>
    public bool AwaitingRedraw { get; private set; }

    /// <summary>Why the last run ended, for the game over screen. Empty until a run is actually lost.</summary>
    public string GameOverReason { get; private set; } = "";

    /// <summary>
    /// How many times a redraw pulled the player out of a deadlock this run. A run stat, not a
    /// round one — surviving the cat twice in one run is the story worth telling at the end.
    /// </summary>
    public int CatSaves { get; private set; }

    private const int MaxOpeningDealAttempts = 50;

    /// <summary>
    /// Deals the opening hand, reshuffling until it holds at least one playable combo.
    ///
    /// A round should never open already lost. With a full 144-tile wall a dead 14-tile hand is
    /// rare enough that this almost always succeeds on the first deal; the attempt cap exists only
    /// so a pathological tile set cannot hang the editor.
    /// </summary>
    private void DealOpeningHand()
    {
        for (int attempt = 1; attempt <= MaxOpeningDealAttempts; attempt++)
        {
            Deck.Initialize(AllTileTypes);
            Hand.Tiles.Clear();
            Hand.AddTiles(Deck.Draw(PlayerHand.MaxSize));

            if (HasPlayableCombo)
            {
                if (attempt > 1) Debug.Log($"[GameManager] Opening hand was dead — redealt {attempt - 1} time(s).");
                return;
            }
        }

        Debug.LogWarning($"[GameManager] Could not deal a playable opening hand in {MaxOpeningDealAttempts} attempts. Check that the tile set is complete.");
    }

    /// <summary>
    /// The one reprieve from a deadlock: bin the whole hand and draw a fresh one.
    ///
    /// Costs no Hand — this is mercy, not a turn. Find a combo and the run continues with an extra
    /// discard as the reward for surviving; find nothing and the run ends, so the reprieve cannot
    /// be leaned on indefinitely. Each cycle also burns tiles off the wall, which bounds it further.
    /// </summary>
    public void RedrawHand()
    {
        if (!AwaitingRedraw) return;
        AwaitingRedraw = false;

        // Redrawn tiles are gone, exactly as they would be after a discard.
        Hand.Tiles.Clear();
        RefillHand();

        if (HasPlayableCombo)
        {
            DiscardsRemaining++;
            CatSaves++;
            Debug.Log($"[GameManager] Redraw found a playable hand. +1 Discard (now {DiscardsRemaining}). Cat saves this run: {CatSaves}. Wall: {Deck.Remaining}.");
        }
        else
        {
            Debug.Log("[GameManager] Redraw came up dead. Run over.");
            EndRun("The cat looked away.\nNo playable combo, and no discards left.");
        }

        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }

    private void EndRun(string reason)
    {
        GameOverReason = reason;
        State = GameState.GameOver;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Replaces the hand with a known-unplayable one and zeroes discards, so the deadlock path can
    /// be exercised on demand instead of waiting for it to occur naturally — which is rare enough
    /// that it would otherwise reach players untested.
    ///
    /// The pattern is every other rank across three suits: no two tiles match (no Pair, Pong or
    /// Kong) and no three are consecutive within a suit (no Chow).
    /// </summary>
    public void DebugForceDeadHand()
    {
        if (State != GameState.Playing || Hand == null)
        {
            Debug.LogWarning("[GameManager] Force Dead Hand needs a round in progress.");
            return;
        }

        (TileSuit suit, int rank)[] deadPattern =
        {
            (TileSuit.Bamboo, 1), (TileSuit.Bamboo, 3), (TileSuit.Bamboo, 5), (TileSuit.Bamboo, 7), (TileSuit.Bamboo, 9),
            (TileSuit.Characters, 1), (TileSuit.Characters, 3), (TileSuit.Characters, 5), (TileSuit.Characters, 7), (TileSuit.Characters, 9),
            (TileSuit.Dots, 1), (TileSuit.Dots, 3), (TileSuit.Dots, 5), (TileSuit.Dots, 7)
        };

        var forced = new List<Tile>();
        foreach (var (suit, rank) in deadPattern)
        {
            TileData data = AllTileTypes.Find(t => t != null && t.Suit == suit && t.Rank == rank);
            if (data != null) forced.Add(new Tile(data));
        }

        Hand.Tiles.Clear();
        Hand.AddTiles(forced);
        DiscardsRemaining = 0;

        Debug.Log($"[GameManager] Forced a dead hand of {forced.Count} tiles with 0 discards.");

        OnHandUpdated?.Invoke();
        CheckExitConditions();
        OnStateChanged?.Invoke();
    }
#endif

    private void RefillHand()
    {
        Hand.ClearSelfDrawnFlags();
        int missing = Hand.MissingCount;
        if (missing > 0 && Deck.Remaining > 0)
        {
            Hand.AddTiles(Deck.Draw(Mathf.Min(missing, Deck.Remaining)));
        }
    }
    
    private void CheckExitConditions()
    {
        if (CurrentScore >= CurrentTargetScore)
        {
            int earnedYuan = 4 + HandsRemaining; // ¥4 base + ¥1 per unused hand
            Yuan += earnedYuan;
            Debug.Log($"Quota Met! Earned ¥{earnedYuan} Yuan. Proceeding to Shop...");
            
            if (CurrentRound >= MaxRounds)
            {
                State = GameState.Victory;
            }
            else
            {
                State = GameState.Shop;
            }
            OnStateChanged?.Invoke();
        }
        else if (HandsRemaining <= 0)
        {
            Debug.Log("Out of hands! Game Over.");
            EndRun($"Out of hands.\nScored {CurrentScore} of the {CurrentTargetScore} needed.");
            OnStateChanged?.Invoke();
        }
        else if (IsDeadlocked)
        {
            // Not a loss yet — the player gets one redraw. Checked after the two conditions above
            // so clearing the quota or running out of hands always takes precedence over a hand
            // that merely happens to be unplayable.
            AwaitingRedraw = true;
            Debug.Log("[GameManager] Dead hand with no discards left. Offering redraw.");
            OnRedrawRequired?.Invoke();
            OnStateChanged?.Invoke();
        }
    }
}
