using System.Collections.Generic;
using System.Linq;

public class ScorePreview
{
    public bool IsValid;
    public string ComboName = "None";
    public string DetailText = "";
    public int BaseFu;
    public int TileFu;
    public int TotalFu;
    public float BaseFan = 1f;
    public float AffinityFan = 1f;
    public float TotalFan = 1f;
    public int ProjectedScore;
    public Combo DetectedCombo;

    // Backwards-compatible aliases
    public int BaseChips { get => BaseFu; set => BaseFu = value; }
    public int TileChips { get => TileFu; set => TileFu = value; }
    public int TotalChips { get => TotalFu; set => TotalFu = value; }
    public float BaseMult { get => BaseFan; set => BaseFan = value; }
    public float AffinityMult { get => AffinityFan; set => AffinityFan = value; }
    public float TotalMult { get => TotalFan; set => TotalFan = value; }
}

public static class ScoreEngine
{
    public static ScorePreview PreviewScore(List<Tile> selectedTiles, List<Tile> fullHand, SuitAffinity affinityManager = null, IEnumerable<SpiritData> activeSpirits = null, GameManager gm = null)
    {
        var preview = new ScorePreview();
        if (selectedTiles == null || selectedTiles.Count == 0)
        {
            preview.IsValid = false;
            preview.ComboName = "No Selection";
            preview.DetailText = "Select tiles to form a combo.";
            return preview;
        }

        if (selectedTiles.Count == 14)
        {
            var (bonusFu, bonusFan) = EvaluateFullHand(selectedTiles);
            if (bonusFu > 0)
            {
                preview.IsValid = true;
                preview.ComboName = "Full Winning Hand";
                preview.DetailText = "14-tile complete Mahjong winning hand!";
                preview.BaseFu = bonusFu;
                preview.TileFu = 0;
                preview.TotalFu = bonusFu;
                preview.BaseFan = bonusFan;
                preview.AffinityFan = 1f;
                preview.TotalFan = bonusFan;
                preview.ProjectedScore = UnityEngine.Mathf.RoundToInt(preview.TotalFu * preview.TotalFan);
                return preview;
            }
        }

        Combo combo = DetectCombo(selectedTiles);
        if (combo == null)
        {
            preview.IsValid = false;
            preview.ComboName = "Invalid Combo";
            preview.DetailText = $"{selectedTiles.Count} tiles selected (not a valid Chow, Pong, Kong, or Pair).";
            return preview;
        }

        return Evaluate(combo, fullHand, affinityManager, activeSpirits, gm, commit: false);
    }

    public static (int fu, float fan) Calculate(Combo combo, List<Tile> fullHand, SuitAffinity affinityManager = null, IEnumerable<SpiritData> activeSpirits = null, GameManager gm = null)
    {
        if (!combo.IsValid())
        {
            return (0, 0f);
        }

        ScorePreview result = Evaluate(combo, fullHand, affinityManager, activeSpirits, gm, commit: true);
        return (result.TotalFu, result.TotalFan);
    }

    /// <summary>
    /// The single scoring path. Previewing and actually playing a combo run through this same
    /// method, so the number shown to the player and the number banked cannot disagree.
    ///
    /// <paramref name="commit"/> is the only difference between them:
    /// <list type="bullet">
    /// <item><b>false</b> — pure. Nothing is mutated. Affinity gain is *simulated* so the preview
    /// shows the multiplier the play will actually score at, not the one from before it.</item>
    /// <item><b>true</b> — applies affinity gain and fires the spirits' side-effect hook.</item>
    /// </list>
    ///
    /// This split exists because preview used to run the full scoring path on every tile click:
    /// RestlessWind handed out a real discard each time a tile was selected, and BambooVow's
    /// accumulator ratcheted up without anything being played. Spirit scoring hooks are pure by
    /// contract now — anything that changes run state belongs in <see cref="SpiritData.OnComboCommitted"/>.
    /// </summary>
    private static ScorePreview Evaluate(Combo combo, List<Tile> fullHand, SuitAffinity affinityManager, IEnumerable<SpiritData> activeSpirits, GameManager gm, bool commit)
    {
        var preview = new ScorePreview
        {
            IsValid = true,
            DetectedCombo = combo,
            ComboName = combo.Name,
            BaseFu = combo.BaseFu,
            TileFu = combo.Tiles.Sum(t => t.Rank),
            BaseFan = combo.BaseFan
        };

        int fu = preview.BaseFu + preview.TileFu;
        float fan = preview.BaseFan;

        ApplyPostCheckBonuses(fullHand, ref fu, ref fan, activeSpirits, gm);

        if (activeSpirits != null)
        {
            foreach (var spirit in activeSpirits)
            {
                spirit.OnComboScored(combo, ref fu, ref fan, gm);
            }
        }

        float affMult = 1f;
        if (affinityManager != null)
        {
            TileSuit comboSuit = combo.Tiles.Count > 0 ? combo.Tiles[0].Suit : TileSuit.Honor;
            float pendingBoost = 0f;

            foreach (var delta in combo.AffinityDeltas)
            {
                float boostMult = 1.0f;
                if (activeSpirits != null)
                {
                    foreach (var spirit in activeSpirits)
                    {
                        // Pure by contract: returns a scaling factor, changes nothing.
                        boostMult *= spirit.OnAffinityBoosted(delta.Key, delta.Value, gm);
                    }
                }

                float amount = delta.Value * boostMult;
                if (delta.Key == comboSuit) pendingBoost += amount;

                if (commit) affinityManager.Boost(delta.Key, amount);
            }

            if (combo.Tiles.Count > 0 && !combo.Tiles[0].IsHonor)
            {
                // Committing has already applied the boost, so a plain read is the post-boost
                // value; previewing has to project it. Both land on the same number.
                affMult = commit
                    ? affinityManager.GetMultiplier(comboSuit)
                    : affinityManager.PeekMultiplierAfterBoost(comboSuit, pendingBoost);

                fan *= affMult;
            }
        }

        if (commit && activeSpirits != null)
        {
            foreach (var spirit in activeSpirits)
            {
                spirit.OnComboCommitted(combo, gm);
            }
        }

        preview.TotalFu = fu;
        preview.AffinityFan = affMult;
        preview.TotalFan = fan;
        preview.ProjectedScore = UnityEngine.Mathf.RoundToInt(fu * fan);
        preview.DetailText = combo.Tiles.Count > 0 ? $"{combo.Tiles[0].Suit} {combo.Name}" : combo.Name;

        return preview;
    }

    private static void ApplyPostCheckBonuses(List<Tile> hand, ref int fu, ref float fan, IEnumerable<SpiritData> activeSpirits, GameManager gm)
    {
        if (hand == null || hand.Count != 13) return; // Added 13 check

        bool isAllHonors = hand.All(t => t.IsHonor);
        if (isAllHonors)
        {
            fu += 180;
            fan *= 12.0f;
            return;
        }

        bool containsHonors = hand.Any(t => t.IsHonor);
        if (!containsHonors)
        {
            TileSuit firstSuit = hand[0].Suit;
            bool isPureHand = hand.All(t => t.Suit == firstSuit);

            if (isPureHand)
            {
                fu += 150;
                fan *= 10.0f;
            }
        }

        if (activeSpirits != null)
        {
            foreach (var spirit in activeSpirits)
            {
                spirit.OnPostCheckBonuses(hand, ref fu, ref fan, gm);
            }
        }
    }
    // Bracket comment removed

    public static Combo DetectCombo(List<Tile> tiles)
    {
        Combo c = new ConcealedKong(tiles);
        if (c.IsValid()) return c;
        
        c = new Kong(tiles);
        if (c.IsValid()) return c;
        
        c = new Pong(tiles);
        if (c.IsValid()) return c;
        
        c = new Chow(tiles);
        if (c.IsValid()) return c;
        
        c = new Pair(tiles);
        if (c.IsValid()) return c;
        
        return null;
    }

    public static (int bonusFu, float bonusFan) EvaluateFullHand(List<Tile> fullHand)
    {
        if (IsWinningMahjongHand(fullHand))
        {
            return (100, 8.0f); // From the design doc baseline
        }

        return (0, 0f);
    }

    private static bool IsWinningMahjongHand(List<Tile> hand)
    {
        // Fix #2: The math strictly requires 14 tiles (4 sets of 3 + 1 pair = 14)
        if (hand.Count != 14) return false;

        var counts = new Dictionary<string, int>();
        foreach (var t in hand)
        {
            string key = $"{t.Suit}_{t.Rank}";
            if (!counts.ContainsKey(key)) counts[key] = 0;
            counts[key]++;
        }

        var uniqueKeys = counts.Keys.ToList();
        foreach (var key in uniqueKeys)
        {
            if (counts[key] >= 2)
            {
                var simCounts = new Dictionary<string, int>(counts);
                simCounts[key] -= 2;
                if (simCounts[key] == 0) simCounts.Remove(key);

                if (CanFormSets(simCounts)) return true;
            }
        }
        return false;
    }

    private static bool CanFormSets(Dictionary<string, int> counts)
    {
        if (counts.Count == 0) return true; // Hand empty, sets matched perfectly

        string firstKey = counts.Keys.First();
        string[] parts = firstKey.Split('_');
        TileSuit suit = (TileSuit)System.Enum.Parse(typeof(TileSuit), parts[0]);
        int rank = int.Parse(parts[1]);

        // Try Kong (4 identical)
        if (counts[firstKey] == 4)
        {
            var sim = new Dictionary<string, int>(counts);
            sim.Remove(firstKey);
            if (CanFormSets(sim)) return true;
        }

        // Try Pong (3 identical)
        if (counts[firstKey] >= 3)
        {
            var sim = new Dictionary<string, int>(counts);
            sim[firstKey] -= 3;
            if (sim[firstKey] == 0) sim.Remove(firstKey);
            if (CanFormSets(sim)) return true;
        }

        // Try Chow (sequential, non-honor)
        if (suit != TileSuit.Honor)
        {
            string k2 = $"{suit}_{rank + 1}";
            string k3 = $"{suit}_{rank + 2}";
            if (counts.ContainsKey(k2) && counts[k2] > 0 &&
                counts.ContainsKey(k3) && counts[k3] > 0)
            {
                var sim = new Dictionary<string, int>(counts);
                sim[firstKey]--; if (sim[firstKey] == 0) sim.Remove(firstKey);
                sim[k2]--; if (sim[k2] == 0) sim.Remove(k2);
                sim[k3]--; if (sim[k3] == 0) sim.Remove(k3);

                if (CanFormSets(sim)) return true;
            }
        }

        return false;
    }

    public static List<(Combo combo, List<Tile> tiles)> FindPlayableCombos(List<Tile> hand)
    {
        var result = new List<(Combo combo, List<Tile> tiles)>();
        if (hand == null || hand.Count == 0) return result;

        // Group tiles by suit & rank
        var tilesByKind = hand.GroupBy(t => $"{t.Suit}_{t.Rank}").ToList();

        // 1. Find Kongs (4 of a kind)
        foreach (var group in tilesByKind)
        {
            if (group.Count() >= 4)
            {
                var tiles = group.Take(4).ToList();
                var c = DetectCombo(tiles);
                if (c != null) result.Add((c, tiles));
            }
        }

        // 2. Find Pongs (3 of a kind)
        foreach (var group in tilesByKind)
        {
            if (group.Count() >= 3)
            {
                var tiles = group.Take(3).ToList();
                var c = DetectCombo(tiles);
                if (c != null && !result.Any(r => r.combo.Name == c.Name && r.tiles.All(tiles.Contains)))
                    result.Add((c, tiles));
            }
        }

        // 3. Find Chows (3 sequential of same non-honor suit)
        var nonHonors = hand.Where(t => !t.IsHonor).GroupBy(t => t.Suit);
        foreach (var suitGroup in nonHonors)
        {
            var ordered = suitGroup.OrderBy(t => t.Rank).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var t1 = ordered[i];
                var t2 = ordered.FirstOrDefault(t => t.Rank == t1.Rank + 1);
                var t3 = ordered.FirstOrDefault(t => t.Rank == t1.Rank + 2);
                if (t2 != null && t3 != null)
                {
                    var tiles = new List<Tile> { t1, t2, t3 };
                    var c = DetectCombo(tiles);
                    if (c != null && !result.Any(r => r.tiles[0] == t1 && r.tiles[1] == t2 && r.tiles[2] == t3))
                    {
                        result.Add((c, tiles));
                    }
                }
            }
        }

        // 4. Find Pairs (2 of a kind)
        foreach (var group in tilesByKind)
        {
            if (group.Count() >= 2)
            {
                var tiles = group.Take(2).ToList();
                var c = DetectCombo(tiles);
                if (c != null) result.Add((c, tiles));
            }
        }

        return result;
    }
}