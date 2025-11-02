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
    }

    private void HandleMerged(Unit u, int row, int col)
    {
        if (u == null) return;
        
        if (u.level <= 1) return;

        if (NewUnitRevealUI.Instance == null)
        {
            return;
        }

        NewUnitRevealUI.Instance.Enqueue(u);
    }
}
