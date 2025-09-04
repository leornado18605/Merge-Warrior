using ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [Header("Refs")]
    [SerializeField] private BotManager botManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private UIManager uiManager;

    [Header("Merge")]
    public float mergeLockSeconds = 0.25f;

    [Header("Anim Keys")]
    [SerializeField] private string runKey = "Running";
    [SerializeField] private string dieKey = "Die";

    [Header("Run Loop")]
    [SerializeField] private float runTick = 0.1f;

    [Header("Death")]
    [SerializeField] private float deadDespawnDelay = 1.2f;

    [Header("Win/End")]
    [SerializeField] private string winTrigger = "Win";
    [SerializeField] private bool endCombatOnWin = true;

    [SerializeField] private GridManager.Board playerBoard = GridManager.Board.Board1;
    [SerializeField] private GridManager.Board enemyBoard = GridManager.Board.Board2;
    // CoinManager
    private int damageByPlayer = 0;
    private int damageByEnemy = 0;


    [SerializeField] private float resultDelay = 0.6f;
    private bool endBattleScheduled = false;
    private Coroutine endBattleRoutine;

    private bool battleEnded = false;
    [Serializable]
    public class UnitUpgradeEntry
    {
        public string unitType;
        public GameObject[] levelPrefabs;
    }

    public UnitUpgradeEntry[] upgradeEntries;

    public event Action<Unit, int, int> OnUnitMerged;
    public event Action<Unit, Unit, int> OnAttack;
    public event Action<Unit> OnUnitDead;

    private Dictionary<string, GameObject[]> prefabMap;
    private readonly Dictionary<UnitCore, Unit> unitMap = new();
    readonly System.Collections.Generic.List<GunController> guns = new();


    // ─────────────────────────────────────────────────────────────────────────────
    #region Lifecycle

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
        StartCoroutine(InitWhenGridReady());
    }

    private IEnumerator InitWhenGridReady()
    {
        while (gridManager == null) yield return null;

        if (!gridManager.IsReady)
        {
            bool done = false;
            gridManager.Built += () => done = true;
            while (!gridManager.IsReady && !done) yield return null;
        }

        if (botManager != null) botManager.SetGridManager(gridManager);
        StartCoroutine(RunLoop());
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Register Units / Death

    public void HookUnit(Unit unit)
    {
        if (unit == null || unit.core == null) return;
        if (unitMap.ContainsKey(unit.core)) return;

        unitMap.Add(unit.core, unit);
        unit.core.onDead += OnDeadCore;
        unit.core.onHit += OnUnitHit;

        if (unit.gun != null)
        {
            RegisterGun(unit.gun);
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ApplyStateToUnit(unit);
        }
    }

    public void UnhookUnit(Unit unit)
    {
        if (unit == null || unit.core == null) return;
        if (!unitMap.ContainsKey(unit.core)) return;

        if (unit.gun != null)
        {
            UnregisterGun(unit.gun);
        }

        unit.core.onDead -= OnDeadCore;
        unitMap.Remove(unit.core);
    }

    public void OnUnitHit(UnitCore core, int damage)
    {
        if (core == null) return;

        if (core.team == Team.Player)
        {
            damageByEnemy += damage;
        }
        else if (core.team == Team.Enemy)
        {
            damageByPlayer += damage;
        }
    }

    private void OnDeadCore(UnitCore core)
    {
        Unit unit;
        if (!unitMap.TryGetValue(core, out unit)) return;

        // Disable targeting
        if (unit.targeting != null)
        {
            unit.targeting.enabled = false;
        }

        // Disable agent
        if (unit.agent != null)
        {
            unit.agent.ResetPath();
            unit.agent.isStopped = true;
            unit.agent.updatePosition = true;
            unit.agent.updateRotation = false;
            unit.agent.enabled = false;
        }

        // Reset rigidbody
        if (unit.rb != null)
        {
            unit.rb.velocity = Vector3.zero;
            unit.rb.angularVelocity = Vector3.zero;
            unit.rb.isKinematic = true;
        }

        PlayDieAnim(unit);
        ClearCell(unit);

        if (OnUnitDead != null)
        {
            OnUnitDead.Invoke(unit);
        }

        StartCoroutine(DelayRelease(unit.gameObject, deadDespawnDelay));
        CheckBattleOver();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Attack

    public bool CanAttack(Unit a, Unit b)
    {
        if (a == null || b == null) return false;
        if (a.core == null || b.core == null) return false;
        if (!a.core.Alive() || !b.core.Alive()) return false;
        if (a.core.team == b.core.team) return false;
        return true;
    }

    public void Attack(Unit a, Unit b)
    {
        if (!CanAttack(a, b)) return;

        int val = Mathf.Max(1, a.core.dmg);
        var d = new DamageData(val, a.core.team, a.transform.position);

        b.core.Hit(d);
        OnAttack?.Invoke(a, b, val);
    }

    public void AttackInRange(Unit a, Unit b, float range)
    {
        if (!CanAttack(a, b)) return;

        float d = Vector3.Distance(a.transform.position, b.transform.position);
        if (d > range) return;

        Attack(a, b);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Running (update Running bool by NavMeshAgent)

    private IEnumerator RunLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(runTick);

        while (true)
        {
            ApplyRunState(GridManager.Board.Board1);
            ApplyRunState(GridManager.Board.Board2);
            yield return wait;
        }
    }

    private void ApplyRunState(GridManager.Board board)
    {
        if (gridManager == null) return;

        for (int row = 0; row < gridManager.Rows; row++)
        {
            for (int col = 0; col < gridManager.Cols; col++)
            {
                GameObject go = gridManager.GetOccupant(board, row, col);
                if (go == null) continue;

                Unit unit = go.GetComponent<Unit>();
                if (unit == null) continue;
                if (unit.agent == null || unit.anim == null) continue;

                bool moving = unit.agent.enabled &&
                              unit.agent.hasPath &&
                              unit.agent.remainingDistance >
                              unit.agent.stoppingDistance + 0.05f;

                unit.anim.SetBool(runKey, moving);
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Merge

    public bool TryMerge(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);
        if (targetObj == null || sourceObj == null) return false;

        Unit targetUnit;
        Unit sourceUnit;
        if (!ValidMerge(targetObj, sourceObj, out targetUnit, out sourceUnit)) return false;

        int newLevel = targetUnit.level + 1;

        GameObject[] prefabs;
        if (!prefabMap.TryGetValue(targetUnit.unitType, out prefabs)) return false;
        if (newLevel - 1 >= prefabs.Length || prefabs[newLevel - 1] == null) return false;

        Vector3 pos = gridManager.GridToWorldPosition(board, targetRow, targetCol, true);

        CleanupSource(sourceUnit, sourceObj);
        PoolManager.Release(targetObj);

        if (newLevel == 2)
        {
            GameObject dummy = CreateMergedUnit(prefabs[0], targetUnit.unitType, 1, board, targetRow, targetCol, pos);
            if (dummy != null)
            {
                Unit unit = dummy.GetComponent<Unit>();
                if (unit != null) unit.MergeWithEffect(prefabs[newLevel - 1], newLevel);
            }
        }
        else
        {
            CreateMergedUnit(prefabs[newLevel - 1], targetUnit.unitType, newLevel, board, targetRow, targetCol, pos);
        }

        return true;
    }

    private bool ValidMerge(GameObject a, GameObject b, out Unit ua, out Unit ub)
    {
        ua = null;
        ub = null;
        if (a == null || b == null) return false;

        ua = a.GetComponent<Unit>();
        ub = b.GetComponent<Unit>();
        if (ua == null || ub == null) return false;
        if (ua.unitType != ub.unitType) return false;
        if (ua.level != ub.level) return false;

        return true;
    }

    private void CleanupSource(Unit unit, GameObject go)
    {
        if (unit != null && unit.Grid != null && unit.Grid.IsValidGridPosition(unit.row, unit.col))
        {
            unit.Grid.SetCellOccupied(unit.Board, unit.row, unit.col, null);
        }

        PoolManager.Release(go);
        UnhookUnit(unit);
    }

    public GameObject CreateMergedUnit(
        GameObject prefab,
        string type,
        int level,
        GridManager.Board board,
        int row,
        int col,
        Vector3 pos)
    {
        GameObject obj = PoolManager.Spawn(prefab, pos, Quaternion.identity);
        Unit unit = obj != null ? obj.GetComponent<Unit>() : null;
        if (unit == null) return null;

        // Destroy cached event systems if any
        if (unit.eventSystems != null)
        {
            for (int i = 0; i < unit.eventSystems.Length; i++)
            {
                if (unit.eventSystems[i] != null)
                    Destroy(unit.eventSystems[i].gameObject);
            }
        }

        // Disable cached raycasters if any
        if (unit.raycasters != null)
        {
            for (int i = 0; i < unit.raycasters.Length; i++)
            {
                if (unit.raycasters[i] != null)
                    unit.raycasters[i].enabled = false;
            }
        }

        unit.Initialize(type, level, gridManager, board, row, col);
        gridManager.SetCellOccupied(board, row, col, obj);

        unit.MergeLockTemporary(mergeLockSeconds);

        HookUnit(unit);
        if (OnUnitMerged != null) OnUnitMerged.Invoke(unit, row, col);

        EnsureEventSystemExists();
        return obj;
    }

    private static void EnsureEventSystemExists()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Swap

    public bool TrySwap(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        GameObject targetObj = gridManager.GetOccupant(board, targetRow, targetCol);
        if (targetObj == null || sourceObj == null) return false;

        Unit sourceUnit;
        Unit targetUnit;
        if (!ValidSwap(targetObj, sourceObj, out sourceUnit, out targetUnit)) return false;

        DoSwap(board, sourceObj, targetObj, sourceUnit, targetUnit, targetRow, targetCol);
        return true;
    }

    private bool ValidSwap(GameObject a, GameObject b, out Unit ua, out Unit ub)
    {
        ua = null;
        ub = null;

        if (a == null || b == null) return false;

        ua = b.GetComponent<Unit>();
        ub = a.GetComponent<Unit>();
        if (ua == null || ub == null) return false;
        if (ua.Board != ub.Board) return false;

        return true;
    }

    private void DoSwap(
        GridManager.Board board,
        GameObject src,
        GameObject tgt,
        Unit sourceUnit,
        Unit targetUnit,
        int targetRow,
        int targetCol)
    {
        int sourceRow = sourceUnit.row;
        int sourceCol = sourceUnit.col;

        Vector3 posA = gridManager.GridToWorldPosition(board, targetRow, targetCol, true);
        Vector3 posB = gridManager.GridToWorldPosition(board, sourceRow, sourceCol, true);

        // clear old cells
        gridManager.SetCellOccupied(board, sourceRow, sourceCol, null);
        gridManager.SetCellOccupied(board, targetRow, targetCol, null);

        // swap references
        gridManager.SetCellOccupied(board, targetRow, targetCol, src);
        gridManager.SetCellOccupied(board, sourceRow, sourceCol, tgt);

        // update row/col
        sourceUnit.row = targetRow;
        sourceUnit.col = targetCol;
        targetUnit.row = sourceRow;
        targetUnit.col = sourceCol;

        // update original position
        sourceUnit.UpdateOriginalPosition(posA, targetRow, targetCol);
        targetUnit.UpdateOriginalPosition(posB, sourceRow, sourceCol);

        // sync NavMeshAgent
        if (sourceUnit.agent != null)
        {
            sourceUnit.agent.Warp(posA);
            sourceUnit.agent.ResetPath();
        }
        if (targetUnit.agent != null)
        {
            targetUnit.agent.Warp(posB);
            targetUnit.agent.ResetPath();
        }

        // reset animator
        if (sourceUnit.anim != null)
        {
            sourceUnit.anim.SetBool(runKey, false);
        }
        if (targetUnit.anim != null)
        {
            targetUnit.anim.SetBool(runKey, false);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Prefab Map / Pools

    private void BuildPrefabMap()
    {
        prefabMap = new Dictionary<string, GameObject[]>();
        if (upgradeEntries == null) return;

        for (int i = 0; i < upgradeEntries.Length; i++)
        {
            UnitUpgradeEntry entry = upgradeEntries[i];
            if (entry == null) continue;
            if (string.IsNullOrEmpty(entry.unitType)) continue;

            prefabMap[entry.unitType] = entry.levelPrefabs;
        }
    }

    private void EnsurePools()
    {
        if (prefabMap == null) return;

        // iterate keys manually, no foreach
        List<string> keys = new List<string>(prefabMap.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            GameObject[] prefabs = prefabMap[key];
            if (prefabs == null) continue;

            for (int j = 0; j < prefabs.Length; j++)
            {
                GameObject prefab = prefabs[j];
                if (prefab == null) continue;

                PoolManager.CreatePool(prefab, 8, 64, true);
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Utils

    // play death animation if animator exists
    private void PlayDieAnim(Unit unit)
    {
        if (unit == null) return;
        if (unit.anim == null) return;
        if (string.IsNullOrEmpty(dieKey)) return;

        unit.anim.SetTrigger(dieKey);
        Debug.Log("PlayDieAnim triggered");
    }

    // clear cell occupancy in grid
    private void ClearCell(Unit unit)
    {
        if (unit == null) return;
        if (unit.Grid == null) return;
        if (!unit.Grid.IsValidGridPosition(unit.row, unit.col)) return;

        unit.Grid.SetCellOccupied(unit.Board, unit.row, unit.col, null);
    }

    // delayed release back to pool
    private IEnumerator DelayRelease(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        PoolManager.Release(go);
    }

    #endregion

    #region End / Results

    // check if battle should end
    private void CheckBattleOver()
    {
        if (battleEnded) return;
        if (endBattleScheduled) return;
        if (gridManager == null) return;

        bool alivePlayer = HasAlive(playerBoard);
        bool aliveEnemy = HasAlive(enemyBoard);

        if (alivePlayer && aliveEnemy) return;

        endBattleScheduled = true;
        if (endBattleRoutine != null)
        {
            StopCoroutine(endBattleRoutine);
        }
        endBattleRoutine = StartCoroutine(EndBattleAfterDelay());
    }

    // delay before declaring result
    private IEnumerator EndBattleAfterDelay()
    {
        yield return new WaitForSeconds(resultDelay);
        if (battleEnded) yield break;

        bool alivePlayer = HasAlive(playerBoard);
        bool aliveEnemy = HasAlive(enemyBoard);

        battleEnded = true;
        endBattleScheduled = false;
        SetGunsEnabled(false);

        int reward = damageByPlayer;
        if (alivePlayer && !aliveEnemy)
        {
            reward = damageByPlayer * 10;
           
            if (uiManager != null) uiManager.ShowResult(true, reward);
        }
        else if (!alivePlayer && aliveEnemy)
        {
            if (uiManager != null) uiManager.ShowResult(false, reward);
        }
        else
        {
            Debug.Log("[Result] DRAW | reward=" + reward);
            if (uiManager != null) uiManager.ShowResult(false, reward);
        }

        if (endCombatOnWin && CombatManager.Instance != null)
        {
            CombatManager.Instance.EndCombat();
        }
    }

    // check if any alive unit exists on board
    private bool HasAlive(GridManager.Board board)
    {
        for (int r = 0; r < gridManager.Rows; r++)
        {
            for (int c = 0; c < gridManager.Cols; c++)
            {
                GameObject occupant = gridManager.GetOccupant(board, r, c);
                if (occupant == null) continue;

                Unit unit = occupant.GetComponent<Unit>();
                if (unit != null && unit.core != null && unit.core.Alive())
                {
                    return true;
                }
            }
        }
        return false;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────────
    #region Guns Control

    public void RegisterGun(GunController g)
    {
        if (!g) return;
        if (!guns.Contains(g)) guns.Add(g);

        bool inCombat = CombatManager.Instance != null &&
                        CombatManager.Instance.CurrentState == CombatManager.State.Combat;
        g.enabled = inCombat;
    }

    public void UnregisterGun(GunController g)
    {
        guns.Remove(g);
    }

    public void SetGunsEnabled(bool on)
    {
        for (int i = 0; i < guns.Count; i++)
            if (guns[i]) guns[i].enabled = on;
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Reset

    public void ResetBattle()
    {
        Debug.Log("🔄 ResetBattle called!");

        // stop pending end routine
        if (endBattleRoutine != null)
        {
            StopCoroutine(endBattleRoutine);
            endBattleRoutine = null;
        }
        endBattleScheduled = false;
        battleEnded = false;

        damageByPlayer = 0;
        damageByEnemy = 0;

        unitMap.Clear();
        guns.Clear();

        if (gridManager) gridManager.ClearAllUnits();
        if (botManager) botManager.SpawnFromInspector();

        if (unitManager != null)
        {
            unitManager.PlaceKnife();
            for (int i = 0; i < 3; i++) unitManager.PlaceGun();
        }

        if (uiManager != null)
        {
            uiManager.HideResultPanel();
            uiManager.ShowPlacementButtons();
        }
    }

    #endregion
    // ─────────────

    #region Scene Handling
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gridManager = FindObjectOfType<GridManager>();
        unitManager = FindObjectOfType<UnitManager>();
        uiManager = FindObjectOfType<UIManager>();
        botManager = FindObjectOfType<BotManager>();

        if (botManager != null && gridManager != null)
            botManager.SetGridManager(gridManager);

        StartCoroutine(InitWhenGridReady());
    }

    public void OnSceneReady(
      GridManager grid,
      UnitManager um,
      UIManager ui,
      BotManager bot)
    {
        gridManager = grid;
        unitManager = um;
        uiManager = ui;
        botManager = bot;

        if (botManager != null && gridManager != null)
        {
            botManager.SetGridManager(gridManager);
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.Bind(gridManager, this, botManager);
            CombatManager.Instance.ForcePrepState();
        }

        if (uiManager != null)
        {
            uiManager.ShowPlacementButtons();
        }
    }
    #endregion
}