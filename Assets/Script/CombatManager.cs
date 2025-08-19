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
        if (!grid) grid = FindAnyObjectByType<GridManager>();
        if (!game) game = FindAnyObjectByType<GameManager>();
        if (!bot) bot = FindAnyObjectByType<BotManager>();

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

        ForEachUnit((u, go) =>
        {
            var targeting = go.GetComponent<UnitTargeting>();
            if (targeting)
            {
                targeting.enabled = true;
            }

            var drag = go.GetComponent<DraggableUnit>();
            if (drag && disableDragDuringCombat)
            {
                drag.enabled = false;
            }

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }

            var ut = go.GetComponent<UnitTargeting>();
            if (ut && ut.role == UnitTargeting.Role.Knife && ut.animator && !string.IsNullOrEmpty(ut.runBool))
            {
                ut.animator.SetBool(ut.runBool, true);
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

        ForEachUnit((u, go) =>
        {
            var targeting = go.GetComponent<UnitTargeting>();
            if (targeting && disableTargetingInPrep)
                targeting.enabled = false;

            var drag = go.GetComponent<DraggableUnit>();
            if (drag) drag.enabled = true;

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent && stopAgentsInPrep)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            var ut = go.GetComponent<UnitTargeting>();
            if (ut && ut.role == UnitTargeting.Role.Knife && ut.animator && !string.IsNullOrEmpty(ut.runBool))
            {
                ut.animator.SetBool(ut.runBool, false);
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

    void ForEachUnit(System.Action<Unit, GameObject> action)
    {
        if (grid == null) return;

        void ScanBoard(GridManager.Board b)
        {
            for (int r = 0; r < grid.Rows; r++)
                for (int c = 0; c < grid.Cols; c++)
                {
                    var go = grid.GetOccupant(b, r, c);
                    if (!go) continue;
                    var u = go.GetComponent<Unit>();
                    if (u) action(u, go);
                }
        }

        ScanBoard(GridManager.Board.Board1);
        ScanBoard(GridManager.Board.Board2);
    }
}
