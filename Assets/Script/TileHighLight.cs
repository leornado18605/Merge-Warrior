using UnityEngine;

[DisallowMultipleComponent]
public class TileHighlight : MonoBehaviour
{
    [Header("Assign explicitly (no GetComponent)")]
    [SerializeField] private Renderer targetRenderer;  

    private MaterialPropertyBlock mpb;  
    private bool isOn;
    private Color lastColor = Color.black;

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

        EnsureMpb();       
        PrepareMaterial();
        Apply(Color.black, false);
    }

    private void EnsureMpb()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    private void PrepareMaterial()
    {
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
