using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitType;
    public int level;
    public int row;
    public int col;

    private Vector3 originalPosition;

    public void Initialize(string unitType, int level, GridManager gridManager, int row, int col)
    {
        this.unitType = unitType;
        this.level = level;
        this.row = row;
        this.col = col;

        originalPosition = gridManager.GridToWorldPosition(row, col);
        transform.position = originalPosition;
    }

    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
}
