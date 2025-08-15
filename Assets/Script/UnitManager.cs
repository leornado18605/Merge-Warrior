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
        for (int row = gridManager.Rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < gridManager.Cols; col++)
            {
                if (!gridManager.IsEmptyCell(row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(row, col);

                GameObject knife = PoolManager.Spawn(knifePrefab, worldPos, Quaternion.identity, gridManager.transform);
                knife.SetActive(true);

                knife.GetComponent<Unit>().Initialize("Knife", 1, gridManager, row, col);

                gridManager.SetCellOccupied(row, col, knife);

                return;
            }
        }
    }

    public void PlaceGun()
    {
        for (int row = 0; row < gridManager.Rows; row++)
        {
            for (int col = gridManager.Cols - 1; col >= 0; col--)
            {
                if (!gridManager.IsEmptyCell(row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPosition(row, col);

                GameObject gun = PoolManager.Spawn(gunPrefab, worldPos, Quaternion.identity, gridManager.transform);

                gun.SetActive(true);
                gun.transform.position = worldPos;

                gun.GetComponent<Unit>().Initialize("Gun", 1, gridManager, row, col);
                gridManager.SetCellOccupied(row, col, gun);
                return;
            }
        }

    }
}
