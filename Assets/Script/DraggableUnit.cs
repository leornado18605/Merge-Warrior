using UnityEngine;

public class DraggableUnit : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    private Vector3 originalPosition;
    private int originalRow = -1;
    private int originalCol = -1;

    [SerializeField] private Unit unit;
    private GridManager Grid => unit != null ? unit.Grid : null;

    void OnMouseDown()
    {
        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;

        if (Grid != null && Grid.IsValidGridPosition(originalRow, originalCol))
            Grid.SetCellOccupied(originalRow, originalCol, null);

        offset = transform.position - GetMouseWorldPosition();
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 newPosition = new Vector3(mouseWorldPos.x + offset.x, transform.position.y, mouseWorldPos.z + offset.z);
        transform.position = newPosition;
    }

    void OnMouseUp()
    {
        if (!isDragging)  return; 
        isDragging = false;

        if (unit == null)   return; 
        if (Grid == null) {  Revert(); return; }

        Vector2Int gridPos = Grid.WorldToGridPosition(transform.position);
        int row = gridPos.x, col = gridPos.y;

        if (!Grid.IsValidGridPosition(row, col))
        {
            Revert();
            return;
        }

        GameObject occupant = Grid.GetOccupant(row, col);

        if (occupant == null)
        {
            // empty cell -> place normally
            Grid.SetCellOccupied(row, col, gameObject);
            unit.row = row; unit.col = col;
            transform.position = Grid.GridToWorldPosition(row, col);
            return;
        }

        if (GameManager.Instance == null)
        {
            Revert();
            return;
        }

        bool merged = GameManager.Instance.TryMerge(GridManager.Board.Board2, row, col, gameObject);

        if (merged)
        {
            return;
        }
        Revert(); 
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

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        if (Camera.main == null) return Vector3.zero; 
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
