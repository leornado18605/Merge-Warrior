using UnityEngine;

public class DraggableUnit : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    private Vector3 originalPosition;
    private int originalRow = -1;
    private int originalCol = -1;

    [SerializeField] private Unit unit;
    private GridManager Grid => unit.Grid;

    void OnMouseDown()
    {
        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;

        if (Grid.IsValidGridPosition(originalRow, originalCol))
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
        isDragging = false;
        Vector2Int gridPos = Grid.WorldToGridPosition(transform.position);
        int row = gridPos.x, col = gridPos.y;

        if (Grid.IsValidGridPosition(row, col) && Grid.IsEmptyCell(row, col))
        {
            Grid.SetCellOccupied(row, col, gameObject);
            unit.row = row; unit.col = col;
            transform.position = Grid.GridToWorldPosition(row, col);
        }
        else
        {
            transform.position = originalPosition;
            Grid.SetCellOccupied(originalRow, originalCol, gameObject);
            unit.row = originalRow; unit.col = originalCol;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
