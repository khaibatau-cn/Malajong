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
    }

    private void TestValidPong()
    {
        List<Tile> tiles = new List<Tile>
        {
            new Tile { Suit = TileSuit.Bamboo, Rank = 5 },
            new Tile { Suit = TileSuit.Bamboo, Rank = 5 },
            new Tile { Suit = TileSuit.Bamboo, Rank = 5 }
        };

        Pong pong = new Pong(tiles);
        var result = ScoreEngine.Calculate(pong, tiles);

        Debug.Log($"[Pong] Valid: {pong.IsValid()} | Chips: {result.chips} | Mult: {result.mult}");
        Debug.Assert(pong.IsValid() == true, "Pong should be valid");
        Debug.Assert(result.chips == 20 && result.mult == 2.0f, "Pong math wrong");
    }

    private void TestValidChow()
    {
        List<Tile> tiles = new List<Tile>
        {
            new Tile { Suit = TileSuit.Dots, Rank = 2 },
            new Tile { Suit = TileSuit.Dots, Rank = 3 },
            new Tile { Suit = TileSuit.Dots, Rank = 4 }
        };

        Chow chow = new Chow(tiles);
        var result = ScoreEngine.Calculate(chow, tiles);

        Debug.Log($"[Chow] Valid: {chow.IsValid()} | Chips: {result.chips} | Mult: {result.mult}");
        Debug.Assert(chow.IsValid() == true, "Chow should be valid");
        Debug.Assert(result.chips == 15 && result.mult == 2.0f, "Chow math wrong");
    }

    private void TestConcealedKong()
    {
        List<Tile> tiles = new List<Tile>
        {
            new Tile { Suit = TileSuit.Characters, Rank = 9, IsSelfDrawn = true },
            new Tile { Suit = TileSuit.Characters, Rank = 9, IsSelfDrawn = true },
            new Tile { Suit = TileSuit.Characters, Rank = 9, IsSelfDrawn = true },
            new Tile { Suit = TileSuit.Characters, Rank = 9, IsSelfDrawn = false }
        };

        ConcealedKong ck = new ConcealedKong(tiles);
        var result = ScoreEngine.Calculate(ck, tiles);

        Debug.Log($"[Concealed Kong (1 Discard)] Valid: {ck.IsValid()} | Chips: {result.chips} | Mult: {result.mult}");
        Debug.Assert(ck.IsValid() == false, "Concealed Kong should be invalid with a discard");
    }

    private void TestPureHand()
    {
        List<Tile> tiles = new List<Tile>();
        for (int i = 0; i < 13; i++) tiles.Add(new Tile { Suit = TileSuit.Bamboo, Rank = 1 });

        Pong dummyCombo = new Pong(tiles.GetRange(0, 3));
        var result = ScoreEngine.Calculate(dummyCombo, tiles);

        Debug.Log($"[Pure Hand] Chips: {result.chips} | Mult: {result.mult}");
        Debug.Assert(result.chips == 170 && result.mult == 20.0f, "Pure Hand math wrong (should be 20 base + 150 bonus, 2.0 base * 10.0 bonus)");
    }

    private void TestAllHonors()
    {
        List<Tile> tiles = new List<Tile>();
        for (int i = 0; i < 13; i++) tiles.Add(new Tile { Suit = TileSuit.Honor, Rank = 0 });

        Pong dummyCombo = new Pong(tiles.GetRange(0, 3));
        var result = ScoreEngine.Calculate(dummyCombo, tiles);

        Debug.Log($"[All Honors] Chips: {result.chips} | Mult: {result.mult}");
        Debug.Assert(result.chips == 200 && result.mult == 24.0f, "All Honors math wrong (should be 20 base + 180 bonus, 2.0 base * 12.0 bonus)");
    }

    private void TestFullMahjongHand()
    {
        List<Tile> tiles = new List<Tile>
        {
            // Pair
            new Tile { Suit = TileSuit.Bamboo, Rank = 1 }, new Tile { Suit = TileSuit.Bamboo, Rank = 1 },
            // Pong
            new Tile { Suit = TileSuit.Dots, Rank = 2 }, new Tile { Suit = TileSuit.Dots, Rank = 2 }, new Tile { Suit = TileSuit.Dots, Rank = 2 },
            // Chow
            new Tile { Suit = TileSuit.Characters, Rank = 4 }, new Tile { Suit = TileSuit.Characters, Rank = 5 }, new Tile { Suit = TileSuit.Characters, Rank = 6 },
            // Pong
            new Tile { Suit = TileSuit.Honor, Rank = 1 }, new Tile { Suit = TileSuit.Honor, Rank = 1 }, new Tile { Suit = TileSuit.Honor, Rank = 1 },
            // Chow
            new Tile { Suit = TileSuit.Bamboo, Rank = 7 }, new Tile { Suit = TileSuit.Bamboo, Rank = 8 }, new Tile { Suit = TileSuit.Bamboo, Rank = 9 }
        };

        var result = ScoreEngine.EvaluateFullHand(tiles);
        Debug.Log($"[Full Hand (14 Tiles)] Chips: {result.bonusChips} | Mult: {result.bonusMult}");
        Debug.Assert(result.bonusChips == 100 && result.bonusMult == 8.0f, "Full hand solver failed to recognize valid hand");
    }
}