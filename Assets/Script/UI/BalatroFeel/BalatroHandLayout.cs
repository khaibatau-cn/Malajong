using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computes fan curves, arc height offsets, and rotation angles for tile hands like in Balatro.
/// </summary>
public class BalatroHandLayout : MonoBehaviour
{
    [Header("Hand Fan Curves")]
    [SerializeField] private float maxFanAngle = 14f;      // Max arc rotation at hand edges
    [SerializeField] private float maxArcHeight = 18f;     // Max arc height lifting center tiles
    [SerializeField] private float cardSpacing = 72f;      // Horizontal spacing between tiles
    [SerializeField] private float smoothLerpSpeed = 16f;

    [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public void ArrangeHand(List<TileUI> tiles)
    {
        if (tiles == null || tiles.Count == 0) return;

        int count = tiles.Count;
        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            TileUI tile = tiles[i];
            if (tile == null) continue;

            float normPos = count > 1 ? (float)i / (count - 1) : 0.5f; // 0.0 to 1.0
            float centeredNorm = (normPos - 0.5f) * 2f;               // -1.0 to +1.0

            // 1. Calculate X Position
            float targetX = startX + (i * cardSpacing);

            // 2. Calculate Arc Y Height (parabola: center is higher)
            float arcY = (1f - (centeredNorm * centeredNorm)) * maxArcHeight;
            if (count < 4) arcY = 0f; // Keep small hands flat

            // 3. Calculate Arc Z Rotation Angle (negative left, positive right)
            float rotZ = -centeredNorm * maxFanAngle;

            // Apply smooth positioning to child Visual/RectTransform
            RectTransform rect = tile.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 targetPos = new Vector3(targetX, arcY, 0f);
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.deltaTime * smoothLerpSpeed);
            }
        }
    }
}
