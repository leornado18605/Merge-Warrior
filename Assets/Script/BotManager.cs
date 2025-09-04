using ObjectPooling;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("Bot Unit Prefabs")]
    [SerializeField] private GameObject[] botLevelPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private bool autoSpawnOnStart = false;
    [SerializeField] private GridManager.Board defaultBoard = GridManager.Board.Board2;
    [SerializeField, Min(1)] private int spawnRows = 3;
    [SerializeField, Min(1)] private int spawnCols = 5;
    [SerializeField, Min(0)] private int spawnLimit = 10;

    [Header("Input")]
    [SerializeField] private bool disableDragForBots = true;

    [Header("Facing")]
    [SerializeField] private float modelYawOffset = 0f;

    [Header("Detect")]
    [SerializeField] private string rangedNameKeyword = "Gun";

    [Header("Refs")]
    [SerializeField] private GridManager gridManager;

    // ──────────────────────────────────────────────
    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoSpawnOnStart && gridManager != null)
        {
            SpawnFromInspector();
        }
    }

    private void OnValidate()
    {
        if (spawnRows < 1) spawnRows = 1;
        if (spawnCols < 1) spawnCols = 1;
        if (spawnLimit < 0) spawnLimit = 0;
    }

    #endregion
    // ──────────────────────────────────────────────
    #region Public API

    public void SetGridManager(GridManager gm)
    {
        gridManager = gm;
    }

    [ContextMenu("Spawn Now (from Inspector settings)")]
    public void SpawnFromInspector()
    {
        SpawnBot(
            defaultBoard,
            spawnRows,
            spawnCols,
            botLevelPrefabs,
            spawnLimit
        );
    }

    public void SpawnBot(
        GridManager.Board botBoard,
        int rows,
        int cols,
        GameObject[] prefabs,
        int limit = int.MaxValue
    )
    {
        if (!ValidateInputs(prefabs, limit)) return;

        int spawned = 0;

        foreach (Vector2Int cell in EnumerateCenteredArea(rows, cols))
        {
            if (spawned >= limit) break;
            if (!gridManager.IsValidGridPosition(cell.x, cell.y)) continue;
            if (!gridManager.IsEmptyCell(botBoard, cell.x, cell.y)) continue;

            int levelIndex = PickLevelIndex(prefabs);
            GameObject prefab = prefabs[levelIndex];

            GameObject bot;
            bool success = TrySpawnAt(botBoard, cell, prefab, out bot);

            if (success)
            {
                ConfigureSpawnedBot(bot, botBoard, cell, levelIndex);
                spawned += 1;
            }
        }
    }
    public void ClearEnemies()
    {
        if (gridManager == null) return;

        for (int r = 0; r < gridManager.Rows; r++)
        {
            for (int c = 0; c < gridManager.Cols; c++)
            {
                GameObject enemy = gridManager.GetOccupant(GridManager.Board.Board2, r, c);
                if (enemy != null)
                {
                    PoolManager.Release(enemy);
                    gridManager.SetCellOccupied(GridManager.Board.Board2, r, c, null);
                }
            }
        }
    }
    #endregion
    // ──────────────────────────────────────────────
    #region Spawn Helpers

    private bool ValidateInputs(GameObject[] prefabs, int limit)
    {
        if (gridManager == null) return false;
        if (prefabs == null || prefabs.Length == 0) return false;
        if (limit <= 0) return false;

        return true;
    }

    private IEnumerable<Vector2Int> EnumerateCenteredArea(int rows, int cols)
    {
        int startRow = 0;
        int startCol = Mathf.Max(0, (gridManager.Cols - cols) / 2);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                yield return new Vector2Int(startRow + r, startCol + c);
            }
        }
    }

    private int PickLevelIndex(GameObject[] prefabs)
    {
        return Random.Range(0, prefabs.Length);
    }

    private bool TrySpawnAt(
        GridManager.Board board,
        Vector2Int cell,
        GameObject prefab,
        out GameObject bot
    )
    {
        Vector3 pos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);
        Quaternion rot = GetRotationFacingCamera(pos, modelYawOffset);

        bot = PoolManager.Spawn(prefab, pos, rot, gridManager.transform);
        if (bot == null) return false;

        gridManager.SetCellOccupied(board, cell.x, cell.y, bot);
        return true;
    }

    private void ConfigureSpawnedBot(
        GameObject bot,
        GridManager.Board board,
        Vector2Int cell,
        int levelIndex
    )
    {
        Unit unit = bot.GetComponent<Unit>();
        if (unit == null) return;

        // Team
        UnitTeam team = bot.GetComponent<UnitTeam>();
        if (team == null) team = bot.AddComponent<UnitTeam>();
        team.team = Team.Enemy;
        bot.tag = "Enemy";

        // Core
        UnitCore core = unit.core;
        if (core != null) core.team = Team.Enemy;

        unit.Initialize(
            unit.unitType,
            levelIndex + 1,
            gridManager,
            board,
            cell.x,
            cell.y
        );

        // Drag
        DraggableUnit drag = unit.drag;
        if (disableDragForBots && drag != null)
        {
            drag.enabled = false;
        }

        // NavMeshAgent
        NavMeshAgent agent = unit.agent;
        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;
            agent.updateRotation = false;

            Vector3 worldPos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);
            agent.Warp(worldPos);
            agent.ResetPath();
        }

        // Hook GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HookUnit(unit);
        }
    }

    private static Quaternion GetRotationFacingCamera(Vector3 pos, float yawOffset)
    {
        Camera cam = Camera.main;
        if (cam == null) return Quaternion.identity;

        Vector3 camPos = cam.transform.position;
        Vector3 toCam = camPos - pos;
        toCam.y = 0f;

        float sqrLen = toCam.sqrMagnitude;
        if (sqrLen < 0.0001f)
        {
            toCam = -cam.transform.forward;
        }

        Quaternion rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);

        if (Mathf.Abs(yawOffset) > 0.001f)
        {
            rot *= Quaternion.Euler(0f, yawOffset, 0f);
        }

        return rot;
    }

    #endregion
}
