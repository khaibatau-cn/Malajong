using System.Collections.Generic;
using UnityEngine;

public class TileBag
{
    private List<Tile> drawPile = new List<Tile>();

    public int Remaining => drawPile.Count;
    public int RemainingCount => Remaining;

    public void Initialize(List<TileData> allTileTypes)
    {
        drawPile.Clear();
        foreach (var tileData in allTileTypes)
        {
            // Standard Mahjong has 4 copies of each tile
            for (int i = 0; i < 4; i++)
            {
                drawPile.Add(new Tile(tileData));
            }
        }
        Shuffle();
    }

    public void Shuffle()
    {
        int n = drawPile.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            Tile value = drawPile[k];
            drawPile[k] = drawPile[n];
            drawPile[n] = value;
        }
    }

    public List<Tile> Draw(int count)
    {
        List<Tile> drawn = new List<Tile>();
        for (int i = 0; i < count && drawPile.Count > 0; i++)
        {
            Tile t = drawPile[0];
            t.IsSelfDrawn = true; // Mark as newly drawn
            drawPile.RemoveAt(0);
            drawn.Add(t);
        }
        return drawn;
    }
}
