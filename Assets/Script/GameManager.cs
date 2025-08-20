using System;
using System.Collections.Generic;
using UnityEngine;
using ObjectPooling;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public BotManager botManager;
    public GridManager gridManager;
    public float mergeLockSeconds = 0.25f;

    [Serializable]
    public class UnitUpgradeEntry
    {
        public string unitType;
        public GameObject[] levelPrefabs;
    }

    public UnitUpgradeEntry[] upgradeEntries;

    private Dictionary<string, GameObject[]> prefabMap;
    public event Action<Unit, int, int> OnUnitMerged;

    // ───────── LIFECYCLE ─────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildPrefabMap();
        EnsurePools();
    }

    private void Start()
    {
        if (botManager != null)
        {
            botManager.SetGridManager(gridManager);
        }
    }

    // ───────── PREFABS ─────────
    private void BuildPrefabMap()
    {
        prefabMap = new Dictionary<string, GameObject[]>();

        if (upgradeEntries == null) return;

        for (int i = 0; i < upgradeEntries.Length; i++)
        {
            UnitUpgradeEntry e = upgradeEntries[i];
            if (e != null && !string.IsNullOrEmpty(e.unitType))
            {
                prefabMap[e.unitType] = e.levelPrefabs;
            }
        }
    }

    private void EnsurePools()
    {
        if (prefabMap == null) return;

        List<string> keys = new List<string>(prefabMap.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            GameObject[] arr = prefabMap[keys[i]];
            if (arr == null) continue;

            for (int j = 0; j < arr.Length; j++)
            {
                if (arr[j] != null)
                {
                    PoolManager.CreatePool(arr[j], 8, 64, true);
                }
            }
        }
    }

    // ───────── MERGE ─────────
    public bool TryMerge(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);

        if (!IsValidMergeCandidate(targetObj, sourceObj, out Unit targetUnit, out Unit sourceUnit))
            return false;

        int newLevel = targetUnit.level + 1;

        if (!prefabMap.TryGetValue(targetUnit.unitType, out GameObject[] prefabs))
            return false;

        if (newLevel - 1 >= prefabs.Length || prefabs[newLevel - 1] == null)
            return false;

        Vector3 spawnPos = gridManager.GridToWorldPosition(board, targetRow, targetCol, true);

        CleanupSource(sourceUnit, sourceObj);
        PoolManager.Release(targetObj);

        GameObject newPrefab = prefabs[newLevel - 1];
        CreateMergedUnit(newPrefab, targetUnit.unitType, newLevel, board, targetRow, targetCol, spawnPos);

        return true;
    }

    private bool IsValidMergeCandidate(GameObject targetObj, GameObject sourceObj,
        out Unit targetUnit, out Unit sourceUnit)
    {
        targetUnit = null;
        sourceUnit = null;

        if (targetObj == null || sourceObj == null) return false;

        targetUnit = targetObj.GetComponent<Unit>();
        sourceUnit = sourceObj.GetComponent<Unit>();

        if (targetUnit == null || sourceUnit == null) return false;
        if (targetUnit.unitType != sourceUnit.unitType) return false;
        if (targetUnit.level != sourceUnit.level) return false;

        return true;
    }

    private void CleanupSource(Unit sourceUnit, GameObject sourceObj)
    {
        if (sourceUnit.Grid != null &&
            sourceUnit.Grid.IsValidGridPosition(sourceUnit.row, sourceUnit.col))
        {
            sourceUnit.Grid.SetCellOccupied(sourceUnit.Board, sourceUnit.row, sourceUnit.col, null);
        }

        PoolManager.Release(sourceObj);
    }

    private void CreateMergedUnit(GameObject prefab, string unitType, int newLevel,
        GridManager.Board board, int row, int col, Vector3 spawnPos)
    {
        GameObject newObj = PoolManager.Spawn(prefab, spawnPos, Quaternion.identity);
        Unit newUnit = newObj.GetComponent<Unit>();

        if (newUnit != null)
        {
            newUnit.Initialize(unitType, newLevel, gridManager, board, row, col);
            gridManager.SetCellOccupied(board, row, col, newObj);
            newUnit.MergeLockTemporary();

            if (OnUnitMerged != null)
                OnUnitMerged(newUnit, row, col);
        }
    }

    // ───────── SWAP ─────────
    public bool TrySwap(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);

        if (!IsValidSwapCandidate(targetObj, sourceObj, out Unit sourceUnit, out Unit targetUnit))
            return false;

        DoSwap(board, sourceObj, targetObj, sourceUnit, targetUnit, targetRow, targetCol);
        return true;
    }

    private bool IsValidSwapCandidate(GameObject targetObj, GameObject sourceObj,
        out Unit sourceUnit, out Unit targetUnit)
    {
        sourceUnit = null;
        targetUnit = null;

        if (sourceObj == null || targetObj == null) return false;

        sourceUnit = sourceObj.GetComponent<Unit>();
        targetUnit = targetObj.GetComponent<Unit>();

        if (sourceUnit == null || targetUnit == null) return false;
        if (sourceUnit.Board != targetUnit.Board) return false;

        return true;
    }

    private void DoSwap(GridManager.Board board, GameObject sourceObj, GameObject targetObj,
        Unit sourceUnit, Unit targetUnit, int targetRow, int targetCol)
    {
        int srcRow = sourceUnit.row;
        int srcCol = sourceUnit.col;

        Vector3 srcNewPos = gridManager.GridToWorldPosition(board, targetRow, targetCol, true);
        srcNewPos.y = sourceObj.transform.position.y;

        Vector3 tgtNewPos = gridManager.GridToWorldPosition(board, srcRow, srcCol, true);
        tgtNewPos.y = targetObj.transform.position.y;

        gridManager.SetCellOccupied(board, srcRow, srcCol, null);
        gridManager.SetCellOccupied(board, targetRow, targetCol, null);

        sourceObj.transform.position = srcNewPos;
        targetObj.transform.position = tgtNewPos;

        gridManager.SetCellOccupied(board, targetRow, targetCol, sourceObj);
        gridManager.SetCellOccupied(board, srcRow, srcCol, targetObj);

        sourceUnit.row = targetRow;
        sourceUnit.col = targetCol;
        targetUnit.row = srcRow;
        targetUnit.col = srcCol;
    }
}
