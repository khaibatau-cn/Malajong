// Standalone regression harness. Compiles the production rules with minimal Unity substitutes.
// It checks logic only; native Unity lifecycle, graphics and input require PolishValidation.Run.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public class ScriptableObject { }
    public class Sprite { }
    public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TextAreaAttribute : Attribute { }
    public static class Debug { public static void Log(object value) { } public static void LogWarning(object value) { } }
    public static class Mathf
    {
        public static int RoundToInt(float x) => (int)Math.Round(x);
        public static float Clamp(float x, float low, float high) => Math.Max(low, Math.Min(high, x));
        public static int Min(int a, int b) => Math.Min(a,b);
    }
    public static class Random
    {
        private static readonly System.Random rng = new System.Random(42);
        public static int Range(int min, int max) => rng.Next(min,max);
    }
}

public static class CoreRegression
{
    private static int count;
    private static Tile Tile(TileSuit suit, int rank) => new Tile(new TileData { Suit = suit, Rank = rank });
    private static List<Tile> Tiles(TileSuit suit, params int[] ranks) => ranks.Select(r => Tile(suit,r)).ToList();
    private static void Check(bool ok, string description)
    {
        if (!ok) throw new Exception(description);
        Console.WriteLine("PASS " + description); count++;
    }
    private static GameManager NewRun()
    {
        var gm = new GameManager { AllTileTypes = new List<TileData>() };
        foreach (TileSuit suit in Enum.GetValues(typeof(TileSuit)))
            for (int r = 1; r <= (suit == TileSuit.Honor ? 7 : 9); r++)
                gm.AllTileTypes.Add(new TileData { Suit = suit, Rank = r });
        gm.InitializeRun();
        return gm;
    }
    public static int Main()
    {
        try
        {
            var gm = NewRun();
            Check(gm.Hand.Tiles.Count == 14 && gm.Deck.Remaining == 122, "Opening hand and wall sizes");
            var winning = Tiles(TileSuit.Bamboo,3,1,2);
            winning.AddRange(Tiles(TileSuit.Characters,6,4,5));
            winning.AddRange(Tiles(TileSuit.Dots,9,7,8));
            winning.AddRange(Tiles(TileSuit.Honor,1,1,1,5,5));
            Check(ScoreEngine.EvaluateFullHand(winning).bonusFu == 100, "Unsorted winning Mahjong hand");
            var invalid = Tiles(TileSuit.Bamboo,1,1,1,1);
            invalid.AddRange(Tiles(TileSuit.Characters,2,2,2,2));
            invalid.AddRange(Tiles(TileSuit.Dots,3,3,3,3));
            invalid.AddRange(Tiles(TileSuit.Honor,1,1));
            Check(ScoreEngine.EvaluateFullHand(invalid).bonusFu == 0, "Three kongs plus pair is not four triples plus pair");
            Check(ScoreEngine.EvaluateFullHand(null).bonusFu == 0, "Null full-hand input is safe");
            gm.Hand.Tiles.Clear(); gm.Hand.AddTiles(winning); gm.CurrentTargetScore = 4000;
            gm.PlaySelectedTiles(winning);
            Check(gm.CurrentScore == 800 && gm.HandsRemaining == 3 && !winning.Any(gm.Hand.Tiles.Contains), "Full-hand play consumes tiles and a hand");
            gm.PlaySelectedTiles(winning);
            Check(gm.CurrentScore == 800, "Consumed tiles cannot be replayed");
            int discards = gm.DiscardsRemaining;
            gm.DiscardSelectedTiles(new List<Tile>());
            gm.DiscardSelectedTiles(new List<Tile>{gm.Hand.Tiles[0],gm.Hand.Tiles[0]});
            Check(gm.DiscardsRemaining == discards, "Empty and duplicate discards rejected");
            gm.DiscardSelectedTiles(gm.Hand.Tiles.Take(5).ToList());
            Check(gm.DiscardsRemaining == discards-1 && gm.Hand.Tiles.Count == 14, "Discard draws replacements");

            var honors = Tiles(TileSuit.Honor,1,1,2,2,3,3,4,4,5,5,6,6,7,7);
            var scholar = new ImperialScholar();
            var hs = ScoreEngine.PreviewScore(honors.Take(2).ToList(),honors,null,new[]{scholar},gm);
            Check(hs.TotalFu == 192 && Math.Abs(hs.TotalFan-30f)<0.001, "14-tile All Honors and Imperial Scholar");
            var pure = Tiles(TileSuit.Bamboo,1,1,2,2,3,3,4,4,5,5,6,6,7,7);
            var ps = ScoreEngine.PreviewScore(pure.Take(2).ToList(),pure,null,new[]{new PuristsFlute()},gm);
            Check(ps.TotalFu == 262 && Math.Abs(ps.TotalFan-15f)<0.001, "14-tile Pure Hand and Purist's Flute");
            var winds = Tiles(TileSuit.Honor,1,1,1);
            var spirits = new SpiritData[]{ new RestlessWind() };
            discards = gm.DiscardsRemaining;
            for(int i=0;i<25;i++) ScoreEngine.PreviewScore(winds,null,gm.Affinity,spirits,gm);
            Check(gm.DiscardsRemaining == discards, "Repeated previews do not grant discards");
            ScoreEngine.Calculate(new Pong(winds),null,gm.Affinity,spirits,gm);
            Check(gm.DiscardsRemaining == discards+1, "Committed Wind Pong grants one discard");
            var vow = new BambooVow();
            var bamboo = Tiles(TileSuit.Bamboo,1,2,3);
            var dots = Tiles(TileSuit.Dots,1,2,3);
            var start = ScoreEngine.PreviewScore(bamboo,null,gm.Affinity,new[]{vow},gm);
            for(int i=0;i<20;i++) ScoreEngine.PreviewScore(bamboo,null,gm.Affinity,new[]{vow},gm);
            var actual = ScoreEngine.Calculate(new Chow(bamboo),null,gm.Affinity,new[]{vow},gm);
            Check(start.TotalFu == actual.fu && Math.Abs(start.TotalFan-actual.fan)<0.001, "Bamboo Vow previews do not stack and match committed affinity");
            var compass = new BrokenCompass();
            ScoreEngine.Calculate(new Chow(bamboo),null,null,new[]{compass},gm);
            var switching = ScoreEngine.PreviewScore(dots,null,null,new[]{compass},gm);
            var switched = ScoreEngine.Calculate(new Chow(dots),null,null,new[]{compass},gm);
            Check(switching.TotalFu == 56 && switched.fu == 56, "Broken Compass rewards switching suits");
            Check(ScoreEngine.PreviewScore(dots,null,null,new[]{compass},gm).TotalFu == 36, "Repeated suit does not receive switching bonus");
            compass.OnRoundStart(gm);
            Check(ScoreEngine.PreviewScore(bamboo,null,null,new[]{compass},gm).TotalFu == 36, "Compass resets between rounds");
            gm = NewRun();
            Check(!gm.BuySpirit(compass,5), "Shop purchase blocked during play");
            gm.CurrentTargetScore=1;
            gm.PlaySelectedTiles(ScoreEngine.FindPlayableCombos(gm.Hand.Tiles).First().tiles);
            Check(gm.State == GameManager.GameState.Shop && gm.Yuan == 12, "Round clear rewards unused hands and opens shop");
            Check(gm.BuySpirit(compass,5) && gm.Yuan == 7, "Shop purchase charges once");
            Check(!gm.BuySpirit(compass,5) && gm.Yuan == 7, "Duplicate purchase rejected");
            gm.NextRound();
            Check(gm.CurrentRound == 2 && gm.HandsRemaining == 4 && gm.DiscardsRemaining == 3, "Next round resets resources");
            gm.NextRound(); Check(gm.CurrentRound == 2, "Cannot skip a playing round");
            gm.CurrentTargetScore = int.MaxValue; gm.HandsRemaining = 1;
            gm.PlaySelectedTiles(ScoreEngine.FindPlayableCombos(gm.Hand.Tiles).First().tiles);
            Check(gm.State == GameManager.GameState.GameOver, "Missing quota on final play ends run");
            gm.InitializeRun();
            Check(gm.CurrentRound == 1 && gm.EquippedSpirits.Count == 0 && gm.Yuan == 5, "Restart resets run");
            Console.WriteLine($"{count} checks passed."); return 0;
        }
        catch(Exception e) { Console.Error.WriteLine("FAIL " + e); return 1; }
    }
}
