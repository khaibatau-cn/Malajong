using UnityEngine;

[CreateAssetMenu(fileName = "RestlessWind", menuName = "Malajong/Spirits/RestlessWind")]
public class RestlessWind : SpiritData
{
    // Honor ranks 1-4 are the Winds (East, South, West, North); 5-7 are the Dragons.
    private const int FirstWindRank = 1;
    private const int LastWindRank = 4;

    /// <summary>
    /// Granting a discard is a change to run state, so it belongs here rather than in
    /// OnComboScored — which runs on every score preview, and was handing out a free discard each
    /// time the player clicked a tile.
    /// </summary>
    public override void OnComboCommitted(Combo combo, GameManager gm)
    {
        if (gm == null || !IsWindPong(combo)) return;

        gm.DiscardsRemaining += 1;
        Debug.Log("[Spirit: Restless Wind] Triggered! Granted +1 Discard.");
    }

    private static bool IsWindPong(Combo combo)
    {
        if (!(combo is Pong) || combo.Tiles == null || combo.Tiles.Count == 0) return false;

        Tile first = combo.Tiles[0];
        return first.IsHonor && first.Rank >= FirstWindRank && first.Rank <= LastWindRank;
    }
}
