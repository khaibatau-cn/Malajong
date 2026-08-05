using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Recreates the iconic 3D card tilt, organic idle wobble, holographic edition reflection,
/// and juicy spring punches from Balatro.
/// </summary>
public class BalatroCardVisual : MonoBehaviour
{
    public enum CardEdition
    {
        Regular,
        Foil,
        Polychrome,
        Negative
    }

    [Header("Card Edition & Shader FX")]
    [SerializeField] private CardEdition edition = CardEdition.Regular;
    [SerializeField] private Material cardShaderMaterial;
    private Material instancedMaterial;

    [Header("3D Tilt Parameters")]
    [SerializeField] private bool enable3DTilt = true;
    [SerializeField] private float manualTiltAmount = 18f;  // Degrees tilt toward mouse cursor
    [SerializeField] private float autoTiltAmount = 2.5f;   // Subtle idle wobble amplitude
    [SerializeField] private float tiltSpeed = 14f;

    [Header("Follow & Movement Lag")]
    [SerializeField] private float rotationAmount = 15f;
    [SerializeField] private float rotationSpeed = 18f;

    [Header("Juicy Spring / Punch")]
    [SerializeField] private float hoverPunchAngle = 6f;
    [SerializeField] private float selectPunchScale = 1.18f;
    [SerializeField] private float scorePunchScale = 1.28f;
    [SerializeField] private float punchRecoverySpeed = 12f;

    [Header("Shadow Offset")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private Vector3 defaultShadowOffset = new Vector3(8f, -12f, 0f);
    [SerializeField] private Vector3 pressedShadowOffset = new Vector3(2f, -4f, 0f);

    // Internal State
    private RectTransform rectTransform;
    private Image cardImage;
    private TileUI parentTileUI;
    private Vector3 targetLocalPosition;
    private Vector3 currentVelocity;
    private Vector3 punchRotationOffset;
    private Vector3 punchScaleOffset = Vector3.one;
    private float seedOffset;

    private Vector3 lastWorldPosition;
    private Vector3 velocityRotation;

    public CardEdition Edition
    {
        get => edition;
        set
        {
            edition = value;
            ApplyEditionKeyword();
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cardImage = GetComponent<Image>();
        parentTileUI = GetComponentInParent<TileUI>();
        seedOffset = Random.Range(0f, 100f);
        lastWorldPosition = transform.position;

        SetupMaterial();
    }

    private void Start()
    {
        if (shadowTransform == null)
        {
            Transform foundShadow = transform.Find("Shadow");
            if (foundShadow != null) shadowTransform = foundShadow;
        }
    }

    private void SetupMaterial()
    {
        if (cardImage != null && cardImage.material != null)
        {
            // Instantiate material clone for unique per-card holographic reflection calculations
            instancedMaterial = new Material(cardImage.material);
            cardImage.material = instancedMaterial;
            ApplyEditionKeyword();
        }
    }

    public void ApplyEditionKeyword()
    {
        if (instancedMaterial == null) return;

        instancedMaterial.DisableKeyword("_EDITION_REGULAR");
        instancedMaterial.DisableKeyword("_EDITION_FOIL");
        instancedMaterial.DisableKeyword("_EDITION_POLYCHROME");
        instancedMaterial.DisableKeyword("_EDITION_NEGATIVE");

        switch (edition)
        {
            case CardEdition.Foil:
                instancedMaterial.EnableKeyword("_EDITION_FOIL");
                break;
            case CardEdition.Polychrome:
                instancedMaterial.EnableKeyword("_EDITION_POLYCHROME");
                break;
            case CardEdition.Negative:
                instancedMaterial.EnableKeyword("_EDITION_NEGATIVE");
                break;
            default:
                instancedMaterial.EnableKeyword("_EDITION_REGULAR");
                break;
        }
    }

    private void Update()
    {
        Update3DTiltAndWobble();
        UpdateShaderReflection();
        UpdateSpringPunchRecovery();
    }

    private void Update3DTiltAndWobble()
    {
        if (!enable3DTilt) return;

        bool isHovered = parentTileUI != null && IsTileHovered();
        bool isSelected = parentTileUI != null && parentTileUI.IsSelected;

        // 1. Calculate Mouse Relative Position
        Vector3 mouseScreenPos = GetMouseScreenPosition();
        Vector3 tileScreenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position) : transform.position;
        
        float deltaX = (mouseScreenPos.x - tileScreenPos.x) / (Screen.width * 0.5f);
        float deltaY = (mouseScreenPos.y - tileScreenPos.y) / (Screen.height * 0.5f);

        deltaX = Mathf.Clamp(deltaX, -1f, 1f);
        deltaY = Mathf.Clamp(deltaY, -1f, 1f);

        // 2. Compute 3D Tilt Angles
        float tiltX = isHovered ? (-deltaY * manualTiltAmount) : 0f;
        float tiltY = isHovered ? (deltaX * manualTiltAmount) : 0f;

        // 3. Idle Sine-Wave Breathing (Organic Balatro Wobble)
        float sineWobble = Mathf.Sin((Time.time * 2.5f) + seedOffset) * (isHovered ? 0.3f : 1f);
        float cosineWobble = Mathf.Cos((Time.time * 2.0f) + seedOffset) * (isHovered ? 0.3f : 1f);

        float targetRotX = tiltX + (sineWobble * autoTiltAmount);
        float targetRotY = tiltY + (cosineWobble * autoTiltAmount);

        // 4. Movement Velocity Roll
        Vector3 posDelta = (transform.position - lastWorldPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        lastWorldPosition = transform.position;
        
        float targetRoll = Mathf.Clamp(-posDelta.x * 0.002f * rotationAmount, -25f, 25f);
        velocityRotation.z = Mathf.Lerp(velocityRotation.z, targetRoll, Time.deltaTime * rotationSpeed);

        // 5. Smoothly Interpolate Euler Rotation
        Vector3 currentEuler = transform.localEulerAngles;
        float currentX = FixAngle(currentEuler.x);
        float currentY = FixAngle(currentEuler.y);
        float currentZ = FixAngle(currentEuler.z);

        float nextX = Mathf.LerpAngle(currentX, targetRotX + punchRotationOffset.x, Time.deltaTime * tiltSpeed);
        float nextY = Mathf.LerpAngle(currentY, targetRotY + punchRotationOffset.y, Time.deltaTime * tiltSpeed);
        float nextZ = Mathf.LerpAngle(currentZ, velocityRotation.z + punchRotationOffset.z, Time.deltaTime * tiltSpeed);

        transform.localRotation = Quaternion.Euler(nextX, nextY, nextZ);

        // Shadow positioning updates
        if (shadowTransform != null)
        {
            Vector3 targetShadow = isHovered || isSelected ? defaultShadowOffset * 1.4f : defaultShadowOffset;
            shadowTransform.localPosition = Vector3.Lerp(shadowTransform.localPosition, targetShadow, Time.deltaTime * 15f);
        }
    }

    private void UpdateShaderReflection()
    {
        if (instancedMaterial == null) return;

        Vector3 currentEuler = transform.localEulerAngles;
        float xAngle = FixAngle(currentEuler.x);
        float yAngle = FixAngle(currentEuler.y);

        // Remap rotation angles to -0.5..0.5 range expected by CardShaderGraph
        float remapX = Remap(xAngle, -30f, 30f, -0.5f, 0.5f);
        float remapY = Remap(yAngle, -30f, 30f, -0.5f, 0.5f);

        instancedMaterial.SetVector("_Rotation", new Vector2(remapX, remapY));
    }

    private void UpdateSpringPunchRecovery()
    {
        // Decaying spring punch offset back to zero
        punchRotationOffset = Vector3.Lerp(punchRotationOffset, Vector3.zero, Time.deltaTime * punchRecoverySpeed);
        punchScaleOffset = Vector3.Lerp(punchScaleOffset, Vector3.one, Time.deltaTime * punchRecoverySpeed);
    }

    public void TriggerHoverPunch()
    {
        float randomDir = Random.value > 0.5f ? 1f : -1f;
        punchRotationOffset = new Vector3(0f, 0f, hoverPunchAngle * randomDir);
        punchScaleOffset = Vector3.one * 1.08f;
    }

    public void TriggerSelectPunch()
    {
        float randomDir = Random.value > 0.5f ? 1f : -1f;
        punchRotationOffset = new Vector3(8f, 0f, 12f * randomDir);
        punchScaleOffset = Vector3.one * selectPunchScale;
    }

    public void TriggerScorePunch()
    {
        punchRotationOffset = new Vector3(-15f, 0f, 0f);
        punchScaleOffset = Vector3.one * scorePunchScale;
    }

    private bool IsTileHovered()
    {
        if (parentTileUI == null) return false;
        var field = typeof(TileUI).GetField("isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(parentTileUI);
    }

    private static Vector3 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 pos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
#endif
        try
        {
            return Input.mousePosition;
        }
        catch
        {
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
    }

    private static float FixAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private static float Remap(float val, float from1, float to1, float from2, float to2)
    {
        return (val - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
