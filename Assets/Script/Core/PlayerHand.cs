using System.Collections.Generic;
using System.Linq;

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

    // Sort by Suit: Bamboo -> Characters -> Dots -> Honors, sorted by Rank 1-9 ascending
    public void SortBySuit()
    {
        Tiles = Tiles
            .OrderBy(t => (int)t.Suit)
            .ThenBy(t => t.Rank)
            .ToList();
    }

    // Sort by Rank: 1 -> 9 across all suits, then Honors at the end
    public void SortByRank()
    {
        Tiles = Tiles
            .OrderBy(t => t.IsHonor ? 100 + t.Rank : t.Rank)
            .ThenBy(t => (int)t.Suit)
            .ToList();
    }
}
