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

        if (AllTileTypes != null && AllTileTypes.Count > 0)
        {
            InitializeRun();
        }
        else
        {
            Debug.LogWarning("[GameManager] No TileData assets found! Run 'Malajong -> Generate All Game Data' first.");
        }
    }
    
    public void InitializeRun()
    {
        CurrentRound = 1;
        Yuan = 5;
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
            State = GameState.GameOver;
            OnStateChanged?.Invoke();
        }
    }
}
