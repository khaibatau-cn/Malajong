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
    [SerializeField] private Color primaryColor = MalajongTheme.MalachiteDeep;   // Imperial beam green
    [SerializeField] private Color secondaryColor = MalajongTheme.VermilionDeep; // Imperial pillar red
    [SerializeField] private Color accentColor = MalajongTheme.Ink;              // Aged lacquer ground

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
