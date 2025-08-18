using ObjectPooling;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    [Header("Unit Prefabs")]
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private GameObject gunPrefab;

    // pool sizes
    [SerializeField] private int poolInitial = 8;
    [SerializeField] private int poolMax = 32;

    [SerializeField] private Unit knifeUnit;
    [SerializeField] private Unit gunUnit;

    private void Start()
    {
        PoolManager.CreatePool(knifePrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
        PoolManager.CreatePool(gunPrefab, initialSize: poolInitial, maxSize: poolMax, autoExpand: true);
    }

    public void PlaceKnife()
    {
        var board = GridManager.Board.Board1;
        Debug.Log($"[PlaceKnife] Board1Origin: {gridManager.Board1Origin}, Board2Origin: {gridManager.Board2Origin}, boardsSwapped: {gridManager.boardsSwapped}");

        for (int row = gridManager.Rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < gridManager.Cols; col++)
            {
                if (!gridManager.IsEmptyCell(board, row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(board, row, col);
                Debug.Log($"[PlaceKnife] Trying to place at row: {row}, col: {col}, worldPos: {worldPos}");

                GameObject knife = PoolManager.Spawn(knifePrefab, worldPos, Quaternion.identity, gridManager.transform);
                knife.SetActive(true);

                knife.GetComponent<Unit>().Initialize("Knife", 1, gridManager, board, row, col);

                gridManager.SetCellOccupied(board, row, col, knife);

                // Đảm bảo có Team
                var team = knife.GetComponent<UnitTeam>();
                if (team == null) team = knife.AddComponent<UnitTeam>();
                team.team = Team.Player;

                // Đảm bảo có AI + inject grid & role
                var ai = knife.GetComponent<UnitAI>();
                if (ai == null) ai = knife.AddComponent<UnitAI>();
                ai.Inject(gridManager, ranged: false); // Knife = melee


                Debug.Log($"[PlaceKnife] Spawned Knife at row: {row}, col: {col}, worldPos: {worldPos}");
                return;
            }
        }

        Debug.LogWarning("[PlaceKnife] No empty cell found on Board1.");
    }

    public void PlaceGun()
    {
        var board = GridManager.Board.Board1;
        Debug.Log($"[PlaceGun] Board1Origin: {gridManager.Board1Origin}, Board2Origin: {gridManager.Board2Origin}, boardsSwapped: {gridManager.boardsSwapped}");

        for (int row = 0; row < gridManager.Rows; row++)
        {
            for (int col = gridManager.Cols - 1; col >= 0; col--)
            {
                if (!gridManager.IsEmptyCell(board, row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(board, row, col);
                Debug.Log($"[PlaceGun] Trying to place at row: {row}, col: {col}, worldPos: {worldPos}");

                GameObject gun = PoolManager.Spawn(gunPrefab, worldPos, Quaternion.identity, gridManager.transform);
                gun.SetActive(true);

                gun.GetComponent<Unit>().Initialize("Gun", 1, gridManager, board, row, col);

                gridManager.SetCellOccupied(board, row, col, gun);

                var team = gun.GetComponent<UnitTeam>();
                if (team == null) team = gun.AddComponent<UnitTeam>();
                team.team = Team.Player;

                var ai = gun.GetComponent<UnitAI>();
                if (ai == null) ai = gun.AddComponent<UnitAI>();
                ai.Inject(gridManager, ranged: true); // Gun = ranged


                Debug.Log($"[PlaceGun] Spawned Gun at row: {row}, col: {col}, worldPos: {worldPos}");
                return;
            }
        }

        Debug.LogWarning("[PlaceGun] No empty cell found on Board1.");
    }
}
