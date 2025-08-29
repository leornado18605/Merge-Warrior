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
    [SerializeField] private bool autoSpawnOnStart = false;              // để false, spawn khi bấm Start
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

    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetGridManager(GridManager gm)
    {
        gridManager = gm;
    }

    private void Start()
    {
        if (autoSpawnOnStart && gridManager != null)
        {
            SpawnFromInspector();
        }
    }

    [ContextMenu("Spawn Now (from Inspector settings)")]
    public void SpawnFromInspector()
    {
        SpawnBot(defaultBoard, spawnRows, spawnCols, botLevelPrefabs, spawnLimit);
    }

    public void SpawnBot(GridManager.Board botBoard,
                         int rows,
                         int cols,
                         GameObject[] prefabs,
                         int limit = int.MaxValue)
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

            if (TrySpawnAt(botBoard, cell, prefab, out GameObject bot))
            {
                ConfigureSpawnedBot(bot, botBoard, cell, levelIndex);
                spawned += 1;
            }
        }
    }

    // ───────────────── helpers ─────────────────

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
            for (int c = 0; c < cols; c++)
                yield return new Vector2Int(startRow + r, startCol + c);
    }

    private int PickLevelIndex(GameObject[] prefabs)
    {
        return Random.Range(0, prefabs.Length);
    }

    private bool TrySpawnAt(GridManager.Board board,
                            Vector2Int cell,
                            GameObject prefab,
                            out GameObject bot)
    {
        Vector3 pos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);
        Quaternion rot = GetRotationFacingCamera(pos, modelYawOffset);

        bot = PoolManager.Spawn(prefab, pos, rot, gridManager.transform);
        if (bot == null) return false;

        gridManager.SetCellOccupied(board, cell.x, cell.y, bot);
        return true;
    }

    private void ConfigureSpawnedBot(GameObject bot,
                                     GridManager.Board board,
                                     Vector2Int cell,
                                     int levelIndex)
    {
        Unit u = bot.GetComponent<Unit>();
        if (u == null) return;

        UnitTeam t = bot.GetComponent<UnitTeam>();
        if (t == null) t = bot.AddComponent<UnitTeam>();
        t.team = Team.Enemy;
        bot.tag = "Enemy";

        UnitCore core = u.core;
        if (core != null) core.team = Team.Enemy;

        u.Initialize(u.unitType, levelIndex + 1, gridManager, board, cell.x, cell.y);

        DraggableUnit drag = u.drag;
        if (disableDragForBots && drag) drag.enabled = false;

        NavMeshAgent ag = u.agent;
        if (ag != null)
        {
            if (!ag.enabled) ag.enabled = true;
            ag.updateRotation = false;
            ag.Warp(gridManager.GridToWorldPosition(board, cell.x, cell.y, true));
            ag.ResetPath();
        }

        GameManager.Instance?.HookUnit(u);
    }

    private static Quaternion GetRotationFacingCamera(Vector3 pos, float yawOffset)
    {
        Camera cam = Camera.main;
        if (!cam) return Quaternion.identity;

        Vector3 toCam = cam.transform.position - pos;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 1e-4f) toCam = -cam.transform.forward;

        Quaternion rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        if (Mathf.Abs(yawOffset) > 0.001f) rot *= Quaternion.Euler(0f, yawOffset, 0f);
        return rot;
    }

    private void OnValidate()
    {
        if (spawnRows < 1) spawnRows = 1;
        if (spawnCols < 1) spawnCols = 1;
        if (spawnLimit < 0) spawnLimit = 0;
    }
}
