using System.Collections.Generic;
using System.Linq;

public abstract class Combo
{
    public List<Tile> Tiles = new List<Tile>();

    public Combo(List<Tile> tiles)
    {
        Tiles = new List<Tile>(tiles);
    }

    public abstract string Name { get; }
    public abstract int BaseChips { get; }
    public abstract float BaseMult { get; }

    // Standardized delta value for subclasses to define (Fix 4)
    public abstract float AffinityBonus { get; }

    public abstract bool IsValid();

    // Lazily computes only if the combo is actually valid (Fix 8)
    public Dictionary<TileSuit, float> AffinityDeltas
    {
        get
        {
            var deltas = new Dictionary<TileSuit, float>();
            if (IsValid() && Tiles.Count > 0 && !Tiles[0].IsHonor)
            {
                deltas[Tiles[0].Suit] = AffinityBonus;
            }
            return deltas;
        }
    }
}

public class Pong : Combo
{
    public override string Name => "Pong";
    public override int BaseChips => 20;
    public override float BaseMult => 2.0f;
    public override float AffinityBonus => 0.1f; // (Fix 3 & 4)

    public Pong(List<Tile> tiles) : base(tiles) { }

    public override bool IsValid()
    {
        if (Tiles.Count != 3) return false;

        bool sameSuit = Tiles[0].Suit == Tiles[1].Suit && Tiles[1].Suit == Tiles[2].Suit;
        bool sameRank = Tiles[0].Rank == Tiles[1].Rank && Tiles[1].Rank == Tiles[2].Rank;

        return sameSuit && sameRank;
    }
}

public class Chow : Combo
{
    public override string Name => "Chow";
    public override int BaseChips => 15;
    public override float BaseMult => 2.0f;
    public override float AffinityBonus => 0.1f; // (Fix 4)

    public Chow(List<Tile> tiles) : base(tiles) { }

    public override bool IsValid()
    {
        if (Tiles.Count != 3) return false;
        if (Tiles[0].IsHonor) return false;

        if (Tiles[0].Suit != Tiles[1].Suit || Tiles[1].Suit != Tiles[2].Suit) return false;

        var sortedTiles = Tiles.OrderBy(t => t.Rank).ToList();
        return sortedTiles[0].Rank + 1 == sortedTiles[1].Rank && sortedTiles[1].Rank + 1 == sortedTiles[2].Rank;
    }
}

public class Kong : Combo // (Fix 2)
{
    public override string Name => "Kong";
    public override int BaseChips => 40;
    public override float BaseMult => 3.0f;
    public override float AffinityBonus => 0.2f; // (Fix 4)

    public Kong(List<Tile> tiles) : base(tiles) { }

    public override bool IsValid()
    {
        if (Tiles.Count != 4) return false;

        TileSuit targetSuit = Tiles[0].Suit;
        int targetRank = Tiles[0].Rank;

        foreach (var tile in Tiles)
        {
            if (tile.Suit != targetSuit || tile.Rank != targetRank) return false;
        }

        return true;
    }
}

public class Pair : Combo // (Fix 2)
{
    public override string Name => "Pair";
    public override int BaseChips => 5;
    public override float BaseMult => 1.0f;
    public override float AffinityBonus => 0.0f; // Pairs don't boost affinity

    public Pair(List<Tile> tiles) : base(tiles) { }

    public override bool IsValid()
    {
        if (Tiles.Count != 2) return false;

        return Tiles[0].Suit == Tiles[1].Suit && Tiles[0].Rank == Tiles[1].Rank;
    }
}
public class ConcealedKong : Kong
{
    public override string Name => "Concealed Kong";
    public override int BaseChips => 55;
    public override float BaseMult => 4.0f;
    public override float AffinityBonus => 0.4f; // Fix #3: Bumped for risk/reward

    public ConcealedKong(List<Tile> tiles) : base(tiles) { }

    public override bool IsValid()
    {
        return base.IsValid() && Tiles.All(t => t.IsSelfDrawn);
    }
}