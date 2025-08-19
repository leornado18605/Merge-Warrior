using System;
using System.Collections.Generic;
using UnityEngine;
using ObjectPooling;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BotManager botManager;
    public GridManager gridManager;
    public float mergeLockSeconds = 0.25f;

    [Serializable]
    public class UnitUpgradeEntry { public string unitType; public GameObject[] levelPrefabs; }
    public UnitUpgradeEntry[] upgradeEntries;

    private Dictionary<string, GameObject[]> prefabMap;

    public event Action<Unit, int, int> OnUnitMerged;
    private void Start()
    {
        botManager.SetGridManager(gridManager);
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildPrefabMap();
        EnsurePools();
    }
    #region Merge Logic
    private void BuildPrefabMap()
    {
        prefabMap = new Dictionary<string, GameObject[]>();
        if (upgradeEntries == null) return;
        foreach (var e in upgradeEntries)
            if (!string.IsNullOrEmpty(e.unitType) && e.levelPrefabs != null)
                prefabMap[e.unitType] = e.levelPrefabs;
    }

    // create pools for all prefabs used
    private void EnsurePools()
    {
        if (prefabMap == null) return;
        foreach (var kv in prefabMap)
            foreach (var p in kv.Value)
                if (p != null)
                    PoolManager.CreatePool(p, initialSize: 8, maxSize: 64, autoExpand: true);
    }

    // Try merge and replace with upgraded prefab
    public bool TryMerge(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        if (sourceObj == null) return false;

        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);
        if (targetObj == null) return false;

        Unit sourceUnit = sourceObj.GetComponent<Unit>();
        Unit targetUnit = targetObj.GetComponent<Unit>();

        if (sourceUnit == null || targetUnit == null) return false;

        if (sourceUnit.unitType != targetUnit.unitType) return false;
        if (sourceUnit.level != targetUnit.level) return false;
        int newLevel = targetUnit.level + 1;

        GameObject newPrefab = GetMergedPrefab(targetUnit, newLevel);
        if (newPrefab == null)
        {
            return false; 
        }

        ReleaseObjectsAndClearGrid(board, targetRow, targetCol, sourceObj, targetObj);
        GameObject newObj = SpawnAndInitializeUnit(newPrefab, board, targetRow, targetCol, newLevel, targetUnit.unitType);

        if (newObj == null)
        {
            return false; 
        }

        Unit newUnit = newObj.GetComponent<Unit>();
        newUnit?.MergeLockTemporary(mergeLockSeconds);
        gridManager.SetCellOccupied(board, targetRow, targetCol, newObj);
        OnUnitMerged?.Invoke(newUnit, targetRow, targetCol);

        return true;
    }

    private GameObject GetMergedPrefab(Unit targetUnit, int newLevel)
    {
        if (!prefabMap.TryGetValue(targetUnit.unitType, out var prefabs)) return null;

        int prefabIndex = newLevel - 1;
        if (prefabIndex < 0 || prefabIndex >= prefabs.Length || prefabs[prefabIndex] == null) return null;

        return prefabs[prefabIndex];
    }
    private void ReleaseObjectsAndClearGrid(GridManager.Board board, int row, int col, GameObject sourceObj, GameObject targetObj)
    {
        var sourceUnit = sourceObj.GetComponent<Unit>();
        if (sourceUnit?.Grid != null && sourceUnit.Grid.IsValidGridPosition(sourceUnit.row, sourceUnit.col))
            sourceUnit.Grid.SetCellOccupied(sourceUnit.row, sourceUnit.col, null);

        gridManager.SetCellOccupied(board, row, col, null);

        PoolManager.Release(sourceObj);
        PoolManager.Release(targetObj);
    }

    private GameObject SpawnAndInitializeUnit(GameObject prefab, GridManager.Board board, int row, int col, int level, string unitType)
    {
        Vector3 spawnPos = gridManager.GridToWorldPosition(board, row, col);
        GameObject newObj = PoolManager.Spawn(prefab, spawnPos, Quaternion.identity, gridManager.transform);

        var unit = newObj.GetComponent<Unit>();
        if (unit == null)
        {
            PoolManager.Release(newObj);
            return null;
        }

        unit.Initialize(unitType, level, gridManager, board, row, col);

        var team = newObj.GetComponent<UnitTeam>() ?? newObj.AddComponent<UnitTeam>();
        team.team = (board == GridManager.Board.Board1) ? Team.Player : Team.Enemy;


        return newObj;
    }
    #endregion 
    public bool TrySwap(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        if (sourceObj == null || gridManager == null) return false;

        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);
        if (targetObj == null) return false; 

        var sourceUnit = sourceObj.GetComponent<Unit>();
        var targetUnit = targetObj.GetComponent<Unit>();
        if (sourceUnit == null || targetUnit == null) return false;

        if (sourceUnit.Board != board || targetUnit.Board != board) return false;

        int srcRow = sourceUnit.row;
        int srcCol = sourceUnit.col;

        Vector3 srcNewPos = gridManager.GridToWorldPosition(board, targetRow, targetCol);
        srcNewPos.y = sourceObj.transform.position.y;

        Vector3 tgtNewPos = gridManager.GridToWorldPosition(board, srcRow, srcCol);
        tgtNewPos.y = targetObj.transform.position.y;

        gridManager.SetCellOccupied(board, srcRow, srcCol, null);
        gridManager.SetCellOccupied(board, targetRow, targetCol, null);

        sourceObj.transform.position = srcNewPos;
        targetObj.transform.position = tgtNewPos;

        gridManager.SetCellOccupied(board, targetRow, targetCol, sourceObj);
        gridManager.SetCellOccupied(board, srcRow, srcCol, targetObj);

        sourceUnit.row = targetRow; sourceUnit.col = targetCol;
        targetUnit.row = srcRow; targetUnit.col = srcCol;

        return true;
    }

}
