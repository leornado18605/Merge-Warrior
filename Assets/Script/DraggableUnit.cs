using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems; // For IsPointerOverGameObject

[DisallowMultipleComponent]
public class DraggableUnit : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Unit unit;

    private GridManager Grid => unit != null ? unit.Grid : null;

    // Drag state
    private bool isDragging = false;
    private Vector3 originalPosition;
    private int originalRow = -1, originalCol = -1;

    private NavMeshAgent agent;

    private void Awake()
    {
        if (!unit) unit = GetComponent<Unit>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (unit == null || Grid == null) return;
        if (Grid.IsInputLocked(unit.Board)) return;

        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;

        var board = unit.Board;
        if (Grid.IsValidGridPosition(originalRow, originalCol))
            Grid.SetCellOccupied(board, originalRow, originalCol, null);

        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        transform.position = new Vector3(mousePos.x, originalPosition.y, mousePos.z);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (unit == null || Grid == null) { Revert(); return; }

        var board = unit.Board;
        Vector2Int gridPos = Grid.WorldToGridNearest(board, transform.position);
        int row = gridPos.x;
        int col = gridPos.y;

        if (!Grid.IsValidGridPosition(row, col)) { Revert(); return; }

        GameObject occupant = Grid.GetOccupant(board, row, col);

        if (occupant == null)
        {
            PlaceIntoEmptyCell(board, row, col);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.TrySwap(board, row, col, gameObject))
        {
            SyncToCurrentGridCell();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.TryMerge(board, row, col, gameObject))
        {
            return;
        }

        Revert();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    private void PlaceIntoEmptyCell(GridManager.Board board, int row, int col)
    {
        Vector3 cellCenter = Grid.GridToWorldPosition(board, row, col, true);

        transform.position = cellCenter;
        Grid.SetCellOccupied(board, row, col, gameObject);
        unit.row = row; unit.col = col;

        unit.UpdateOriginalPosition(cellCenter, row, col);
        SyncAgent(cellCenter);
    }

    private void SyncToCurrentGridCell()
    {
        if (unit == null || Grid == null) return;

        Vector3 cellCenter = Grid.GridToWorldPosition(unit.Board, unit.row, unit.col, true);
        transform.position = cellCenter;

        unit.UpdateOriginalPosition(cellCenter, unit.row, unit.col);
        SyncAgent(cellCenter);
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

        SyncAgent(transform.position);
    }

    private void SyncAgent(Vector3 worldPos)
    {
        if (agent == null) return;

        agent.Warp(worldPos);
        agent.ResetPath();
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
