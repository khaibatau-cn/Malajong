using System.Collections.Generic;
using UnityEngine;

public class ArtifactTester : MonoBehaviour
{
    void Start()
    {
        TestPuristsFlute();
        TestImperialScholar();
        TestBambooWeaver();
        TestRestlessWind();
    }

    private Tile CreateTile(TileSuit suit, int rank)
    {
        var data = ScriptableObject.CreateInstance<TileData>();
        data.Suit = suit;
        data.Rank = rank;
        return new Tile(data);
    }

    private void TestPuristsFlute()
    {
        var flute = ScriptableObject.CreateInstance<PuristsFlute>();
        
        List<Tile> hand = new List<Tile>();
        for (int i = 0; i < 13; i++) hand.Add(CreateTile(TileSuit.Dots, 1));
        
        Pong combo = new Pong(hand.GetRange(0, 3));
        
        var (chips, mult) = ScoreEngine.Calculate(combo, hand, null, new List<SpiritData> { flute }, null);
        
        // Base Pure Hand is +150 chips. Pong base is 20. Flute should add +100 chips. Total = 270.
        Debug.Log($"[Artifact: Purist's Flute] Chips: {chips} | Expected: 270");
        Debug.Assert(chips == 270, "Purist's Flute failed to add +100 chips.");
    }

    private void TestImperialScholar()
    {
        var scholar = ScriptableObject.CreateInstance<ImperialScholar>();
        
        List<Tile> hand = new List<Tile>();
        for (int i = 0; i < 13; i++) hand.Add(CreateTile(TileSuit.Honor, 1));
        
        Pong combo = new Pong(hand.GetRange(0, 3));
        
        var (chips, mult) = ScoreEngine.Calculate(combo, hand, null, new List<SpiritData> { scholar }, null);
        
        // Base All Honors is x12. Scholar upgrades it to x20. Pong base mult is 2.0x. Total mult = 40.0x.
        Debug.Log($"[Artifact: Imperial Scholar] Mult: {mult} | Expected: 40");
        Debug.Assert(Mathf.Approximately(mult, 40.0f), "Imperial Scholar failed to upgrade multiplier to x20.");
    }

    private void TestBambooWeaver()
    {
        var weaver = ScriptableObject.CreateInstance<BambooWeaver>();
        var affinity = new SuitAffinity();
        
        List<Tile> hand = new List<Tile> {
            CreateTile(TileSuit.Bamboo, 1),
            CreateTile(TileSuit.Bamboo, 1),
            CreateTile(TileSuit.Bamboo, 1)
        };
        
        Pong combo = new Pong(hand);
        
        ScoreEngine.Calculate(combo, hand, affinity, new List<SpiritData> { weaver }, null);
        
        // Pong gives +0.1 affinity to its suit. Weaver multiplies incoming by 1.5x. So it should be +0.15.
        Debug.Log($"[Artifact: Bamboo Weaver] Bamboo Affinity: {affinity.GetLevel(TileSuit.Bamboo)} | Expected: 0.15");
        Debug.Assert(Mathf.Approximately(affinity.GetLevel(TileSuit.Bamboo), 0.15f), "Bamboo Weaver failed to boost affinity by 1.5x.");
    }

    private void TestRestlessWind()
    {
        var wind = ScriptableObject.CreateInstance<RestlessWind>();
        
        // Mock a GameManager
        GameObject go = new GameObject();
        GameManager gm = go.AddComponent<GameManager>();
        gm.InitializeRun();
        int initialDiscards = gm.DiscardsRemaining;
        
        List<Tile> hand = new List<Tile> {
            CreateTile(TileSuit.Honor, 0), // East Wind
            CreateTile(TileSuit.Honor, 0),
            CreateTile(TileSuit.Honor, 0)
        };
        
        Pong combo = new Pong(hand);
        
        ScoreEngine.Calculate(combo, hand, null, new List<SpiritData> { wind }, gm);
        
        Debug.Log($"[Artifact: Restless Wind] Discards Remaining: {gm.DiscardsRemaining} | Expected: {initialDiscards + 1}");
        Debug.Assert(gm.DiscardsRemaining == initialDiscards + 1, "Restless Wind failed to add +1 discard.");
    }
}
