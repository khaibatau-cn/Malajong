using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Repeatable regression checks and rendered previews for the playable scene.</summary>
[InitializeOnLoad]
public static class PolishValidation
{
    private static int stage;
    private static double nextStep;
    private const string Output = "Build/PolishReview";

    static PolishValidation()
    {
        EditorApplication.delayCall += () =>
        {
            // An explicit local request lets an already-open editor run this same validation.
            const string request = "Build/polish-run.request";
            if (!Application.isPlaying && File.Exists(request))
            {
                File.Delete(request);
                Run();
            }
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool("MalajongPolishReview", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                stage = 0;
                nextStep = EditorApplication.timeSinceStartup + 3;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("MalajongPolishReview", false);
                int exitCode = SessionState.GetInt("MalajongPolishExit", 0);
                if (exitCode == 0) BuildScript.BuildWindowsTo("Build/PolishedWindows");
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        };
    }

    [MenuItem("Malajong/Validate Polish and Build")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/ILoveCoCa.unity");
        SceneSetupTool.SetupPlayableScene();
        EditorSceneManager.SaveOpenScenes();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/ILoveCoCa.unity", true) };
        AssetDatabase.SaveAssets();
        CheckRules();
        SessionState.SetInt("MalajongPolishExit", 0);
        SessionState.SetBool("MalajongPolishReview", true);
        EditorApplication.EnterPlaymode();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("POLISH CHECK FAILED: " + message);
        Debug.Log("POLISH PASS: " + message);
    }

    private static Tile T(TileSuit suit, int rank)
    {
        var data = AssetDatabase.LoadAssetAtPath<TileData>($"Assets/ScriptableObjects/Tiles/{suit}_{rank}.asset");
        return new Tile(data);
    }

    private static void CheckRules()
    {
        var host = new GameObject("PolishRuleChecks");
        var gm = host.AddComponent<GameManager>();
        gm.AllTileTypes = AssetDatabase.FindAssets("t:TileData").Select(g =>
            AssetDatabase.LoadAssetAtPath<TileData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
        gm.InitializeRun();
        var winning = new List<Tile>();
        foreach (int r in new[] { 3, 1, 2 }) winning.Add(T(TileSuit.Bamboo, r));
        foreach (int r in new[] { 6, 4, 5 }) winning.Add(T(TileSuit.Characters, r));
        foreach (int r in new[] { 9, 7, 8 }) winning.Add(T(TileSuit.Dots, r));
        for (int i = 0; i < 3; i++) winning.Add(T(TileSuit.Honor, 1));
        for (int i = 0; i < 2; i++) winning.Add(T(TileSuit.Honor, 5));
        Require(ScoreEngine.EvaluateFullHand(winning).bonusFu == 100, "Unsorted winning hand is recognized");
        gm.Hand.Tiles.Clear(); gm.Hand.AddTiles(winning);
        gm.CurrentTargetScore = 4000;
        gm.PlaySelectedTiles(winning);
        Require(gm.CurrentScore == 800 && gm.HandsRemaining == 3 && !winning.Any(gm.Hand.Tiles.Contains), "Full hand consumes one play and every tile");
        gm.PlaySelectedTiles(winning);
        Require(gm.CurrentScore == 800, "Old tile references cannot score again");
        int discards = gm.DiscardsRemaining;
        gm.DiscardSelectedTiles(new List<Tile>());
        Require(gm.DiscardsRemaining == discards, "Empty discard rejected");

        var honorHand = Enumerable.Range(0, 14).Select(i => T(TileSuit.Honor, 1 + i % 7)).ToList();
        var scholar = ScriptableObject.CreateInstance<ImperialScholar>();
        var honorPair = new List<Tile> { honorHand[0], honorHand[7] };
        var bonus = ScoreEngine.PreviewScore(honorPair, honorHand, null, new[] { scholar }, gm);
        Require(Mathf.Approximately(bonus.TotalFan, 30f), "All Honors reaches Imperial Scholar at 14 tiles");
        Object.DestroyImmediate(scholar);
        var flute = ScriptableObject.CreateInstance<PuristsFlute>();
        var pure = Enumerable.Range(0, 14).Select(i => T(TileSuit.Bamboo, 1 + i % 7)).ToList();
        var pureScore = ScoreEngine.PreviewScore(new List<Tile>{pure[0],pure[7]}, pure, null, new[]{flute}, gm);
        Require(pureScore.TotalFu == 262 && Mathf.Approximately(pureScore.TotalFan, 15f), "Pure Hand and Flute work on 14 tiles");
        Object.DestroyImmediate(flute);

        var wind = ScriptableObject.CreateInstance<RestlessWind>();
        var pong = new Pong(new List<Tile>{T(TileSuit.Honor,1),T(TileSuit.Honor,1),T(TileSuit.Honor,1)});
        for (int i = 0; i < 10; i++) ScoreEngine.PreviewScore(pong.Tiles, null, gm.Affinity, new[]{wind}, gm);
        Require(gm.DiscardsRemaining == discards, "Repeated previews grant no discards");
        ScoreEngine.Calculate(pong, null, gm.Affinity, new[]{wind}, gm);
        Require(gm.DiscardsRemaining == discards + 1, "Committed Wind Pong grants exactly one discard");
        Object.DestroyImmediate(wind);
        var compass = ScriptableObject.CreateInstance<BrokenCompass>();
        var bamboo = new Pair(new List<Tile>{T(TileSuit.Bamboo,1),T(TileSuit.Bamboo,1)});
        var dots = new Pair(new List<Tile>{T(TileSuit.Dots,1),T(TileSuit.Dots,1)});
        compass.OnRoundStart(gm);
        ScoreEngine.Calculate(bamboo, null, null, new[]{compass}, gm);
        var preview = ScoreEngine.PreviewScore(dots.Tiles, null, gm.Affinity, new[]{compass}, gm);
        var actual = ScoreEngine.Calculate(dots, null, gm.Affinity, new[]{compass}, gm);
        Require(preview.TotalFu == 32 && preview.TotalFu == actual.fu && Mathf.Approximately(preview.TotalFan,actual.fan), "Suit switch bonus and preview agree");
        Object.DestroyImmediate(compass);
        Object.DestroyImmediate(host);
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextStep) return;
        nextStep = EditorApplication.timeSinceStartup + 3;
        try
        {
            var ui = Object.FindAnyObjectByType<UIManager>();
            var gm = ui.gameManager;
            switch (stage++)
            {
                case 0: Capture(ui, "menu"); ui.StartRun(); break;
                case 1:
                    Capture(ui, "gameplay");
                    Require(ui.ScoreProgressBar.rectTransform.anchorMax.x == 0, "Zero score shows an empty progress bar");
                    ui.AutoSelectBestCombo(); break;
                case 2: Capture(ui, "selection"); ui.PlaySelected(); nextStep += 3; break;
                case 3:
                    Require(gm.CurrentScore > 0 && gm.HandsRemaining == 3, "UI selection and animated scoring commit a play");
                    if (gm.State == GameManager.GameState.Playing)
                    {
                        gm.CurrentTargetScore = 1;
                        gm.PlaySelectedTiles(ScoreEngine.FindPlayableCombos(gm.Hand.Tiles).First().tiles);
                    }
                    break;
                case 4:
                    Require(gm.State == GameManager.GameState.Shop, "Clearing a round opens the shop");
                    Capture(ui, "shop");
                    int money = gm.Yuan;
                    ui.BuyShopItem(0);
                    Require(gm.EquippedSpirits.Count == 1 && gm.Yuan == money - 5, "Shop purchase deducts its price");
                    ui.BuyShopItem(0);
                    Require(gm.EquippedSpirits.Count == 1, "Duplicate spirit purchase rejected");
                    ui.NextRound(); break;
                case 5:
                    Require(gm.CurrentRound == 2 && gm.HandsRemaining == 4 && gm.DiscardsRemaining == 3, "Next round resets plays and discards");
                    gm.CurrentTargetScore = int.MaxValue;
                    gm.HandsRemaining = 1;
                    gm.PlaySelectedTiles(ScoreEngine.FindPlayableCombos(gm.Hand.Tiles).First().tiles);
                    Require(gm.State == GameManager.GameState.GameOver, "Last play below quota ends the run");
                    ui.RestartRun();
                    Require(gm.CurrentRound == 1 && gm.EquippedSpirits.Count == 0, "Restart resets the run");
                    File.WriteAllText(Output + "/result.txt", "PASS: scoring, previews, full hands, purchases, round transitions, loss and restart.\n");
                    Finish(0); break;
            }
        }
        catch (Exception ex) { Debug.LogException(ex); Finish(1); }
    }

    private static void Capture(UIManager ui, string name)
    {
        var canvas = ui.PlayingPanel.GetComponentInParent<Canvas>();
        var cameraHost = new GameObject("ReviewCamera");
        var camera = cameraHost.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = MalajongTheme.Ink;
        camera.orthographic = true;
        var target = new RenderTexture(1920,1080,24);
        camera.targetTexture = target;
        var oldMode = canvas.renderMode;
        var oldCamera = canvas.worldCamera;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 10;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(1920,1080,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1920,1080),0,0);
        texture.Apply();
        File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous;
        canvas.renderMode = oldMode;
        canvas.worldCamera = oldCamera;
        camera.targetTexture = null;
        Object.Destroy(texture); Object.Destroy(target); Object.Destroy(cameraHost);
    }

    private static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetInt("MalajongPolishExit",code);
        EditorApplication.ExitPlaymode();
    }
}
