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

        // 2. Ensure Canvas exists with 1920x1080 ScaleMode
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

        // 3. Find or Create Main Panel with Dark Background
        Transform mainPanel = canvasObj.transform.Find("MainGameUI");
        if (mainPanel == null)
        {
            GameObject panelObj = new GameObject("MainGameUI", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = panelObj.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 1f); // Dark Slate Blue

            mainPanel = panelObj.transform;
            Undo.RegisterCreatedObjectUndo(panelObj, "Create MainGameUI");
        }

        // 4. Create / Find Status Text (Top Header)
        Transform statusExisting = mainPanel.Find("StatusText");
        GameObject statusObj = statusExisting != null ? statusExisting.gameObject : new GameObject("StatusText", typeof(RectTransform));
        statusObj.transform.SetParent(mainPanel, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.1f, 0.82f);
        statusRect.anchorMax = new Vector2(0.9f, 0.96f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        TextMeshProUGUI statusText = statusObj.GetComponent<TextMeshProUGUI>();
        if (statusText == null) statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 26;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.text = "Score: 0 / 150\nHands: 4 | Discards: 3\nState: Playing";
        statusText.color = Color.white;
        Undo.RegisterCreatedObjectUndo(statusObj, "Create StatusText");

        // 5. Create / Find Debug Hint Panel
        Transform debugPanel = mainPanel.Find("DebugHintText");
        TextMeshProUGUI debugHintText = null;
        if (debugPanel == null)
        {
            GameObject debugObj = new GameObject("DebugHintText", typeof(RectTransform));
            debugObj.transform.SetParent(mainPanel, false);
            RectTransform debugRect = debugObj.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(0.15f, 0.62f);
            debugRect.anchorMax = new Vector2(0.85f, 0.80f);
            debugRect.offsetMin = Vector2.zero;
            debugRect.offsetMax = Vector2.zero;

            debugHintText = debugObj.AddComponent<TextMeshProUGUI>();
            debugHintText.fontSize = 20;
            debugHintText.alignment = TextAlignmentOptions.Center;
            debugHintText.text = "<b><color=#2ECC71>DEBUG HINT:</color></b> Playable combos will appear here.";
            Undo.RegisterCreatedObjectUndo(debugObj, "Create DebugHintText");
        }
        else
        {
            debugHintText = debugPanel.GetComponent<TextMeshProUGUI>();
        }

        // 6. Create / Find Hand Container (Bottom Center Card Layout)
        Transform handContainer = mainPanel.Find("HandContainer");
        if (handContainer == null)
        {
            GameObject handObj = new GameObject("HandContainer", typeof(RectTransform));
            handObj.transform.SetParent(mainPanel, false);
            RectTransform handRect = handObj.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.02f, 0.25f);
            handRect.anchorMax = new Vector2(0.98f, 0.58f);
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = handObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            handContainer = handObj.transform;
            Undo.RegisterCreatedObjectUndo(handObj, "Create HandContainer");
        }

        // 7. Action Buttons Container
        Transform buttonContainer = mainPanel.Find("ButtonContainer");
        if (buttonContainer == null)
        {
            GameObject btnContainerObj = new GameObject("ButtonContainer", typeof(RectTransform));
            btnContainerObj.transform.SetParent(mainPanel, false);
            RectTransform btnContainerRect = btnContainerObj.GetComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.15f, 0.06f);
            btnContainerRect.anchorMax = new Vector2(0.85f, 0.18f);
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
        Button debugAutoButton = CreateOrFindButton(buttonContainer, "DebugAutoButton", "⚡ AUTO-SELECT COMBO", new Color(0.2f, 0.6f, 0.9f));

        // 8. Generate & Save TilePrefab
        GameObject tilePrefab = CreateOrUpdateTilePrefab();

        // 9. Find or Create GameManager & UIManager
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

        // Load TileData Assets
        List<TileData> tiles = new List<TileData>();
        string[] guids = AssetDatabase.FindAssets("t:TileData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileData tile = AssetDatabase.LoadAssetAtPath<TileData>(path);
            if (tile != null) tiles.Add(tile);
        }
        gameManager.AllTileTypes = tiles;

        // Wire up UIManager
        uiManager.gameManager = gameManager;
        uiManager.HandContainer = handContainer;
        uiManager.StatusText = statusText;
        uiManager.DebugHintText = debugHintText;
        uiManager.TilePrefab = tilePrefab;

        // Wire Button Click Events
        playButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(playButton.onClick, uiManager.PlaySelected);

        discardButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(discardButton.onClick, uiManager.DiscardSelected);

        debugAutoButton.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(debugAutoButton.onClick, uiManager.AutoSelectBestCombo);

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(uiManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("🎉 Playable Scene Placeholder updated! Clean hand card layout + balanced 150-point Round 1!");
    }

    private static GameObject CreateOrUpdateTilePrefab()
    {
        string prefabPath = "Assets/Script/UI/TilePrefab.prefab";

        // Parent root object (handles layout element spacing)
        GameObject rootObj = new GameObject("TilePrefab", typeof(RectTransform), typeof(LayoutElement), typeof(TileUI));
        
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(85, 125);

        LayoutElement layout = rootObj.GetComponent<LayoutElement>();
        layout.minWidth = 85;
        layout.preferredWidth = 85;
        layout.minHeight = 125;
        layout.preferredHeight = 125;

        // Child object: TileFace (handles visual card image, text, button, and vertical lift)
        GameObject faceObj = new GameObject("TileFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        faceObj.transform.SetParent(rootObj.transform, false);
        
        RectTransform faceRect = faceObj.GetComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = Vector2.zero;
        faceRect.offsetMax = Vector2.zero;

        Image img = faceObj.GetComponent<Image>();
        img.color = new Color(0.96f, 0.96f, 0.96f, 1f);

        // Add Text Child inside TileFace
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
        tileUI.TileText = text;

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
        Object.DestroyImmediate(rootObj);

        return prefabAsset;
    }

    private static Button CreateOrFindButton(Transform parent, string name, string labelText, Color color)
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
