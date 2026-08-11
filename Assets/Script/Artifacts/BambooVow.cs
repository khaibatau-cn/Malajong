using UnityEngine;

[CreateAssetMenu(fileName = "BambooVow", menuName = "Malajong/Artifacts/Bamboo Vow")]
public class BambooVow : SpiritData
{
    private const float StackPerCombo = 0.5f;

    /// <summary>
    /// Stacks earned so far this round. Only <see cref="OnComboCommitted"/> may change it —
    /// scoring hooks project from it instead, or previewing a combo would ratchet the stack up
    /// on every tile click.
    /// </summary>
    private float accumulatedMultBonus = 0f;

    public override void OnRoundStart(GameManager gm)
    {
        accumulatedMultBonus = 0f;
    }

    public override void OnComboScored(Combo combo, ref int chips, ref float mult, GameManager gm)
    {
        // Projects the stack this combo *would* earn, without banking it. An off-suit combo scores
        // no bonus and resets the stack, but the reset itself happens on commit.
        if (!IsAllBamboo(combo)) return;

        mult += accumulatedMultBonus + StackPerCombo;
    }

    public override void OnComboCommitted(Combo combo, GameManager gm)
    {
        if (IsAllBamboo(combo))
        {
            accumulatedMultBonus += StackPerCombo;
            Debug.Log($"[Bamboo Vow] Stacked +{StackPerCombo:F1}x! Current bonus: +{accumulatedMultBonus:F1}x Mult.");
        }
        else
        {
            accumulatedMultBonus = 0f;
            Debug.Log("[Bamboo Vow] Off-suit combo played! Bamboo Vow multiplier reset to 0.");
        }
    }

    private static bool IsAllBamboo(Combo combo)
    {
        if (combo == null || combo.Tiles == null || combo.Tiles.Count == 0) return false;

        foreach (var tile in combo.Tiles)
        {
            if (tile.Suit != TileSuit.Bamboo) return false;
        }

        return true;
    }
}
