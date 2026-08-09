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

        // Frame sprites must be sliced and set to Point before anything references them, or the
        // first build bakes in blurry, unstretchable borders. No-ops once they are configured.
        MalajongSkin.ConfigureAll();
        MalajongSkin.WireSpiritIcons();

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
        Transform startPanel = CreatePanel(canvasObj.transform, "StartMenuUI", MalajongTheme.Ink);
        CreateText(startPanel, "TitleText", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f),
            "<b><size=200%><color=#D9A93A>MALAJONG</color></size></b>\n<size=100%><color=#B8AC97>A Mahjong Roguelike Deckbuilder</color></size>", 48, TextAlignmentOptions.Center);
        Button startRunBtn = CreateButton(startPanel, "StartRunButton", "START RUN", MalajongTheme.VermilionBright, 36, new Vector2(0.38f, 0.32f), new Vector2(0.62f, 0.44f));

        // ==========================================
        // 5. Panel 2: MainGameUI (3-Column Balatro + Mahjong Layout)
        // ==========================================
        Transform mainPanel = CreatePanel(canvasObj.transform, "MainGameUI", MalajongTheme.Ink);

        // ----------------------------------------------------
        // COLUMN 1: LEFT PANEL ("Blind & Stakes" - Inspired by Image 1)
        // ----------------------------------------------------
        Transform leftPanel = CreateSubPanel(mainPanel, "LeftBlindPanel", new Vector2(0.015f, 0.025f), new Vector2(0.21f, 0.975f), MalajongTheme.Vermilion, cabinet: true);

        // Blind Boss / Wind Card Box
        Transform blindCardBox = CreateSubPanel(leftPanel, "BlindCardBox", new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.97f), MalajongTheme.VermilionDeep);
        
        // Gold Wind Header
        Transform blindHeader = CreateSubPanel(blindCardBox, "BlindHeader", new Vector2(0.0f, 0.84f), new Vector2(1.0f, 1.0f), MalajongTheme.Gold);
        TextMeshProUGUI blindTitleText = CreateText(blindHeader, "BlindTitleText", Vector2.zero, Vector2.one,
            "<color=#1A1006><b>EAST WIND</b></color>", 32, TextAlignmentOptions.Center);

        // Wind Icon Sprite Container (Displays actual Mahjong Wind Tile!)
        Transform windIconBox = CreateSubPanel(blindCardBox, "WindIconBox", new Vector2(0.30f, 0.45f), new Vector2(0.70f, 0.80f), MalajongTheme.Bone);
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
            "Score at least\n<color=#D8402E><size=140%><b>150</b></size></color>", 26, TextAlignmentOptions.Center);

        TextMeshProUGUI rewardText = CreateText(blindCardBox, "RewardText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f),
            "Reward: <color=#D9A93A><b>¥5</b></color>", 24, TextAlignmentOptions.Center);

        // Money Badge (Yuan)
        Transform moneyBox = CreateSubPanel(leftPanel, "MoneyBox", new Vector2(0.05f, 0.385f), new Vector2(0.95f, 0.485f), MalajongTheme.BoxFill);
        TextMeshProUGUI yuanText = CreateText(moneyBox, "YuanText", Vector2.zero, Vector2.one,
            "<color=#D9A93A><b>¥5</b></color>", 44, TextAlignmentOptions.Center);

        // Ante & Round Badges
        Transform anteBox = CreateSubPanel(leftPanel, "AnteBox", new Vector2(0.05f, 0.27f), new Vector2(0.48f, 0.37f), MalajongTheme.BoxFill);
        TextMeshProUGUI anteText = CreateText(anteBox, "AnteText", Vector2.zero, Vector2.one,
            "Ante\n<color=#D9A93A><b>1/4</b></color>", 24, TextAlignmentOptions.Center);

        Transform roundBox = CreateSubPanel(leftPanel, "RoundBox", new Vector2(0.52f, 0.27f), new Vector2(0.95f, 0.37f), MalajongTheme.BoxFill);
        TextMeshProUGUI roundText = CreateText(roundBox, "RoundText", Vector2.zero, Vector2.one,
            "Round\n<color=#D9A93A><b>1/5</b></color>", 24, TextAlignmentOptions.Center);

        // Hands & Discards — moved here from the right column so the left column owns the whole
        // run state, leaving the right column as a pure score engine.
        Transform handsBox = CreateSubPanel(leftPanel, "HandsRemainingBox", new Vector2(0.05f, 0.155f), new Vector2(0.48f, 0.255f), MalajongTheme.BoxFill);
        TextMeshProUGUI handsText = CreateText(handsBox, "HandsText", Vector2.zero, Vector2.one,
            "Hands\n<color=#43B87A><b>4</b></color>", 24, TextAlignmentOptions.Center);

        Transform discardsBox = CreateSubPanel(leftPanel, "DiscardsRemainingBox", new Vector2(0.52f, 0.155f), new Vector2(0.95f, 0.255f), MalajongTheme.BoxFill);
        TextMeshProUGUI discardsText = CreateText(discardsBox, "DiscardsText", Vector2.zero, Vector2.one,
            "Discards\n<color=#D9A93A><b>3</b></color>", 24, TextAlignmentOptions.Center);

        // Run Info & Option Buttons
        Button runInfoBtn = CreateButton(leftPanel, "RunInfoButton", "RUN INFO", MalajongTheme.VermilionRaised, 22, new Vector2(0.05f, 0.03f), new Vector2(0.48f, 0.14f));
        Button optionsBtn = CreateButton(leftPanel, "OptionsButton", "SHOP / OPT", MalajongTheme.VermilionRaised, 22, new Vector2(0.52f, 0.03f), new Vector2(0.95f, 0.14f));

        // ----------------------------------------------------
        // COLUMN 2: CENTER STAGE ("Mahjong Mat & Spirit Rack" - Inspired by Image 2)
        // ----------------------------------------------------
        Transform centerStage = CreateSubPanel(mainPanel, "CenterStageMat", new Vector2(0.22f, 0.025f), new Vector2(0.77f, 0.975f), MalajongTheme.Malachite, cabinet: true);

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
            slotImg.color = MalajongTheme.SlotEmpty;

            // Icon sits above the name. UIManager looks this child up by name and hides it for an
            // empty slot, so the placeholder reads as a vacancy rather than a broken sprite.
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(slot.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.62f);
            iconRect.anchorMax = new Vector2(0.5f, 0.62f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            iconRect.anchoredPosition = Vector2.zero;

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconObj.SetActive(false);

            CreateText(slot.transform, "Label", new Vector2(0f, 0f), new Vector2(1f, 0.38f),
                "<color=#96826F>Empty</color>", 20, TextAlignmentOptions.Center);
        }

        // Middle: Suit Affinity HUD — one blocked-out meter per suit. Stacked rather than in a
        // row because the comparison that matters is "which suit am I committed to", and three
        // bars sharing a left edge make that readable at a glance.
        Transform affinityBox = CreateSubPanel(centerStage, "SuitAffinityHUD", new Vector2(0.05f, 0.695f), new Vector2(0.95f, 0.835f), MalajongTheme.BoxFill);

        // Row 0 is the bottom row, since anchors run bottom-up.
        SuitAffinityMeter dotsMeter = CreateAffinityMeter(affinityBox, TileSuit.Dots, "DOTS", 0);
        SuitAffinityMeter charactersMeter = CreateAffinityMeter(affinityBox, TileSuit.Characters, "CHARS", 1);
        SuitAffinityMeter bambooMeter = CreateAffinityMeter(affinityBox, TileSuit.Bamboo, "BAMBOO", 2);

        // Center Area: Hand Container (Adjacency layout with 14 tiles)
        GameObject handObj = new GameObject("HandContainer", typeof(RectTransform));
        handObj.transform.SetParent(centerStage, false);
        RectTransform handRect = handObj.GetComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.01f, 0.32f);
        handRect.anchorMax = new Vector2(0.99f, 0.68f);
        handRect.offsetMin = Vector2.zero;
        handRect.offsetMax = Vector2.zero;

        // Attach Balatro Fan Curve hand layout component
        handObj.AddComponent<BalatroHandLayout>();

        HorizontalLayoutGroup handLayout = handObj.AddComponent<HorizontalLayoutGroup>();
        handLayout.spacing = 0; // 0px spacing for contiguous adjacent tiles
        handLayout.childAlignment = TextAnchor.MiddleCenter;
        handLayout.childControlWidth = false;
        handLayout.childControlHeight = false;
        handLayout.childForceExpandWidth = false;
        handLayout.childForceExpandHeight = false;

        // Sorting Quick Bar
        Button sortSuitBtn = CreateButton(centerStage, "SortSuitButton", "SORT SUIT", MalajongTheme.MalachiteDeep, 24, new Vector2(0.08f, 0.22f), new Vector2(0.34f, 0.30f));
        Button sortRankBtn = CreateButton(centerStage, "SortRankButton", "SORT RANK", MalajongTheme.MalachiteDeep, 24, new Vector2(0.37f, 0.22f), new Vector2(0.63f, 0.30f));
        Button autoComboBtn = CreateButton(centerStage, "AutoComboButton", "AUTO SELECT", MalajongTheme.MalachiteDeep, 24, new Vector2(0.66f, 0.22f), new Vector2(0.92f, 0.30f));

        // Bottom Action Bar: Play Combo & Discard
        // Play Combo is the one element on the screen that earns the accent colour. Everything
        // else stays lacquer, which is what makes it read as primary.
        Button playButton = CreateButton(centerStage, "PlayComboButton", "PLAY COMBO", MalajongTheme.VermilionBright, 32, new Vector2(0.10f, 0.04f), new Vector2(0.48f, 0.18f));
        Button discardButton = CreateButton(centerStage, "DiscardButton", "DISCARD", MalajongTheme.MalachiteRaised, 32, new Vector2(0.52f, 0.04f), new Vector2(0.90f, 0.18f));

        // ----------------------------------------------------
        // COLUMN 3: RIGHT PANEL ("Score Engine & Dual-Box HUD" - Balatro Style)
        // ----------------------------------------------------
        Transform rightPanel = CreateSubPanel(mainPanel, "RightScorePanel", new Vector2(0.78f, 0.025f), new Vector2(0.985f, 0.975f), MalajongTheme.Vermilion, cabinet: true);

        // Hands & Discards now live in the left column — this column is the score engine only.

        // Round Score & Progress Bar
        Transform roundScoreBox = CreateSubPanel(rightPanel, "RoundScoreBox", new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.97f), MalajongTheme.Ink);
        TextMeshProUGUI roundScoreText = CreateText(roundScoreBox, "RoundScoreText", new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.95f),
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
        bgImg.color = MalajongTheme.Ink;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image scoreFillImage = fillObj.GetComponent<Image>();
        scoreFillImage.color = MalajongTheme.MalachiteBright;
        scoreFillImage.type = Image.Type.Filled;
        scoreFillImage.fillMethod = Image.FillMethod.Horizontal;
        scoreFillImage.fillAmount = 0f;

        // Chop the bar into discrete blocks. Overlaying dividers on top of the fill keeps
        // Image.fillAmount working exactly as before, so UIManager's score roll-up is untouched —
        // but the bar now quantises like pixel art instead of sliding smoothly.
        GameObject segmentsObj = new GameObject("Segments", typeof(RectTransform));
        segmentsObj.transform.SetParent(bgObj.transform, false);
        RectTransform segmentsRect = segmentsObj.GetComponent<RectTransform>();
        segmentsRect.anchorMin = Vector2.zero;
        segmentsRect.anchorMax = Vector2.one;
        segmentsRect.offsetMin = Vector2.zero;
        segmentsRect.offsetMax = Vector2.zero;

        for (int i = 1; i < MalajongTheme.MeterSegments; i++)
        {
            float t = (float)i / MalajongTheme.MeterSegments;

            GameObject dividerObj = new GameObject($"Divider_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerObj.transform.SetParent(segmentsObj.transform, false);

            RectTransform dividerRect = dividerObj.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(t, 0f);
            dividerRect.anchorMax = new Vector2(t, 1f);
            dividerRect.sizeDelta = new Vector2(MalajongTheme.SegmentGap, 0f);
            dividerRect.anchoredPosition = Vector2.zero;

            Image dividerImg = dividerObj.GetComponent<Image>();
            dividerImg.color = MalajongTheme.Ink;
            dividerImg.raycastTarget = false;
        }

        // Balatro Dual-Box Score HUD (ComboPreviewBox)
        Transform comboPreviewBox = CreateSubPanel(rightPanel, "ComboPreviewBox", new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.68f), MalajongTheme.Ink);

        TextMeshProUGUI previewComboNameText = CreateText(comboPreviewBox, "ComboNameText", new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f),
            "<color=#96826F><b>SELECT TILES</b></color>", 28, TextAlignmentOptions.Center);

        // Blue Fu Box & Red Fan Box
        Transform fuBox = CreateSubPanel(comboPreviewBox, "FuBox", new Vector2(0.08f, 0.26f), new Vector2(0.44f, 0.64f), MalajongTheme.FuFill);
        TextMeshProUGUI fuBoxText = CreateText(fuBox, "FuValue", Vector2.zero, Vector2.one,
            "<b>0</b>", 40, TextAlignmentOptions.Center);

        CreateText(comboPreviewBox, "MultiplySymbol", new Vector2(0.44f, 0.26f), new Vector2(0.56f, 0.64f),
            "<color=#D8402E><b>x</b></color>", 34, TextAlignmentOptions.Center);

        Transform fanBox = CreateSubPanel(comboPreviewBox, "FanBox", new Vector2(0.56f, 0.26f), new Vector2(0.92f, 0.64f), MalajongTheme.FanFill);
        TextMeshProUGUI fanBoxText = CreateText(fanBox, "FanValue", Vector2.zero, Vector2.one,
            "<b>1.0</b>", 40, TextAlignmentOptions.Center);

        TextMeshProUGUI previewTotalScoreText = CreateText(comboPreviewBox, "TotalScoreText", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.24f),
            "<color=#96826F>--</color>", 26, TextAlignmentOptions.Center);

        // Playable Combos List / Yaku Breakdown
        Transform comboListBox = CreateSubPanel(rightPanel, "PlayableCombosBox", new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.40f), MalajongTheme.BoxFill);
        TextMeshProUGUI playableCombosText = CreateText(comboListBox, "PlayableCombosText", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
            "<b>PLAYABLE IN HAND:</b>\n<color=#96826F><i>Combos will appear here</i></color>", 22, TextAlignmentOptions.Left);

        // ==========================================
        // 6. Panel 3: ShopUI
        // ==========================================
        Transform shopPanel = CreatePanel(canvasObj.transform, "ShopUI", MalajongTheme.Ink);
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
            cardImg.color = MalajongTheme.Vermilion;

            // The catalog is built at edit time, so the icon can be assigned straight from the
            // asset — no runtime lookup needed.
            if (spirit.Icon != null)
            {
                GameObject shopIconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                shopIconObj.transform.SetParent(cardObj.transform, false);
                RectTransform shopIconRect = shopIconObj.GetComponent<RectTransform>();
                shopIconRect.anchorMin = new Vector2(0.5f, 0.88f);
                shopIconRect.anchorMax = new Vector2(0.5f, 0.88f);
                shopIconRect.sizeDelta = new Vector2(56f, 56f);
                shopIconRect.anchoredPosition = Vector2.zero;

                Image shopIcon = shopIconObj.GetComponent<Image>();
                shopIcon.sprite = spirit.Icon;
                shopIcon.preserveAspect = true;
                shopIcon.raycastTarget = false;
            }

            CreateText(cardObj.transform, "CardText", new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.80f),
                $"<b><color=#D9A93A>{spirit.SpiritName}</color></b>\n\n{spirit.Description}", 24, TextAlignmentOptions.Center);

            Button buyBtn = CreateButton(cardObj.transform, "BuyButton", "BUY (¥5)", MalajongTheme.MalachiteRaised, 24,
                new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.28f));
        }

        Button nextRoundBtn = CreateButton(shopPanel, "NextRoundButton", "NEXT ROUND >>", MalajongTheme.VermilionRaised, 32,
            new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.20f));

        // ==========================================
        // 7. Panel 4: GameOverUI
        // ==========================================
        Transform gameOverPanel = CreatePanel(canvasObj.transform, "GameOverUI", MalajongTheme.Ink);
        TextMeshProUGUI gameOverText = CreateText(gameOverPanel, "GameOverSummaryText", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.8f),
            "<b>GAME OVER</b>\n\nQuota was not met.", 36, TextAlignmentOptions.Center);
        Button restartBtn = CreateButton(gameOverPanel, "RestartButton", "TRY AGAIN", MalajongTheme.VermilionRaised, 30,
            new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.32f));

        // ==========================================
        // 8. Panel 5: VictoryUI
        // ==========================================
        Transform victoryPanel = CreatePanel(canvasObj.transform, "VictoryUI", MalajongTheme.MalachiteDeep);
        TextMeshProUGUI victoryText = CreateText(victoryPanel, "VictorySummaryText", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.8f),
            "<b>VICTORY!</b>\n\nYou completed all rounds of Malajong!", 36, TextAlignmentOptions.Center);
        Button victoryRestartBtn = CreateButton(victoryPanel, "VictoryRestartButton", "MAIN MENU", MalajongTheme.MalachiteRaised, 30,
            new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.32f));

        // ==========================================
        // 9. Panel 6: RunInfoModal (Overlay)
        // ==========================================
        Transform runInfoModal = CreatePanel(canvasObj.transform, "RunInfoModal", MalajongTheme.Scrim);
        Transform runInfoCard = CreateSubPanel(runInfoModal, "RunInfoCard", new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), MalajongTheme.Vermilion);
        
        TextMeshProUGUI runInfoTitle = CreateText(runInfoCard, "Title", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f),
            "<b>RUN INFORMATION & YAKU GUIDE</b>", 30, TextAlignmentOptions.Center);
        
        TextMeshProUGUI runInfoBody = CreateText(runInfoCard, "RunInfoContentText", new Vector2(0.06f, 0.13f), new Vector2(0.94f, 0.87f),
            "Loading Run Info...", 22, TextAlignmentOptions.TopLeft);
        
        Button closeRunInfoBtn = CreateButton(runInfoCard, "CloseButton", "CLOSE [X]", MalajongTheme.VermilionRaised, 24,
            new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.11f));
        
        runInfoModal.gameObject.SetActive(false);

        // ==========================================
        // 10. Panel 7: OptionsModal (Overlay)
        // ==========================================
        Transform optionsModal = CreatePanel(canvasObj.transform, "OptionsModal", MalajongTheme.Scrim);
        Transform optionsCard = CreateSubPanel(optionsModal, "OptionsCard", new Vector2(0.24f, 0.14f), new Vector2(0.76f, 0.86f), MalajongTheme.Vermilion);
        
        TextMeshProUGUI optionsTitle = CreateText(optionsCard, "Title", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f),
            "<b>GAME OPTIONS & SETTINGS</b>", 30, TextAlignmentOptions.Center);
        
        Button toggleAudioBtn = CreateButton(optionsCard, "ToggleAudioButton", "SFX: ENABLED", MalajongTheme.MalachiteRaised, 26,
            new Vector2(0.15f, 0.64f), new Vector2(0.85f, 0.78f));
        TextMeshProUGUI toggleAudioText = toggleAudioBtn.GetComponentInChildren<TextMeshProUGUI>();

        Transform rulesBox = CreateSubPanel(optionsCard, "RulesBox", new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.58f), MalajongTheme.InkSoft);
        CreateText(rulesBox, "RulesText", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
            "<b>HOW TO PLAY MALAJONG:</b>\n\n" +
            "• Select tiles to form valid Mahjong Combos (Pair, Chow, Pong, Kong).\n" +
            "• Pure Hand (all matching suits) grants huge Fu & Fan bonuses!\n" +
            "• Beat Round Target Scores to earn Yuan and buy powerful Spirits in the Shop.", 22, TextAlignmentOptions.Center);

        Button forfeitBtn = CreateButton(optionsCard, "ForfeitButton", "ABANDON RUN", MalajongTheme.VermilionBright, 24,
            new Vector2(0.12f, 0.12f), new Vector2(0.48f, 0.24f));

        Button closeOptionsBtn = CreateButton(optionsCard, "ResumeButton", "RESUME [X]", MalajongTheme.VermilionRaised, 24,
            new Vector2(0.52f, 0.12f), new Vector2(0.88f, 0.24f));

        optionsModal.gameObject.SetActive(false);

        // 11. Generate & Save Upscaled TilePrefab (70x95)
        GameObject tilePrefab = CreateOrUpdateTilePrefab();

        // 12. Find or Create GameManager & UIManager
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
        uiManager.BambooAffinityMeter = bambooMeter;
        uiManager.CharactersAffinityMeter = charactersMeter;
        uiManager.DotsAffinityMeter = dotsMeter;
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

        // Modals & Overlays
        uiManager.RunInfoModal = runInfoModal.gameObject;
        uiManager.RunInfoContentText = runInfoBody;
        uiManager.CloseRunInfoButton = closeRunInfoBtn;

        uiManager.OptionsModal = optionsModal.gameObject;
        uiManager.ToggleAudioButton = toggleAudioBtn;
        uiManager.ToggleAudioText = toggleAudioText;
        uiManager.ForfeitRunButton = forfeitBtn;
        uiManager.CloseOptionsButton = closeOptionsBtn;

        // Wire Button Listeners
        startRunBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startRunBtn.onClick, uiManager.StartRun);

        runInfoBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(runInfoBtn.onClick, uiManager.OpenRunInfoModal);

        optionsBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(optionsBtn.onClick, uiManager.OpenOptionsModal);

        closeRunInfoBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeRunInfoBtn.onClick, uiManager.CloseRunInfoModal);

        closeOptionsBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeOptionsBtn.onClick, uiManager.CloseOptionsModal);

        toggleAudioBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(toggleAudioBtn.onClick, uiManager.ToggleAudio);

        forfeitBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(forfeitBtn.onClick, uiManager.ForfeitRun);

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

    [MenuItem("Malajong/Rebuild Pixel Font Asset")]
    public static TMP_FontAsset RebuildPixelFontAsset()
    {
        cachedPixelFont = null;
        string fontAssetPath = "Assets/Fonts/m5x7_FontAsset.asset";
        string directPath = "Assets/Fonts/m5x7.ttf";
        
        Font ttfFont = AssetDatabase.LoadAssetAtPath<Font>(directPath);
        if (ttfFont == null)
        {
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
        }

        if (ttfFont == null)
        {
            Debug.LogError("[SceneSetupTool] Could not locate m5x7.ttf font file!");
            return null;
        }

        // Delete existing corrupted font asset if present
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(fontAssetPath);
        }

        TMP_FontAsset newFontAsset = TMP_FontAsset.CreateFontAsset(ttfFont);
        if (newFontAsset != null)
        {
            newFontAsset.name = "m5x7_FontAsset";
            AssetDatabase.CreateAsset(newFontAsset, fontAssetPath);

            // Crucial: Embed atlas textures and material as sub-assets so Unity serializes them properly
            if (newFontAsset.atlasTextures != null)
            {
                for (int i = 0; i < newFontAsset.atlasTextures.Length; i++)
                {
                    var tex = newFontAsset.atlasTextures[i];
                    if (tex != null)
                    {
                        tex.name = $"{ttfFont.name} Atlas";
                        AssetDatabase.AddObjectToAsset(tex, newFontAsset);
                    }
                }
            }

            if (newFontAsset.material != null)
            {
                newFontAsset.material.name = $"{ttfFont.name} Material";
                AssetDatabase.AddObjectToAsset(newFontAsset.material, newFontAsset);
            }

            // Assign default LiberationSans SDF as fallback for any rare unicode symbols
            TMP_FontAsset defaultFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (defaultFallback != null)
            {
                if (newFontAsset.fallbackFontAssetTable == null)
                {
                    newFontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                }
                if (!newFontAsset.fallbackFontAssetTable.Contains(defaultFallback))
                {
                    newFontAsset.fallbackFontAssetTable.Add(defaultFallback);
                }
            }

            EditorUtility.SetDirty(newFontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            cachedPixelFont = newFontAsset;
            Debug.Log($"[SceneSetupTool] Successfully created and embedded TMP_FontAsset for '{ttfFont.name}' at '{fontAssetPath}'!");
        }

        return newFontAsset;
    }

    public static TMP_FontAsset GetOrCreatePixelFont()
    {
        if (cachedPixelFont != null && cachedPixelFont.material != null && cachedPixelFont.atlasTextures != null && cachedPixelFont.atlasTextures.Length > 0 && cachedPixelFont.atlasTextures[0] != null)
        {
            return cachedPixelFont;
        }

        string fontAssetPath = "Assets/Fonts/m5x7_FontAsset.asset";
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (fontAsset != null && fontAsset.material != null && fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
        {
            cachedPixelFont = fontAsset;
            return cachedPixelFont;
        }

        return RebuildPixelFontAsset();
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

    /// <summary>
    /// Creates a framed box.
    ///
    /// With a skin sprite available (<see cref="MalajongSkin"/>) the panel image carries the fill
    /// colour and a Frame child draws the tinted border art on top of it. Without one it falls
    /// back to the original flat bevel, where the panel image *is* the border and an inset Fill
    /// child carries the colour. Both paths keep the same object names.
    ///
    /// The returned transform is still the named panel (not the fill), so every existing reference
    /// and SetActive call keeps working. Fill and Frame are added first, so anything a caller
    /// parents to this panel afterwards renders on top of both.
    /// </summary>
    /// <param name="cabinet">
    /// True for the three full-height columns, which get the heavier frame. Inner value boxes keep
    /// the lighter one so the two do not read at the same weight.
    /// </param>
    private static Transform CreateSubPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color, bool cabinet = false)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(parent, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Sprite frameSprite = cabinet ? MalajongSkin.PanelFrame : MalajongSkin.BoxFrame;
        bool skinned = frameSprite != null;

        Image border = panelObj.GetComponent<Image>();
        border.color = skinned ? color : MalajongTheme.GoldDark;

        // Skinned, the frame art supplies the edge, so the fill runs the full rect. Unskinned, the
        // inset is what leaves the border visible.
        int inset = skinned ? 0 : MalajongTheme.Border;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(panelObj.transform, false);

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(inset, inset);
        fillRect.offsetMax = new Vector2(-inset, -inset);

        Image fill = fillObj.GetComponent<Image>();
        fill.color = color;
        fill.raycastTarget = false;

        // Added after Fill so the border art sits on top of the colour rather than under it.
        if (skinned) CreateFrameOverlay(panelObj.transform, frameSprite, MalajongTheme.Gold);

        Undo.RegisterCreatedObjectUndo(panelObj, "Create " + name);
        return panelObj.transform;
    }

    /// <summary>
    /// Stretches a 9-sliced border sprite over its parent as a non-interactive overlay.
    ///
    /// The art is white line work, so the tint here is what gives it a colour — which keeps
    /// MalajongTheme the single source of truth even though the shape now comes from a sprite.
    /// </summary>
    private static Image CreateFrameOverlay(Transform parent, Sprite sprite, Color tint)
    {
        GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObj.transform.SetParent(parent, false);

        RectTransform frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        Image frame = frameObj.GetComponent<Image>();
        frame.sprite = sprite;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = MalajongSkin.PixelsPerUnitMultiplier;
        frame.color = tint;
        // The parent already handles hit-testing; a raycasting overlay would swallow button clicks.
        frame.raycastTarget = false;

        return frame;
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

    /// <summary>
    /// One affinity row: suit label on the left, a strip of discrete blocks, live multiplier on
    /// the right.
    ///
    /// The blocks are separate Images rather than a filled Image with dividers laid over it — the
    /// trick the score bar uses — because affinity has no roll-up animation to protect, and
    /// individually addressable blocks can pop as they light.
    /// </summary>
    /// <param name="row">0 is the bottom row. Anchors run bottom-up.</param>
    private static SuitAffinityMeter CreateAffinityMeter(Transform parent, TileSuit suit, string label, int row)
    {
        // Three rows plus two gutters, inset 0.04 top and bottom.
        const float RowHeight = 0.27f;
        const float RowGap = 0.055f;

        float y0 = 0.04f + row * (RowHeight + RowGap);

        GameObject rowObj = new GameObject($"AffinityMeter_{suit}", typeof(RectTransform));
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.03f, y0);
        rowRect.anchorMax = new Vector2(0.97f, y0 + RowHeight);
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        Color lit = MalajongTheme.ForSuit(suit);
        Color unlit = MalajongTheme.MeterUnlit(lit);

        TextMeshProUGUI labelText = CreateText(rowObj.transform, "Label",
            new Vector2(0f, 0f), new Vector2(0.20f, 1f), label, 24, TextAlignmentOptions.Left);
        labelText.color = lit;

        GameObject trackObj = new GameObject("Track", typeof(RectTransform));
        trackObj.transform.SetParent(rowObj.transform, false);

        RectTransform trackRect = trackObj.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.22f, 0.18f);
        trackRect.anchorMax = new Vector2(0.85f, 0.82f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;

        Image[] segments = new Image[MalajongTheme.MeterSegments];
        float slotWidth = 1f / MalajongTheme.MeterSegments;

        for (int i = 0; i < MalajongTheme.MeterSegments; i++)
        {
            GameObject segObj = new GameObject($"Segment_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            segObj.transform.SetParent(trackObj.transform, false);

            RectTransform segRect = segObj.GetComponent<RectTransform>();
            segRect.anchorMin = new Vector2(i * slotWidth, 0f);
            segRect.anchorMax = new Vector2((i + 1) * slotWidth, 1f);
            // Inset half a gap on each side, so every gutter between blocks is one full gap wide
            // and the strip still ends flush with the track at both ends.
            segRect.offsetMin = new Vector2(MalajongTheme.SegmentGap * 0.5f, 0f);
            segRect.offsetMax = new Vector2(-MalajongTheme.SegmentGap * 0.5f, 0f);

            Image segImg = segObj.GetComponent<Image>();
            segImg.color = unlit;
            segImg.raycastTarget = false;

            segments[i] = segImg;
        }

        TextMeshProUGUI valueText = CreateText(rowObj.transform, "Value",
            new Vector2(0.86f, 0f), new Vector2(1f, 1f), "1.0x", 24, TextAlignmentOptions.Right);
        valueText.color = MalajongTheme.Smoke;

        SuitAffinityMeter meter = rowObj.AddComponent<SuitAffinityMeter>();
        meter.ValueText = valueText;
        meter.Segments = segments;
        meter.LitColor = lit;
        meter.UnlitColor = unlit;
        meter.IdleTextColor = MalajongTheme.Smoke;

        Undo.RegisterCreatedObjectUndo(rowObj, "Create " + rowObj.name);
        return meter;
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

        GameObject faceObj = new GameObject("TileFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(BalatroCardVisual));
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
        // Attach Balatro Retro Tooltip popup
        CreateTooltipUI(faceObj.transform);

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

    private static void CreateTooltipUI(Transform parent)
    {
        GameObject tooltipObj = new GameObject("BalatroTooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(BalatroTileTooltip));
        tooltipObj.transform.SetParent(parent, false);

        RectTransform tRect = tooltipObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 0f);
        tRect.anchoredPosition = new Vector2(0, 8);
        tRect.sizeDelta = new Vector2(160, 72);

        // Frame (Dark Crisp Outer Border)
        GameObject frameObj = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(tooltipObj.transform, false);
        RectTransform fRect = frameObj.GetComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero;
        fRect.anchorMax = Vector2.one;
        fRect.offsetMin = Vector2.zero;
        fRect.offsetMax = Vector2.zero;
        frameObj.GetComponent<Image>().color = MalajongTheme.InkSoft;

        // Header Box (Pure Plain White Container)
        GameObject headerObj = new GameObject("HeaderBox", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(frameObj.transform, false);
        RectTransform hRect = headerObj.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.03f, 0.51f);
        hRect.anchorMax = new Vector2(0.97f, 0.95f);
        hRect.offsetMin = Vector2.zero;
        hRect.offsetMax = Vector2.zero;
        headerObj.GetComponent<Image>().color = Color.white;

        TextMeshProUGUI headerText = CreateText(headerObj.transform, "HeaderTitleText", Vector2.zero, Vector2.one,
            "<b>Rank</b> of <color=#D8402E>Suit</color>", 22, TextAlignmentOptions.Center);
        headerText.color = MalajongTheme.InkSoft;

        // Body Box (Pure Plain White Container)
        GameObject bodyObj = new GameObject("BodyBox", typeof(RectTransform), typeof(Image));
        bodyObj.transform.SetParent(frameObj.transform, false);
        RectTransform bRect = bodyObj.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.03f, 0.05f);
        bRect.anchorMax = new Vector2(0.97f, 0.49f);
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;
        bodyObj.GetComponent<Image>().color = Color.white;

        TextMeshProUGUI bodyText = CreateText(bodyObj.transform, "BodyScoreText", new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.90f),
            "<color=#6FB8EE><b>+5 Fu</b></color>", 24, TextAlignmentOptions.Center);

        TextMeshProUGUI editionText = CreateText(bodyObj.transform, "EditionText", new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.40f),
            "", 16, TextAlignmentOptions.Center);
        editionText.gameObject.SetActive(false);
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

        // Same construction as CreateSubPanel. The Button component stays on the outer object so
        // hit-testing is unchanged either way.
        Sprite btnFrameSprite = MalajongSkin.ButtonFrame;
        bool btnSkinned = btnFrameSprite != null;

        Image img = btnObj.GetComponent<Image>();
        img.color = btnSkinned ? color : MalajongTheme.GoldDark;

        int btnInset = btnSkinned ? 0 : MalajongTheme.Border;

        GameObject btnFillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        btnFillObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnFillRect = btnFillObj.GetComponent<RectTransform>();
        btnFillRect.anchorMin = Vector2.zero;
        btnFillRect.anchorMax = Vector2.one;
        btnFillRect.offsetMin = new Vector2(btnInset, btnInset);
        btnFillRect.offsetMax = new Vector2(-btnInset, -btnInset);

        Image btnFill = btnFillObj.GetComponent<Image>();
        btnFill.color = color;
        btnFill.raycastTarget = false;

        if (btnSkinned) CreateFrameOverlay(btnObj.transform, btnFrameSprite, MalajongTheme.Gold);

        Button btn = btnObj.GetComponent<Button>();
        // Tint the fill rather than the frame, so hover/press keeps the border art intact.
        btn.targetGraphic = btnFill;

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
