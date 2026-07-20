using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<TileData> AllTileTypes; // Assign in editor
    
    public int CurrentTargetScore = 500;
    public int HandsRemaining = 4;
    public int DiscardsRemaining = 3;
    public int CurrentScore = 0;
    
    public TileBag Deck { get; private set; }
    public PlayerHand Hand { get; private set; }
    public SuitAffinity Affinity { get; private set; }
    public List<SpiritData> EquippedSpirits { get; private set; } = new List<SpiritData>();
    
    // UI Events
    public event System.Action OnHandUpdated;
    public event System.Action OnStateChanged;
    
    public enum GameState { StartMenu, Playing, Shop, GameOver, Victory }
    public GameState State { get; private set; }
    
    void Start()
    {
        // For testing, we won't automatically start if AllTileTypes is empty
        if (AllTileTypes != null && AllTileTypes.Count > 0)
        {
            InitializeRun();
        }
    }
    
    public void InitializeRun()
    {
        Deck = new TileBag();
        Hand = new PlayerHand();
        Affinity = new SuitAffinity();
        
        StartRound(500); // Ante 1 mock target
    }
    
    public void StartRound(int targetScore)
    {
        CurrentTargetScore = targetScore;
        CurrentScore = 0;
        HandsRemaining = 4;
        DiscardsRemaining = 3;
        State = GameState.Playing;
        
        Deck.Initialize(AllTileTypes);
        Hand.Tiles.Clear();
        Hand.AddTiles(Deck.Draw(PlayerHand.MaxSize));
        
        foreach (var spirit in EquippedSpirits)
        {
            if (spirit != null) spirit.OnRoundStart(this);
        }
        
        Debug.Log($"Round Started. Target: {targetScore}. Deck: {Deck.Remaining}. Hand: {Hand.Tiles.Count}");
        
        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }
    
    // Call this from UI when player selects tiles and clicks "Play Combo"
    public void PlaySelectedTiles(List<Tile> selectedTiles)
    {
        if (State != GameState.Playing) return;
        
        // 1. Try to play Full Hand first (instant win)
        if (selectedTiles.Count == 14)
        {
            var (bonusChips, bonusMult) = ScoreEngine.EvaluateFullHand(selectedTiles);
            if (bonusChips > 0)
            {
                int scoreGained = Mathf.RoundToInt(bonusChips * bonusMult);
                CurrentScore += scoreGained;
                Debug.Log($"FULL HAND ACHIEVED! Scored {scoreGained} bonus points.");
                State = GameState.Shop; // Round ends instantly
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
        var (chips, mult) = ScoreEngine.Calculate(combo, Hand.Tiles, Affinity, EquippedSpirits, this);
        int comboScore = Mathf.RoundToInt(chips * mult);
        
        CurrentScore += comboScore;
        HandsRemaining--;
        
        Debug.Log($"Played {combo.Name}. Scored {comboScore}. Total: {CurrentScore}/{CurrentTargetScore}. Hands left: {HandsRemaining}");
        
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
        // Discarding does not cost a hand, but we still check exit conditions just in case
        CheckExitConditions();
        
        OnHandUpdated?.Invoke();
        OnStateChanged?.Invoke();
    }
    
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
            Debug.Log("Quota Met! Proceeding to Shop...");
            State = GameState.Shop;
        }
        else if (HandsRemaining <= 0)
        {
            Debug.Log("Out of hands! Game Over.");
            State = GameState.GameOver;
        }
    }
}
