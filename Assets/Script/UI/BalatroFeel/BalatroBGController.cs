using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates and controls the iconic Balatro swirling vortex background shader.
/// </summary>
public class BalatroBGController : MonoBehaviour
{
    [Header("Shader Properties")]
    [SerializeField] private Material bgMaterial;
    [SerializeField] private float spinSpeed = 0.8f;
    [SerializeField] private float swirlFrequency = 3.5f;

    [Header("Colors Palette")]
    [SerializeField] private Color primaryColor = new Color(0.12f, 0.22f, 0.45f, 1f);   // Deep Balatro Blue
    [SerializeField] private Color secondaryColor = new Color(0.85f, 0.25f, 0.35f, 1f); // Vibrant Red/Crimson
    [SerializeField] private Color accentColor = new Color(0.08f, 0.08f, 0.18f, 1f);

    private Image bgImage;
    private Material instancedMaterial;

    private void Awake()
    {
        bgImage = GetComponent<Image>();
        if (bgImage != null && bgImage.material != null)
        {
            instancedMaterial = new Material(bgImage.material);
            bgImage.material = instancedMaterial;
        }
        else if (bgMaterial != null)
        {
            instancedMaterial = new Material(bgMaterial);
            if (bgImage != null) bgImage.material = instancedMaterial;
        }
    }

    private void Update()
    {
        if (instancedMaterial == null) return;

        // Animate shader properties over time
        float timeVal = Time.time * spinSpeed;
        instancedMaterial.SetFloat("_SpinSpeed", spinSpeed);
        instancedMaterial.SetFloat("_SwirlFreq", swirlFrequency);
        instancedMaterial.SetColor("_Color1", primaryColor);
        instancedMaterial.SetColor("_Color2", secondaryColor);
    }

    public void SetPalette(Color color1, Color color2)
    {
        primaryColor = color1;
        secondaryColor = color2;
    }
}
