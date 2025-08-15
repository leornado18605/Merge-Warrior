using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitType;
    public int level;
    public int row;
    public int col;

    private Vector3 originalPosition;
    private GridManager gridManager;

    public void Initialize(string unitType, int level, GridManager gridManager, int row, int col)
    {
        this.unitType = unitType;
        this.level = level;
        this.row = row;
        this.col = col;
        this.gridManager = gridManager;

        originalPosition = gridManager.GridToWorldPosition(row, col);
        transform.position = originalPosition;
    }

    public Vector3 GetOriginalPosition() => originalPosition;

    public GridManager Grid => gridManager;
}
