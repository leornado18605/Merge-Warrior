using UnityEngine;
using ObjectPooling;

public class UnitManager : MonoBehaviour
{
    public GridManager gridManager;

    [Header("Unit Prefabs")]
    public GameObject knifePrefab;
    public GameObject gunPrefab;

    public void PlaceKnife()
    {
        for (int row = gridManager.rows - 1; row >= 0; row--)
        {
            for (int col = gridManager.cols - 1; col >= 0; col--)
            {
                if (!gridManager.IsEmptyCell(row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPositionBoard1(row, col);
                GameObject knife = PoolManager.Spawn(knifePrefab, worldPos, Quaternion.identity, gridManager.transform);

                gridManager.SetCellOccupied(row, col, true);

                // TODO: Add merge check here if needed

                return;
            }
        }
    }

    public void PlaceGun()
    {
        for (int row = 0; row < gridManager.rows; row++)
        {
            for (int col = gridManager.cols - 1; col >= 0; col--)
            {
                if (!gridManager.IsEmptyCell(row, col))
                    continue;

                Vector3 worldPos = gridManager.GridToWorldPositionBoard1(row, col);
                GameObject gun = PoolManager.Spawn(gunPrefab, worldPos, Quaternion.identity, gridManager.transform);

                gridManager.SetCellOccupied(row, col, true);

                return;
            }
        }
    }

    // Add more methods: PlaceRocket(), PlaceShield(), etc.
}
