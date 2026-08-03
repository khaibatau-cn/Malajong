using UnityEngine;

[CreateAssetMenu(fileName = "GreenDragonSpirit", menuName = "Malajong/Artifacts/Green Dragon Spirit")]
public class GreenDragonSpirit : SpiritData
{
    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo == null || combo.Tiles == null) return;

        // Honor 6 corresponds to Green Dragon (發)
        bool hasGreenDragon = false;
        foreach (var t in combo.Tiles)
        {
            if (t.Suit == TileSuit.Honor && t.Rank == 6)
            {
                hasGreenDragon = true;
                break;
            }
        }

        if (hasGreenDragon)
        {
            chips += 50;
            mult += 2.0f;
            Debug.Log("[Green Dragon Spirit] Green Dragon played! +50 Chips and +2.0x Mult!");
        }
    }
}
