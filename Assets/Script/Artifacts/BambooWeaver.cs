using UnityEngine;

[CreateAssetMenu(fileName = "BambooWeaver", menuName = "Malajong/Spirits/BambooWeaver")]
public class BambooWeaver : SpiritData
{
    public override float OnAffinityBoosted(TileSuit suit, float incomingBoost, GameManager gm)
    {
        if (suit == TileSuit.Bamboo)
        {
            Debug.Log("[Spirit: Bamboo Weaver] Triggered! Boosting Bamboo affinity 1.5x faster.");
            return 1.5f;
        }
        return 1.0f;
    }
}
