using UnityEditor;
using UnityEngine;

/// <summary>
/// Play-mode shortcuts for states that are hard to reach by playing normally.
///
/// A deadlock is rare by design, which makes it exactly the sort of path that ships untested — this
/// puts it one menu click away. Editor-only; nothing here is compiled into a build.
/// </summary>
public static class MalajongDebugMenu
{
    [MenuItem("Malajong/Debug/Force Dead Hand %#d", validate = true)]
    private static bool ValidateForceDeadHand()
    {
        return Application.isPlaying && Object.FindAnyObjectByType<GameManager>() != null;
    }

    /// <summary>Ctrl+Shift+D during Play. Deals an unplayable hand with zero discards, firing the redraw prompt.</summary>
    [MenuItem("Malajong/Debug/Force Dead Hand %#d")]
    private static void ForceDeadHand()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[MalajongDebugMenu] No GameManager in the scene.");
            return;
        }

        gm.DebugForceDeadHand();
    }
}
