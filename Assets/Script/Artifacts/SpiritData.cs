using System.Collections.Generic;
using UnityEngine;

public abstract class SpiritData : ScriptableObject
{
    public string SpiritName;
    [TextArea]
    public string Description;
    public Sprite Icon;

    // Called when a combo is played
    public virtual void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm) { }

    // Called when calculating whole-hand bonuses like Pure/All Honors
    public virtual void OnPostCheckBonuses(List<Tile> fullHand, ref int chips, ref float mult, GameManager gm) { }

    // Called when suit affinity is being boosted, returns a multiplier to the boost amount
    public virtual float OnAffinityBoosted(TileSuit suit, float incomingBoost, GameManager gm) 
    {
        return 1.0f; // Default is no change to the boost
    }
    
    // Called when the round starts
    public virtual void OnRoundStart(GameManager gm) { }
}
