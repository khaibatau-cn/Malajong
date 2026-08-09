using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One suit's affinity, drawn as a row of discrete blocks.
///
/// The underlying value is already quantised — affinity runs 0..1 and the combos that feed it
/// boost in steps of 0.1 / 0.15 / 0.3 / 0.5 — so blocks are an honest reading of it rather than
/// decoration laid over a smooth bar. At <see cref="MalajongTheme.MeterSegments"/> = 10, one block
/// is 0.1 affinity and the smallest boost in the game lights exactly one.
///
/// Blocks are lit by recolouring, never by enabling and disabling them: the unlit track has to
/// stay on screen so an empty meter still reads as something that could fill up.
/// </summary>
public class SuitAffinityMeter : MonoBehaviour
{
    public TextMeshProUGUI ValueText;
    public Image[] Segments;

    [Header("Colours")]
    public Color LitColor = Color.white;
    public Color UnlitColor = Color.grey;
    /// <summary>The multiplier readout at zero affinity — dim, but still legible.</summary>
    public Color IdleTextColor = Color.grey;

    /// <summary>Starts at -1 so the first SetLevel always paints, even when it is painting zero.</summary>
    private int litCount = -1;

    /// <param name="level01">Raw affinity, 0..1. <see cref="SuitAffinity.GetLevel"/>.</param>
    /// <param name="multiplier">The 1.0x..2.0x figure shown beside the blocks.</param>
    public void SetLevel(float level01, float multiplier)
    {
        if (Segments == null || Segments.Length == 0) return;

        // Half-up by hand rather than Mathf.RoundToInt, which banker's-rounds an exact .5 to even.
        // Decay lands on half-block values constantly (it is half the boost), so with RoundToInt a
        // 0.05 step would light or unlight a block depending on which block it happened to be.
        int target = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Clamp01(level01) * Segments.Length + 0.5f),
            0, Segments.Length);

        if (target != litCount)
        {
            bool gained = litCount >= 0 && target > litCount;
            int firstNewBlock = litCount;

            for (int i = 0; i < Segments.Length; i++)
            {
                if (Segments[i] == null) continue;
                Segments[i].color = i < target ? LitColor : UnlitColor;
            }

            // Pop only the blocks that just lit, and only while the meter is actually on screen.
            // A pop-in scales its target to zero first and runs its coroutine on the UIJuice
            // singleton, so one interrupted by a panel hide would leave a block stuck invisible.
            if (gained && isActiveAndEnabled)
            {
                for (int i = firstNewBlock; i < target; i++)
                {
                    if (Segments[i] == null) continue;
                    UIJuice.PopIn(Segments[i].transform, (i - firstNewBlock) * 0.04f);
                }
            }

            litCount = target;
        }

        if (ValueText != null)
        {
            ValueText.text = $"{multiplier:F1}x";
            ValueText.color = target > 0 ? LitColor : IdleTextColor;
        }
    }

    /// <summary>
    /// Deactivating this object stops nothing — the pop-ins run on UIJuice, not here — so a panel
    /// hidden mid-pop leaves blocks frozen at a partial scale. Clear that on the way back in.
    /// </summary>
    private void OnEnable()
    {
        if (Segments == null) return;

        foreach (Image segment in Segments)
        {
            if (segment != null) segment.transform.localScale = Vector3.one;
        }
    }
}
