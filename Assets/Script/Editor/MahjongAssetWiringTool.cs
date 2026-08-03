using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MahjongAssetWiringTool
{
    private const string SheetPath = "Assets/Sprites/RiichiAssetByGambleMountain/sheet.png";
    private const string TilesFolderPath = "Assets/ScriptableObjects/Tiles";

    [MenuItem("Malajong/Auto-Slice Spritesheet and Wire Tiles")]
    public static void SliceAndWireAllTiles()
    {
        // 1. Configure Texture Importer & Auto-Slice Sprites
        SliceSpritesheet();

        // 2. Ensure TileData assets exist and link Sprites to TileData
        WireSpritesToTileData();
    }

    public static void SliceSpritesheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[MahjongAssetWiringTool] Could not find spritesheet at '{SheetPath}'!");
            return;
        }

        // Configure pixel art texture settings
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32;
        importer.alphaIsTransparency = true;

        // Load the texture to get its exact pixel dimensions
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        if (texture == null)
        {
            // Force import if not loaded yet
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        }

        int texWidth = texture != null ? texture.width : 288;
        int texHeight = texture != null ? texture.height : 768;
        int cellWidth = 32;
        int cellHeight = 32;

        int totalRows = texHeight / cellHeight;
        int totalCols = texWidth / cellWidth;

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();

        // Row indices from top of the texture (0 = top row)
        // In Unity Rect coordinates: y = texHeight - (topRowIndex + 1) * cellHeight
        
        // --- Row 2: Tile Backs & Blanks ---
        AddSprite(metaDataList, "Tile_Back_Blue", 0, 2, cellWidth, cellHeight, texHeight);
        AddSprite(metaDataList, "Tile_Face_Blank", 1, 2, cellWidth, cellHeight, texHeight);

        // --- Row 3: Characters (Wan / 萬) 1-9 ---
        for (int i = 1; i <= 9; i++)
        {
            AddSprite(metaDataList, $"Characters_{i}", i - 1, 3, cellWidth, cellHeight, texHeight);
        }

        // --- Row 4: Bamboo (Sou / 索) 1-9 ---
        for (int i = 1; i <= 9; i++)
        {
            AddSprite(metaDataList, $"Bamboo_{i}", i - 1, 4, cellWidth, cellHeight, texHeight);
        }

        // --- Row 5: Dots (Pin / 筒) 1-9 ---
        for (int i = 1; i <= 9; i++)
        {
            AddSprite(metaDataList, $"Dots_{i}", i - 1, 5, cellWidth, cellHeight, texHeight);
        }

        // --- Row 6: Honors (Winds & Dragons) ---
        // 1=East, 2=South, 3=West, 4=North, 5=White Dragon, 6=Green Dragon, 7=Red Dragon
        AddSprite(metaDataList, "Honor_1", 0, 6, cellWidth, cellHeight, texHeight); // East
        AddSprite(metaDataList, "Honor_2", 1, 6, cellWidth, cellHeight, texHeight); // South
        AddSprite(metaDataList, "Honor_3", 2, 6, cellWidth, cellHeight, texHeight); // West
        AddSprite(metaDataList, "Honor_4", 3, 6, cellWidth, cellHeight, texHeight); // North
        AddSprite(metaDataList, "Honor_5", 4, 6, cellWidth, cellHeight, texHeight); // White Dragon (Blank/Border)
        AddSprite(metaDataList, "Honor_6", 5, 6, cellWidth, cellHeight, texHeight); // Green Dragon (Fa)
        AddSprite(metaDataList, "Honor_7", 6, 6, cellWidth, cellHeight, texHeight); // Red Dragon (Chun)
        AddSprite(metaDataList, "Tile_Mystery", 8, 6, cellWidth, cellHeight, texHeight);

        // --- Row 7: Red Dora 5s ---
        AddSprite(metaDataList, "RedDora_Characters_5", 0, 7, cellWidth, cellHeight, texHeight);
        AddSprite(metaDataList, "RedDora_Bamboo_5", 1, 7, cellWidth, cellHeight, texHeight);
        AddSprite(metaDataList, "RedDora_Dots_5", 2, 7, cellWidth, cellHeight, texHeight);

        // Apply metadata to importer
        importer.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"[MahjongAssetWiringTool] Sliced {metaDataList.Count} sprites from '{SheetPath}' successfully!");
    }

    private static void AddSprite(List<SpriteMetaData> list, string name, int col, int topRow, int cellW, int cellH, int texH)
    {
        SpriteMetaData meta = new SpriteMetaData();
        meta.name = name;
        meta.alignment = (int)SpriteAlignment.Center;
        meta.pivot = new Vector2(0.5f, 0.5f);
        
        int x = col * cellW;
        int y = texH - (topRow + 1) * cellH;
        meta.rect = new Rect(x, y, cellW, cellH);
        
        list.Add(meta);
    }

    public static void WireSpritesToTileData()
    {
        // First ensure default tiles are generated
        TileDataGenerator.GenerateDefaultTiles();

        // Load all sub-sprites from the sliced texture
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

        foreach (var obj in subAssets)
        {
            if (obj is Sprite sp)
            {
                spriteDict[sp.name] = sp;
            }
        }

        Sprite tileBack = spriteDict.ContainsKey("Tile_Back_Blue") ? spriteDict["Tile_Back_Blue"] : null;
        int wiredCount = 0;

        // Wire Characters, Bamboo, Dots
        TileSuit[] suits = { TileSuit.Dots, TileSuit.Bamboo, TileSuit.Characters };
        foreach (var suit in suits)
        {
            for (int i = 1; i <= 9; i++)
            {
                string assetName = $"{suit}_{i}";
                string assetPath = $"{TilesFolderPath}/{assetName}.asset";
                TileData tileData = AssetDatabase.LoadAssetAtPath<TileData>(assetPath);

                if (tileData != null)
                {
                    if (spriteDict.TryGetValue(assetName, out Sprite sprite))
                    {
                        tileData.TileSprite = sprite;
                    }
                    tileData.TileBackSprite = tileBack;
                    EditorUtility.SetDirty(tileData);
                    wiredCount++;
                }
            }
        }

        // Wire Honors (1-7)
        for (int i = 1; i <= 7; i++)
        {
            string assetName = $"Honor_{i}";
            string assetPath = $"{TilesFolderPath}/{assetName}.asset";
            TileData tileData = AssetDatabase.LoadAssetAtPath<TileData>(assetPath);

            if (tileData != null)
            {
                if (spriteDict.TryGetValue(assetName, out Sprite sprite))
                {
                    tileData.TileSprite = sprite;
                }
                tileData.TileBackSprite = tileBack;
                EditorUtility.SetDirty(tileData);
                wiredCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MahjongAssetWiringTool] Successfully wired pixel sprites to {wiredCount} TileData assets!");
    }
}
