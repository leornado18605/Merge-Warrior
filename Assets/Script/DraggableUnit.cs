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

        if (Grid.IsInputLocked(unit.Board)) return;

        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;
        var board = unit.Board;

        if (Grid.IsValidGridPosition(originalRow, originalCol))
            Grid.SetCellOccupied(originalRow, originalCol, null);

        if (Grid.IsValidGridPosition(originalRow, originalCol))
            Grid.SetCellOccupied(board, originalRow, originalCol, null);
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
        var board = unit.Board;
        Vector2Int gridPos = Grid.WorldToGridNearest(board, transform.position);

        int row = gridPos.x;
        int col = gridPos.y;

        if (!Grid.IsValidGridPosition(row, col))
        {
            Revert();
            return;
        }

        Vector3 cellCenter = Grid.GridToWorldPosition(board, row, col);
        transform.position = cellCenter;

        GameObject occupant = Grid.GetOccupant(board, row, col);
        if (occupant == null)
        {
            Grid.SetCellOccupied(board, row, col, gameObject);
            unit.row = row; unit.col = col;
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.TryMerge(board, row, col, gameObject))
        {
            Revert();
        }
    }

    private void Revert()
    {
        transform.position = originalPosition;
        if (unit != null && unit.Grid != null && unit.Grid.IsValidGridPosition(originalRow, originalCol))
        {
            unit.Grid.SetCellOccupied(unit.Board, originalRow, originalCol, gameObject);
            unit.row = originalRow; unit.col = originalCol;
        }
        else if (unit != null)
        {
            unit.row = -1; unit.col = -1;
        }
    }
    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return transform.position;
    }

}
