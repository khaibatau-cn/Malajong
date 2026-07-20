using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PuristsFlute", menuName = "Malajong/Spirits/PuristsFlute")]
public class PuristsFlute : SpiritData
{
    public override void OnPostCheckBonuses(List<Tile> fullHand, ref int chips, ref float mult, GameManager gm)
    {
        if (fullHand == null || fullHand.Count != 13) return;
        
        bool containsHonors = fullHand.Any(t => t.IsHonor);
        if (!containsHonors)
        {
            TileSuit firstSuit = fullHand[0].Suit;
            bool isPureHand = fullHand.All(t => t.Suit == firstSuit);

            if (isPureHand)
            {
                chips += 100;
                Debug.Log("[Spirit: Purist's Flute] Triggered! Added +100 chips to Pure Hand.");
            }
        }
    }
}
