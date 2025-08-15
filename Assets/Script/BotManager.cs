using System.Collections.Generic;
using UnityEngine;
using ObjectPooling;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("Bot Unit Prefabs")]
    public GameObject[] botLevelPrefabs;

    private GridManager gridManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetGridManager(GridManager gm)
    {
        gridManager = gm;
    }


    public void SpawnBot(GridManager.Board botBoard, int spawnRows, int spawnCols, GameObject[] botPrefabs)
    {
        if (gridManager == null || botPrefabs == null || botPrefabs.Length == 0)
            return;

        Vector3 boardOrigin = (botBoard == GridManager.Board.Board2) ? gridManager.Board2Origin : gridManager.Board1Origin;

        int startCol = (gridManager.Cols - spawnCols) / 2;
        int startRow = 0; 

        for (int r = 0; r < spawnRows; r++)
        {
            for (int c = 0; c < spawnCols; c++)
            {
                int levelIndex = Random.Range(0, botPrefabs.Length);
                GameObject prefab = botPrefabs[levelIndex];

                Vector3 spawnPos = gridManager.GridToWorldPosition(GridManager.Board.Board1, startRow + r, startCol + c);

                GameObject bot = PoolManager.Spawn(prefab, spawnPos, Quaternion.identity, gridManager.transform);

                gridManager.SetCellOccupied(botBoard, startRow + r, startCol + c, bot);

                Unit unitComp = bot.GetComponent<Unit>();
                if (unitComp != null)
                {
                    unitComp.Initialize(unitComp.unitType, levelIndex + 1, gridManager, startRow + r, startCol + c);
                }
            }
        }
    }



    public void ClearBots(GridManager.Board botBoard)
    {
        if (gridManager == null) return;

        for (int r = 0; r < gridManager.Rows; r++)
        {
            for (int c = 0; c < gridManager.Cols; c++)
            {
                GameObject occupant = gridManager.GetOccupant(botBoard, r, c);
                if (occupant != null)
                {
                    PoolManager.Release(occupant);
                    gridManager.SetCellOccupied(botBoard, r, c, null);
                }
            }
        }
    }
}
