using UnityEngine;

public class DraggableUnit : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;
    private Vector3 originalPosition;

    [SerializeField] private GridManager gridManager; 
    [SerializeField] private Unit unit;


    void OnMouseDown()
    {
        offset = transform.position - GetMouseWorldPosition();
        isDragging = true;

        originalPosition = transform.position;
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            Vector3 newPosition = new Vector3(mouseWorldPos.x, transform.position.y, mouseWorldPos.z);

            transform.position = newPosition;
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;

            Vector2Int gridPos = gridManager.WorldToGridPosition(transform.position);

            if (gridManager.IsValidGridPosition(gridPos.x, gridPos.y) && gridManager.IsEmptyCell(gridPos.x, gridPos.y))
            {
                gridManager.SetCellOccupied(gridPos.x, gridPos.y, true);

                unit.row = gridPos.x;
                unit.col = gridPos.y;

                CheckForMerge(unit);

                transform.position = gridManager.GridToWorldPosition(gridPos.x, gridPos.y);
            }
            else
            {
                transform.position = unit.GetOriginalPosition();

                gridManager.SetCellOccupied(unit.row, unit.col, false);
            }
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private void CheckForMerge(Unit unitToCheck)
    {
        var neighbors = gridManager.GetNeighbors(unitToCheck.row, unitToCheck.col);
        foreach (var neighbor in neighbors)
        {
            GameObject targetUnit = gridManager.GetTileAt(neighbor.x, neighbor.y);
            if (targetUnit != null)
            {
                Unit targetUnitScript = targetUnit.GetComponent<Unit>();
                if (targetUnitScript != null && targetUnitScript.unitType == unitToCheck.unitType && targetUnitScript.level == unitToCheck.level)
                {
                    gridManager.MergeUnits(unitToCheck, targetUnitScript);
                }
            }
        }
    }
}
