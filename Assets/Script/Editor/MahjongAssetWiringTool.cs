using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MahjongAssetWiringTool
{
    private const string LightDeckPath = "Assets/Sprites/Tilesets/Blueeyedrat/deck_mahjong_light_0.png";
    private const string DarkDeckPath = "Assets/Sprites/Tilesets/Blueeyedrat/deck_mahjong_dark_0.png";
    private const string BacksDeckPath = "Assets/Sprites/Tilesets/Blueeyedrat/deck_mahjong_backs.png";
    private const string TilesFolderPath = "Assets/ScriptableObjects/Tiles";

    [MenuItem("Malajong/Auto-Slice Spritesheet and Wire Tiles")]
    public static void SliceAndWireAllTiles()
    {
        // 1. Slice and configure the new Blueeyedrat tile decks
        SliceNewDeck(LightDeckPath);
        SliceNewDeck(DarkDeckPath);
        SliceBacksDeck(BacksDeckPath);

        // 2. Ensure TileData assets exist and link Sprites to TileData
        WireSpritesToTileData();
    }

    public static void SliceNewDeck(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[MahjongAssetWiringTool] Could not find spritesheet at '{path}'!");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32;
        importer.alphaIsTransparency = true;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        int texHeight = texture != null ? texture.height : 320;
        int tileW = 46;
        int tileH = 62;
        int stepX = 64;
        int stepY = 64;
        int startX = 9;
        int startYFromTop = 1;

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();

        // Row 0: Characters 1-9 (Wan / 萬) + Red 5
        for (int i = 1; i <= 9; i++)
        {
            AddTileMeta(metaDataList, $"Characters_{i}", startX + (i - 1) * stepX, 0, tileW, tileH, stepY, startYFromTop, texHeight);
        }
        AddTileMeta(metaDataList, "RedDora_Characters_5", startX + 9 * stepX, 0, tileW, tileH, stepY, startYFromTop, texHeight);

        // Row 1: Bamboo 1-9 (Sou / 索) + Red 5
        for (int i = 1; i <= 9; i++)
        {
            AddTileMeta(metaDataList, $"Bamboo_{i}", startX + (i - 1) * stepX, 1, tileW, tileH, stepY, startYFromTop, texHeight);
        }
        AddTileMeta(metaDataList, "RedDora_Bamboo_5", startX + 9 * stepX, 1, tileW, tileH, stepY, startYFromTop, texHeight);

        // Row 2: Dots 1-9 (Pin / 筒) + Red 5
        for (int i = 1; i <= 9; i++)
        {
            AddTileMeta(metaDataList, $"Dots_{i}", startX + (i - 1) * stepX, 2, tileW, tileH, stepY, startYFromTop, texHeight);
        }
        AddTileMeta(metaDataList, "RedDora_Dots_5", startX + 9 * stepX, 2, tileW, tileH, stepY, startYFromTop, texHeight);

        // Row 3: Honors (1=East, 2=South, 3=West, 4=North, 5=White, 6=Green, 7=Red)
        AddTileMeta(metaDataList, "Honor_1", startX + 0 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // East
        AddTileMeta(metaDataList, "Honor_2", startX + 1 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // South
        AddTileMeta(metaDataList, "Honor_3", startX + 2 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // West
        AddTileMeta(metaDataList, "Honor_4", startX + 3 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // North
        AddTileMeta(metaDataList, "Honor_5", startX + 4 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // White Dragon (Blank)
        AddTileMeta(metaDataList, "Honor_6", startX + 5 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // Green Dragon (Fa)
        AddTileMeta(metaDataList, "Honor_7", startX + 6 * stepX, 3, tileW, tileH, stepY, startYFromTop, texHeight); // Red Dragon (Chun)

#pragma warning disable 0618
        importer.spritesheet = metaDataList.ToArray();
#pragma warning restore 0618
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        Debug.Log($"[MahjongAssetWiringTool] Sliced {metaDataList.Count} tiles from '{path}' successfully!");
    }

    public static void SliceBacksDeck(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32;
        importer.alphaIsTransparency = true;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int texHeight = texture != null ? texture.height : 64;

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();
        AddTileMeta(metaDataList, "Tile_Back_Blue", 9, 0, 46, 62, 64, 1, texHeight);
        AddTileMeta(metaDataList, "Tile_Back_Green", 9 + 64, 0, 46, 62, 64, 1, texHeight);
        AddTileMeta(metaDataList, "Tile_Back_Red", 9 + 128, 0, 46, 62, 64, 1, texHeight);
        AddTileMeta(metaDataList, "Tile_Back_Black", 9 + 192, 0, 46, 62, 64, 1, texHeight);

#pragma warning disable 0618
        importer.spritesheet = metaDataList.ToArray();
#pragma warning restore 0618
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
    }

    private static void AddTileMeta(List<SpriteMetaData> list, string name, int x, int rowIndex, int tileW, int tileH, int stepY, int topMargin, int texHeight)
    {
        SpriteMetaData meta = new SpriteMetaData();
        meta.name = name;
        meta.alignment = (int)SpriteAlignment.Center;
        meta.pivot = new Vector2(0.5f, 0.5f);

        int yFromTop = topMargin + rowIndex * stepY;
        int y = texHeight - yFromTop - tileH;
        meta.rect = new Rect(x, y, tileW, tileH);

        list.Add(meta);
    }

    public static void WireSpritesToTileData()
    {
        // First ensure default tile assets exist
        TileDataGenerator.GenerateDefaultTiles();

        // Load all sub-sprites from the sliced light deck texture
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(LightDeckPath);
        Dictionary<string, Sprite> spriteDict = new Dictionary<string, Sprite>();

        foreach (var obj in subAssets)
        {
            if (obj is Sprite sp)
            {
                spriteDict[sp.name] = sp;
            }
        }

        // Load back sprite
        Object[] backAssets = AssetDatabase.LoadAllAssetsAtPath(BacksDeckPath);
        Sprite tileBack = null;
        if (backAssets != null)
        {
            foreach (var obj in backAssets)
            {
                if (obj is Sprite sp && sp.name == "Tile_Back_Blue")
                {
                    tileBack = sp;
                    break;
                }
            }
        }

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
                    if (tileBack != null) tileData.TileBackSprite = tileBack;
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
                if (tileBack != null) tileData.TileBackSprite = tileBack;
                EditorUtility.SetDirty(tileData);
                wiredCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MahjongAssetWiringTool] Successfully wired new high-res pixel sprites to {wiredCount} TileData assets!");
    }
}
