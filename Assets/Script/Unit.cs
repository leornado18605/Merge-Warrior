using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitType;
    public int level;
    public int row;
    public int col;

    private Vector3 originalPosition;
    private GridManager gridManager;

    private bool mergeLock = false;
    [SerializeField] private float defaultMergeLockSeconds = 0.25f;

    public void Initialize(string unitType, int level, GridManager gridManager, int row, int col)
    {
        this.unitType = unitType;
        this.level = level;
        this.gridManager = gridManager;
        this.row = row;
        this.col = col;
        originalPosition = gridManager.GridToWorldPosition(row, col);
        transform.position = originalPosition;
        gameObject.name = $"{unitType}_L{level}_R{row}C{col}";
    }

    public Vector3 GetOriginalPosition() => originalPosition;
    public GridManager Grid => gridManager;
    public bool IsMergeLocked() => mergeLock;

    public void MergeIncrement()
    {
        level++;
    }

    public void MergeLockTemporary(float seconds = -1f)
    {
        if (seconds <= 0f) seconds = defaultMergeLockSeconds;
        StartCoroutine(MergeLockCoroutine(seconds));
    }

    private IEnumerator MergeLockCoroutine(float seconds)
    {
        mergeLock = true;
        yield return new WaitForSeconds(seconds);
        mergeLock = false;
    }
}
