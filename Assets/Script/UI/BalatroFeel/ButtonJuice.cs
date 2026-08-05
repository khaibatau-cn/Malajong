using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Press/hover spring feedback for any UI Button. Purely a transform effect, so it composes
/// with whatever colour transition the Button itself is using.
///
/// UIManager auto-attaches this to every Button under the canvas at startup, so buttons
/// created by SceneSetupTool pick it up without needing the scene rebuilt.
/// </summary>
[DisallowMultipleComponent]
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Targets")]
    public float HoverScale = 1.05f;
    public float PressedScale = 0.94f;
    public float ReleaseOvershoot = 1.08f;

    [Header("Response")]
    public float SmoothSpeed = 18f;
    public bool PlayHoverSound = true;

    private Vector3 targetScale = Vector3.one;
    private bool isHovered;
    private bool isPressed;

    private void Update()
    {
        // Unscaled so buttons stay responsive during a hitstop freeze.
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * SmoothSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        RefreshTarget();

        if (PlayHoverSound) MalajongAudio.Instance?.PlayTileHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        RefreshTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        // Snap down immediately rather than lerping in — a press should feel instant.
        transform.localScale = Vector3.one * PressedScale;
        RefreshTarget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        // Kick past the resting scale so the release springs back.
        transform.localScale = Vector3.one * ReleaseOvershoot;
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        if (isPressed) targetScale = Vector3.one * PressedScale;
        else if (isHovered) targetScale = Vector3.one * HoverScale;
        else targetScale = Vector3.one;
    }
}
