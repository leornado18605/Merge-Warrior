using UnityEngine;

public class NewUnitUnlockTracker : MonoBehaviour
{
    [SerializeField] private NewUnitRevealUI revealUI;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnUnitMerged += HandleMerged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnUnitMerged -= HandleMerged;
    }

    private void HandleMerged(Unit u, int row, int col)
    {
        if (u == null || revealUI == null) return;

        revealUI.Enqueue(u);
    }
}
