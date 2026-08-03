using UnityEngine;
using UnityEditor;

public class TileDataGenerator
{
    [MenuItem("Malajong/Generate All Game Data")]
    public static void GenerateAllGameData()
    {
        GenerateDefaultTiles();
        GenerateDefaultSpirits();
        MahjongAssetWiringTool.SliceAndWireAllTiles();
    }

    [MenuItem("Malajong/Generate Default Tile Data")]
    public static void GenerateDefaultTiles()
    {
        string path = "Assets/ScriptableObjects/Tiles";
        
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Tiles"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tiles");

        TileSuit[] suits = { TileSuit.Dots, TileSuit.Bamboo, TileSuit.Characters };
        foreach (var suit in suits)
        {
            for (int i = 1; i <= 9; i++)
            {
                CreateTileAsset(suit, i, HonorEffect.None, $"{suit}_{i}", path);
            }
        }

        for (int i = 1; i <= 7; i++)
        {
            CreateTileAsset(TileSuit.Honor, i, HonorEffect.None, $"Honor_{i}", path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated all 34 Default TileData Assets in {path}!");
    }

    [MenuItem("Malajong/Generate Default Spirits")]
    public static void GenerateDefaultSpirits()
    {
        string path = "Assets/ScriptableObjects/Spirits";

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Spirits"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Spirits");

        CreateSpiritAsset<BambooWeaver>("BambooWeaver", "Bamboo Weaver", "Boosts Bamboo suit affinity buildup rate by 1.5x.", path);
        CreateSpiritAsset<ImperialScholar>("ImperialScholar", "Imperial Scholar", "Upgrades All Honors hand multiplier from x12 to x20.", path);
        CreateSpiritAsset<PuristsFlute>("PuristsFlute", "Purist's Flute", "Grants +100 bonus chips for a Pure Hand.", path);
        CreateSpiritAsset<RestlessWind>("RestlessWind", "Restless Wind", "Scoring a Wind Pong grants +1 Discard.", path);
        CreateSpiritAsset<BambooVow>("BambooVow", "Bamboo Vow", "+0.5x mult per Bamboo combo this round; resets on off-suit.", path);
        CreateSpiritAsset<BrokenCompass>("BrokenCompass", "Broken Compass", "Playing mixed suits grants +20 Chips burst.", path);
        CreateSpiritAsset<GreenDragonSpirit>("GreenDragonSpirit", "Green Dragon Spirit", "Playing Green Dragon grants +50 Chips & +2.0x Mult.", path);
        CreateSpiritAsset<CompassRose>("CompassRose", "Compass Rose", "Grants +5 bonus chips per tile matching your highest affinity suit.", path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated all SpiritData Assets in {path}!");
    }

    private static void CreateTileAsset(TileSuit suit, int rank, HonorEffect effect, string name, string path)
    {
        string fullPath = $"{path}/{name}.asset";
        
        if (AssetDatabase.LoadAssetAtPath<TileData>(fullPath) != null) return;
        
        TileData tile = ScriptableObject.CreateInstance<TileData>();
        tile.Suit = suit;
        tile.Rank = rank;
        tile.Effect = effect;

        AssetDatabase.CreateAsset(tile, fullPath);
    }

    private static void CreateSpiritAsset<T>(string fileName, string spiritName, string description, string path) where T : SpiritData
    {
        string fullPath = $"{path}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<T>(fullPath) != null) return;

        T spirit = ScriptableObject.CreateInstance<T>();
        spirit.SpiritName = spiritName;
        spirit.Description = description;

        AssetDatabase.CreateAsset(spirit, fullPath);
    }
}
