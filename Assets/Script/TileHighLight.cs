using UnityEngine;

[DisallowMultipleComponent]
public class TileHighlight : MonoBehaviour
{
    [Header("Auto assign nếu để trống")]
    [SerializeField] private Renderer targetRenderer;

    private string colorProperty = "_Color"; // mặc định Standard
    private Color originalColor;
    private bool hasOriginal = false;

    // cache property block
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError($"[TileHighlight] ❌ Không tìm thấy Renderer trên {name}");
            enabled = false;
            return;
        }

        if (targetRenderer.sharedMaterial.HasProperty("_Color"))
            colorProperty = "_Color";

        // Lưu màu gốc
        originalColor = targetRenderer.material.GetColor(colorProperty);
        hasOriginal = true;

        // init property block
        mpb = new MaterialPropertyBlock();

        Debug.Log($"[TileHighlight] 🎨 {name} dùng property: {colorProperty}, màu gốc: {originalColor}");
    }

    public void SetHighlight(bool enable, Color color)
    {
        if (!hasOriginal) return;

        targetRenderer.GetPropertyBlock(mpb);

        if (enable)
        {
            Color neonGreen = new Color(0f, 1f, 0f, 1f);
            mpb.SetColor("_EmissionColor", neonGreen * 5f); // nhân 5 để sáng mạnh
            Debug.Log($"✨ [TileHighlight] {name} → Highlight xanh neon {neonGreen}");
        }
        else
        {
            mpb.SetColor("_EmissionColor", Color.black);
            Debug.Log($"🔙 [TileHighlight] {name} → Tắt highlight");
        }

        targetRenderer.SetPropertyBlock(mpb);
    }

    public void Clear()
    {
        if (!hasOriginal) return;

        targetRenderer.material.SetColor(colorProperty, originalColor);

        // tắt emission
        mpb.SetColor("_EmissionColor", Color.black);
        targetRenderer.SetPropertyBlock(mpb);

        Debug.Log($"🧹 [TileHighlight] {name} → Clear (reset {originalColor})");
    }
}
