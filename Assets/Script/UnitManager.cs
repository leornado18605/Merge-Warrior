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

    public void SetGrid(GridManager g)
    {
        if (g != null) gridManager = g;
    }

    private void TryAssignGrid()
    {
        gridManager = GridManager.Instance;  // fallback
    }

    private void Start()
    {
        PoolManager.CreatePool(knifePrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
        PoolManager.CreatePool(gunPrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
        PlaceKnife();
    }

    public void PlaceKnife()
    {

        var board = GridManager.Board.Board1;

        for (int row = gridManager.Rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < gridManager.Cols; col++)
            {
                if (!gridManager.IsEmptyCell(board, row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(board, row, col, true);

                GameObject knife = PoolManager.Spawn(knifePrefab, worldPos, Quaternion.identity, gridManager.transform);
                var u = knife.GetComponent<Unit>();
                knife.SetActive(false);
                
                knife.GetComponent<Unit>().Initialize("Knife", 1, gridManager, board, row, col);

                gridManager.SetCellOccupied(board, row, col, knife);
                GameManager.Instance.HookUnit(u);

                var core = u ? u.core : null;
                if (core) core.team = Team.Player;

                var team = knife.GetComponent<UnitTeam>();
                if (team == null) team = knife.AddComponent<UnitTeam>();
                team.team = Team.Player;

                knife.tag = "Player";
                knife.SetActive(true);
                return;
            }
        }

    }

    public void PlaceGun()
    {
        var board = GridManager.Board.Board1;

        for (int row = 0; row < gridManager.Rows; row++)
        {
            for (int col = gridManager.Cols - 1; col >= 0; col--)
            {
                if (!gridManager.IsEmptyCell(board, row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(board, row, col, true);

                GameObject gun = PoolManager.Spawn(gunPrefab, worldPos, Quaternion.identity, gridManager.transform);
                var u = gun.GetComponent<Unit>();
                gun.SetActive(false);

                gun.GetComponent<Unit>().Initialize("Gun", 1, gridManager, board, row, col);

                gridManager.SetCellOccupied(board, row, col, gun);
                GameManager.Instance.HookUnit(u);

                var core = u ? u.core : null;
                if (core) core.team = Team.Player;

                var team = gun.GetComponent<UnitTeam>();
                if (team == null) team = gun.AddComponent<UnitTeam>();
                team.team = Team.Player;

                gun.tag = "Player";
                gun.SetActive(true);
                return;
            }
        }
    }

    public bool IsBoardFull()
    {
        var board = GridManager.Board.Board1; // Player board
        for (int r = 0; r < gridManager.Rows; r++)
        {
            for (int c = 0; c < gridManager.Cols; c++)
            {
                if (gridManager.IsEmptyCell(board, r, c))
                    return false;
            }
        }
        return true;
    }
}
