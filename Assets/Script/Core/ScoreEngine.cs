using System.Collections.Generic;
using System.Linq;

public static class ScoreEngine
{
    public static (int chips, float mult) Calculate(Combo combo, List<Tile> fullHand, SuitAffinity affinityManager = null, IEnumerable<SpiritData> activeSpirits = null, GameManager gm = null)
    {
        if (!combo.IsValid())
        {
            return (0, 0f);
        }

        int chips = combo.BaseChips;
        float mult = combo.BaseMult;

        ApplyPostCheckBonuses(fullHand, ref chips, ref mult, activeSpirits, gm);

        if (activeSpirits != null)
        {
            foreach (var spirit in activeSpirits)
            {
                spirit.OnComboScored(combo, ref chips, ref mult, gm);
            }
        }

        if (affinityManager != null)
        {
            foreach (var delta in combo.AffinityDeltas) 
            {
                float boostMult = 1.0f;
                if (activeSpirits != null)
                {
                    foreach (var spirit in activeSpirits)
                    {
                        boostMult *= spirit.OnAffinityBoosted(delta.Key, delta.Value, gm);
                    }
                }
                affinityManager.Boost(delta.Key, delta.Value * boostMult);
            }
            if (combo.Tiles.Count > 0 && !combo.Tiles[0].IsHonor) 
            {
                mult *= affinityManager.GetMultiplier(combo.Tiles[0].Suit);
            }
        }

        return (chips, mult);
    }

    private static void ApplyPostCheckBonuses(List<Tile> hand, ref int chips, ref float mult, IEnumerable<SpiritData> activeSpirits, GameManager gm)
    {
        if (hand == null || hand.Count != 13) return; // Added 13 check

        bool isAllHonors = hand.All(t => t.IsHonor);
        if (isAllHonors)
        {
            chips += 180;
            mult *= 12.0f;
            return;
        }

        bool containsHonors = hand.Any(t => t.IsHonor);
        if (!containsHonors)
        {
            TileSuit firstSuit = hand[0].Suit;
            bool isPureHand = hand.All(t => t.Suit == firstSuit);

            if (isPureHand)
            {
                chips += 150;
                mult *= 10.0f;
            }
        }

        if (activeSpirits != null)
        {
            foreach (var spirit in activeSpirits)
            {
                spirit.OnPostCheckBonuses(hand, ref chips, ref mult, gm);
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

    public static (int bonusChips, float bonusMult) EvaluateFullHand(List<Tile> fullHand)
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
}