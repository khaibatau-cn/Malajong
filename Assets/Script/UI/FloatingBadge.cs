using UnityEngine;
using TMPro;

public class FloatingBadge : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private float lifetime = 0.85f;
    private float elapsedTime = 0f;
    private Vector2 moveVelocity = new Vector2(0, 80f);
    private Vector3 initialScale = Vector3.one * 0.4f;
    private Vector3 targetScale = Vector3.one;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public static FloatingBadge Spawn(Transform parent, Vector3 screenPosition, string message, Color textColor, float fontSize = 28f)
    {
        GameObject badgeObj = new GameObject("FloatingBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        badgeObj.transform.SetParent(parent, false);
        badgeObj.transform.position = screenPosition;

        TextMeshProUGUI tmp = badgeObj.GetComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform rect = badgeObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 60);

        FloatingBadge badge = badgeObj.AddComponent<FloatingBadge>();
        return badge;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / lifetime;

        // Punchy scale up and smooth upward drift
        if (t < 0.25f)
        {
            float popT = t / 0.25f;
            transform.localScale = Vector3.LerpUnclamped(initialScale, targetScale * 1.25f, popT);
        }
        else if (t < 0.45f)
        {
            float settleT = (t - 0.25f) / 0.2f;
            transform.localScale = Vector3.Lerp(targetScale * 1.25f, targetScale, settleT);
        }

        rectTransform.anchoredPosition += moveVelocity * Time.deltaTime;

        // Fade out
        if (t > 0.5f)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
        }

        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
