using UnityEngine;

public class SceneRefs : MonoBehaviour
{
    [SerializeField] private GridManager grid;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BotManager botManager;
    [SerializeField] private EconomyUIBinder economyUI;
    private void Awake()
    {
        if (economyUI != null) economyUI.Rebind();

    }
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSceneReady(
                grid,
                unitManager,
                uiManager,
                botManager);
        }


    }
}
