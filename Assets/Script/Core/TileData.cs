public enum TileSuit
{
    Bamboo,
    Characters,
    Dots,
    Honor
}

public class Tile
{
    public TileSuit Suit;
    public int Rank;
    public bool IsSelfDrawn = false; // Fix #1: Default is now false

    public bool IsHonor => Suit == TileSuit.Honor;
}