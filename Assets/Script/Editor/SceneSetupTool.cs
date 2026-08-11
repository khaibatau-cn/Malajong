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
        // Modals belong here too. They were missing, so every run of this tool left the previous
        // pair behind and stacked a new one on top — a scene set up a dozen times carried a dozen
        // dead copies of each, all still catching raycasts.
        string[] panelsToClear =
        {
            "StartMenuUI", "MainGameUI", "ShopUI", "GameOverUI", "VictoryUI",
            "RunInfoModal", "OptionsModal", "AbandonConfirmModal", "RedrawModal"
        };
        // Walks backwards over every child rather than Find()ing one by name, so a scene that
        // already accumulated duplicates is cleaned out in a single pass instead of shedding one
        // copy per run.
        var clearSet = new HashSet<string>(panelsToClear);
        for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasObj.transform.GetChild(i);
            if (clearSet.Contains(child.name))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // ==========================================
        // 4. Panel 1: StartMenuUI
        // ==========================================
        Transform startPanel = CreatePanel(canvasObj.transform, "StartMenuUI", MalajongTheme.Ink);
        Button startRunBtn = BuildStartMenu(startPanel);

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
        var (shopStatusText, catObj, spiritCatalog, nextRoundBtn) = BuildShop(shopPanel);

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

        // ==========================================
        // 10b. Panel 8: AbandonConfirmModal (Overlay)
        // ==========================================
        // Created after OptionsModal so it draws on top of it — abandoning is reached *from* the
        // options screen, which stays open behind the confirmation.
        Transform abandonModal = CreatePanel(canvasObj.transform, "AbandonConfirmModal", MalajongTheme.Scrim);
        Transform abandonCard = CreateSubPanel(abandonModal, "AbandonCard", new Vector2(0.31f, 0.34f), new Vector2(0.69f, 0.66f), MalajongTheme.VermilionDeep, cabinet: true);

        TextMeshProUGUI abandonBody = CreateText(abandonCard, "AbandonConfirmText", new Vector2(0.07f, 0.30f), new Vector2(0.93f, 0.92f),
            "<b>ABANDON THIS RUN?</b>\n\nAll run progress will be lost.", 26, TextAlignmentOptions.Center);

        // Cancel takes the accent and sits on the right, where the eye lands last. Destructive
        // choices should never be the easy default.
        Button confirmAbandonBtn = CreateButton(abandonCard, "ConfirmAbandonButton", "YES, ABANDON", MalajongTheme.Vermilion, 24,
            new Vector2(0.08f, 0.10f), new Vector2(0.48f, 0.26f));
        Button cancelAbandonBtn = CreateButton(abandonCard, "CancelAbandonButton", "KEEP PLAYING", MalajongTheme.MalachiteRaised, 24,
            new Vector2(0.52f, 0.10f), new Vector2(0.92f, 0.26f));

        abandonModal.gameObject.SetActive(false);

        // ==========================================
        // 10c. Panel 9: RedrawModal (Dead Hand Reprieve)
        // ==========================================
        Transform redrawModal = CreatePanel(canvasObj.transform, "RedrawModal", MalajongTheme.Scrim);
        Transform redrawCard = CreateSubPanel(redrawModal, "RedrawCard", new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f), MalajongTheme.MalachiteDeep, cabinet: true);

        // The maneki-neko from the title art, if it has been baked out — the prompt is written in
        // the cat's voice, so showing the cat is worth more than another block of text.
        Sprite catSprite = LoadTitleSprite();
        if (catSprite != null)
        {
            GameObject luckyCatObj = new GameObject("LuckyCat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            luckyCatObj.transform.SetParent(redrawCard, false);

            RectTransform luckyCatRect = luckyCatObj.GetComponent<RectTransform>();
            luckyCatRect.anchorMin = new Vector2(0.20f, 0.74f);
            luckyCatRect.anchorMax = new Vector2(0.80f, 0.95f);
            luckyCatRect.offsetMin = Vector2.zero;
            luckyCatRect.offsetMax = Vector2.zero;

            Image luckyCatImg = luckyCatObj.GetComponent<Image>();
            luckyCatImg.sprite = catSprite;
            luckyCatImg.preserveAspect = true;
            luckyCatImg.raycastTarget = false;
        }

        TextMeshProUGUI redrawBody = CreateText(redrawCard, "RedrawText", new Vector2(0.07f, 0.28f), new Vector2(0.93f, 0.72f),
            "<b>DEAD HAND</b>\n\nNo combo to play. No discards left.", 26, TextAlignmentOptions.Center);

        // One button, no dismiss: a redraw is the only move left on the board.
        Button redrawBtn = CreateButton(redrawCard, "RedrawButton", "ASK THE CAT", MalajongTheme.VermilionBright, 28,
            new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.24f));

        redrawModal.gameObject.SetActive(false);

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
        uiManager.OptionsTitleText = optionsTitle;
        uiManager.ToggleAudioButton = toggleAudioBtn;
        uiManager.ToggleAudioText = toggleAudioText;
        uiManager.ForfeitRunButton = forfeitBtn;
        uiManager.CloseOptionsButton = closeOptionsBtn;

        uiManager.RedrawModal = redrawModal.gameObject;
        uiManager.RedrawText = redrawBody;
        uiManager.RedrawButton = redrawBtn;

        uiManager.AbandonConfirmModal = abandonModal.gameObject;
        uiManager.AbandonConfirmText = abandonBody;
        uiManager.ConfirmAbandonButton = confirmAbandonBtn;
        uiManager.CancelAbandonButton = cancelAbandonBtn;

        // Wire Button Listeners
        startRunBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startRunBtn.onClick, uiManager.StartRun);

        // The options modal already holds the rules box, so the menu reuses it rather than
        // maintaining a second copy of the same text.
        Button howToPlayBtn = startPanel.Find("HowToPlayButton").GetComponent<Button>();
        howToPlayBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(howToPlayBtn.onClick, uiManager.OpenOptionsModal);

        Button quitBtn = startPanel.Find("QuitButton").GetComponent<Button>();
        quitBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, uiManager.QuitGame);

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
        UnityEditor.Events.UnityEventTools.AddPersistentListener(forfeitBtn.onClick, uiManager.OpenAbandonConfirm);

        redrawBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(redrawBtn.onClick, uiManager.TakeRedraw);

        confirmAbandonBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmAbandonBtn.onClick, uiManager.ConfirmAbandonRun);

        cancelAbandonBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(cancelAbandonBtn.onClick, uiManager.CancelAbandon);

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

        // Labelled MAIN MENU, so it goes to the menu — it restarted the run outright back when the
        // menu was unreachable.
        victoryRestartBtn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(victoryRestartBtn.onClick, uiManager.ReturnToMainMenu);

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

    // ==========================================
    // Start Menu
    // ==========================================

    /// <summary>
    /// Builds the title screen: framed logo card, a swaying fan of real tiles resting on a
    /// malachite mat, and the three entry buttons.
    ///
    /// The logo is the imported Aseprite art when it is present and falls back to type-set text
    /// when it is not, so the menu is never a blank rectangle on a fresh clone.
    /// </summary>
    /// <returns>The START RUN button, which the caller wires to UIManager.StartRun.</returns>
    private static Button BuildStartMenu(Transform startPanel)
    {
        List<TileData> tiles = LoadAllTileAssets();

        // --- Drifting tile field. First child, so everything else draws over it ---
        CreateFloatingTileField(startPanel, tiles, count: 18);

        // --- Mat: the tiles need something to rest on, or the fan reads as floating text ---
        CreateRect(startPanel, "FloorBand", new Vector2(0f, 0f), new Vector2(1f, 0.19f), MalajongTheme.MalachiteDeep);
        CreateRect(startPanel, "FloorEdge", new Vector2(0f, 0.185f), new Vector2(1f, 0.19f), MalajongTheme.Gold);

        // Corner seal tiles used to sit here as wallpaper. The drifting field does that job now, and
        // two static giants alongside it just read as clutter.

        // --- Title card ---
        Transform titleCard = CreateSubPanel(startPanel, "TitleCard", new Vector2(0.17f, 0.51f), new Vector2(0.83f, 0.90f), MalajongTheme.VermilionDeep, cabinet: true);

        Sprite logoSprite = LoadTitleSprite();
        RectTransform logoRect = null;

        if (logoSprite != null)
        {
            GameObject logoObj = new GameObject("TitleLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObj.transform.SetParent(titleCard, false);

            // Near-flush with the card. The baked sprite is trimmed to its own artwork, so this is
            // the logo's real size rather than a canvas full of margin.
            logoRect = logoObj.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.035f, 0.20f);
            logoRect.anchorMax = new Vector2(0.965f, 0.95f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;

            Image logoImg = logoObj.GetComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning("[SceneSetupTool] No title art found under Assets/Sprites/UI. Falling back to a text logo.");
            CreateText(titleCard, "TitleText", new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.92f),
                $"<b><size=200%><color={MalajongTheme.HexGold}>MALAJONG</color></size></b>", 48, TextAlignmentOptions.Center);
        }

        TextMeshProUGUI tagline = CreateText(titleCard, "Tagline", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.21f),
            $"<color={MalajongTheme.HexBoneDim}>A MAHJONG ROGUELIKE DECKBUILDER</color>", 26, TextAlignmentOptions.Center);
        tagline.characterSpacing = 8f;

        // --- Tile fan on the mat ---
        GameObject fanObj = new GameObject("TileFan", typeof(RectTransform));
        fanObj.transform.SetParent(startPanel, false);

        // Sits high enough in the mat that the arc's lowest tiles still clear the footer line, and
        // the whole fan stays inside the band instead of bleeding off the bottom of the screen.
        RectTransform fanRect = fanObj.GetComponent<RectTransform>();
        fanRect.anchorMin = new Vector2(0.5f, 0.105f);
        fanRect.anchorMax = new Vector2(0.5f, 0.105f);
        fanRect.sizeDelta = Vector2.zero;
        fanRect.anchoredPosition = Vector2.zero;

        // A hand you could almost read: two suits and a wind, so the fan advertises what the game
        // is made of rather than being nine copies of the same tile.
        (TileSuit suit, int rank)[] fanHand =
        {
            (TileSuit.Bamboo, 1), (TileSuit.Bamboo, 2), (TileSuit.Bamboo, 3),
            (TileSuit.Dots, 5), (TileSuit.Dots, 5), (TileSuit.Dots, 5),
            (TileSuit.Characters, 7), (TileSuit.Characters, 8), (TileSuit.Honor, 1)
        };

        // Sized against the mat rather than the hand: at 104px tall the fan reads as decoration at
        // the bottom of the frame, where the old 141px version dominated the lower third.
        const float TileHeight = 104f;
        const float TileSpacing = 92f;
        // Total arc depth is ArcDrop x offset², so the outermost tile of nine falls 16 x this.
        const float ArcDrop = 2.2f;
        const float TiltStep = 3.2f;

        RectTransform[] fanTiles = new RectTransform[fanHand.Length];
        float centre = (fanHand.Length - 1) * 0.5f;

        for (int i = 0; i < fanHand.Length; i++)
        {
            float offset = i - centre;
            var (suit, rank) = fanHand[i];

            GameObject tileObj = new GameObject($"FanTile_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tileObj.transform.SetParent(fanObj.transform, false);

            RectTransform tileRect = tileObj.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.sizeDelta = new Vector2(TileHeight * 0.74f, TileHeight);
            // Squared falloff, so the fan curves away from the centre instead of stepping down.
            tileRect.anchoredPosition = new Vector2(offset * TileSpacing, -offset * offset * ArcDrop);
            tileRect.localRotation = Quaternion.Euler(0f, 0f, -offset * TiltStep);

            Image tileImg = tileObj.GetComponent<Image>();
            tileImg.preserveAspect = true;
            tileImg.raycastTarget = false;

            TileData data = FindTile(tiles, suit, rank);
            if (data != null && data.TileSprite != null)
            {
                tileImg.sprite = data.TileSprite;
                // Slightly knocked back so the fan stays scenery and START RUN keeps the eye.
                tileImg.color = new Color(1f, 1f, 1f, 0.88f);
            }
            else
            {
                tileImg.color = MalajongTheme.Hex("EDE6D6", 0.35f);
            }

            fanTiles[i] = tileRect;
        }

        // --- Buttons ---
        Button startRunBtn = CreateButton(startPanel, "StartRunButton", "START RUN", MalajongTheme.VermilionBright, 38,
            new Vector2(0.385f, 0.375f), new Vector2(0.615f, 0.475f));
        CreateButton(startPanel, "HowToPlayButton", "HOW TO PLAY", MalajongTheme.MalachiteRaised, 26,
            new Vector2(0.395f, 0.295f), new Vector2(0.605f, 0.365f));
        CreateButton(startPanel, "QuitButton", "QUIT", MalajongTheme.Vermilion, 26,
            new Vector2(0.395f, 0.215f), new Vector2(0.605f, 0.285f));

        // Kept clear of the fan's lowest point, sway included — the credit line is the one piece of
        // text on this screen that must never be half-covered.
        CreateText(startPanel, "FooterText", new Vector2(0.02f, 0.004f), new Vector2(0.98f, 0.040f),
            $"<color={MalajongTheme.HexSmoke}>TEAM SANROKUNANA  ·  CODECATALYST  ·  SWINBURNE</color>", 20, TextAlignmentOptions.Center);

        TitleMenuDecor decor = startPanel.gameObject.AddComponent<TitleMenuDecor>();
        decor.Logo = logoRect;
        decor.Tiles = fanTiles;

        return startRunBtn;
    }

    // ==========================================
    // Shop
    // ==========================================

    /// <summary>
    /// Builds the shop as a market stall rather than a settings screen: a hanging wooden sign,
    /// paper lanterns, a drifting tile field behind it and the spirits laid out as framed cards on
    /// a malachite counter.
    ///
    /// The catalog is a grid rather than one long row — eight spirits sharing a single row left
    /// each card too narrow to read its own description.
    /// </summary>
    private static (TextMeshProUGUI status, GameObject catalog, List<SpiritData> spirits, Button nextRound) BuildShop(Transform shopPanel)
    {
        // --- Same drifting field as the title screen, so the two read as one place ---
        CreateFloatingTileField(shopPanel, LoadAllTileAssets(), count: 12);

        // --- Counter the stall sits behind ---
        CreateRect(shopPanel, "CounterBand", new Vector2(0f, 0f), new Vector2(1f, 0.17f), MalajongTheme.MalachiteDeep);
        CreateRect(shopPanel, "CounterEdge", new Vector2(0f, 0.165f), new Vector2(1f, 0.17f), MalajongTheme.Gold);

        // --- Hanging sign. Native 48x41 art, point-filtered and scaled up ---
        Sprite signSprite = TitleSpriteBaker.FirstSpriteAt(ShopSignPath);
        if (signSprite != null)
        {
            GameObject signObj = new GameObject("ShopSign", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            signObj.transform.SetParent(shopPanel, false);

            RectTransform signRect = signObj.GetComponent<RectTransform>();
            signRect.anchorMin = new Vector2(0.40f, 0.80f);
            signRect.anchorMax = new Vector2(0.60f, 0.99f);
            signRect.offsetMin = Vector2.zero;
            signRect.offsetMax = Vector2.zero;

            Image signImg = signObj.GetComponent<Image>();
            signImg.sprite = signSprite;
            signImg.preserveAspect = true;
            signImg.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning($"[SceneSetupTool] Shop sign missing at '{ShopSignPath}'. The shop header falls back to text.");
            CreateText(shopPanel, "ShopSignFallback", new Vector2(0.3f, 0.86f), new Vector2(0.7f, 0.97f),
                $"<b><color={MalajongTheme.HexGold}>SPIRIT SHOP</color></b>", 44, TextAlignmentOptions.Center);
        }

        // --- Lanterns flanking the sign, hung from the top edge ---
        CreateLantern(shopPanel, "LanternLeft", new Vector2(0.16f, 0.66f), new Vector2(0.24f, 0.99f));
        CreateLantern(shopPanel, "LanternRight", new Vector2(0.76f, 0.66f), new Vector2(0.84f, 0.99f));

        TextMeshProUGUI shopStatusText = CreateText(shopPanel, "ShopStatusText", new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.80f),
            "<b>SPIRIT SHOP</b>   |   Yuan: ¥5", 30, TextAlignmentOptions.Center);

        // --- Catalog grid ---
        GameObject catObj = new GameObject("ShopCatalogContainer", typeof(RectTransform));
        catObj.transform.SetParent(shopPanel, false);

        RectTransform catRect = catObj.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0.06f, 0.20f);
        catRect.anchorMax = new Vector2(0.94f, 0.71f);
        catRect.offsetMin = Vector2.zero;
        catRect.offsetMax = Vector2.zero;

        GridLayoutGroup catLayout = catObj.AddComponent<GridLayoutGroup>();
        catLayout.cellSize = new Vector2(360f, 240f);
        catLayout.spacing = new Vector2(24f, 20f);
        catLayout.childAlignment = TextAnchor.UpperCenter;
        catLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        catLayout.constraintCount = 4;

        List<SpiritData> spiritCatalog = LoadAllSpiritAssets();
        for (int i = 0; i < spiritCatalog.Count; i++)
        {
            CreateShopCard(catObj.transform, spiritCatalog[i], i);
        }

        Button nextRoundBtn = CreateButton(shopPanel, "NextRoundButton", "NEXT ROUND >>", MalajongTheme.VermilionBright, 30,
            new Vector2(0.38f, 0.035f), new Vector2(0.62f, 0.135f));

        return (shopStatusText, catObj, spiritCatalog, nextRoundBtn);
    }

    private const string ShopSignPath = "Assets/Sprites/UI/Shop/ShopSign.png";
    private const string LanternPath = "Assets/Sprites/UI/Shop/Lantern.aseprite";

    /// <summary>
    /// One spirit card: framed box, icon, name and description, price tag, buy button.
    ///
    /// The child names <c>Icon</c>, <c>CardText</c> and <c>BuyButton</c> are load-bearing —
    /// <c>UIManager.RefreshShopCards</c> looks them up by name every time the shop opens.
    /// </summary>
    private static void CreateShopCard(Transform parent, SpiritData spirit, int index)
    {
        // Anchors are overwritten by the GridLayoutGroup; the frame art is what this buys us.
        Transform card = CreateSubPanel(parent, $"ShopItem_{index}", Vector2.zero, Vector2.one, MalajongTheme.Vermilion);

        if (spirit.Icon != null)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(card, false);

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.80f);
            iconRect.anchorMax = new Vector2(0.5f, 0.80f);
            iconRect.sizeDelta = new Vector2(64f, 64f);
            iconRect.anchoredPosition = Vector2.zero;

            Image icon = iconObj.GetComponent<Image>();
            icon.sprite = spirit.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        CreateText(card, "CardText", new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.68f),
            $"<b><color={MalajongTheme.HexGold}>{spirit.SpiritName}</color></b>\n\n<size=80%>{spirit.Description}</size>",
            22, TextAlignmentOptions.Center);

        CreateButton(card, "BuyButton", "BUY (¥5)", MalajongTheme.MalachiteRaised, 22,
            new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.26f));
    }

    /// <summary>A paper lantern on a cord, hung from the top of its anchor box.</summary>
    private static void CreateLantern(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        Sprite lantern = TitleSpriteBaker.FirstSpriteAt(LanternPath);
        if (lantern == null)
        {
            Debug.LogWarning($"[SceneSetupTool] Lantern art missing at '{LanternPath}'. Skipping '{name}'.");
            return;
        }

        GameObject lanternObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lanternObj.transform.SetParent(parent, false);

        RectTransform rect = lanternObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = lanternObj.GetComponent<Image>();
        image.sprite = lantern;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    /// <summary>
    /// Spawns the drifting tile wallpaper as the first child of a panel, so everything built
    /// afterwards draws over it.
    /// </summary>
    private static void CreateFloatingTileField(Transform parent, List<TileData> tiles, int count)
    {
        GameObject fieldObj = new GameObject("FloatingTileField", typeof(RectTransform));
        fieldObj.transform.SetParent(parent, false);

        RectTransform fieldRect = fieldObj.GetComponent<RectTransform>();
        fieldRect.anchorMin = Vector2.zero;
        fieldRect.anchorMax = Vector2.one;
        fieldRect.offsetMin = Vector2.zero;
        fieldRect.offsetMax = Vector2.zero;

        List<Sprite> tilePool = new List<Sprite>();
        foreach (TileData data in tiles)
        {
            if (data != null && data.TileSprite != null) tilePool.Add(data.TileSprite);
        }

        if (tilePool.Count == 0)
        {
            Debug.LogWarning("[SceneSetupTool] No tile sprites available — the floating tile field will be empty.");
            return;
        }

        FloatingTileField field = fieldObj.AddComponent<FloatingTileField>();
        field.TilePool = tilePool.ToArray();
        field.Count = count;
    }

    /// <summary>
    /// Prefers the baked sprite — background knocked out and canvas trimmed — and falls back to the
    /// raw art if baking failed, which shows the logo letterboxed rather than not at all.
    /// </summary>
    private static Sprite LoadTitleSprite()
    {
        Sprite baked = TitleSpriteBaker.BakeIfStale();
        if (baked != null) return baked;

        return TitleSpriteBaker.FirstSpriteAt("Assets/Sprites/UI/Title.aseprite")
            ?? TitleSpriteBaker.FirstSpriteAt("Assets/Sprites/UI/Title.png");
    }

    private static TileData FindTile(List<TileData> tiles, TileSuit suit, int rank)
    {
        return tiles.Find(t => t != null && t.Suit == suit && t.Rank == rank);
    }

    /// <summary>Flat colour block. Unlike CreateSubPanel this carries no frame art — it is background, not furniture.</summary>
    private static Image CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject rectObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rectObj.transform.SetParent(parent, false);

        RectTransform rect = rectObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rectObj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
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
        if (skinned)
        {
            float ppu = cabinet ? MalajongSkin.PanelPixelsPerUnitMultiplier : MalajongSkin.BoxPixelsPerUnitMultiplier;
            CreateFrameOverlay(panelObj.transform, frameSprite, MalajongTheme.Gold, ppu);
        }

        Undo.RegisterCreatedObjectUndo(panelObj, "Create " + name);
        return panelObj.transform;
    }

    /// <summary>
    /// Stretches a 9-sliced border sprite over its parent as a non-interactive overlay.
    ///
    /// The art is white line work, so the tint here is what gives it a colour — which keeps
    /// MalajongTheme the single source of truth even though the shape now comes from a sprite.
    /// </summary>
    private static Image CreateFrameOverlay(Transform parent, Sprite sprite, Color tint, float pixelsPerUnitMultiplier)
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
        frame.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
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

        if (btnSkinned) CreateFrameOverlay(btnObj.transform, btnFrameSprite, MalajongTheme.Gold, MalajongSkin.ButtonPixelsPerUnitMultiplier);

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
