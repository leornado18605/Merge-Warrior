using System.Collections.Generic;
using UnityEngine;
using ObjectPooling;

public sealed class GridManager : MonoBehaviour
{
    public enum Board { Board1, Board2 }

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int rows = 8;
    public int cols = 8;
    public float tileSize = 1f;
    public float boardGap = 1f;

    [Header("Runtime")]
    [Tooltip("When true the two boards are swapped (board1 <-> board2 positions).")]
    public bool boardsSwapped = false;

    [Header("Debug")]
    public bool drawGizmos = true;

    // Visual tile arrays (persist tile GameObjects so we can move them)
    private GameObject[,] board1Tiles;
    private GameObject[,] board2Tiles;

    // Occupants tracked separately for each board
    private GameObject[,] occupantsB1;
    private GameObject[,] occupantsB2;

    // Origins: world position of tile [0,0] for each board
    private Vector3 board1Origin;
    private Vector3 board2Origin;

    // Public read-only accessors used by other scripts
    public int Rows => rows;
    public int Cols => cols;
    public float TileSize => tileSize;
    public Vector3 Board1Origin => board1Origin;
    public Vector3 Board2Origin => board2Origin;

    private void Awake()
    {
        AllocateArrays();
        PrecreatePools();
        ComputeOrigins();
        SpawnBoards();
    }

    #region Allocation / Pooling / Rebuild

    private void AllocateArrays()
    {
        board1Tiles = new GameObject[rows, cols];
        board2Tiles = new GameObject[rows, cols];
        occupantsB1 = new GameObject[rows, cols];
        occupantsB2 = new GameObject[rows, cols];
    }

    private void PrecreatePools()
    {
        if (tilePrefab == null) return;
        int totalTiles = rows * cols * 2;
        PoolManager.CreatePool(tilePrefab, initialSize: totalTiles, maxSize: totalTiles, autoExpand: false);
    }

    /// <summary>
    /// Rebuild visuals & occupancy arrays (clears existing visuals).
    /// Call after changing rows/cols/tileSize.
    /// </summary>
    public void Rebuild()
    {
        ClearAllVisuals();
        AllocateArrays();
        PrecreatePools();
        ComputeOrigins();
        SpawnBoards();

        // clear occupant buffers
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                occupantsB1[r, c] = null;
                occupantsB2[r, c] = null;
            }
    }

    private void ClearAllVisuals()
    {
        ClearBuffer(board1Tiles);
        ClearBuffer(board2Tiles);
    }

    private void ClearBuffer(GameObject[,] buffer)
    {
        if (buffer == null) return;
        for (int r = 0; r < buffer.GetLength(0); r++)
            for (int c = 0; c < buffer.GetLength(1); c++)
            {
                if (buffer[r, c] != null)
                {
                    PoolManager.Release(buffer[r, c]);
                    buffer[r, c] = null;
                }
            }
    }

    #endregion

    #region Origins & Spawning

    private void ComputeOrigins()
    {
        // By convention: non-swapped => Board2 at transform.position, Board1 below (-Z)
        float fullDepth = rows * tileSize;

        if (!boardsSwapped)
        {
            board2Origin = transform.position;
            board1Origin = transform.position + new Vector3(0f, 0f, -(fullDepth + boardGap));
        }
        else
        {
            // swapped: Board1 at transform.position, Board2 below (-Z)
            board1Origin = transform.position;
            board2Origin = transform.position + new Vector3(0f, 0f, -(fullDepth + boardGap));
        }
    }

    private void SpawnBoards()
    {
        SpawnSingleBoard(board2Tiles, board2Origin);
        SpawnSingleBoard(board1Tiles, board1Origin);
    }

    private void SpawnSingleBoard(GameObject[,] buffer, Vector3 origin)
    {
        if (tilePrefab == null) return;

        for (int r = 0; r < rows; r++)
        {
            float z = r * tileSize;
            for (int c = 0; c < cols; c++)
            {
                float x = c * tileSize;
                Vector3 pos = origin + new Vector3(x, 0f, z);
                buffer[r, c] = PoolManager.Spawn(tilePrefab, pos, Quaternion.identity, transform);
            }
        }
    }

    #endregion

    #region Coordinate Helpers

    // Board2 (default playboard) helper
    public Vector3 GridToWorldPosition(int row, int col)
    {
        return board2Origin + new Vector3(col * tileSize, 0f, row * tileSize);
    }

    // Board1 helper
    public Vector3 GridToWorldPositionBoard1(int row, int col)
    {
        return board1Origin + new Vector3(col * tileSize, 0f, row * tileSize);
    }

    // Board2 explicit
    public Vector3 GridToWorldPositionBoard2(int row, int col)
    {
        return board2Origin + new Vector3(col * tileSize, 0f, row * tileSize);
    }

    public Vector3 GridToWorldPosition(Board board, int row, int col)
    {
        return (board == Board.Board1) ? GridToWorldPositionBoard1(row, col) : GridToWorldPositionBoard2(row, col);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos) // maps to Board2 origin
    {
        Vector3 local = worldPos - board2Origin;
        int col = Mathf.FloorToInt(local.x / tileSize);
        int row = Mathf.FloorToInt(local.z / tileSize);
        return new Vector2Int(row, col);
    }

    public Vector2Int WorldToGridPosition(Board board, Vector3 worldPos)
    {
        Vector3 local = (board == Board.Board1) ? worldPos - board1Origin : worldPos - board2Origin;
        int col = Mathf.FloorToInt(local.x / tileSize);
        int row = Mathf.FloorToInt(local.z / tileSize);
        return new Vector2Int(row, col);
    }

    #endregion

    #region Validation & Occupancy

    public bool IsValidGridPosition(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }

    // Board-aware emptiness
    public bool IsEmptyCell(Board board, int row, int col)
    {
        if (!IsValidGridPosition(row, col)) return false;
        return GetOccupant(board, row, col) == null;
    }

    // Backwards-compatible IsEmptyCell(row,col) -> checks Board2 by default
    public bool IsEmptyCell(int row, int col) => IsEmptyCell(Board.Board2, row, col);

    public GameObject GetOccupant(Board board, int row, int col)
    {
        if (!IsValidGridPosition(row, col)) return null;
        return (board == Board.Board1) ? occupantsB1[row, col] : occupantsB2[row, col];
    }

    public GameObject GetOccupant(int row, int col) => GetOccupant(Board.Board2, row, col);

    public void SetCellOccupied(Board board, int row, int col, GameObject unitObject)
    {
        if (!IsValidGridPosition(row, col)) return;
        if (board == Board.Board1) occupantsB1[row, col] = unitObject;
        else occupantsB2[row, col] = unitObject;
    }

    // Backwards-compatible SetCellOccupied(row,col,GameObject) uses Board2
    public void SetCellOccupied(int row, int col, GameObject unitObject) => SetCellOccupied(Board.Board2, row, col, unitObject);

    // Boolean overload (compatibility with older code)
    public void SetCellOccupied(int row, int col, bool occupied)
    {
        if (!IsValidGridPosition(row, col)) return;
        occupantsB2[row, col] = occupied ? new GameObject() : null; // placeholder flag object if needed
        if (!occupied) DestroyIfPlaceholder(occupantsB2[row, col]);
    }

    private void DestroyIfPlaceholder(GameObject g)
    {
        if (g == null) return;
        // if placeholder (unnamed or specific), remove safely; keep real GameObjects intact
        if (g.name == "Placeholder") Destroy(g);
    }

    public void ClearOccupants()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                occupantsB1[r, c] = null;
                occupantsB2[r, c] = null;
            }
    }

    #endregion

    #region Neighbors / Utilities

    public List<GameObject> GetNeighbors(Board board, int row, int col)
    {
        var list = new List<GameObject>(4);
        if (!IsValidGridPosition(row, col)) return list;

        int[] ro = { -1, 1, 0, 0 };
        int[] co = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + ro[i];
            int nc = col + co[i];
            if (!IsValidGridPosition(nr, nc)) continue;

            var occ = GetOccupant(board, nr, nc);
            if (occ != null) list.Add(occ);
        }

        return list;
    }

    // Backwards-compatible GetNeighbors(row,col) -> Board2
    public List<GameObject> GetNeighbors(int row, int col) => GetNeighbors(Board.Board2, row, col);

    #endregion

    #region Swap Boards (visual move without respawn)

    /// <summary>
    /// Toggle visual positions of Board1 and Board2. Moves tiles & occupant GameObjects to the swapped origins.
    /// </summary>
    public void ToggleSwapBoards()
    {
        // flip flag
        boardsSwapped = !boardsSwapped;

        // recompute origins for new layout
        ComputeOrigins();

        // reposition all tile visuals
        RepositionTiles(board2Tiles, board2Origin);
        RepositionTiles(board1Tiles, board1Origin);

        // reposition occupant GameObjects (if any)
        RepositionOccupants(occupantsB2, board2Origin);
        RepositionOccupants(occupantsB1, board1Origin);
    }

    private void RepositionTiles(GameObject[,] buffer, Vector3 origin)
    {
        if (buffer == null) return;
        for (int r = 0; r < buffer.GetLength(0); r++)
            for (int c = 0; c < buffer.GetLength(1); c++)
            {
                var t = buffer[r, c];
                if (t == null) continue;
                Vector3 target = origin + new Vector3(c * tileSize, 0f, r * tileSize);
                t.transform.position = target;
            }
    }

    private void RepositionOccupants(GameObject[,] occBuffer, Vector3 origin)
    {
        if (occBuffer == null) return;
        for (int r = 0; r < occBuffer.GetLength(0); r++)
            for (int c = 0; c < occBuffer.GetLength(1); c++)
            {
                var obj = occBuffer[r, c];
                if (obj == null) continue;
                Vector3 target = origin + new Vector3(c * tileSize, obj.transform.position.y, r * tileSize);
                obj.transform.position = target;
            }
    }

    #endregion

    #region Clear & Helpers

    public void ClearBoard(Board board)
    {
        var buffer = board == Board.Board1 ? board1Tiles : board2Tiles;
        ClearBuffer(buffer);

        if (board == Board.Board1)
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) occupantsB1[r, c] = null;
        else
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) occupantsB2[r, c] = null;
    }

    public void ClearAll()
    {
        ClearBoard(Board.Board1);
        ClearBoard(Board.Board2);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!drawGizmos || rows <= 0 || cols <= 0) return;

        // preview origins when not playing use current transform arrangement
        float fullDepth = rows * tileSize;
        Vector3 previewB2 = boardsSwapped ? (transform.position + new Vector3(0f, 0f, -(fullDepth + boardGap))) : transform.position;
        Vector3 previewB1 = boardsSwapped ? transform.position : (transform.position + new Vector3(0f, 0f, -(fullDepth + boardGap)));

        DrawBoardGizmos(previewB1, Color.cyan);
        DrawBoardGizmos(previewB2, Color.green);
    }

    private void DrawBoardGizmos(Vector3 origin, Color color)
    {
        Gizmos.color = color;
        Vector3 size = new Vector3(tileSize, 0.05f, tileSize);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 p = origin + new Vector3(c * tileSize, 0f, r * tileSize);
                Gizmos.DrawWireCube(p, size);
            }
        }
    }

    #endregion
}
