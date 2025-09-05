using System;
using UnityEngine;
using UnityEngine.AI;

public class CombatManager : MonoBehaviour
{
    public enum State { Prep, Combat }
    public static CombatManager Instance { get; private set; }

    public static event Action OnCombatStart;
    public static event Action OnCombatEnd;
    #region References
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameManager game;
    [SerializeField] private BotManager bot;
    #endregion

    #region Options
    [Header("Options")]
    [SerializeField] private bool snapUnitsBackInPrep = true;
    [SerializeField] private bool spawnBotsOnFight = false;
    [SerializeField] private bool disableDragDuringCombat = true;
    [SerializeField] private bool lockOnlyBoard1 = true;

    [Header("Nav/AI Defaults")]
    [SerializeField] private bool stopAgentsInPrep = true;
    [SerializeField] private bool disableTargetingInPrep = true;
    #endregion

    public State CurrentState { get; private set; } = State.Prep;

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
        if (grid != null) ApplyPrepState();
    }
    #endregion

    #region Binding
    public void Bind(GridManager gm, GameManager g, BotManager bm)
    {
        grid = gm;
        game = g;
        bot = bm;
    }
    #endregion

    #region State Control
    public void StartCombat()
    {
        if (CurrentState == State.Combat) return;

        CurrentState = State.Combat;
        LockInput(true);

        if (spawnBotsOnFight && bot != null) SpawnBots();

        ForEachUnit(HandleStartForUnit);
        if (game != null) game.SetGunsEnabled(true);

    }

    public void EndCombat()
    {
        if (CurrentState == State.Prep) return;

        CurrentState = State.Prep;
        ApplyPrepState();

    }

    public void ToggleFight()
    {
        if (CurrentState == State.Prep) StartCombat();
        else EndCombat();
    }

    public void ForcePrepState()
    {
        CurrentState = State.Prep;
        ApplyPrepState();
    }
    #endregion

    #region Prep State
    private void ApplyPrepState()
    {
        if (grid == null) return;

        LockInput(false);
        ForEachUnit(HandlePrepForUnit);

        if (snapUnitsBackInPrep) SnapAllUnitsToCells();
        if (game != null) game.SetGunsEnabled(false);
    }

    private void LockInput(bool locked)
    {
        if (grid == null) return;

        if (lockOnlyBoard1) grid.lockBoard1Input = locked;
        else
        {
            grid.lockBoard1Input = locked;
            grid.lockBoard2Input = locked;
        }
    }
    #endregion

    #region Unit Iteration
    private void ForEachUnit(Action<Unit> action)
    {
        if (grid == null || action == null) return;

        IterateBoard(GridManager.Board.Board1, action);
        IterateBoard(GridManager.Board.Board2, action);
    }

    private void IterateBoard(GridManager.Board board, Action<Unit> action)
    {
        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                Unit u = grid.GetOccupantUnit(board, r, c);
                if (u != null) action(u);
            }
        }
    }
    #endregion

    #region Unit State Handlers
    private void HandleStartForUnit(Unit u)
    {
        EnsureAgentOnNavMesh(u);

        if (u.targeting != null) u.targeting.enabled = true;

        var gun = u.GetComponent<GunController>();
        if (gun != null) gun.enabled = true;

        if (disableDragDuringCombat && u.drag != null) u.drag.enabled = false;

        EnableAgentMovement(u);
        TriggerRunAnimation(u);
    }

    private void HandlePrepForUnit(Unit u)
    {
        if (disableTargetingInPrep && u.targeting != null) u.targeting.enabled = false;
        if (u.drag != null) u.drag.enabled = true;

        StopAgentIfNeeded(u);
        StopRunAnimation(u);
    }
    #endregion

    #region Agent Helpers
    private void EnsureAgentOnNavMesh(Unit u)
    {
        if (u == null || u.agent == null || grid == null) return;

        if (!u.agent.enabled) u.agent.enabled = true;

        Vector3 pos = grid.GridToWorldPosition(u.Board, u.row, u.col, true);

        NavMeshHit hit;
        bool ok = NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas);

        u.agent.Warp(ok ? hit.position : pos);
        u.agent.ResetPath();
    }

    private void EnableAgentMovement(Unit u)
    {
        if (u.agent != null)
        {
            u.agent.isStopped = false;
            u.agent.ResetPath();
        }
    }

    private void StopAgentIfNeeded(Unit u)
    {
        if (stopAgentsInPrep && u.agent != null)
        {
            u.agent.isStopped = true;
            u.agent.ResetPath();
            u.agent.velocity = Vector3.zero;
        }
    }
    #endregion

    #region Snap Helpers
    private void SnapAllUnitsToCells()
    {
        if (grid == null) return;

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                SnapCell(GridManager.Board.Board1, r, c);
                SnapCell(GridManager.Board.Board2, r, c);
            }
        }
    }

    private void SnapCell(GridManager.Board board, int row, int col)
    {
        Unit u = grid.GetOccupantUnit(board, row, col);
        if (u == null) return;

        Vector3 pos = grid.GridToWorldPosition(board, row, col, true);
        u.transform.position = pos;
        u.UpdateOriginalPosition(pos, row, col);

        SnapAgentToPosition(u, pos);
        StopRunAnimation(u);
    }

    private void SnapAgentToPosition(Unit u, Vector3 pos)
    {
        if (u.agent == null) return;
        if (!u.agent.enabled) u.agent.enabled = true;

        NavMeshHit hit;
        bool ok = NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas);

        u.agent.Warp(ok ? hit.position : pos);
        u.agent.ResetPath();
        u.agent.isStopped = true;
        u.agent.velocity = Vector3.zero;
    }
    #endregion

    #region Animation Helpers
    private void TriggerRunAnimation(Unit u)
    {
        if (u.targeting != null &&
            u.anim != null &&
            !string.IsNullOrEmpty(u.targeting.runBool) &&
            u.targeting.role == UnitTargeting.Role.Knife)
        {
            u.anim.SetBool(u.targeting.runBool, true);
        }
    }

    private void StopRunAnimation(Unit u)
    {
        if (u.targeting != null &&
            u.anim != null &&
            !string.IsNullOrEmpty(u.targeting.runBool))
        {
            u.anim.SetBool(u.targeting.runBool, false);
        }
    }
    #endregion

    #region Bots
    private void SpawnBots()
    {
        bot.SetGridManager(grid);
        bot.SpawnFromInspector();
    }
    #endregion

    #region Apply State
    public void ApplyStateToUnit(Unit u)
    {
        if (u == null) return;

        bool isPrep = CurrentState == State.Prep;

        if (u.drag != null) u.drag.enabled = isPrep;

        if (u.agent != null)
        {
            if (!u.agent.enabled) u.agent.enabled = true;
            u.agent.isStopped = isPrep && stopAgentsInPrep;
            u.agent.ResetPath();
        }

        if (disableTargetingInPrep)
        {
            if (u.targeting != null) u.targeting.enabled = !isPrep;
        }
        else
        {
            if (u.targeting != null) u.targeting.enabled = true;
        }

        if (!isPrep) EnsureAgentOnNavMesh(u);
    }
    #endregion
}
