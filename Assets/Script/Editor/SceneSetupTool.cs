using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneSetupTool
{
    private static TMP_FontAsset cachedPixelFont = null;

    [MenuItem("Malajong/Setup Playable Scene Placeholder")]
    public static void SetupPlayableScene()
    {
        // 0. Ensure default TileData, SpiritData assets exist and wire newest pixel sprites
        TileDataGenerator.GenerateAllGameData();
        MahjongAssetWiringTool.SliceAndWireAllTiles();

        // 1. Ensure EventSystem exists and supports New Input System (Unity 6)
        GameObject eventSystemObj = GameObject.Find("EventSystem");
        if (eventSystemObj == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
        }

        var standalone = eventSystemObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        System.Type inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemType != null)
        {
            if (standalone != null) Object.DestroyImmediate(standalone);
            if (eventSystemObj.GetComponent(inputSystemType) == null)
            {
                eventSystemObj.AddComponent(inputSystemType);
            }
        }
        else if (standalone == null)
        {
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Ensure Audio Manager exists
        MalajongAudio audioManager = Object.FindAnyObjectByType<MalajongAudio>();
        if (audioManager == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioManager = audioObj.AddComponent<MalajongAudio>();
            Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");
        }

        // 3. Ensure Canvas exists with 1920x1080 ScaleMode
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }
        else
        {
            canvasObj = canvas.gameObject;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
        }

        // ----------------------------------------------------
        // CRITICAL: Wipe clean all existing UI panels to eliminate duplicate ghosting!
        // ----------------------------------------------------
        string[] panelsToClear = { "StartMenuUI", "MainGameUI", "ShopUI", "GameOverUI", "VictoryUI" };
        foreach (string panelName in panelsToClear)
        {
            Transform existing = canvasObj.transform.Find(panelName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        // ==========================================
        // 4. Panel 1: StartMenuUI
        // ==========================================
        Transform startPanel = CreatePanel(canvasObj.transform, "StartMenuUI", new Color(0.06f, 0.08f, 0.11f, 1f));
        CreateText(startPanel, "TitleText", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f),
            "<b><size=200%><color=#F1C40F>MALAJONG</color></size></b>\n<size=100%><color=#BDC3C7>A Mahjong Roguelike Deckbuilder</color></size>", 48, TextAlignmentOptions.Center);
        Button startRunBtn = CreateButton(startPanel, "StartRunButton", "START RUN", new Color(0.18f, 0.75f, 0.35f), 36, new Vector2(0.38f, 0.32f), new Vector2(0.62f, 0.44f));

        // ==========================================
        // 5. Panel 2: MainGameUI (3-Column Balatro + Mahjong Layout)
        // ==========================================
        Transform mainPanel = CreatePanel(canvasObj.transform, "MainGameUI", new Color(0.06f, 0.08f, 0.11f, 1f));

        // ----------------------------------------------------
        // COLUMN 1: LEFT PANEL ("Blind & Stakes" - Inspired by Image 1)
        // ----------------------------------------------------
        Transform leftPanel = CreateSubPanel(mainPanel, "LeftBlindPanel", new Vector2(0.015f, 0.025f), new Vector2(0.21f, 0.975f), new Color(0.11f, 0.14f, 0.19f, 0.95f));

        // Blind Boss / Wind Card Box
        Transform blindCardBox = CreateSubPanel(leftPanel, "BlindCardBox", new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.96f), new Color(0.15f, 0.19f, 0.25f, 1f));
        
        // Gold Wind Header
        Transform blindHeader = CreateSubPanel(blindCardBox, "BlindHeader", new Vector2(0.0f, 0.84f), new Vector2(1.0f, 1.0f), new Color(0.88f, 0.68f, 0.15f, 1f));
        TextMeshProUGUI blindTitleText = CreateText(blindHeader, "BlindTitleText", Vector2.zero, Vector2.one,
            "<color=#1A1A1A><b>EAST WIND</b></color>", 32, TextAlignmentOptions.Center);

        // Wind Icon Sprite Container (Displays actual Mahjong Wind Tile!)
        Transform windIconBox = CreateSubPanel(blindCardBox, "WindIconBox", new Vector2(0.30f, 0.45f), new Vector2(0.70f, 0.80f), new Color(0.92f, 0.92f, 0.92f, 1f));
        GameObject windSpriteObj = new GameObject("WindSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        windSpriteObj.transform.SetParent(windIconBox, false);
        RectTransform windSpriteRect = windSpriteObj.GetComponent<RectTransform>();
        windSpriteRect.anchorMin = Vector2.zero;
        windSpriteRect.anchorMax = Vector2.one;
        windSpriteRect.offsetMin = new Vector2(4, 4);
        windSpriteRect.offsetMax = new Vector2(-4, -4);
        Image blindTileImage = windSpriteObj.GetComponent<Image>();
        blindTileImage.preserveAspect = true;

        TextMeshProUGUI targetScoreText = CreateText(blindCardBox, "TargetScoreText", new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.42f),
            "Score at least\n<color=#E74C3C><size=140%><b>150</b></size></color>", 26, TextAlignmentOptions.Center);

        TextMeshProUGUI rewardText = CreateText(blindCardBox, "RewardText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f),
            "Reward: <color=#F1C40F><b>¥5</b></color>", 24, TextAlignmentOptions.Center);

        // Money Badge (Yuan)
        Transform moneyBox = CreateSubPanel(leftPanel, "MoneyBox", new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.43f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI yuanText = CreateText(moneyBox, "YuanText", Vector2.zero, Vector2.one,
            "<color=#F1C40F><b>¥5</b></color>", 44, TextAlignmentOptions.Center);

        // Ante & Round Badges
        Transform anteBox = CreateSubPanel(leftPanel, "AnteBox", new Vector2(0.05f, 0.18f), new Vector2(0.48f, 0.30f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI anteText = CreateText(anteBox, "AnteText", Vector2.zero, Vector2.one,
            "Ante\n<color=#F39C12><b>1/4</b></color>", 24, TextAlignmentOptions.Center);

        Transform roundBox = CreateSubPanel(leftPanel, "RoundBox", new Vector2(0.52f, 0.18f), new Vector2(0.95f, 0.30f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI roundText = CreateText(roundBox, "RoundText", Vector2.zero, Vector2.one,
            "Round\n<color=#F39C12><b>1/5</b></color>", 24, TextAlignmentOptions.Center);

        // Run Info & Option Buttons
        Button runInfoBtn = CreateButton(leftPanel, "RunInfoButton", "RUN INFO", new Color(0.85f, 0.3f, 0.25f), 22, new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.15f));
        Button optionsBtn = CreateButton(leftPanel, "OptionsButton", "SHOP / OPT", new Color(0.2f, 0.5f, 0.85f), 22, new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.15f));

        // ----------------------------------------------------
        // COLUMN 2: CENTER STAGE ("Mahjong Mat & Spirit Rack" - Inspired by Image 2)
        // ----------------------------------------------------
        Transform centerStage = CreateSubPanel(mainPanel, "CenterStageMat", new Vector2(0.22f, 0.025f), new Vector2(0.77f, 0.975f), new Color(0.07f, 0.15f, 0.12f, 0.95f));

        // Top: 5-Slot Spirit Rack
        GameObject rackObj = new GameObject("SpiritRackContainer", typeof(RectTransform));
        rackObj.transform.SetParent(centerStage, false);
        RectTransform rackRect = rackObj.GetComponent<RectTransform>();
        rackRect.anchorMin = new Vector2(0.05f, 0.84f);
        rackRect.anchorMax = new Vector2(0.95f, 0.97f);
        rackRect.offsetMin = Vector2.zero;
        rackRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup rackLayout = rackObj.AddComponent<HorizontalLayoutGroup>();
        rackLayout.spacing = 14;
        rackLayout.childAlignment = TextAnchor.MiddleCenter;
        rackLayout.childControlWidth = true;
        rackLayout.childControlHeight = true;

        for (int i = 0; i < 5; i++)
        {
            GameObject slot = new GameObject($"SpiritSlot_{i}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(rackObj.transform, false);
            Image slotImg = slot.GetComponent<Image>();
            slotImg.color = new Color(0.10f, 0.14f, 0.18f, 0.65f);

            CreateText(slot.transform, "Label", Vector2.zero, Vector2.one,
                "<color=#7F8C8D>Empty</color>", 24, TextAlignmentOptions.Center);
        }

        // Middle: Suit Affinity HUD
        Transform affinityBox = CreateSubPanel(centerStage, "SuitAffinityHUD", new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.82f), new Color(0.05f, 0.08f, 0.07f, 0.8f));
        TextMeshProUGUI suitAffinityText = CreateText(affinityBox, "AffinityText", Vector2.zero, Vector2.one,
            "<color=#2ECC71><b>Bamboo:</b> 1.0x</color>   |   <color=#E74C3C><b>Chars:</b> 1.0x</color>   |   <color=#3498DB><b>Dots:</b> 1.0x</color>", 28, TextAlignmentOptions.Center);

        // Center Area: Hand Container (Adjacency layout with 14 tiles)
        GameObject handObj = new GameObject("HandContainer", typeof(RectTransform));
        handObj.transform.SetParent(centerStage, false);
        RectTransform handRect = handObj.GetComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.01f, 0.32f);
        handRect.anchorMax = new Vector2(0.99f, 0.68f);
        handRect.offsetMin = Vector2.zero;
        handRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup handLayout = handObj.AddComponent<HorizontalLayoutGroup>();
        handLayout.spacing = 0; // 0px spacing for contiguous adjacent tiles
        handLayout.childAlignment = TextAnchor.MiddleCenter;
        handLayout.childControlWidth = false;
        handLayout.childControlHeight = false;
        handLayout.childForceExpandWidth = false;
        handLayout.childForceExpandHeight = false;

        // Sorting Quick Bar
        Button sortSuitBtn = CreateButton(centerStage, "SortSuitButton", "SORT SUIT", new Color(0.18f, 0.45f, 0.32f), 24, new Vector2(0.08f, 0.22f), new Vector2(0.34f, 0.30f));
        Button sortRankBtn = CreateButton(centerStage, "SortRankButton", "SORT RANK", new Color(0.18f, 0.45f, 0.32f), 24, new Vector2(0.37f, 0.22f), new Vector2(0.63f, 0.30f));
        Button autoComboBtn = CreateButton(centerStage, "AutoComboButton", "AUTO SELECT", new Color(0.25f, 0.35f, 0.55f), 24, new Vector2(0.66f, 0.22f), new Vector2(0.92f, 0.30f));

        // Bottom Action Bar: Play Combo & Discard
        Button playButton = CreateButton(centerStage, "PlayComboButton", "PLAY COMBO", new Color(0.18f, 0.65f, 0.38f), 32, new Vector2(0.10f, 0.04f), new Vector2(0.48f, 0.18f));
        Button discardButton = CreateButton(centerStage, "DiscardButton", "DISCARD", new Color(0.85f, 0.3f, 0.25f), 32, new Vector2(0.52f, 0.04f), new Vector2(0.90f, 0.18f));

        // ----------------------------------------------------
        // COLUMN 3: RIGHT PANEL ("Score Engine & Dual-Box HUD" - Balatro Style)
        // ----------------------------------------------------
        Transform rightPanel = CreateSubPanel(mainPanel, "RightScorePanel", new Vector2(0.78f, 0.025f), new Vector2(0.985f, 0.975f), new Color(0.08f, 0.10f, 0.14f, 0.98f));

        // Hands & Discards Badges
        Transform handsBox = CreateSubPanel(rightPanel, "HandsRemainingBox", new Vector2(0.05f, 0.86f), new Vector2(0.48f, 0.96f), new Color(0.11f, 0.31f, 0.55f, 1f));
        TextMeshProUGUI handsText = CreateText(handsBox, "HandsText", Vector2.zero, Vector2.one,
            "Hands\n<color=#3498DB><b>4</b></color>", 24, TextAlignmentOptions.Center);

        Transform discardsBox = CreateSubPanel(rightPanel, "DiscardsRemainingBox", new Vector2(0.52f, 0.86f), new Vector2(0.95f, 0.96f), new Color(0.65f, 0.35f, 0.12f, 1f));
        TextMeshProUGUI discardsText = CreateText(discardsBox, "DiscardsText", Vector2.zero, Vector2.one,
            "Discards\n<color=#E67E22><b>3</b></color>", 24, TextAlignmentOptions.Center);

        // Round Score & Progress Bar
        Transform roundScoreBox = CreateSubPanel(rightPanel, "RoundScoreBox", new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.84f), new Color(0.06f, 0.08f, 0.11f, 1f));
        TextMeshProUGUI roundScoreText = CreateText(roundScoreBox, "RoundScoreText", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.95f),
            "Round score\n<b>0</b>", 24, TextAlignmentOptions.Center);

        // Progress Bar
        GameObject bgObj = new GameObject("ScoreProgressBarBG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(roundScoreBox, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.08f, 0.08f);
        bgRect.anchorMax = new Vector2(0.92f, 0.24f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.15f, 0.18f, 0.24f, 1f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image scoreFillImage = fillObj.GetComponent<Image>();
        scoreFillImage.color = new Color(0.18f, 0.85f, 0.35f, 1f);
        scoreFillImage.type = Image.Type.Filled;
        scoreFillImage.fillMethod = Image.FillMethod.Horizontal;
        scoreFillImage.fillAmount = 0f;

        // Balatro Dual-Box Score HUD (ComboPreviewBox)
        Transform comboPreviewBox = CreateSubPanel(rightPanel, "ComboPreviewBox", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.68f), new Color(0.06f, 0.08f, 0.11f, 1f));

        TextMeshProUGUI previewComboNameText = CreateText(comboPreviewBox, "ComboNameText", new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f),
            "<color=#7F8C8D><b>SELECT TILES</b></color>", 28, TextAlignmentOptions.Center);

        // Blue Fu Box & Red Fan Box
        Transform fuBox = CreateSubPanel(comboPreviewBox, "FuBox", new Vector2(0.08f, 0.26f), new Vector2(0.44f, 0.64f), new Color(0.11f, 0.31f, 0.55f, 1f));
        TextMeshProUGUI fuBoxText = CreateText(fuBox, "FuValue", Vector2.zero, Vector2.one,
            "<b>0</b>", 40, TextAlignmentOptions.Center);

        CreateText(comboPreviewBox, "MultiplySymbol", new Vector2(0.44f, 0.26f), new Vector2(0.56f, 0.64f),
            "<color=#E74C3C><b>x</b></color>", 34, TextAlignmentOptions.Center);

        Transform fanBox = CreateSubPanel(comboPreviewBox, "FanBox", new Vector2(0.56f, 0.26f), new Vector2(0.92f, 0.64f), new Color(0.65f, 0.18f, 0.14f, 1f));
        TextMeshProUGUI fanBoxText = CreateText(fanBox, "FanValue", Vector2.zero, Vector2.one,
            "<b>1.0</b>", 40, TextAlignmentOptions.Center);

        TextMeshProUGUI previewTotalScoreText = CreateText(comboPreviewBox, "TotalScoreText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.24f),
            "<color=#7F8C8D>--</color>", 26, TextAlignmentOptions.Center);

        // Playable Combos List / Yaku Breakdown
        Transform comboListBox = CreateSubPanel(rightPanel, "PlayableCombosBox", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.40f), new Color(0.07f, 0.09f, 0.12f, 0.9f));
        TextMeshProUGUI playableCombosText = CreateText(comboListBox, "PlayableCombosText", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
            "<b>PLAYABLE IN HAND:</b>\n<color=#7F8C8D><i>Combos will appear here</i></color>", 22, TextAlignmentOptions.Left);

        // ==========================================
        // 6. Panel 3: ShopUI
        // ==========================================
        Transform shopPanel = CreatePanel(canvasObj.transform, "ShopUI", new Color(0.1f, 0.12f, 0.18f, 1f));
        TextMeshProUGUI shopStatusText = CreateText(shopPanel, "ShopStatusText", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.95f),
            "<b>SPIRIT SHOP</b>   |   Yuan: ¥5", 38, TextAlignmentOptions.Center);

        GameObject catObj = new GameObject("ShopCatalogContainer", typeof(RectTransform));
        catObj.transform.SetParent(shopPanel, false);
        RectTransform catRect = catObj.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0.1f, 0.30f);
        catRect.anchorMax = new Vector2(0.9f, 0.72f);
        catRect.offsetMin = Vector2.zero;
        catRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup catLayout = catObj.AddComponent<HorizontalLayoutGroup>();
        catLayout.spacing = 20;
        catLayout.childAlignment = TextAnchor.MiddleCenter;
        catLayout.childControlWidth = true;
        catLayout.childControlHeight = true;

        List<SpiritData> spiritCatalog = LoadAllSpiritAssets();
        for (int i = 0; i < spiritCatalog.Count; i++)
        {
            SpiritData spirit = spiritCatalog[i];
            GameObject cardObj = new GameObject($"ShopItem_{i}", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(catObj.transform, false);
            Image cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.16f, 0.2f, 0.28f, 1f);

            CreateText(cardObj.transform, "CardText", new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.95f),
                $"<b><color=#F1C40F>{spirit.SpiritName}</color></b>\n\n{spirit.Description}", 24, TextAlignmentOptions.Center);

            Button buyBtn = CreateButton(cardObj.transform, "BuyButton", "BUY (¥5)", new Color(0.18f, 0.65f, 0.38f), 24,
                new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.28f));
        }

        Button nextRoundBtn = CreateButton(shopPanel, "NextRoundButton", "NEXT ROUND ➔", new Color(0.2f, 0.5f, 0.85f), 32,
            new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.20f));

        // ==========================================
        // 7. Panel 4: GameOverUI
        // ==========================================
        Transform gameOverPanel = CreatePanel(canvasObj.transform, "GameOverUI", new Color(0.12f, 0.04f, 0.04f, 1f));
        TextMeshProUGUI gameOverText = CreateText(gameOverPanel, "GameOverSummaryText", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.8f),
            "<b>GAME OVER</b>\n\nQuota was not met.", 36, TextAlignmentOptions.Center);
        Button restartBtn = CreateButton(gameOverPanel, "RestartButton", "TRY AGAIN", new Color(0.85f, 0.3f, 0.25f), 30,
            new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.32f));

        // ==========================================
        // 8. Panel 5: VictoryUI
        // ==========================================
        Transform victoryPanel = CreatePanel(canvasObj.transform, "VictoryUI", new Color(0.04f, 0.12f, 0.06f, 1f));
        TextMeshProUGUI victoryText = CreateText(victoryPanel, "VictorySummaryText", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.8f),
            "<b>VICTORY!</b>\n\nYou completed all rounds of Malajong!", 36, TextAlignmentOptions.Center);
        Button victoryRestartBtn = CreateButton(victoryPanel, "VictoryRestartButton", "MAIN MENU", new Color(0.18f, 0.65f, 0.38f), 30,
            new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.32f));

        // 9. Generate & Save Upscaled TilePrefab (70x95)
        GameObject tilePrefab = CreateOrUpdateTilePrefab();

        // 10. Find or Create GameManager & UIManager
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gameManager = gmObj.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        }

        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiManager = uiObj.AddComponent<UIManager>();
            Undo.RegisterCreatedObjectUndo(uiObj, "Create UIManager");
        }

        // Assign Tile Assets to GameManager
        gameManager.AllTileTypes = LoadAllTileAssets();

        // ==========================================
        // 11. Wire References to UIManager
        // ==========================================
        uiManager.gameManager = gameManager;
        uiManager.StartMenuPanel = startPanel.gameObject;
        uiManager.PlayingPanel = mainPanel.gameObject;
        uiManager.ShopPanel = shopPanel.gameObject;
        uiManager.GameOverPanel = gameOverPanel.gameObject;
        uiManager.VictoryPanel = victoryPanel.gameObject;

        // Left Blind Panel
        uiManager.BlindTitleText = blindTitleText;
        uiManager.BlindTileIcon = blindTileImage;
        uiManager.TargetScoreText = targetScoreText;
        uiManager.RewardText = rewardText;
        uiManager.YuanText = yuanText;
        uiManager.AnteText = anteText;
        uiManager.RoundText = roundText;
        uiManager.RunInfoButton = runInfoBtn;
        uiManager.OptionsButton = optionsBtn;

        // Center Stage
        uiManager.SpiritRackContainer = rackObj.transform;
        uiManager.SuitAffinityText = suitAffinityText;
        uiManager.HandContainer = handObj.transform;
        uiManager.TilePrefab = tilePrefab;
        uiManager.SortSuitButton = sortSuitBtn;
        uiManager.SortRankButton = sortRankBtn;
        uiManager.AutoComboButton = autoComboBtn;
        uiManager.PlayButton = playButton;
        uiManager.DiscardButton = discardButton;

        // Right Score Engine
        uiManager.HandsRemainingText = handsText;
        uiManager.DiscardsRemainingText = discardsText;
        uiManager.RoundScoreText = roundScoreText;
        uiManager.ScoreProgressBar = scoreFillImage;
        uiManager.ComboPreviewBox = comboPreviewBox.gameObject;
        uiManager.PreviewComboNameText = previewComboNameText;
        uiManager.PreviewFuBoxText = fuBoxText;
        uiManager.PreviewFanBoxText = fanBoxText;
        uiManager.PreviewTotalScoreText = previewTotalScoreText;
        uiManager.PlayableCombosText = playableCombosText;

        // Shop & End Panels
        uiManager.ShopStatusText = shopStatusText;
        uiManager.ShopCatalogContainer = catObj.transform;
        uiManager.ShopCatalog = spiritCatalog;
        uiManager.GameOverSummaryText = gameOverText;
        uiManager.VictorySummaryText = victoryText;

        // Wire Button Listeners
        startRunBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startRunBtn.onClick, uiManager.StartRun);

        sortSuitBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sortSuitBtn.onClick, uiManager.SortHandBySuit);

        sortRankBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sortRankBtn.onClick, uiManager.SortHandByRank);

        autoComboBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(autoComboBtn.onClick, uiManager.AutoSelectBestCombo);

        playButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(playButton.onClick, uiManager.PlaySelected);

        discardButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(discardButton.onClick, uiManager.DiscardSelected);

        nextRoundBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextRoundBtn.onClick, uiManager.NextRound);

        restartBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(restartBtn.onClick, uiManager.RestartRun);

        victoryRestartBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(victoryRestartBtn.onClick, uiManager.RestartRun);

        // Apply pixel font to all TextMeshProUGUI components in the Canvas
        TMP_FontAsset pixelFont = GetOrCreatePixelFont();
        if (pixelFont != null)
        {
            foreach (var tmp in canvasObj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.font = pixelFont;
                EditorUtility.SetDirty(tmp);
            }
        }

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("🎉 Upscaled Adjacent Tile UI with Large Punchy Retro Pixel Font Generated Successfully!");
    }

    public static TMP_FontAsset GetOrCreatePixelFont()
    {
        if (cachedPixelFont != null) return cachedPixelFont;

        // 1. Search for existing TMP_FontAsset
        string[] tmpGuids = AssetDatabase.FindAssets("t:TMP_FontAsset m5x7");
        if (tmpGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(tmpGuids[0]);
            cachedPixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (cachedPixelFont != null) return cachedPixelFont;
        }

        // 2. Locate the TTF font file
        Font ttfFont = null;
        string[] ttfGuids = AssetDatabase.FindAssets("m5x7");
        foreach (string guid in ttfGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".ttf") || path.EndsWith(".otf"))
            {
                ttfFont = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (ttfFont != null) break;
            }
        }

        if (ttfFont == null)
        {
            string directPath = "Assets/Fonts/m5x7.ttf";
            AssetDatabase.ImportAsset(directPath, ImportAssetOptions.ForceUpdate);
            ttfFont = AssetDatabase.LoadAssetAtPath<Font>(directPath);
        }

        // 3. Create and save TMP_FontAsset if needed
        if (ttfFont != null)
        {
            string fontAssetPath = "Assets/Fonts/m5x7_FontAsset.asset";
            cachedPixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (cachedPixelFont == null)
            {
                cachedPixelFont = TMP_FontAsset.CreateFontAsset(ttfFont);
                if (cachedPixelFont != null)
                {
                    AssetDatabase.CreateAsset(cachedPixelFont, fontAssetPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[SceneSetupTool] Created TMP_FontAsset from '{ttfFont.name}' at '{fontAssetPath}'!");
                }
            }
        }

        return cachedPixelFont;
    }

    private static Transform CreatePanel(Transform canvas, string name, Color color)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas, false);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panelObj.GetComponent<Image>();
        bg.color = color;

        Undo.RegisterCreatedObjectUndo(panelObj, "Create " + name);
        return panelObj.transform;
    }

    private static Transform CreateSubPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(parent, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panelObj.GetComponent<Image>();
        bg.color = color;

        Undo.RegisterCreatedObjectUndo(panelObj, "Create " + name);
        return panelObj.transform;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string defaultText, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        
        TMP_FontAsset pixelFont = GetOrCreatePixelFont();
        if (pixelFont != null)
        {
            text.font = pixelFont;
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = defaultText;
        text.color = Color.white;

        Undo.RegisterCreatedObjectUndo(textObj, "Create " + name);
        return text;
    }

    private static List<TileData> LoadAllTileAssets()
    {
        List<TileData> tiles = new List<TileData>();
        string[] guids = AssetDatabase.FindAssets("t:TileData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileData tile = AssetDatabase.LoadAssetAtPath<TileData>(path);
            if (tile != null) tiles.Add(tile);
        }
        return tiles;
    }

    private static List<SpiritData> LoadAllSpiritAssets()
    {
        List<SpiritData> spirits = new List<SpiritData>();
        string[] guids = AssetDatabase.FindAssets("t:SpiritData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpiritData spirit = AssetDatabase.LoadAssetAtPath<SpiritData>(path);
            if (spirit != null) spirits.Add(spirit);
        }
        return spirits;
    }

    private static GameObject CreateOrUpdateTilePrefab()
    {
        string prefabPath = "Assets/Script/UI/TilePrefab.prefab";

        GameObject rootObj = new GameObject("TilePrefab", typeof(RectTransform), typeof(LayoutElement), typeof(TileUI));
        
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(70, 95);

        LayoutElement layout = rootObj.GetComponent<LayoutElement>();
        layout.minWidth = 70;
        layout.preferredWidth = 70;
        layout.minHeight = 95;
        layout.preferredHeight = 95;

        GameObject faceObj = new GameObject("TileFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        faceObj.transform.SetParent(rootObj.transform, false);
        
        RectTransform faceRect = faceObj.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = Vector2.zero;
        faceRect.offsetMax = Vector2.zero;

        Image img = faceObj.GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;

        Button btn = faceObj.GetComponent<Button>();
        if (btn != null) btn.transition = Selectable.Transition.None;

        // Pixel Art Sprite Image (Scaled to fill 70x95 tile)
        GameObject spriteObj = new GameObject("TileSpriteImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        spriteObj.transform.SetParent(faceObj.transform, false);
        RectTransform spriteRect = spriteObj.GetComponent<RectTransform>();
        spriteRect.anchorMin = Vector2.zero;
        spriteRect.anchorMax = Vector2.one;
        spriteRect.offsetMin = Vector2.zero;
        spriteRect.offsetMax = Vector2.zero;

        Image spriteImg = spriteObj.GetComponent<Image>();
        spriteImg.preserveAspect = true;
        spriteImg.raycastTarget = false;

        // Text Fallback
        GameObject textObj = new GameObject("TileText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(faceObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(2, 2);
        textRect.offsetMax = new Vector2(-2, -2);

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset pixelFont = GetOrCreatePixelFont();
        if (pixelFont != null)
        {
            text.font = pixelFont;
        }
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        TileUI tileUI = rootObj.GetComponent<TileUI>();
        tileUI.CardVisual = faceObj.transform;
        tileUI.BackgroundImage = img;
        tileUI.TileSpriteImage = spriteImg;
        tileUI.TileText = text;
        tileUI.LiftHeight = 36f;

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
        Object.DestroyImmediate(rootObj);

        return prefabAsset;
    }

    private static Button CreateButton(Transform parent, string name, string labelText, Color color, float fontSize = 24, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        if (anchorMin.HasValue && anchorMax.HasValue)
        {
            rect.anchorMin = anchorMin.Value;
            rect.anchorMax = anchorMax.Value;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Image img = btnObj.GetComponent<Image>();
        img.color = color;

        Button btn = btnObj.GetComponent<Button>();

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset pixelFont = GetOrCreatePixelFont();
        if (pixelFont != null)
        {
            text.font = pixelFont;
        }
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = labelText;

        Undo.RegisterCreatedObjectUndo(btnObj, "Create " + name);
        return btn;
    }
}
