using ObjectPooling;
using System.Collections.Generic;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    public static BotManager Instance { get; private set; }

    [Header("Bot Unit Prefabs")]
    public GameObject[] botLevelPrefabs;

    [Header("Spawn Settings (Inspector)")]
    [SerializeField] private bool autoSpawnOnStart = true;
    [SerializeField] private GridManager.Board defaultBoard = GridManager.Board.Board2;
    [SerializeField, Min(1)] private int spawnRows = 3;
    [SerializeField, Min(1)] private int spawnCols = 5;
    [SerializeField, Min(0)] private int spawnLimit = 10;

    [Header("Input")]
    [SerializeField] private bool disableDragForBots = true;

    [Header("Facing Settings")]
    [SerializeField] private float modelYawOffset = 0f;

    [Header("Ranged Detect")]
    [SerializeField] private string rangedNameKeyword = "Gun";

    private GridManager gridManager;

    // ─────────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetGridManager(GridManager gm) => gridManager = gm;

    private void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (autoSpawnOnStart && gridManager != null) SpawnFromInspector();
    }

    [ContextMenu("Spawn Now (from Inspector settings)")]
    public void SpawnFromInspector()
    {
        SpawnBot(defaultBoard, spawnRows, spawnCols, botLevelPrefabs, spawnLimit);
    }

    // ───────────────────────────── Orchestrator ────────────────────────────────

    public void SpawnBot(GridManager.Board botBoard,
                         int rows,
                         int cols,
                         GameObject[] prefabs,
                         int limit = int.MaxValue)
    {
        if (!ValidateInputs(prefabs, limit)) return;

        int spawned = 0;
        foreach (var cell in EnumerateCenteredArea(rows, cols))
        {
            if (spawned >= limit) break;
            if (!gridManager.IsValidGridPosition(cell.x, cell.y)) continue;
            if (!gridManager.IsEmptyCell(botBoard, cell.x, cell.y)) continue;

            int levelIndex = PickLevelIndex(prefabs);
            var prefab = prefabs[levelIndex];

            if (TrySpawnAt(botBoard, cell, prefab, levelIndex, out var bot))
            {
                ConfigureSpawnedBot(bot, botBoard, cell, levelIndex);
                spawned++;
                Debug.Log($"[BOT SPAWN] Board:{botBoard} Row:{cell.x} Col:{cell.y} Go:{bot.name}");
            }
        }
    }

    // ───────────────────────────── Helpers: flow ───────────────────────────────

    private bool ValidateInputs(GameObject[] prefabs, int limit)
    {
        if (gridManager == null) { return false; }
        if (prefabs == null || prefabs.Length == 0) { return false; }
        if (limit <= 0) { return false; }
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
                            int levelIndex,
                            out GameObject bot)
    {
        Vector3 spawnPos = gridManager.GridToWorldPosition(board, cell.x, cell.y, true);
        Quaternion rot = GetRotationFacingCamera(spawnPos, modelYawOffset);

        bot = PoolManager.Spawn(prefab, spawnPos, rot, gridManager.transform);
        if (bot == null) return false;

        gridManager.SetCellOccupied(board, cell.x, cell.y, bot);
        return true;
    }

    private void ConfigureSpawnedBot(GameObject bot,
                                     GridManager.Board board,
                                     Vector2Int cell,
                                     int levelIndex)
    {
        SetupUnit(bot, board, cell, levelIndex);
        SetupTeam(bot);
        DisableDragIfNeeded(bot);
        SetupNavMesh(bot, gridManager.GridToWorldPosition(board, cell.x, cell.y, true));
    }

    // ───────────────────────────── Helpers: per-system ─────────────────────────

    private void SetupUnit(GameObject bot,
                           GridManager.Board board,
                           Vector2Int cell,
                           int levelIndex)
    {
        var unit = bot.GetComponent<Unit>();
        if (unit != null)
        {
            unit.Initialize(unit.unitType, levelIndex + 1, gridManager, board, cell.x, cell.y);
        }
    }

    private void SetupTeam(GameObject bot)
    {
        var team = bot.GetComponent<UnitTeam>() ?? bot.AddComponent<UnitTeam>();
        team.team = Team.Enemy;

        bot.tag = "Enemy";
    }

    private void DisableDragIfNeeded(GameObject bot)
    {
        if (!disableDragForBots) return;
        var drag = bot.GetComponent<DraggableUnit>();
        if (drag) drag.enabled = false;
    }

    private void SetupNavMesh(GameObject bot, Vector3 spawnPos)
    {
        var agent = bot.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPos);
            agent.updateRotation = false;
        }
    }

    // ───────────────────────────── Utilities ───────────────────────────────────

    private static Quaternion GetRotationFacingCamera(Vector3 spawnPos, float yawOffset = 0f)
    {
        var cam = Camera.main;
        if (!cam) return Quaternion.identity;

        Vector3 toCam = cam.transform.position - spawnPos;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 1e-4f) toCam = -cam.transform.forward;

        var rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
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