using UnityEngine;

public class Tile
{
    public TileData Data;
    public bool IsSelfDrawn = false;

    // Convenience properties so that existing code doesn't break
    public TileSuit Suit => Data.Suit;
    public int Rank => Data.Rank;
    public bool IsHonor => Data.IsHonor;

    public Tile(TileData data)
    {
        Data = data;
    }
}
