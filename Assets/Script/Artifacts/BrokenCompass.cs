using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BrokenCompass", menuName = "Malajong/Artifacts/Broken Compass")]
public class BrokenCompass : SpiritData
{
    private TileSuit? previousSuit;

    public override void OnRoundStart(GameManager gm) => previousSuit = null;

    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo == null || combo.Tiles == null) return;

        if (combo.Tiles.Count > 0 && !combo.Tiles[0].IsHonor &&
            previousSuit.HasValue && previousSuit.Value != combo.Tiles[0].Suit)
        {
            chips += 20;
        }
    }

    public override void OnComboCommitted(Combo combo, GameManager gm)
    {
        if (combo != null && combo.Tiles.Count > 0 && !combo.Tiles[0].IsHonor)
            previousSuit = combo.Tiles[0].Suit;
    }
}
