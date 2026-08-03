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

        // --- Panel 1: StartMenuUI ---
        Transform startPanel = CreateOrFindPanel(canvasObj.transform, "StartMenuUI", new Color(0.08f, 0.1f, 0.15f, 1f));
        CreateOrFindText(startPanel, "TitleText", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f),
            "<b><size=160%><color=#F1C40F>MALAJONG</color></size></b>\n<size=85%>A Mahjong Roguelike Deckbuilder</size>", 36, TextAlignmentOptions.Center);
        Button startRunBtn = CreateOrFindButton(startPanel, "StartRunButton", "START RUN 🀄", new Color(0.18f, 0.75f, 0.35f), new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.45f));

        // --- Panel 2: MainGameUI ---
        Transform mainPanel = CreateOrFindPanel(canvasObj.transform, "MainGameUI", new Color(0.11f, 0.14f, 0.19f, 1f));
        
        TextMeshProUGUI statusText = CreateOrFindText(mainPanel, "StatusText", new Vector2(0.25f, 0.84f), new Vector2(0.75f, 0.98f),
            "Score: 0 / 150\nHands: 4 | Discards: 3", 22, TextAlignmentOptions.Center);

        // Score Progress Bar
        Transform progressBarObj = mainPanel.Find("ScoreProgressBar");
        Image scoreFillImage = null;
        if (progressBarObj == null)
        {
            GameObject bgObj = new GameObject("ScoreProgressBar", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(mainPanel, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.28f, 0.825f);
            bgRect.anchorMax = new Vector2(0.72f, 0.835f);
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

        TextMeshProUGUI affinityHUDText = CreateOrFindText(mainPanel, "AffinityHUDText", new Vector2(0.02f, 0.80f), new Vector2(0.24f, 0.98f),
            "<b>SUIT AFFINITY</b>\nBamboo: 1.0x | Chars: 1.0x | Dots: 1.0x", 15, TextAlignmentOptions.Left);

        TextMeshProUGUI spiritsHUDText = CreateOrFindText(mainPanel, "SpiritsHUDText", new Vector2(0.76f, 0.80f), new Vector2(0.98f, 0.98f),
            "<b>EQUIPPED SPIRITS (0/5)</b>\nNone", 15, TextAlignmentOptions.Right);

        TextMeshProUGUI debugHintText = CreateOrFindText(mainPanel, "DebugHintText", new Vector2(0.15f, 0.71f), new Vector2(0.85f, 0.80f),
            "<b><color=#2ECC71>HINT:</color></b> Select tiles to preview combos.", 18, TextAlignmentOptions.Center);

        // Balatro Combo Preview HUD Box
        Transform previewBoxObj = mainPanel.Find("ComboPreviewBox");
        if (previewBoxObj == null)
        {
            GameObject pObj = new GameObject("ComboPreviewBox", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(mainPanel, false);
            RectTransform pRect = pObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.2f, 0.58f);
            pRect.anchorMax = new Vector2(0.8f, 0.69f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            Image pImg = pObj.GetComponent<Image>();
            pImg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

            HorizontalLayoutGroup pLayout = pObj.AddComponent<HorizontalLayoutGroup>();
            pLayout.spacing = 16;
            pLayout.padding = new RectOffset(16, 16, 8, 8);
            pLayout.childAlignment = TextAnchor.MiddleCenter;
            pLayout.childControlWidth = true;
            pLayout.childControlHeight = true;

            previewBoxObj = pObj.transform;
            Undo.RegisterCreatedObjectUndo(pObj, "Create ComboPreviewBox");
        }

        TextMeshProUGUI previewComboNameText = CreateOrFindText(previewBoxObj, "ComboNameText", Vector2.zero, Vector2.one,
            "<color=#F1C40F><b>NO SELECTION</b></color>", 20, TextAlignmentOptions.Center);
        TextMeshProUGUI previewChipsText = CreateOrFindText(previewBoxObj, "ChipsText", Vector2.zero, Vector2.one,
            "<color=#3498DB><b>0</b></color> Chips", 20, TextAlignmentOptions.Center);
        TextMeshProUGUI previewMultText = CreateOrFindText(previewBoxObj, "MultText", Vector2.zero, Vector2.one,
            "<color=#E74C3C><b>1.0X</b></color> Mult", 20, TextAlignmentOptions.Center);
        TextMeshProUGUI previewTotalText = CreateOrFindText(previewBoxObj, "TotalScoreText", Vector2.zero, Vector2.one,
            "<color=#2ECC71><b>= 0 PTS</b></color>", 22, TextAlignmentOptions.Center);

        // Sorting Quick Bar (Above HandContainer)
        Transform sortBar = mainPanel.Find("SortBarContainer");
        if (sortBar == null)
        {
            GameObject sortBarObj = new GameObject("SortBarContainer", typeof(RectTransform));
            sortBarObj.transform.SetParent(mainPanel, false);
            RectTransform sortRect = sortBarObj.GetComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(0.28f, 0.51f);
            sortRect.anchorMax = new Vector2(0.72f, 0.57f);
            sortRect.offsetMin = Vector2.zero;
            sortRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup sLayout = sortBarObj.AddComponent<HorizontalLayoutGroup>();
            sLayout.spacing = 20;
            sLayout.childAlignment = TextAnchor.MiddleCenter;
            sLayout.childControlWidth = true;
            sLayout.childControlHeight = true;

            sortBar = sortBarObj.transform;
            Undo.RegisterCreatedObjectUndo(sortBarObj, "Create SortBarContainer");
        }

        Button sortSuitBtn = CreateOrFindButton(sortBar, "SortSuitButton", "↕ SORT BY SUIT", new Color(0.12f, 0.55f, 0.45f));
        Button sortRankBtn = CreateOrFindButton(sortBar, "SortRankButton", "↔ SORT BY RANK", new Color(0.45f, 0.25f, 0.65f));

        // Hand Container
        Transform handContainer = mainPanel.Find("HandContainer");
        if (handContainer == null)
        {
            GameObject handObj = new GameObject("HandContainer", typeof(RectTransform));
            handObj.transform.SetParent(mainPanel, false);
            RectTransform handRect = handObj.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.02f, 0.20f);
            handRect.anchorMax = new Vector2(0.98f, 0.49f);
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

        // Gameplay Buttons
        Transform buttonContainer = mainPanel.Find("ButtonContainer");
        if (buttonContainer == null)
        {
            GameObject btnContainerObj = new GameObject("ButtonContainer", typeof(RectTransform));
            btnContainerObj.transform.SetParent(mainPanel, false);
            RectTransform btnContainerRect = btnContainerObj.GetComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.15f, 0.05f);
            btnContainerRect.anchorMax = new Vector2(0.85f, 0.16f);
            btnContainerRect.offsetMin = Vector2.zero;
            btnContainerRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup btnLayout = btnContainerObj.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 16;
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.childControlWidth = true;
            btnLayout.childControlHeight = true;

            buttonContainer = btnContainerObj.transform;
            Undo.RegisterCreatedObjectUndo(btnContainerObj, "Create ButtonContainer");
        }

        Button playButton = CreateOrFindButton(buttonContainer, "PlayButton", "PLAY COMBO", new Color(0.18f, 0.75f, 0.35f));
        Button discardButton = CreateOrFindButton(buttonContainer, "DiscardButton", "DISCARD", new Color(0.85f, 0.3f, 0.25f));
        Button debugAutoButton = CreateOrFindButton(buttonContainer, "DebugAutoButton", "⚡ AUTO-SELECT", new Color(0.2f, 0.6f, 0.9f));

        // Score Tally & Juice Animation Banner
        Transform tallyBannerObj = mainPanel.Find("ScoreTallyBanner");
        if (tallyBannerObj == null)
        {
            GameObject tObj = new GameObject("ScoreTallyBanner", typeof(RectTransform), typeof(Image));
            tObj.transform.SetParent(mainPanel, false);
            RectTransform tRect = tObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.22f, 0.42f);
            tRect.anchorMax = new Vector2(0.78f, 0.58f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            Image tImg = tObj.GetComponent<Image>();
            tImg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            VerticalLayoutGroup tLayout = tObj.AddComponent<VerticalLayoutGroup>();
            tLayout.spacing = 6;
            tLayout.padding = new RectOffset(16, 16, 12, 12);
            tLayout.childAlignment = TextAnchor.MiddleCenter;
            tLayout.childControlWidth = true;
            tLayout.childControlHeight = true;

            tallyBannerObj = tObj.transform;
            tObj.SetActive(false);
            Undo.RegisterCreatedObjectUndo(tObj, "Create ScoreTallyBanner");
        }

        TextMeshProUGUI tallyResultText = CreateOrFindText(tallyBannerObj, "TallyResultText", Vector2.zero, Vector2.one,
            "<b>PONG!</b>", 28, TextAlignmentOptions.Center);
        TextMeshProUGUI tallyChipsText = CreateOrFindText(tallyBannerObj, "TallyChipsText", Vector2.zero, Vector2.one,
            "<color=#3498DB>+45 CHIPS</color>", 24, TextAlignmentOptions.Center);
        TextMeshProUGUI tallyMultText = CreateOrFindText(tallyBannerObj, "TallyMultText", Vector2.zero, Vector2.one,
            "<color=#E74C3C>4.0X MULT</color>", 24, TextAlignmentOptions.Center);

        // --- Panel 3: ShopUI ---
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

        // --- Panel 4: GameOverUI ---
        Transform gameOverPanel = CreateOrFindPanel(canvasObj.transform, "GameOverUI", new Color(0.2f, 0.05f, 0.05f, 1f));
        TextMeshProUGUI gameOverText = CreateOrFindText(gameOverPanel, "GameOverSummaryText", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.85f),
            "<b>GAME OVER</b>", 32, TextAlignmentOptions.Center);
        Button restartBtn = CreateOrFindButton(gameOverPanel, "RestartButton", "PLAY AGAIN ↺", new Color(0.85f, 0.3f, 0.25f), new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.38f));

        // --- Panel 5: VictoryUI ---
        Transform victoryPanel = CreateOrFindPanel(canvasObj.transform, "VictoryUI", new Color(0.05f, 0.18f, 0.1f, 1f));
        TextMeshProUGUI victoryText = CreateOrFindText(victoryPanel, "VictorySummaryText", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.85f),
            "<b>VICTORY!</b>", 32, TextAlignmentOptions.Center);
        Button victoryPlayAgainBtn = CreateOrFindButton(victoryPanel, "PlayAgainButton", "PLAY AGAIN ↺", new Color(0.18f, 0.75f, 0.35f), new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.38f));

        // Generate & Save TilePrefab
        GameObject tilePrefab = CreateOrUpdateTilePrefab();

        // Find or Create GameManager & UIManager
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

        uiManager.HandContainer = handContainer;
        uiManager.StatusText = statusText;
        uiManager.ScoreProgressBar = scoreFillImage;
        uiManager.DebugHintText = debugHintText;
        uiManager.AffinityHUDText = affinityHUDText;
        uiManager.SpiritsHUDText = spiritsHUDText;

        uiManager.PlayButton = playButton;
        uiManager.DiscardButton = discardButton;
        uiManager.SortSuitButton = sortSuitBtn;
        uiManager.SortRankButton = sortRankBtn;
        uiManager.AutoComboButton = debugAutoButton;

        uiManager.ComboPreviewBox = previewBoxObj.gameObject;
        uiManager.PreviewComboNameText = previewComboNameText;
        uiManager.PreviewChipsText = previewChipsText;
        uiManager.PreviewMultText = previewMultText;
        uiManager.PreviewTotalScoreText = previewTotalText;

        uiManager.ScoreTallyBanner = tallyBannerObj.gameObject;
        uiManager.TallyResultText = tallyResultText;
        uiManager.TallyChipsText = tallyChipsText;
        uiManager.TallyMultText = tallyMultText;

        uiManager.ShopStatusText = shopStatusText;
        uiManager.ShopCatalogContainer = shopCatalogContainer;
        uiManager.ShopCatalog = spiritCatalog;
        uiManager.GameOverSummaryText = gameOverText;
        uiManager.VictorySummaryText = victoryText;
        uiManager.TilePrefab = tilePrefab;

        // Wire Button Listeners
        startRunBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startRunBtn.onClick, uiManager.StartRun);

        sortSuitBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sortSuitBtn.onClick, uiManager.SortHandBySuit);

        sortRankBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(sortRankBtn.onClick, uiManager.SortHandByRank);

        playButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(playButton.onClick, uiManager.PlaySelected);

        discardButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(discardButton.onClick, uiManager.DiscardSelected);

        debugAutoButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(debugAutoButton.onClick, uiManager.AutoSelectBestCombo);

        nextRoundBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextRoundBtn.onClick, uiManager.NextRound);

        restartBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(restartBtn.onClick, uiManager.RestartRun);

        victoryPlayAgainBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(victoryPlayAgainBtn.onClick, uiManager.RestartRun);

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("🎉 Balatro-Style Scene Setup Complete! Hand sorting, combo preview HUD, audio manager, and animated scoring banner wired!");
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
        rootRect.sizeDelta = new Vector2(64, 76);

        LayoutElement layout = rootObj.GetComponent<LayoutElement>();
        layout.minWidth = 64;
        layout.preferredWidth = 64;
        layout.minHeight = 76;
        layout.preferredHeight = 76;

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
        textRect.offsetMin = new Vector2(4, 4);
        textRect.offsetMax = new Vector2(-4, -4);

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
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
        }
        text.text = labelText;

        return btn;
    }
}
