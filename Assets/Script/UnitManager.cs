using ObjectPooling;
using Unity.VisualScripting;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    [SerializeField] private GridManager gridManager;

    [Header("Unit Prefabs")]
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private GameObject gunPrefab;

    // pool sizes
    [SerializeField] private int poolInitial = 8;
    [SerializeField] private int poolMax = 32;

    [SerializeField] private Unit knifeUnit;
    [SerializeField] private Unit gunUnit;

    private void Awake()
    {
        Instance = this;
        TryAssignGrid();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        TryAssignGrid();
    }
    
    private void TryAssignGrid()
    {
        gridManager = GridManager.Instance;  // fallback
    }

    private void Start()
    {
        PoolManager.CreatePool(knifePrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
        PoolManager.CreatePool(gunPrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
       // PlaceKnife();
    }

    
    public void PlaceKnife()
    {
        var board = GridManager.Board.Board1;
        Vector2Int cell = FindEmptyCellForKnife(board);

        if (cell.x < 0 || cell.y < 0)
            return; 

        Vector3 pos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);

        GameObject knife = SpawnUnit(knifePrefab, pos, "Knife", 1, board, cell.x, cell.y);
        SetupUnitTeam(knife, Team.Player);
    }

    private Vector2Int FindEmptyCellForKnife(GridManager.Board board)
    {
        for (int row = gridManager.Rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < gridManager.Cols; col++)
            {
                if (gridManager.IsEmptyCell(board, row, col))
                    return new Vector2Int(row, col);
            }
        }
        return new Vector2Int(-1, -1);
    }

    public void PlaceGun()
    {
        var board = GridManager.Board.Board1;
        Vector2Int cell = FindFirstEmptyCell(board);

        if (cell.x < 0 || cell.y < 0)
            return;

        Vector3 pos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);

        GameObject gun = SpawnUnit(gunPrefab, pos, "Gun", 1, board, cell.x, cell.y);
        SetupUnitTeam(gun, Team.Player);
    }
    
    private Vector2Int FindFirstEmptyCell(GridManager.Board board)
    {
        for (int row = 0; row < gridManager.Rows; row++)
        {
            for (int col = gridManager.Cols - 1; col >= 0; col--)
            {
                if (gridManager.IsEmptyCell(board, row, col))
                    return new Vector2Int(row, col);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private GameObject SpawnUnit(GameObject prefab, Vector3 pos, string type, int level,
        GridManager.Board board, int row, int col)
    {
        GameObject obj = PoolManager.Spawn(prefab, pos, Quaternion.identity, gridManager.transform);
        if (!obj) return null;

        obj.SetActive(false);

        Unit u = obj.GetComponent<Unit>();
        if (u)
        {
            u.Initialize(type, level, gridManager, board, row, col);
            gridManager.SetCellOccupied(board, row, col, obj);
            GameManager.Instance.HookUnit(u);
        }

        obj.SetActive(true);
        return obj;
    }

    private void SetupUnitTeam(GameObject unitObj, Team team)
    {
        if (!unitObj) return;

        var unit = unitObj.GetComponent<Unit>();
        if (unit && unit.core)
            unit.core.team = team;

        var unitTeam = unitObj.GetComponent<UnitTeam>() ?? unitObj.AddComponent<UnitTeam>();
        unitTeam.team = team;

        unitObj.tag = team == Team.Player ? "Player" : "Enemy";
    }

}
