using UnityEngine;

[CreateAssetMenu(fileName = "RestlessWind", menuName = "Malajong/Spirits/RestlessWind")]
public class RestlessWind : SpiritData
{
    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        if (combo is Pong && combo.Tiles.Count > 0 && combo.Tiles[0].IsHonor) // "Pong of any Wind tile"
        {
            // Assuming ranks 0-3 are Winds (East, South, West, North) and 4-6 are Dragons
            if (combo.Tiles[0].Rank >= 0 && combo.Tiles[0].Rank <= 3)
            {
                if (gm != null)
                {
                    gm.DiscardsRemaining += 1;
                    Debug.Log("[Spirit: Restless Wind] Triggered! Granted +1 Discard.");
                }
            }
        }
    }
}
