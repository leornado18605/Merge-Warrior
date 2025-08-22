using System;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public enum State { Prep, Combat }
    public static CombatManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GridManager grid;     
    [SerializeField] private GameManager game;     
    [SerializeField] private BotManager bot;       

    [Header("Options")]
    public bool spawnBotsOnFight = false;
    public bool disableDragDuringCombat = true;
    public bool lockOnlyBoard1 = true;

    [Header("Nav/AI Defaults")]
    public bool stopAgentsInPrep = true;
    public bool disableTargetingInPrep = true;

    public State CurrentState { get; private set; } = State.Prep;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ApplyPrepState();
    }

    // ───────────────────────── Public API ─────────────────────────

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

        ForEachUnit(u =>
        {
            if (u.targeting) u.targeting.enabled = true;

            if (disableDragDuringCombat && u.drag) u.drag.enabled = false;

            if (u.agent)
            {
                u.agent.isStopped = false;
                u.agent.ResetPath();
            }

            if (u.targeting && u.targeting.role == UnitTargeting.Role.Knife && u.anim && !string.IsNullOrEmpty(u.targeting.runBool))
            {
                u.anim.SetBool(u.targeting.runBool, true);
            }
        });
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
        if (CurrentState == State.Prep) StartCombat();
        else EndCombat();
    }

    // ───────────────────────── Internals ─────────────────────────

    void ApplyPrepState()
    {
        LockInput(false);

        ForEachUnit(u =>
        {
            if (disableTargetingInPrep && u.targeting) u.targeting.enabled = false;
            if (u.drag) u.drag.enabled = true;

            if (stopAgentsInPrep && u.agent)
            {
                u.agent.isStopped = true;
                u.agent.ResetPath();
            }

            if (u.targeting && u.targeting.role == UnitTargeting.Role.Knife && u.anim && !string.IsNullOrEmpty(u.targeting.runBool))
            {
                u.anim.SetBool(u.targeting.runBool, false);
            }
        });
    }

    void LockInput(bool locked)
    {
        if (!grid) return;
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

    void ForEachUnit(Action<Unit> action)
    {
        if (!grid) return;

        void Scan(GridManager.Board b)
        {
            for (int r = 0; r < grid.Rows; r++)
            {
                for (int c = 0; c < grid.Cols; c++)
                {
                    var u = grid.GetOccupantUnit(b, r, c);
                    if (u != null) action(u);
                }
            }
        }

        Scan(GridManager.Board.Board1);
        Scan(GridManager.Board.Board2);
    }
}
