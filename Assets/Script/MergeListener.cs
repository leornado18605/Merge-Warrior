using System.Collections;
using UnityEngine;

public class MergeRevealListener : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnUnitMerged -= HandleMerged;
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (GameManager.Instance == null) yield return null;
        GameManager.Instance.OnUnitMerged -= HandleMerged; 
        GameManager.Instance.OnUnitMerged += HandleMerged;
        Debug.Log("[Reveal] Subscribed to GameManager.OnUnitMerged");
    }

    private void HandleMerged(Unit u, int row, int col)
    {
        if (u == null) return;

        Debug.Log($"[Reveal] OnUnitMerged -> {u.unitType} L{u.level}");

        // Nếu bạn chỉ muốn popup cho level >= 2:
        if (u.level <= 1) return;

        if (NewUnitRevealUI.Instance == null)
        {
            Debug.LogWarning("[Reveal] NewUnitRevealUI.Instance is NULL. Check your UI prefab in scene!");
            return;
        }

        NewUnitRevealUI.Instance.Enqueue(u);
    }
}
