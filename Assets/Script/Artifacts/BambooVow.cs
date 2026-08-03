using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BambooVow", menuName = "Malajong/Artifacts/Bamboo Vow")]
public class BambooVow : SpiritData
{
    private float accumulatedMultBonus = 0f;

    public override void OnRoundStart(GameManager gm)
    {
        accumulatedMultBonus = 0f;
    }

    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo == null || combo.Tiles == null || combo.Tiles.Count == 0) return;

        bool allBamboo = true;
        foreach (var tile in combo.Tiles)
        {
            if (tile.Suit != TileSuit.Bamboo)
            {
                allBamboo = false;
                break;
            }
        }

        if (allBamboo)
        {
            accumulatedMultBonus += 0.5f;
            mult += accumulatedMultBonus;
            Debug.Log($"[Bamboo Vow] Stacked +0.5x! Current bonus: +{accumulatedMultBonus:F1}x Mult.");
        }
        else
        {
            accumulatedMultBonus = 0f;
            Debug.Log("[Bamboo Vow] Off-suit combo played! Bamboo Vow multiplier reset to 0.");
        }
    }
}
