using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BrokenCompass", menuName = "Malajong/Artifacts/Broken Compass")]
public class BrokenCompass : SpiritData
{
    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo == null || combo.Tiles == null) return;

        // Check if combo contains multiple distinct suits (e.g. mixed hands or paired plays)
        HashSet<TileSuit> distinctSuits = new HashSet<TileSuit>();
        foreach (var t in combo.Tiles)
        {
            distinctSuits.Add(t.Suit);
        }

        if (distinctSuits.Count >= 2)
        {
            chips += 20;
            Debug.Log("[Broken Compass] Mixed suit burst! Granted +20 Chips.");
        }
    }
}
