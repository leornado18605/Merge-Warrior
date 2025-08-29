using System;
using UnityEngine;
using UnityEngine.AI;

public class CombatManager : MonoBehaviour
{
    public enum State { Prep, Combat }

    public static CombatManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameManager game;
    [SerializeField] private BotManager bot;

    [Header("Options")]
    [SerializeField] private bool snapUnitsBackInPrep = true;
    public bool spawnBotsOnFight = false;
    public bool disableDragDuringCombat = true;
    public bool lockOnlyBoard1 = true;

    [Header("Nav/AI Defaults")]
    public bool stopAgentsInPrep = true;
    public bool disableTargetingInPrep = true;

    public State CurrentState { get; private set; } = State.Prep;

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
        ApplyPrepState();
    }

    [ContextMenu("Start Combat")]
    public void StartCombat()
    {
        if (CurrentState == State.Combat) return;

        CurrentState = State.Combat;
        LockInput(true);

        if (spawnBotsOnFight && bot != null)
        {
            bot.SetGridManager(grid);
            bot.SpawnFromInspector();
        }

        ForEachUnit(HandleStartForUnit);
        GameManager.Instance?.SetGunsEnabled(true);
    }

    [ContextMenu("End Combat")]
    public void EndCombat()
    {
        if (CurrentState == State.Prep) return;

        CurrentState = State.Prep;
        ApplyPrepState();
    }

    public void ToggleFight()
    {
        if (CurrentState == State.Prep)
            StartCombat();
        else
            EndCombat();
    }

    public void ForcePrepState()
    {
        CurrentState = State.Prep;
        ApplyPrepState();
    }

    private void ApplyPrepState()
    {
        LockInput(false);

        ForEachUnit(HandlePrepForUnit);

        if (snapUnitsBackInPrep)
            SnapAllUnitsToCells();

        GameManager.Instance?.SetGunsEnabled(false);
    }

    private void LockInput(bool locked)
    {
        if (grid == null) return;

        if (lockOnlyBoard1)
        {
            grid.lockBoard1Input = locked;
        }
        else
        {
            grid.lockBoard1Input = locked;
            grid.lockBoard2Input = locked;
        }
    }

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

    private void HandleStartForUnit(Unit u)
    {
        EnsureAgentOnNavMesh(u);

        if (u.targeting != null)
            u.targeting.enabled = true;

        if (disableDragDuringCombat && u.drag != null)
            u.drag.enabled = false;

        if (u.agent != null)
        {
            u.agent.isStopped = false;
            u.agent.ResetPath();
        }

        if (u.targeting != null && u.anim != null && !string.IsNullOrEmpty(u.targeting.runBool))
            if (u.targeting.role == UnitTargeting.Role.Knife)
                u.anim.SetBool(u.targeting.runBool, true);
    }

    private void HandlePrepForUnit(Unit u)
    {
        if (disableTargetingInPrep && u.targeting != null)
            u.targeting.enabled = false;

        if (u.drag != null)
            u.drag.enabled = true;

        if (stopAgentsInPrep && u.agent != null)
        {
            u.agent.isStopped = true;
            u.agent.ResetPath();
            u.agent.velocity = Vector3.zero;
        }

        if (u.targeting != null && u.anim != null && !string.IsNullOrEmpty(u.targeting.runBool))
            u.anim.SetBool(u.targeting.runBool, false);
    }

    private void EnsureAgentOnNavMesh(Unit u)
    {
        if (u == null) return;
        if (u.agent == null) return;
        if (grid == null) return;

        if (!u.agent.enabled)
            u.agent.enabled = true;

        Vector3 pos = grid.GridToWorldPosition(u.Board, u.row, u.col, true);

        NavMeshHit hit;
        bool ok = NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas);

        if (ok)
            u.agent.Warp(hit.position);
        else
            u.agent.Warp(pos);

        u.agent.ResetPath();
    }

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

        if (u.agent != null)
        {
            if (!u.agent.enabled)
                u.agent.enabled = true;

            NavMeshHit hit;
            bool ok = NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas);

            if (ok)
                u.agent.Warp(hit.position);
            else
                u.agent.Warp(pos);

            u.agent.ResetPath();
            u.agent.isStopped = true;
            u.agent.velocity = Vector3.zero;
        }

        if (u.targeting != null && u.anim != null && !string.IsNullOrEmpty(u.targeting.runBool))
            u.anim.SetBool(u.targeting.runBool, false);
    }
}
