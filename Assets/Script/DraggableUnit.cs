using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class DraggableUnit : MonoBehaviour
{
    #region References
    [Header("References")]
    [SerializeField] private Unit unit;
    [SerializeField] private NavMeshAgent agent;

    private GridManager Grid
    {
        get { return unit != null ? unit.Grid : null; }
    }
    #endregion

    #region Highlight Settings
    [Header("Highlight (raycast)")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private float raycastMaxDist = 200f;
    [SerializeField] private Color hoverColor = Color.cyan;
    #endregion

    #region Drag State
    private bool isDragging = false;
    private Vector3 originalPosition;
    private int originalRow = -1;
    private int originalCol = -1;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        // All references assigned in Inspector
    }
    #endregion

    #region Mouse Events
    private void OnMouseDown()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject()) return;

        if (unit == null || Grid == null) return;
        if (Grid.IsInputLocked(unit.Board)) return;

        SaveOriginalPosition();
        ReleaseFromCell();

        isDragging = true;
        if (agent != null) agent.updatePosition = false;

    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        UpdateDragPosition();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (unit == null || Grid == null) { Revert(); return; }

        HandleDrop();
    }
    #endregion

    #region Drag Logic
    private void SaveOriginalPosition()
    {
        originalPosition = transform.position;
        originalRow = unit.row;
        originalCol = unit.col;
    }

    private void ReleaseFromCell()
    {
        if (Grid.IsValidGridPosition(originalRow, originalCol))
        {
            Grid.SetCellOccupied(unit.Board, originalRow, originalCol, null);
        }
    }

    private void UpdateDragPosition()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        transform.position = new Vector3(mousePos.x, originalPosition.y, mousePos.z);
    }

    private void HandleDrop()
    {
        Vector2Int gridPos = Grid.WorldToGridNearest(unit.Board, transform.position);
        int row = gridPos.x;
        int col = gridPos.y;

        if (!Grid.IsValidGridPosition(row, col)) { Revert(); return; }

        GameObject occupant = Grid.GetOccupant(unit.Board, row, col);

        if (occupant == null) { PlaceIntoEmptyCell(unit.Board, row, col); return; }

        if (GameManager.Instance != null && GameManager.Instance.TryMerge(unit.Board, row, col, gameObject)) return;

        if (GameManager.Instance != null && GameManager.Instance.TrySwap(unit.Board, row, col, gameObject))
        {
            SyncToCurrentGridCell();
            return;
        }

        RestoreAgent();
        Revert();
    }
    #endregion

    #region Placement
    private void PlaceIntoEmptyCell(GridManager.Board board, int row, int col)
    {
        Vector3 cellCenter = Grid.GridToWorldPosition(board, row, col, true);

        transform.position = cellCenter;
        Grid.SetCellOccupied(board, row, col, gameObject);
        unit.row = row;
        unit.col = col;

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

        if (unit != null && unit.Grid != null &&
            unit.Grid.IsValidGridPosition(originalRow, originalCol))
        {
            unit.Grid.SetCellOccupied(unit.Board, originalRow, originalCol, gameObject);
            unit.row = originalRow;
            unit.col = originalCol;
        }
        else if (unit != null)
        {
            unit.row = -1;
            unit.col = -1;
        }

        SyncAgent(transform.position);
    }
    #endregion

    #region Agent Helpers
    private void SyncAgent(Vector3 worldPos)
    {
        if (agent == null) return;
        agent.Warp(worldPos);
        agent.ResetPath();
    }

    private void RestoreAgent()
    {
        if (agent == null) return;
        agent.updatePosition = true;
        agent.Warp(transform.position);
        agent.ResetPath();
    }
    #endregion

    #region Mouse Position
    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        float enter;
        if (plane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter);
        }

        return transform.position;
    }
    #endregion
}
