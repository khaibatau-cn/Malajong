using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneSetupTool
{
    [MenuItem("Malajong/Setup Playable Scene Placeholder")]
    public static void SetupPlayableScene()
    {
        // 0. Ensure default TileData and SpiritData assets exist
        TileDataGenerator.GenerateAllGameData();

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
        MalajongAudio audioManager = Object.FindFirstObjectByType<MalajongAudio>();
        if (audioManager == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioManager = audioObj.AddComponent<MalajongAudio>();
            Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");
        }

        // 3. Ensure Canvas exists with 1920x1080 ScaleMode
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
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

        // ==========================================
        // 4. Panel 1: StartMenuUI
        // ==========================================
        Transform startPanel = CreateOrFindPanel(canvasObj.transform, "StartMenuUI", new Color(0.06f, 0.08f, 0.11f, 1f));
        CreateOrFindText(startPanel, "TitleText", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f),
            "<b><size=180%><color=#F1C40F>MALAJONG</color></size></b>\n<size=90%><color=#BDC3C7>A Mahjong Roguelike Deckbuilder</color></size>", 36, TextAlignmentOptions.Center);
        Button startRunBtn = CreateOrFindButton(startPanel, "StartRunButton", "START RUN 🀄", new Color(0.18f, 0.75f, 0.35f), new Vector2(0.38f, 0.32f), new Vector2(0.62f, 0.44f));

        // ==========================================
        // 5. Panel 2: MainGameUI (3-Column Balatro + Mahjong Layout)
        // ==========================================
        Transform mainPanel = CreateOrFindPanel(canvasObj.transform, "MainGameUI", new Color(0.07f, 0.09f, 0.12f, 1f));

        // ----------------------------------------------------
        // COLUMN 1: LEFT PANEL ("Blind & Stakes" - Inspired by Image 1)
        // ----------------------------------------------------
        Transform leftPanel = CreateOrFindSubPanel(mainPanel, "LeftBlindPanel", new Vector2(0.015f, 0.025f), new Vector2(0.20f, 0.975f), new Color(0.11f, 0.14f, 0.19f, 0.95f));

        // Blind Boss / Wind Card Box
        Transform blindCardBox = CreateOrFindSubPanel(leftPanel, "BlindCardBox", new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.96f), new Color(0.15f, 0.19f, 0.25f, 1f));
        
        // Gold Wind Header
        Transform blindHeader = CreateOrFindSubPanel(blindCardBox, "BlindHeader", new Vector2(0.0f, 0.82f), new Vector2(1.0f, 1.0f), new Color(0.85f, 0.65f, 0.15f, 1f));
        TextMeshProUGUI blindTitleText = CreateOrFindText(blindHeader, "BlindTitleText", Vector2.zero, Vector2.one,
            "<color=#1A1A1A><b>SOUTH WIND</b></color>", 20, TextAlignmentOptions.Center);

        // Wind Icon Box
        Transform windIconBox = CreateOrFindSubPanel(blindCardBox, "WindIconBox", new Vector2(0.32f, 0.44f), new Vector2(0.68f, 0.80f), new Color(0.92f, 0.92f, 0.92f, 1f));
        TextMeshProUGUI windGlyphText = CreateOrFindText(windIconBox, "WindGlyph", Vector2.zero, Vector2.one,
            "<color=#1A1A1A><size=130%><b>🀄</b></size></color>", 24, TextAlignmentOptions.Center);

        TextMeshProUGUI targetScoreText = CreateOrFindText(blindCardBox, "TargetScoreText", new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.42f),
            "Score at least\n<color=#E74C3C><size=130%><b>200</b></size></color>", 18, TextAlignmentOptions.Center);

        TextMeshProUGUI rewardText = CreateOrFindText(blindCardBox, "RewardText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f),
            "Rewards: <color=#F1C40F><b>$5</b></color>", 16, TextAlignmentOptions.Center);

        // Money Badge
        Transform moneyBox = CreateOrFindSubPanel(leftPanel, "MoneyBox", new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.46f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI coinsText = CreateOrFindText(moneyBox, "CoinsText", Vector2.zero, Vector2.one,
            "<size=140%><color=#F1C40F><b>$10</b></color></size>", 28, TextAlignmentOptions.Center);

        // Ante & Round Badges
        Transform anteBox = CreateOrFindSubPanel(leftPanel, "AnteBox", new Vector2(0.05f, 0.18f), new Vector2(0.48f, 0.31f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI anteText = CreateOrFindText(anteBox, "AnteText", Vector2.zero, Vector2.one,
            "Ante\n<color=#F39C12><b>1/4</b></color>", 16, TextAlignmentOptions.Center);

        Transform roundBox = CreateOrFindSubPanel(leftPanel, "RoundBox", new Vector2(0.52f, 0.18f), new Vector2(0.95f, 0.31f), new Color(0.08f, 0.10f, 0.14f, 1f));
        TextMeshProUGUI roundText = CreateOrFindText(roundBox, "RoundText", Vector2.zero, Vector2.one,
            "Round\n<color=#F39C12><b>1/5</b></color>", 16, TextAlignmentOptions.Center);

        // Run Info & Option Buttons
        Button runInfoBtn = CreateOrFindButton(leftPanel, "RunInfoButton", "RUN INFO", new Color(0.85f, 0.3f, 0.25f), new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.15f));
        Button optionsBtn = CreateOrFindButton(leftPanel, "OptionsButton", "SHOP / OPT", new Color(0.2f, 0.5f, 0.85f), new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.15f));

        // ----------------------------------------------------
        // COLUMN 2: CENTER STAGE ("Mahjong Mat & Spirit Rack" - Inspired by Image 2)
        // ----------------------------------------------------
        Transform centerStage = CreateOrFindSubPanel(mainPanel, "CenterStageMat", new Vector2(0.21f, 0.025f), new Vector2(0.77f, 0.975f), new Color(0.08f, 0.16f, 0.13f, 0.95f));

        // Top: 5-Slot Spirit Rack
        Transform spiritRackContainer = centerStage.Find("SpiritRackContainer");
        if (spiritRackContainer == null)
        {
            GameObject rackObj = new GameObject("SpiritRackContainer", typeof(RectTransform));
            rackObj.transform.SetParent(centerStage, false);
            RectTransform rackRect = rackObj.GetComponent<RectTransform>();
            rackRect.anchorMin = new Vector2(0.05f, 0.82f);
            rackRect.anchorMax = new Vector2(0.95f, 0.97f);
            rackRect.offsetMin = Vector2.zero;
            rackRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup rLayout = rackObj.AddComponent<HorizontalLayoutGroup>();
            rLayout.spacing = 14;
            rLayout.childAlignment = TextAnchor.MiddleCenter;
            rLayout.childControlWidth = true;
            rLayout.childControlHeight = true;

            spiritRackContainer = rackObj.transform;
            Undo.RegisterCreatedObjectUndo(rackObj, "Create SpiritRackContainer");
        }

        // Ensure 5 slots exist in rack
        for (int i = 0; i < 5; i++)
        {
            Transform slot = spiritRackContainer.Find($"SpiritSlot_{i}");
            if (slot == null)
            {
                GameObject slotObj = new GameObject($"SpiritSlot_{i}", typeof(RectTransform), typeof(Image));
                slotObj.transform.SetParent(spiritRackContainer, false);
                Image slotImg = slotObj.GetComponent<Image>();
                slotImg.color = new Color(0.12f, 0.22f, 0.18f, 0.85f);

                CreateOrFindText(slotObj.transform, "Label", Vector2.zero, Vector2.one, "<color=#7F8C8D><size=65%>Empty</size></color>", 14, TextAlignmentOptions.Center);
                Undo.RegisterCreatedObjectUndo(slotObj, $"Create SpiritSlot_{i}");
            }
        }

        // Middle-Top: Suit Affinity Multipliers Bar
        TextMeshProUGUI suitAffinityText = CreateOrFindText(centerStage, "SuitAffinityText", new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.81f),
            "<color=#2ECC71><b>🎋 Bamboo:</b> 1.0x</color>   |   <color=#E74C3C><b>🀄 Chars:</b> 1.0x</color>   |   <color=#3498DB><b>⚪ Dots:</b> 1.0x</color>", 16, TextAlignmentOptions.Center);

        // Sorting Quick Bar
        Transform sortBar = centerStage.Find("SortBarContainer");
        if (sortBar == null)
        {
            GameObject sortObj = new GameObject("SortBarContainer", typeof(RectTransform));
            sortObj.transform.SetParent(centerStage, false);
            RectTransform sortRect = sortObj.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0.15f, 0.58f);
            sortRect.anchorMax = new Vector2(0.85f, 0.65f);
            sortRect.offsetMin = Vector2.zero;
            sortRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup sLayout = sortObj.AddComponent<HorizontalLayoutGroup>();
            sLayout.spacing = 16;
            sLayout.childAlignment = TextAnchor.MiddleCenter;
            sLayout.childControlWidth = true;
            sLayout.childControlHeight = true;

            sortBar = sortObj.transform;
            Undo.RegisterCreatedObjectUndo(sortObj, "Create SortBarContainer");
        }

        Button sortSuitBtn = CreateOrFindButton(sortBar, "SortSuitButton", "↕ SORT SUIT", new Color(0.14f, 0.55f, 0.42f));
        Button sortRankBtn = CreateOrFindButton(sortBar, "SortRankButton", "↔ SORT RANK", new Color(0.48f, 0.25f, 0.65f));
        Button autoComboBtn = CreateOrFindButton(sortBar, "AutoComboButton", "⚡ AUTO-SELECT", new Color(0.20f, 0.50f, 0.75f));

        // Upright Hand Container (14 Tiles)
        Transform handContainer = centerStage.Find("HandContainer");
        if (handContainer == null)
        {
            GameObject handObj = new GameObject("HandContainer", typeof(RectTransform));
            handObj.transform.SetParent(centerStage, false);
            RectTransform handRect = handObj.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.02f, 0.18f);
            handRect.anchorMax = new Vector2(0.98f, 0.55f);
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = handObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            handContainer = handObj.transform;
            Undo.RegisterCreatedObjectUndo(handObj, "Create HandContainer");
        }

        // Action Buttons Row (Play Combo & Discard)
        Transform actionRow = centerStage.Find("ActionRowContainer");
        if (actionRow == null)
        {
            GameObject actObj = new GameObject("ActionRowContainer", typeof(RectTransform));
            actObj.transform.SetParent(centerStage, false);
            RectTransform actRect = actObj.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.20f, 0.04f);
            actRect.anchorMax = new Vector2(0.80f, 0.14f);
            actRect.offsetMin = Vector2.zero;
            actRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup actLayout = actObj.AddComponent<HorizontalLayoutGroup>();
            actLayout.spacing = 24;
            actLayout.childAlignment = TextAnchor.MiddleCenter;
            actLayout.childControlWidth = true;
            actLayout.childControlHeight = true;

            actionRow = actObj.transform;
            Undo.RegisterCreatedObjectUndo(actObj, "Create ActionRowContainer");
        }

        Button playButton = CreateOrFindButton(actionRow, "PlayButton", "PLAY COMBO 🀄", new Color(0.18f, 0.75f, 0.35f));
        Button discardButton = CreateOrFindButton(actionRow, "DiscardButton", "DISCARD ✕", new Color(0.85f, 0.28f, 0.22f));

        // ----------------------------------------------------
        // COLUMN 3: RIGHT PANEL ("Score Engine & Balatro Dual-Box" - Inspired by Image 1)
        // ----------------------------------------------------
        Transform rightPanel = CreateOrFindSubPanel(mainPanel, "RightScoreEnginePanel", new Vector2(0.78f, 0.025f), new Vector2(0.985f, 0.975f), new Color(0.10f, 0.13f, 0.17f, 0.95f));

        // Hands & Discards Pills
        Transform handsPill = CreateOrFindSubPanel(rightPanel, "HandsPill", new Vector2(0.05f, 0.85f), new Vector2(0.48f, 0.96f), new Color(0.08f, 0.18f, 0.28f, 1f));
        TextMeshProUGUI handsText = CreateOrFindText(handsPill, "HandsText", Vector2.zero, Vector2.one,
            "Hands\n<size=130%><color=#3498DB><b>10</b></color></size>", 16, TextAlignmentOptions.Center);

        Transform discardsPill = CreateOrFindSubPanel(rightPanel, "DiscardsPill", new Vector2(0.52f, 0.85f), new Vector2(0.95f, 0.96f), new Color(0.28f, 0.14f, 0.08f, 1f));
        TextMeshProUGUI discardsText = CreateOrFindText(discardsPill, "DiscardsText", Vector2.zero, Vector2.one,
            "Discards\n<size=130%><color=#E67E22><b>10</b></color></size>", 16, TextAlignmentOptions.Center);

        // Round Score
        TextMeshProUGUI roundScoreText = CreateOrFindText(rightPanel, "RoundScoreText", new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.83f),
            "Round score\n<size=150%><b>0</b></size>", 20, TextAlignmentOptions.Center);

        // Score Progress Bar
        Transform progressBarObj = rightPanel.Find("ScoreProgressBar");
        Image scoreFillImage = null;
        if (progressBarObj == null)
        {
            GameObject bgObj = new GameObject("ScoreProgressBar", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(rightPanel, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.08f, 0.70f);
            bgRect.anchorMax = new Vector2(0.92f, 0.715f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.8f);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            scoreFillImage = fillObj.GetComponent<Image>();
            scoreFillImage.color = new Color(0.18f, 0.85f, 0.35f, 1f);
            scoreFillImage.type = Image.Type.Filled;
            scoreFillImage.fillMethod = Image.FillMethod.Horizontal;
            scoreFillImage.fillAmount = 0f;

            Undo.RegisterCreatedObjectUndo(bgObj, "Create ScoreProgressBar");
        }
        else
        {
            scoreFillImage = progressBarObj.Find("Fill")?.GetComponent<Image>();
        }

        // Balatro Dual-Box Score HUD (ComboPreviewBox)
        Transform comboPreviewBox = CreateOrFindSubPanel(rightPanel, "ComboPreviewBox", new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.68f), new Color(0.06f, 0.08f, 0.11f, 1f));

        TextMeshProUGUI previewComboNameText = CreateOrFindText(comboPreviewBox, "ComboNameText", new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f),
            "<color=#F1C40F><b>SELECT TILES</b></color>", 20, TextAlignmentOptions.Center);

        // Blue Chips Box & Red Mult Box
        Transform chipsBox = CreateOrFindSubPanel(comboPreviewBox, "ChipsBox", new Vector2(0.08f, 0.26f), new Vector2(0.44f, 0.64f), new Color(0.11f, 0.31f, 0.55f, 1f));
        TextMeshProUGUI chipsBoxText = CreateOrFindText(chipsBox, "ChipsValue", Vector2.zero, Vector2.one,
            "<size=130%><b>0</b></size>", 26, TextAlignmentOptions.Center);

        CreateOrFindText(comboPreviewBox, "MultiplySymbol", new Vector2(0.44f, 0.26f), new Vector2(0.56f, 0.64f),
            "<size=120%><color=#E74C3C><b>×</b></color></size>", 24, TextAlignmentOptions.Center);

        Transform multBox = CreateOrFindSubPanel(comboPreviewBox, "MultBox", new Vector2(0.56f, 0.26f), new Vector2(0.92f, 0.64f), new Color(0.65f, 0.18f, 0.14f, 1f));
        TextMeshProUGUI multBoxText = CreateOrFindText(multBox, "MultValue", Vector2.zero, Vector2.one,
            "<size=130%><b>1.0</b></size>", 26, TextAlignmentOptions.Center);

        TextMeshProUGUI previewTotalScoreText = CreateOrFindText(comboPreviewBox, "TotalScoreText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.24f),
            "<color=#2ECC71><b>= 0 PTS</b></color>", 18, TextAlignmentOptions.Center);

        // Playable Combos List / Yaku Breakdown
        Transform comboListBox = CreateOrFindSubPanel(rightPanel, "PlayableCombosBox", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.42f), new Color(0.07f, 0.09f, 0.12f, 0.9f));
        TextMeshProUGUI playableCombosText = CreateOrFindText(comboListBox, "PlayableCombosText", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.95f),
            "<b>PLAYABLE IN HAND:</b>\n<color=#7F8C8D><i>Combos will appear here</i></color>", 14, TextAlignmentOptions.Left);

        // ==========================================
        // 6. Panel 3: ShopUI
        // ==========================================
        Transform shopPanel = CreateOrFindPanel(canvasObj.transform, "ShopUI", new Color(0.1f, 0.12f, 0.18f, 1f));
        TextMeshProUGUI shopStatusText = CreateOrFindText(shopPanel, "ShopStatusText", new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.95f),
            "<b>SPIRIT SHOP</b>   |   Coins: $5", 26, TextAlignmentOptions.Center);

        Transform shopCatalogContainer = shopPanel.Find("ShopCatalogContainer");
        if (shopCatalogContainer == null)
        {
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

            shopCatalogContainer = catObj.transform;
            Undo.RegisterCreatedObjectUndo(catObj, "Create ShopCatalogContainer");
        }

        // Generate Spirit Shop item cards
        List<SpiritData> spiritCatalog = LoadAllSpiritAssets();
        List<Button> buyButtons = new List<Button>();
        for (int i = 0; i < spiritCatalog.Count; i++)
        {
            SpiritData spirit = spiritCatalog[i];
            Transform itemCard = shopCatalogContainer.Find($"ShopItem_{i}");
            if (itemCard == null)
            {
                GameObject cardObj = new GameObject($"ShopItem_{i}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(shopCatalogContainer, false);
                Image cardImg = cardObj.GetComponent<Image>();
                cardImg.color = new Color(0.16f, 0.2f, 0.28f, 1f);

                CreateOrFindText(cardObj.transform, "CardText", new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.95f),
                    $"<b><color=#F1C40F>{spirit.SpiritName}</color></b>\n\n<size=80%>{spirit.Description}</size>", 16, TextAlignmentOptions.Center);

                Button buyBtn = CreateOrFindButton(cardObj.transform, "BuyButton", "BUY ($5)", new Color(0.18f, 0.75f, 0.35f), new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.3f));
                buyButtons.Add(buyBtn);
                Undo.RegisterCreatedObjectUndo(cardObj, $"Create ShopItem_{i}");
            }
            else
            {
                Button buyBtn = itemCard.Find("BuyButton")?.GetComponent<Button>();
                if (buyBtn != null) buyButtons.Add(buyBtn);
            }
        }

        Button nextRoundBtn = CreateOrFindButton(shopPanel, "NextRoundButton", "NEXT ROUND ➔", new Color(0.9f, 0.6f, 0.1f), new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.22f));

        // ==========================================
        // 7. Panel 4: GameOverUI
        // ==========================================
        Transform gameOverPanel = CreateOrFindPanel(canvasObj.transform, "GameOverUI", new Color(0.2f, 0.05f, 0.05f, 1f));
        TextMeshProUGUI gameOverText = CreateOrFindText(gameOverPanel, "GameOverSummaryText", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.85f),
            "<b>GAME OVER</b>", 32, TextAlignmentOptions.Center);
        Button restartBtn = CreateOrFindButton(gameOverPanel, "RestartButton", "PLAY AGAIN ↺", new Color(0.85f, 0.3f, 0.25f), new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.38f));

        // ==========================================
        // 8. Panel 5: VictoryUI
        // ==========================================
        Transform victoryPanel = CreateOrFindPanel(canvasObj.transform, "VictoryUI", new Color(0.05f, 0.18f, 0.1f, 1f));
        TextMeshProUGUI victoryText = CreateOrFindText(victoryPanel, "VictorySummaryText", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.85f),
            "<b>VICTORY!</b>", 32, TextAlignmentOptions.Center);
        Button victoryPlayAgainBtn = CreateOrFindButton(victoryPanel, "PlayAgainButton", "PLAY AGAIN ↺", new Color(0.18f, 0.75f, 0.35f), new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.38f));

        // 9. Generate & Save TilePrefab
        GameObject tilePrefab = CreateOrUpdateTilePrefab();

        // 10. Find or Create GameManager & UIManager
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gameManager = gmObj.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        }

        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiManager = uiObj.AddComponent<UIManager>();
            Undo.RegisterCreatedObjectUndo(uiObj, "Create UIManager");
        }

        // Assign Tile Assets to GameManager
        gameManager.AllTileTypes = LoadAllTileAssets();

        // Wire up UIManager references
        uiManager.gameManager = gameManager;
        uiManager.StartMenuPanel = startPanel.gameObject;
        uiManager.PlayingPanel = mainPanel.gameObject;
        uiManager.ShopPanel = shopPanel.gameObject;
        uiManager.GameOverPanel = gameOverPanel.gameObject;
        uiManager.VictoryPanel = victoryPanel.gameObject;

        // Left Blind Panel
        uiManager.BlindTitleText = blindTitleText;
        uiManager.TargetScoreText = targetScoreText;
        uiManager.RewardText = rewardText;
        uiManager.CoinsText = coinsText;
        uiManager.AnteText = anteText;
        uiManager.RoundText = roundText;
        uiManager.RunInfoButton = runInfoBtn;
        uiManager.OptionsButton = optionsBtn;

        // Center Stage
        uiManager.SpiritRackContainer = spiritRackContainer;
        uiManager.SuitAffinityText = suitAffinityText;
        uiManager.HandContainer = handContainer;
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
        uiManager.PreviewChipsBoxText = chipsBoxText;
        uiManager.PreviewMultBoxText = multBoxText;
        uiManager.PreviewTotalScoreText = previewTotalScoreText;
        uiManager.PlayableCombosText = playableCombosText;

        // Shop & End Panels
        uiManager.ShopStatusText = shopStatusText;
        uiManager.ShopCatalogContainer = shopCatalogContainer;
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

        victoryPlayAgainBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(victoryPlayAgainBtn.onClick, uiManager.RestartRun);

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("🎉 Balatro + Mahjong 3-Column UI Generated Successfully! Left Blind Panel, Center Mahjong Mat with Spirit Rack, and Right Balatro Score Engine wired!");
    }

    private static Transform CreateOrFindPanel(Transform canvas, string name, Color color)
    {
        Transform existing = canvas.Find(name);
        if (existing != null) return existing;

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

    private static Transform CreateOrFindSubPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject panelObj;
        if (existing == null)
        {
            panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(panelObj, "Create " + name);
        }
        else
        {
            panelObj = existing.gameObject;
        }

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panelObj.GetComponent<Image>();
        bg.color = color;

        return panelObj.transform;
    }

    private static TextMeshProUGUI CreateOrFindText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string defaultText, float fontSize, TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(name);
        GameObject textObj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = defaultText;
        text.color = Color.white;

        if (existing == null) Undo.RegisterCreatedObjectUndo(textObj, "Create " + name);
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
        rootRect.sizeDelta = new Vector2(62, 82);

        LayoutElement layout = rootObj.GetComponent<LayoutElement>();
        layout.minWidth = 62;
        layout.preferredWidth = 62;
        layout.minHeight = 82;
        layout.preferredHeight = 82;

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

        // Pixel Art Sprite Image
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
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        TileUI tileUI = rootObj.GetComponent<TileUI>();
        tileUI.CardVisual = faceObj.transform;
        tileUI.BackgroundImage = img;
        tileUI.TileSpriteImage = spriteImg;
        tileUI.TileText = text;

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
        Object.DestroyImmediate(rootObj);

        return prefabAsset;
    }

    private static Button CreateOrFindButton(Transform parent, string name, string labelText, Color color, Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        Transform existing = parent.Find(name);
        GameObject btnObj;
        if (existing == null)
        {
            btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(btnObj, "Create " + name);
        }
        else
        {
            btnObj = existing.gameObject;
        }

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

        TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
        }
        text.text = labelText;

        return btn;
    }
}
