using UnityEngine;

[DisallowMultipleComponent]
public class TileHighlight : MonoBehaviour
{
    [Header("Assign explicitly (no GetComponent)")]
    [SerializeField] private Renderer targetRenderer;   // GÁN SẴN TRONG INSPECTOR

    private MaterialPropertyBlock mpb;  // <-- KHÔNG khởi tạo ở đây
    private bool isOn;
    private Color lastColor = Color.black;

    // Cho phép gán bằng code khi spawn
    public void Init(Renderer rendererRef)
    {
        targetRenderer = rendererRef;
        EnsureMpb();
        PrepareMaterial();
        Apply(Color.black, false);
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            Debug.LogError($"[TileHighlight] Missing Renderer on {name}. " +
                           "Assign via Inspector hoặc gọi Init(renderer) trước khi dùng.");
            enabled = false;
            return;
        }

        EnsureMpb();        // <-- tạo MPB đúng chỗ
        PrepareMaterial();
        Apply(Color.black, false);
    }

    private void EnsureMpb()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    private void PrepareMaterial()
    {
        // Bật keyword emission nếu material hỗ trợ
        if (targetRenderer != null)
            targetRenderer.material.EnableKeyword("_EMISSION");
    }

    public void SetHighlight(bool enable, Color color)
    {
        isOn = enable;
        lastColor = color;
        Apply(color, enable);
    }

    public void Clear() => SetHighlight(false, Color.black);

    private void Apply(Color c, bool enable)
    {
        if (targetRenderer == null) return;
        EnsureMpb();

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", enable ? c : Color.black);
        targetRenderer.SetPropertyBlock(mpb);

        if (enable) targetRenderer.material.EnableKeyword("_EMISSION");
        else targetRenderer.material.DisableKeyword("_EMISSION");
    }
}
