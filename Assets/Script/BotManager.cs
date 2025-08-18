using ObjectPooling;
using System.Collections.Generic;
using UnityEngine;
using static GridManager;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("Bot Unit Prefabs")]
    public GameObject[] botLevelPrefabs;

    [Header("Facing Settings")]
    [SerializeField] private float modelYawOffset = 0f;

    private GridManager gridManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetGridManager(GridManager gm) => gridManager = gm;

    private static Quaternion GetRotationFacingCamera(Vector3 spawnPos, float yawOffset = 0f)
    {
        var cam = Camera.main;
        if (!cam) return Quaternion.identity;

        Vector3 toCam = cam.transform.position - spawnPos;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 1e-4f) toCam = -cam.transform.forward; // fallback

        var rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        if (Mathf.Abs(yawOffset) > 0.001f) rot *= Quaternion.Euler(0f, yawOffset, 0f);
        return rot;
    }

    public void SpawnBot(GridManager.Board botBoard, int spawnRows, int spawnCols, GameObject[] botPrefabs)
    {
        if (gridManager == null || botPrefabs == null || botPrefabs.Length == 0)
            return;

        int startCol = Mathf.Max(0, (gridManager.Cols - spawnCols) / 2);
        int startRow = 0;
        for (int r = 0; r < spawnRows; r++)
        {
            for (int c = 0; c < spawnCols; c++)
            {
                int row = startRow + r;
                int col = startCol + c;

                if (row < 0 || row >= gridManager.Rows || col < 0 || col >= gridManager.Cols)
                    continue;
                if (!gridManager.IsEmptyCell(botBoard, row, col))
                    continue;

                int levelIndex = Random.Range(0, botPrefabs.Length);
                GameObject prefab = botPrefabs[levelIndex];

                Vector3 spawnPos = gridManager.GridToWorldPosition(botBoard, row, col);

                Quaternion faceCamRot = GetRotationFacingCamera(spawnPos, modelYawOffset);

                GameObject bot = PoolManager.Spawn(prefab, spawnPos, faceCamRot, gridManager.transform);

                gridManager.SetCellOccupied(botBoard, row, col, bot);

                Unit unitComp = bot.GetComponent<Unit>();
                if (unitComp != null)
                {
                    unitComp.Initialize(unitComp.unitType, levelIndex + 1, gridManager, botBoard, row, col);
                }

                var agent = bot.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.Warp(spawnPos);     
                    agent.updateRotation = true; 
                }

                Debug.Log($"[BOT SPAWN] Board: {botBoard}, Row: {row}, Col: {col}, Pos: {spawnPos}");
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
