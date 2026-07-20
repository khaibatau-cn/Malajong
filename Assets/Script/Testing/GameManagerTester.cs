using System.Collections.Generic;
using UnityEngine;

public class GameManagerTester : MonoBehaviour
{
    private GameManager gameManager;

    void Start()
    {
        // 1. Create dummy TileData for our test
        List<TileData> mockTypes = new List<TileData>();
        
        // Add a few tiles so we don't have an empty deck
        mockTypes.Add(CreateTileData(TileSuit.Bamboo, 1));
        mockTypes.Add(CreateTileData(TileSuit.Bamboo, 2));
        mockTypes.Add(CreateTileData(TileSuit.Bamboo, 3));
        mockTypes.Add(CreateTileData(TileSuit.Characters, 5));
        
        // 2. Set up GameManager
        gameManager = gameObject.AddComponent<GameManager>();
        gameManager.AllTileTypes = mockTypes; // 16 tiles total (4 of each)
        
        // 3. Run Loop
        gameManager.InitializeRun();
        
        // Hand should have 14 tiles
        Debug.Log($"[Tester] Hand count after start: {gameManager.Hand.Tiles.Count}");
        
        // Let's force play a 3-tile selection.
        if (gameManager.Hand.Tiles.Count >= 3)
        {
            List<Tile> selected = new List<Tile> 
            { 
                gameManager.Hand.Tiles[0], 
                gameManager.Hand.Tiles[1], 
                gameManager.Hand.Tiles[2] 
            };
            
            // This might not be a valid combo depending on the random shuffle, 
            // but it will successfully test the 'play combo' or 'discard fallback' flow!
            Debug.Log("[Tester] Attempting to play 3 random tiles...");
            gameManager.PlaySelectedTiles(selected);
        }
    }

    private TileData CreateTileData(TileSuit suit, int rank)
    {
        var data = ScriptableObject.CreateInstance<TileData>();
        data.Suit = suit;
        data.Rank = rank;
        return data;
    }
}
