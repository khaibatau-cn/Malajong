using UnityEngine;

[CreateAssetMenu(fileName = "CompassRose", menuName = "Malajong/Artifacts/Compass Rose")]
public class CompassRose : SpiritData
{
    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo == null || combo.Tiles == null || gm == null || gm.Affinity == null) return;

        // Find which suit currently has the highest affinity
        float bamboo = gm.Affinity.GetLevel(TileSuit.Bamboo);
        float chars = gm.Affinity.GetLevel(TileSuit.Characters);
        float dots = gm.Affinity.GetLevel(TileSuit.Dots);

        TileSuit highestSuit = TileSuit.Bamboo;
        float maxVal = bamboo;

        if (chars > maxVal)
        {
            highestSuit = TileSuit.Characters;
            maxVal = chars;
        }
        if (dots > maxVal)
        {
            highestSuit = TileSuit.Dots;
            maxVal = dots;
        }

        // Count tiles in combo matching the highest suit
        int matchingTiles = 0;
        foreach (var t in combo.Tiles)
        {
            if (t.Suit == highestSuit) matchingTiles++;
        }

        if (matchingTiles > 0)
        {
            int chipBonus = matchingTiles * 5; // +5 chips per matching tile
            chips += chipBonus;
            Debug.Log($"[Compass Rose] High-affinity match ({highestSuit})! +{chipBonus} Chips.");
        }
    }
}
