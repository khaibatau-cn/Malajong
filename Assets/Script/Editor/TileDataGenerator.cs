using UnityEngine;
using UnityEditor;

public class TileDataGenerator
{
    // This adds a new button to the top menu bar in Unity!
    [MenuItem("Malajong/Generate Default Tile Data")]
    public static void GenerateDefaultTiles()
    {
        string path = "Assets/ScriptableObjects/Tiles";
        
        // Create the folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Tiles"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tiles");

        // Generate Suits (Dots, Bamboo, Characters) Ranks 1-9
        TileSuit[] suits = { TileSuit.Dots, TileSuit.Bamboo, TileSuit.Characters };
        foreach (var suit in suits)
        {
            for (int i = 1; i <= 9; i++)
            {
                CreateTileAsset(suit, i, HonorEffect.None, $"{suit}_{i}", path);
            }
        }

        // Generate Honors (Winds: 1-4) and (Dragons: 5-7)
        for (int i = 1; i <= 7; i++)
        {
            CreateTileAsset(TileSuit.Honor, i, HonorEffect.None, $"Honor_{i}", path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated all 34 Default TileData Assets in {path}!");
    }

    private static void CreateTileAsset(TileSuit suit, int rank, HonorEffect effect, string name, string path)
    {
        string fullPath = $"{path}/{name}.asset";
        
        // Don't overwrite if it already exists
        if (AssetDatabase.LoadAssetAtPath<TileData>(fullPath) != null) return;
        
        TileData tile = ScriptableObject.CreateInstance<TileData>();
        tile.Suit = suit;
        tile.Rank = rank;
        tile.Effect = effect;

        AssetDatabase.CreateAsset(tile, fullPath);
    }
}
