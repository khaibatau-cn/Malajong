using System.Collections.Generic;
using UnityEngine;

public class SuitAffinity
{
    private Dictionary<TileSuit, float> affinityLevels;
    
    // Max affinity cap (1.0f means a max +1.0x bonus, meaning 2.0x total multiplier)
    private const float MaxAffinity = 1.0f;
    private const float MinAffinity = 0.0f;

    public SuitAffinity()
    {
        affinityLevels = new Dictionary<TileSuit, float>
        {
            { TileSuit.Bamboo, 0f },
            { TileSuit.Characters, 0f },
            { TileSuit.Dots, 0f }
        };
    }

    public void Boost(TileSuit targetSuit, float amount)
    {
        // Honor tiles do not participate in suit affinity scaling
        if (targetSuit == TileSuit.Honor) return;

        // Boost the target suit
        if (affinityLevels.ContainsKey(targetSuit))
        {
            affinityLevels[targetSuit] = Mathf.Clamp(affinityLevels[targetSuit] + amount, MinAffinity, MaxAffinity);
        }

        // Decay other suits (tension mechanic)
        // They decay by half the boost amount to make switching suits punishing but not instant zero.
        float decayAmount = amount / 2.0f;
        
        List<TileSuit> suits = new List<TileSuit>(affinityLevels.Keys);
        foreach (var suit in suits)
        {
            if (suit != targetSuit)
            {
                affinityLevels[suit] = Mathf.Clamp(affinityLevels[suit] - decayAmount, MinAffinity, MaxAffinity);
            }
        }
    }

    public float GetMultiplier(TileSuit suit)
    {
        if (affinityLevels.ContainsKey(suit))
        {
            return 1.0f + affinityLevels[suit];
        }
        return 1.0f;
    }

    public float GetLevel(TileSuit suit)
    {
        if (affinityLevels.ContainsKey(suit))
        {
            return affinityLevels[suit];
        }
        return 0f;
    }
}
