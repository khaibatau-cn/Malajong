using System.Collections.Generic;
using UnityEngine;

public class ScoringTester : MonoBehaviour
{
    void Start()
    {
        TestValidPong();
        TestValidChow();
        TestConcealedKong();
        TestPureHand();
        TestAllHonors();
        TestFullMahjongHand();
        TestSuitAffinityScale();
        TestSuitAffinityDecay();
    }

    private Tile CreateTile(TileSuit suit, int rank, bool isSelfDrawn = false)
    {
        var data = ScriptableObject.CreateInstance<TileData>();
        data.Suit = suit;
        data.Rank = rank;
        return new Tile(data) { IsSelfDrawn = isSelfDrawn };
    }

    private void TestValidPong()
    {
        List<Tile> tiles = new List<Tile>
        {
            CreateTile(TileSuit.Bamboo, 5),
            CreateTile(TileSuit.Bamboo, 5),
            CreateTile(TileSuit.Bamboo, 5)
        };

        Pong pong = new Pong(tiles);
        var result = ScoreEngine.Calculate(pong, tiles);

        Debug.Log($"[Pong] Valid: {pong.IsValid()} | Fu: {result.fu} | Fan: {result.fan}");
        Debug.Assert(pong.IsValid() == true, "Pong should be valid");
        Debug.Assert(result.fu == 20 && result.fan == 2.0f, "Pong math wrong");
    }

    private void TestValidChow()
    {
        List<Tile> tiles = new List<Tile>
        {
            CreateTile(TileSuit.Dots, 2),
            CreateTile(TileSuit.Dots, 3),
            CreateTile(TileSuit.Dots, 4)
        };

        Chow chow = new Chow(tiles);
        var result = ScoreEngine.Calculate(chow, tiles);

        Debug.Log($"[Chow] Valid: {chow.IsValid()} | Fu: {result.fu} | Fan: {result.fan}");
        Debug.Assert(chow.IsValid() == true, "Chow should be valid");
        Debug.Assert(result.fu == 15 && result.fan == 2.0f, "Chow math wrong");
    }

    private void TestConcealedKong()
    {
        List<Tile> tiles = new List<Tile>
        {
            CreateTile(TileSuit.Characters, 9, true),
            CreateTile(TileSuit.Characters, 9, true),
            CreateTile(TileSuit.Characters, 9, true),
            CreateTile(TileSuit.Characters, 9, false)
        };

        ConcealedKong ck = new ConcealedKong(tiles);
        var result = ScoreEngine.Calculate(ck, tiles);

        Debug.Log($"[Concealed Kong (1 Discard)] Valid: {ck.IsValid()} | Fu: {result.fu} | Fan: {result.fan}");
        Debug.Assert(ck.IsValid() == false, "Concealed Kong should be invalid with a discard");
    }

    private void TestPureHand()
    {
        List<Tile> tiles = new List<Tile>();
        for (int i = 0; i < 13; i++) tiles.Add(CreateTile(TileSuit.Bamboo, 1));

        Pong dummyCombo = new Pong(tiles.GetRange(0, 3));
        var result = ScoreEngine.Calculate(dummyCombo, tiles);

        Debug.Log($"[Pure Hand] Fu: {result.fu} | Fan: {result.fan}");
        Debug.Assert(result.fu == 170 && result.fan == 20.0f, "Pure Hand math wrong (should be 20 base + 150 bonus, 2.0 base * 10.0 bonus)");
    }

    private void TestAllHonors()
    {
        List<Tile> tiles = new List<Tile>();
        for (int i = 0; i < 13; i++) tiles.Add(CreateTile(TileSuit.Honor, 0));

        Pong dummyCombo = new Pong(tiles.GetRange(0, 3));
        var result = ScoreEngine.Calculate(dummyCombo, tiles);

        Debug.Log($"[All Honors] Fu: {result.fu} | Fan: {result.fan}");
        Debug.Assert(result.fu == 200 && result.fan == 24.0f, "All Honors math wrong (should be 20 base + 180 bonus, 2.0 base * 12.0 bonus)");
    }

    private void TestFullMahjongHand()
    {
        List<Tile> tiles = new List<Tile>
        {
            // Pair
            CreateTile(TileSuit.Bamboo, 1), CreateTile(TileSuit.Bamboo, 1),
            // Pong
            CreateTile(TileSuit.Dots, 2), CreateTile(TileSuit.Dots, 2), CreateTile(TileSuit.Dots, 2),
            // Chow
            CreateTile(TileSuit.Characters, 4), CreateTile(TileSuit.Characters, 5), CreateTile(TileSuit.Characters, 6),
            // Pong
            CreateTile(TileSuit.Honor, 1), CreateTile(TileSuit.Honor, 1), CreateTile(TileSuit.Honor, 1),
            // Chow
            CreateTile(TileSuit.Bamboo, 7), CreateTile(TileSuit.Bamboo, 8), CreateTile(TileSuit.Bamboo, 9)
        };

        var result = ScoreEngine.EvaluateFullHand(tiles);
        Debug.Log($"[Full Hand (14 Tiles)] Fu: {result.bonusFu} | Fan: {result.bonusFan}");
        Debug.Assert(result.bonusFu == 100 && result.bonusFan == 8.0f, "Full hand solver failed to recognize valid hand");
    }

    private void TestSuitAffinityScale()
    {
        SuitAffinity affinity = new SuitAffinity();
        List<Tile> tiles = new List<Tile>
        {
            CreateTile(TileSuit.Bamboo, 5),
            CreateTile(TileSuit.Bamboo, 5),
            CreateTile(TileSuit.Bamboo, 5)
        };
        Pong pong = new Pong(tiles);

        var firstPlay = ScoreEngine.Calculate(pong, tiles, affinity);
        Debug.Assert(affinity.GetLevel(TileSuit.Bamboo) == pong.AffinityBonus, "Affinity should boost by Pong bonus (0.1)");
        
        var secondPlay = ScoreEngine.Calculate(pong, tiles, affinity);
        Debug.Assert(affinity.GetLevel(TileSuit.Bamboo) == pong.AffinityBonus * 2, "Affinity should stack to 0.2");
        
        Debug.Log($"[Affinity Scale] First Fan: {firstPlay.fan} | Second Fan: {secondPlay.fan} | Bamboo Affinity: {affinity.GetLevel(TileSuit.Bamboo)}");
    }

    private void TestSuitAffinityDecay()
    {
        SuitAffinity affinity = new SuitAffinity();
        
        // Manually boost Bamboo to 0.4
        affinity.Boost(TileSuit.Bamboo, 0.4f);
        
        // Play a Dots combo to see Bamboo decay
        List<Tile> tiles = new List<Tile>
        {
            CreateTile(TileSuit.Dots, 2),
            CreateTile(TileSuit.Dots, 3),
            CreateTile(TileSuit.Dots, 4)
        };
        Chow chow = new Chow(tiles);
        
        ScoreEngine.Calculate(chow, tiles, affinity);
        
        // Dots gains 0.1 (Chow bonus), Bamboo should decay by 0.05
        Debug.Assert(affinity.GetLevel(TileSuit.Dots) == chow.AffinityBonus, "Dots should gain 0.1");
        Debug.Assert(Mathf.Approximately(affinity.GetLevel(TileSuit.Bamboo), 0.35f), "Bamboo should decay by half of 0.1 (0.05) from 0.4 to 0.35");
        
        Debug.Log($"[Affinity Decay] Dots: {affinity.GetLevel(TileSuit.Dots)} | Bamboo: {affinity.GetLevel(TileSuit.Bamboo)}");
    }
}