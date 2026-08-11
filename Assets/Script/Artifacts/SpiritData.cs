using System.Collections.Generic;
using UnityEngine;

public abstract class SpiritData : ScriptableObject
{
    public string SpiritName;
    [TextArea]
    public string Description;
    public Sprite Icon;

    // ------------------------------------------------------------------------
    // Scoring hooks — MUST BE PURE.
    //
    // These run on every score preview, which fires each time the player clicks a tile. They may
    // read run state and adjust the fu/fan being calculated, and must not change anything else:
    // no granting discards, no accumulating counters, no touching GameManager.
    //
    // Anything that changes run state goes in OnComboCommitted, which only runs when a combo is
    // actually played. Breaking this rule is not a subtle bug — a spirit that grants a discard here
    // grants one per mouse click.
    // ------------------------------------------------------------------------

    /// <summary>Adjust fu/fan for a combo being scored. Pure — read state, change nothing.</summary>
    public virtual void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm) { }

    /// <summary>Adjust fu/fan for whole-hand properties like Pure Hand or All Honors. Pure.</summary>
    public virtual void OnPostCheckBonuses(List<Tile> fullHand, ref int chips, ref float mult, GameManager gm) { }

    /// <summary>Scale an incoming affinity boost. Pure — return a factor, change nothing.</summary>
    public virtual float OnAffinityBoosted(TileSuit suit, float incomingBoost, GameManager gm)
    {
        return 1.0f; // Default is no change to the boost
    }

    // ------------------------------------------------------------------------
    // Side-effect hook — runs once, only when a combo is actually played.
    // ------------------------------------------------------------------------

    /// <summary>
    /// The combo has been played and its score banked. This is where a spirit changes run state:
    /// granting discards, accumulating stacks, freezing tiles.
    /// </summary>
    public virtual void OnComboCommitted(Combo combo, GameManager gm) { }
    
    // Called when the round starts
    public virtual void OnRoundStart(GameManager gm) { }
}
