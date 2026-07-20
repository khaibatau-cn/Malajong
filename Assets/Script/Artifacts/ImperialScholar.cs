using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ImperialScholar", menuName = "Malajong/Spirits/ImperialScholar")]
public class ImperialScholar : SpiritData
{
    public override void OnPostCheckBonuses(List<Tile> fullHand, ref int chips, ref float mult, GameManager gm)
    {
        if (fullHand == null || fullHand.Count != 13) return;
        
        bool isAllHonors = fullHand.All(t => t.IsHonor);
        if (isAllHonors)
        {
            // The base logic in ScoreEngine already multiplied by 12.0f.
            // We want it to be 20.0f instead, so we divide by 12 and multiply by 20.
            mult = (mult / 12.0f) * 20.0f;
            Debug.Log("[Spirit: Imperial Scholar] Triggered! Upgraded All Honors multiplier to x20.");
        }
    }
}
