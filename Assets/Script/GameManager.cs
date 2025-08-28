using ObjectPooling;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [Header("Refs")]
    [SerializeField] private BotManager botManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;   // 👈 thêm
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

    public event System.Action<Team?> OnBattleEnded;

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
        if (botManager != null)
            botManager.SetGridManager(gridManager);

        StartCoroutine(RunLoop());
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Register Units / Death

    public void HookUnit(Unit u)
    {
        if (u == null || u.core == null) return;
        if (unitMap.ContainsKey(u.core)) return;

        unitMap.Add(u.core, u);
        u.core.onDead += OnDeadCore;
        u.core.onHit += OnUnitHit; //add Hook
        if (u.gun)
            RegisterGun(u.gun);
    }

    public void UnhookUnit(Unit u)
    {
        if (u == null || u.core == null) return;
        if (!unitMap.ContainsKey(u.core)) return;


        if (u.gun) UnregisterGun(u.gun);
        u.core.onDead -= OnDeadCore;
        unitMap.Remove(u.core);
    }

    public void OnUnitHit(UnitCore core, int dmg)
    {
        if (core == null) return;
        if (core.team == Team.Player)
        {
            damageByEnemy += dmg;
        }
        else if (core.team == Team.Enemy)
        {
            damageByPlayer += dmg;
        }
    }

    private void OnDeadCore(UnitCore c)
    {
        if (!unitMap.TryGetValue(c, out var u)) return;

        var targeting = u.GetComponent<UnitTargeting>();
        if (targeting) targeting.enabled = false;

        var agent = u.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.enabled = false;
        }
        var rb = u.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        PlayDieAnim(u);
        ClearCell(u);
        OnUnitDead?.Invoke(u);
        StartCoroutine(DelayRelease(u.gameObject, deadDespawnDelay));
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
    #region Running (set Running bool theo Agent)

    private IEnumerator RunLoop()
    {
        var wait = new WaitForSeconds(runTick);

        while (true)
        {
            ApplyRunState(GridManager.Board.Board1);
            ApplyRunState(GridManager.Board.Board2);
            yield return wait;
        }
    }

    private void ApplyRunState(GridManager.Board b)
    {
        if (gridManager == null) return;

        for (int r = 0; r < gridManager.Rows; r++)
        {
            for (int c = 0; c < gridManager.Cols; c++)
            {
                var go = gridManager.GetOccupant(b, r, c);
                if (!go) continue;

                var agent = go.GetComponent<NavMeshAgent>();
                var anim = go.GetComponent<Animator>();

                if (!agent || !anim) continue;

                bool moving =
                    agent.enabled &&
                    agent.hasPath &&
                    agent.remainingDistance >
                    agent.stoppingDistance + 0.05f;

                anim.SetBool(runKey, moving);
            }
        }
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Merge

    public bool TryMerge(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        var targetObj = gridManager.GetOccupant(board, targetRow, targetCol);

        if (!ValidMerge(targetObj, sourceObj, out var targetUnit, out var sourceUnit))
            return false;

        int newLv = targetUnit.level + 1;

        if (!prefabMap.TryGetValue(targetUnit.unitType, out var arr))
            return false;

        if (newLv - 1 >= arr.Length || arr[newLv - 1] == null)
            return false;

        var pos = gridManager.GridToWorldPosition(board, targetRow, targetCol, true);

        CleanupSource(sourceUnit, sourceObj);
        PoolManager.Release(targetObj);

        CreateMergedUnit(arr[newLv - 1], targetUnit.unitType, newLv, board, targetRow, targetCol, pos);
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

    private void CleanupSource(Unit u, GameObject go)
    {
        if (u != null && u.Grid != null && u.Grid.IsValidGridPosition(u.row, u.col))
            u.Grid.SetCellOccupied(u.Board, u.row, u.col, null);

        PoolManager.Release(go);
        UnhookUnit(u);
    }

    private void CreateMergedUnit(
        GameObject prefab,
        string type,
        int lv,
        GridManager.Board b,
        int row,
        int col,
        Vector3 pos)
    {
        var obj = PoolManager.Spawn(prefab, pos, Quaternion.identity);
        var u = obj.GetComponent<Unit>();
        if (u == null) return;

        //Security Button
        var es = obj.GetComponentsInChildren<EventSystem>(true);
        for (int i = 0; i < es.Length; i++) Destroy(es[i].gameObject);

        var raycasters = obj.GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++) raycasters[i].enabled = false;

        u.Initialize(type, lv, gridManager, b, row, col);
        gridManager.SetCellOccupied(b, row, col, obj);

        u.MergeLockTemporary(mergeLockSeconds);

        HookUnit(u);
        OnUnitMerged?.Invoke(u, row, col);

        EnsureEventSystemExists();
    }

    private static void EnsureEventSystemExists()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Swap

    public bool TrySwap(GridManager.Board board, int targetRow, int targetCol, GameObject sourceObj)
    {
        var targetObj = gridManager.GetOccupant(board, targetRow, targetCol);

        if (!ValidSwap(targetObj, sourceObj, out var su, out var tu))
            return false;

        DoSwap(board, sourceObj, targetObj, su, tu, targetRow, targetCol);
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
        GridManager.Board b,
        GameObject src,
        GameObject tgt,
        Unit su,
        Unit tu,
        int tr,
        int tc)
    {
        int sr = su.row;
        int sc = su.col;

        var posA = gridManager.GridToWorldPosition(b, tr, tc, true);
        var posB = gridManager.GridToWorldPosition(b, sr, sc, true);

        gridManager.SetCellOccupied(b, sr, sc, null);
        gridManager.SetCellOccupied(b, tr, tc, null);

        gridManager.SetCellOccupied(b, tr, tc, src);
        gridManager.SetCellOccupied(b, sr, sc, tgt);

        su.row = tr; su.col = tc;
        tu.row = sr; tu.col = sc;

        su.UpdateOriginalPosition(posA, tr, tc);
        tu.UpdateOriginalPosition(posB, sr, sc);

        var sa = src.GetComponent<NavMeshAgent>();
        var ta = tgt.GetComponent<NavMeshAgent>();

        if (sa != null)
        {
            sa.Warp(posA);
            sa.ResetPath();
        }
        if (ta != null)
        {
            ta.Warp(posB);
            ta.ResetPath();
        }

        var sanim = src.GetComponent<Animator>();
        if (sanim) sanim.SetBool(runKey, false);

        var tanim = tgt.GetComponent<Animator>();
        if (tanim) tanim.SetBool(runKey, false);
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
            var e = upgradeEntries[i];
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.unitType)) continue;

            prefabMap[e.unitType] = e.levelPrefabs;
        }
    }

    private void EnsurePools()
    {
        if (prefabMap == null) return;

        foreach (var kv in prefabMap)
        {
            var arr = kv.Value;
            if (arr == null) continue;

            for (int j = 0; j < arr.Length; j++)
                if (arr[j] != null)
                    PoolManager.CreatePool(arr[j], 8, 64, true);
        }
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Utils

    private void PlayDieAnim(Unit u)
    {
        var anim = u.GetComponent<Animator>();
        if (anim && !string.IsNullOrEmpty(dieKey))
        {
            anim.SetTrigger(dieKey);
            Debug.Log("anbim die");
        }
    }

    private void ClearCell(Unit u)
    {
        var g = u.Grid;
        if (g == null) return;
        if (!g.IsValidGridPosition(u.row, u.col)) return;

        g.SetCellOccupied(u.Board, u.row, u.col, null);
    }

    private IEnumerator DelayRelease(GameObject go, float sec)
    {
        yield return new WaitForSeconds(sec);
        PoolManager.Release(go);
    }

    #endregion

    private void CheckBattleOver()
    {
        if (battleEnded || gridManager == null) return;

        bool alivePlayer = HasAlive(playerBoard);
        bool aliveEnemy = HasAlive(enemyBoard);

        Debug.Log($"[CheckBattleOver] alivePlayer={alivePlayer}, aliveEnemy={aliveEnemy}, " +
                  $"playerBoard={playerBoard}, enemyBoard={enemyBoard}");

        if (alivePlayer && aliveEnemy) return;

        battleEnded = true;
        SetGunsEnabled(false);

        int reward = 0;

        if (alivePlayer && !aliveEnemy)
        {
            reward = damageByPlayer * 10;
            Debug.Log($"[Result] WIN | reward={reward}");
            uiManager?.ShowResult(false /*hide?*/, 0); // đảm bảo xoá trạng thái cũ (optional)
            uiManager?.ShowResult(true, reward);       // WIN
            if (endCombatOnWin) CombatManager.Instance?.EndCombat();
            return;
        }

        if (!alivePlayer && aliveEnemy)
        {
            reward = damageByPlayer;
            Debug.Log($"[Result] LOSE | reward={reward}");
            uiManager?.ShowResult(false, reward);      // LOSE
            if (endCombatOnWin) CombatManager.Instance?.EndCombat();
            return;
        }

        // cả hai chết
        reward = damageByPlayer;
        Debug.Log($"[Result] DRAW | reward={reward}");
        uiManager?.ShowResult(false, reward);
        if (endCombatOnWin) CombatManager.Instance?.EndCombat();
    }

    private bool HasAlive(GridManager.Board b)
    {
        for (int r = 0; r < gridManager.Rows; r++)
            for (int c = 0; c < gridManager.Cols; c++)
            {
                var go = gridManager.GetOccupant(b, r, c);
                if (!go) continue;

                var u = go.GetComponent<Unit>();
                if (u != null && u.core != null && u.core.Alive())
                    return true;
            }
        return false;
    }

    public void RegisterGun(GunController g)
    {
        if (g && !guns.Contains(g)) guns.Add(g);
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

    public void ResetBattle()
    {
        Debug.Log("🔄 ResetBattle called!");

        battleEnded = false;
        damageByPlayer = 0;
        damageByEnemy = 0;

        unitMap.Clear();
        guns.Clear();

        if (gridManager)
        {
            gridManager.ClearAllUnits();
        }

        if (botManager)
        {
            botManager.SpawnFromInspector();
        }

        if (unitManager != null)
        {
            unitManager.PlaceKnife();
            for(int i = 0; i < 3; i++)
            {
                unitManager.PlaceGun();
            }
        }

        if (uiManager != null)
        {
            uiManager.HideResultPanel();
            uiManager.ShowPlacementButtons();
        }
    }
}
