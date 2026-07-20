using System.Collections.Generic;

public class PlayerHand
{
    public List<Tile> Tiles { get; private set; } = new List<Tile>();
    public const int MaxSize = 14;

    public void AddTiles(List<Tile> newTiles)
    {
        Tiles.AddRange(newTiles);
    }

    public void RemoveTiles(List<Tile> usedTiles)
    {
        foreach (var t in usedTiles)
        {
            Tiles.Remove(t);
        }
    }

    public int MissingCount => MaxSize - Tiles.Count;

    // Reset IsSelfDrawn flag at the start of a turn (since they are no longer "just drawn")
    public void ClearSelfDrawnFlags()
    {
        foreach (var t in Tiles)
        {
            t.IsSelfDrawn = false;
        }
    }
}
