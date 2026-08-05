using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared UI game-feel helpers: hitstop freeze frames, impact-scaled screen shake, and
/// pop-in scaling so no panel or element ever appears instantly.
///
/// Everything here runs on unscaled time so it keeps animating during a hitstop freeze.
/// </summary>
public class UIJuice : MonoBehaviour
{
    public static UIJuice Instance { get; private set; }

    [Header("Hitstop")]
    [Range(0f, 0.5f)] public float MaxHitstopDuration = 0.11f;

    [Header("Screen Shake")]
    public float MaxShakeAmplitude = 24f;
    public float ShakeFrequency = 46f;

    [Header("Pop-In")]
    public float PopInDuration = 0.18f;
    public float PopInStartScale = 0.85f;

    private Coroutine activeHitstop;
    private readonly Dictionary<RectTransform, Coroutine> activeShakes = new Dictionary<RectTransform, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        // A scene change mid-hitstop would otherwise kill the coroutine before it restores
        // timeScale, leaving the whole game frozen.
        if (Instance == this && activeHitstop != null) Time.timeScale = 1f;
    }

    /// <summary>
    /// Returns the singleton, creating it at runtime if the scene has not been re-run
    /// through SceneSetupTool. Lets every call site stay a one-liner.
    /// </summary>
    public static UIJuice EnsureExists()
    {
        if (Instance == null)
        {
            GameObject host = new GameObject("UIJuice");
            host.AddComponent<UIJuice>();
        }
        return Instance;
    }

    // --- Hitstop -----------------------------------------------------------

    /// <summary>
    /// Freezes scaled time briefly so a big score lands with weight. Duration is clamped
    /// to MaxHitstopDuration; anything longer reads as a frame hitch rather than impact.
    /// </summary>
    public static void Hitstop(float duration)
    {
        UIJuice juice = EnsureExists();
        if (juice.activeHitstop != null) juice.StopCoroutine(juice.activeHitstop);
        juice.activeHitstop = juice.StartCoroutine(juice.HitstopRoutine(Mathf.Min(duration, juice.MaxHitstopDuration)));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        // Never latch onto an already-frozen scale, or an overlapping hitstop would
        // restore 0 and lock the game permanently.
        float restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = restoreScale;

        activeHitstop = null;
    }

    // --- Screen shake ------------------------------------------------------

    /// <summary>
    /// Shakes a UI container. Pass a child of the Canvas, not the Canvas itself —
    /// Unity overrides the RectTransform of a Screen Space Overlay canvas every frame.
    /// </summary>
    /// <param name="strength">0..1, normally scoreGained / targetScore.</param>
    public static void Shake(RectTransform target, float strength, float duration = 0.3f)
    {
        if (target == null) return;

        UIJuice juice = EnsureExists();
        if (juice.activeShakes.TryGetValue(target, out Coroutine running) && running != null)
        {
            juice.StopCoroutine(running);
        }
        juice.activeShakes[target] = juice.StartCoroutine(juice.ShakeRoutine(target, Mathf.Clamp01(strength), duration));
    }

    private IEnumerator ShakeRoutine(RectTransform target, float strength, float duration)
    {
        Vector2 origin = target.anchoredPosition;
        float amplitude = MaxShakeAmplitude * strength;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / duration);

            float offsetX = Mathf.Sin(elapsed * ShakeFrequency) * amplitude * decay;
            float offsetY = Mathf.Cos(elapsed * ShakeFrequency * 1.3f) * amplitude * decay * 0.6f;
            target.anchoredPosition = origin + new Vector2(offsetX, offsetY);

            yield return null;
        }

        target.anchoredPosition = origin;
        activeShakes.Remove(target);
    }

    // --- Pop-in ------------------------------------------------------------

    /// <summary>Scales an element up from small with an overshoot settle. Nothing should ever just appear.</summary>
    public static void PopIn(Transform target, float delay = 0f)
    {
        if (target == null) return;
        UIJuice juice = EnsureExists();
        juice.StartCoroutine(juice.PopInRoutine(target, delay));
    }

    /// <summary>Pop-in for a group, staggered so items arrive in sequence rather than all at once.</summary>
    public static void PopInStaggered(IEnumerable<Transform> targets, float stagger = 0.04f)
    {
        if (targets == null) return;

        int index = 0;
        foreach (Transform target in targets)
        {
            if (target == null) continue;
            PopIn(target, index * stagger);
            index++;
        }
    }

    private IEnumerator PopInRoutine(Transform target, float delay)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.one * PopInStartScale;
        target.localScale = startScale;

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;
        while (elapsed < PopInDuration)
        {
            if (target == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PopInDuration);
            target.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, EaseOutBack(t));

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    /// <summary>Overshoots past the target then settles exactly on it — the "back out" curve.</summary>
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
