using UnityEngine;

public enum TileSuit
{
    Bamboo,
    Characters,
    Dots,
    Honor
}

public enum HonorEffect
{
    None,
    PeekWall,
    ForceRedraw,
    FreezeTile
}

[CreateAssetMenu(fileName = "New Tile Data", menuName = "Malajong/Tile Data")]
public class TileData : ScriptableObject
{
    public TileSuit Suit;
    public int Rank;
    public HonorEffect Effect = HonorEffect.None;

    [Header("Art & Visuals")]
    public Sprite TileSprite;
    public Sprite TileBackSprite;

    public bool IsHonor => Suit == TileSuit.Honor;
}