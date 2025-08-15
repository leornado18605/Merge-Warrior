using UnityEngine;

public class DraggableUnit : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 originalPosition;
    private int originalRow = -1, originalCol = -1;

    [SerializeField] private Unit unit;
    private GridManager Grid => unit != null ? unit.Grid : null;

    void OnMouseDown()
    {
        if (unit == null || Grid == null) return;

        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;

        if (Grid.IsValidGridPosition(originalRow, originalCol))
            Grid.SetCellOccupied(originalRow, originalCol, null);

        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        transform.position = new Vector3(mousePos.x, originalPosition.y, mousePos.z);
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (unit == null || Grid == null)
        {
            Revert();
            return;
        }

        Vector2Int gridPos = Grid.WorldToGridPosition(transform.position);
        int row = gridPos.x;
        int col = gridPos.y;

        if (!Grid.IsValidGridPosition(row, col))
        {
            Revert();
            return;
        }

        Vector3 cellCenter = Grid.GridToWorldPosition(row, col);
        transform.position = cellCenter;

        GameObject occupant = Grid.GetOccupant(row, col);
        if (occupant == null)
        {
            Grid.SetCellOccupied(row, col, gameObject);
            unit.row = row; unit.col = col;
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.TryMerge(GridManager.Board.Board2, row, col, gameObject))
        {
            Revert();
        }
    }

    private void Revert()
    {
        transform.position = originalPosition;
        if (unit != null && unit.Grid != null && unit.Grid.IsValidGridPosition(originalRow, originalCol))
        {
            unit.Grid.SetCellOccupied(originalRow, originalCol, gameObject);
            unit.row = originalRow; unit.col = originalCol;
        }
        else if (unit != null)
        {
            unit.row = -1; unit.col = -1;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        if (Camera.main == null) return Vector3.zero;

        mousePos.z = Vector3.Distance(Camera.main.transform.position, transform.position);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
