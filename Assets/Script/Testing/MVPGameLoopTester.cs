using System.Collections.Generic;
using UnityEngine;

public class MVPGameLoopTester : MonoBehaviour
{
    void Start()
    {
        TestGameInit();
        TestRoundProgressionAndCoins();
        TestSpiritPurchase();
        TestVictoryCondition();
        TestHandSorting();
        TestScorePreview();
        Debug.Log("✅ All MVP Game Loop & QOL Tests Passed Successfully!");
    }

    private TileData CreateTileData(TileSuit suit, int rank)
    {
        var data = ScriptableObject.CreateInstance<TileData>();
        data.Suit = suit;
        data.Rank = rank;
        return data;
    }

    private SpiritData CreateMockSpirit(string name)
    {
        var spirit = ScriptableObject.CreateInstance<BambooWeaver>();
        spirit.SpiritName = name;
        spirit.Description = "Test Spirit";
        return spirit;
    }

    private List<TileData> CreateFullDeckTypes()
    {
        var types = new List<TileData>();
        types.Add(CreateTileData(TileSuit.Bamboo, 1));
        types.Add(CreateTileData(TileSuit.Bamboo, 2));
        types.Add(CreateTileData(TileSuit.Bamboo, 3));
        types.Add(CreateTileData(TileSuit.Characters, 5));
        types.Add(CreateTileData(TileSuit.Dots, 9));
        return types;
    }

    private void TestGameInit()
    {
        GameObject gmObj = new GameObject("TestGM");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.AllTileTypes = CreateFullDeckTypes();
        
        gm.StartGame();

        Debug.Assert(gm.CurrentRound == 1, "Initial round should be 1");
        Debug.Assert(gm.Coins == 5, "Initial coins should be 5");
        Debug.Assert(gm.State == GameManager.GameState.Playing, "Initial state should be Playing");
        Debug.Assert(gm.Hand.Tiles.Count == 14, "Initial hand count should be 14");
        Debug.Assert(gm.CurrentTargetScore == 150, "Round 1 target score should be 150");

        DestroyImmediate(gmObj);
    }

    private void TestRoundProgressionAndCoins()
    {
        GameObject gmObj = new GameObject("TestGM");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.AllTileTypes = CreateFullDeckTypes();
        gm.StartGame();

        gm.CurrentScore = 200;
        gm.NextRound();

        Debug.Assert(gm.CurrentRound == 2, "Current round should be 2 after NextRound()");
        Debug.Assert(gm.CurrentTargetScore == 350, "Round 2 target score should be 350");
        Debug.Assert(gm.State == GameManager.GameState.Playing, "State should be Playing for Round 2");

        DestroyImmediate(gmObj);
    }

    private void TestSpiritPurchase()
    {
        GameObject gmObj = new GameObject("TestGM");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.AllTileTypes = CreateFullDeckTypes();
        gm.StartGame();

        SpiritData spirit = CreateMockSpirit("Bamboo Master");
        bool success = gm.BuySpirit(spirit, 5);

        Debug.Assert(success == true, "Spirit purchase should succeed when player has $5");
        Debug.Assert(gm.Coins == 0, "Coins should be 0 after buying $5 spirit");
        Debug.Assert(gm.EquippedSpirits.Count == 1, "Equipped spirits count should be 1");
        Debug.Assert(gm.EquippedSpirits[0].SpiritName == "Bamboo Master", "Equipped spirit name should match");

        // Try buying without coins
        SpiritData spirit2 = CreateMockSpirit("Second Spirit");
        bool failBuy = gm.BuySpirit(spirit2, 5);
        Debug.Assert(failBuy == false, "Spirit purchase should fail when player has 0 coins");

        DestroyImmediate(gmObj);
    }

    private void TestVictoryCondition()
    {
        GameObject gmObj = new GameObject("TestGM");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.AllTileTypes = CreateFullDeckTypes();
        gm.StartGame();

        gm.CurrentRound = 5;
        gm.NextRound();

        Debug.Assert(gm.State == GameManager.GameState.Victory, "State should be Victory after completing round 5");

        DestroyImmediate(gmObj);
    }

    private void TestHandSorting()
    {
        PlayerHand hand = new PlayerHand();
        hand.AddTiles(new List<Tile>
        {
            new Tile(CreateTileData(TileSuit.Dots, 9)),
            new Tile(CreateTileData(TileSuit.Bamboo, 3)),
            new Tile(CreateTileData(TileSuit.Characters, 1)),
            new Tile(CreateTileData(TileSuit.Bamboo, 1))
        });

        // Test SortBySuit
        hand.SortBySuit();
        Debug.Assert(hand.Tiles[0].Suit == TileSuit.Bamboo && hand.Tiles[0].Rank == 1, "First tile after SortBySuit should be Bamboo 1");
        Debug.Assert(hand.Tiles[1].Suit == TileSuit.Bamboo && hand.Tiles[1].Rank == 3, "Second tile after SortBySuit should be Bamboo 3");
        Debug.Assert(hand.Tiles[2].Suit == TileSuit.Characters && hand.Tiles[2].Rank == 1, "Third tile after SortBySuit should be Characters 1");
        Debug.Assert(hand.Tiles[3].Suit == TileSuit.Dots && hand.Tiles[3].Rank == 9, "Fourth tile after SortBySuit should be Dots 9");

        // Test SortByRank
        hand.SortByRank();
        Debug.Assert(hand.Tiles[0].Rank == 1 && hand.Tiles[0].Suit == TileSuit.Bamboo, "First tile after SortByRank should be rank 1 Bamboo");
        Debug.Assert(hand.Tiles[1].Rank == 1 && hand.Tiles[1].Suit == TileSuit.Characters, "Second tile after SortByRank should be rank 1 Characters");
        Debug.Assert(hand.Tiles[2].Rank == 3 && hand.Tiles[2].Suit == TileSuit.Bamboo, "Third tile after SortByRank should be rank 3 Bamboo");
        Debug.Assert(hand.Tiles[3].Rank == 9 && hand.Tiles[3].Suit == TileSuit.Dots, "Fourth tile after SortByRank should be rank 9 Dots");
    }

    private void TestScorePreview()
    {
        var chowTiles = new List<Tile>
        {
            new Tile(CreateTileData(TileSuit.Bamboo, 1)),
            new Tile(CreateTileData(TileSuit.Bamboo, 2)),
            new Tile(CreateTileData(TileSuit.Bamboo, 3))
        };

        var preview = ScoreEngine.PreviewScore(chowTiles, chowTiles);
        Debug.Assert(preview.IsValid == true, "Chow 1-2-3 Bamboo should be valid preview");
        Debug.Assert(preview.ComboName == "Chow", "Combo name should be Chow");
        Debug.Assert(preview.TotalChips == 36, "Total chips should be 30 + 1 + 2 + 3 = 36");
        Debug.Assert(preview.TotalMult == 3.0f, "Base mult for Chow should be 3.0f");
        Debug.Assert(preview.ProjectedScore == 108, "Projected score should be 36 * 3 = 108");

        var invalidTiles = new List<Tile>
        {
            new Tile(CreateTileData(TileSuit.Bamboo, 1))
        };
        var invalidPreview = ScoreEngine.PreviewScore(invalidTiles, invalidTiles);
        Debug.Assert(invalidPreview.IsValid == false, "Single tile should be invalid combo preview");
    }
}
