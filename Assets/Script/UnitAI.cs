using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(NavMeshAgent))]
public class UnitTargeting : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    #region Role & Config

    public enum Role { Knife, Gun }

    [Header("Role")]
    public Role role = Role.Knife;

    [Header("Detect (tiles)")]
    public int knifeDetectTiles = 6;
    public int gunDetectTiles = 99;

    [Header("Ranges (m)")]
    public float knifeStopDistance = 0.1f;
    public float gunStopDistance = 6f;

    [Header("Move")]
    public float knifeSpeed = 8f;
    public float gunSpeed = 6f;
    public float knifeTurn = 720f;
    public float gunTurn = 540f;
    public float minSetDestInterval = 0.08f;
    public float agentRadius = 0.25f;

    [Header("Anim")]
    public Animator animator;
    public string runBool = "Running";
    public string attackTrigger = "Attack";

    [Header("Think")]
    public float thinkInterval = 0.1f;

    [Header("Attack")]
    public float attackGap = 0.6f;
    [SerializeField] private UnitCore selfCore;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizAttack = new(1f, 0.3f, 0f, 0.8f);
    public Color gizStop = new(0f, 1f, 0.2f, 0.8f);
    public Color gizDetect = new(0f, 0.6f, 1f, 0.6f);
    public Color gizPath = new(0.2f, 1f, 0.2f, 0.9f);
    public Color gizTarget = new(1f, 1f, 0f, 0.9f);

    [Header("Agent Avoidance")]
    public bool disableAgentAvoidance = true;                  
    public ObstacleAvoidanceType avoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    [Range(0, 99)] public int avoidancePriority = 50;            
    [Min(0.2f)] public float minSafeRadius = 0.25f;
    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Links & State

    Unit self;
    NavMeshAgent agent;
    Unit target;

    float lastSetTime;
    float lastHitTime;
    Vector3 cachedDest;

    GridManager Grid => self?.Grid;

    float StopDistance =>
        (role == Role.Knife) ? knifeStopDistance : gunStopDistance;

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Unity

    void Awake()
    {
        self = GetComponent<Unit>();
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        SetupAgent();
        SnapToNav();
    }

    void OnEnable()
    {
        SnapToNav();
        StartCoroutine(Loop());
    }

    void LateUpdate()
    {
        if (agent && !agent.updatePosition)
            transform.position = agent.nextPosition;
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Loop

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(thinkInterval);

        while (true)
        {
            if (!Ready())
            {
                SetRun(false);
                yield return wait;
                continue;
            }

            target = PickTarget();

            if (target) DoBehavior();
            else Idle();

            yield return wait;
        }
    }

    void DoBehavior()
    {
        float d = Vector3.Distance(transform.position, target.transform.position);

        if (role == Role.Knife)
        {
            if (d > knifeStopDistance + 0.05f)
            {
                MoveTo(target.transform.position);
                FaceToPath();
                SetRun(agent.velocity.sqrMagnitude > 0.01f);
            }
            else
            {
                StopMove();
                FaceTo(target.transform.position);
                TryHit();
            }
        }
        else
        {
            StopMove();
            FaceTo(target.transform.position);
            if (d <= gunStopDistance) TryHit();
        }
    }

    void Idle()
    {
        StopMove();
        SetRun(false);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Move

    void SetupAgent()
    {
        if (role == Role.Knife)
        {
            agent.speed = knifeSpeed;
            agent.angularSpeed = knifeTurn;
            agent.acceleration = knifeSpeed * 10f;
            agent.avoidancePriority = 40;
        }
        else
        {
            agent.speed = gunSpeed;
            agent.angularSpeed = gunTurn;
            agent.acceleration = gunSpeed * 10f;
            agent.avoidancePriority = 50;
        }

        agent.updateRotation = false;
        agent.updatePosition = true;

        agent.stoppingDistance = StopDistance;

        if (agentRadius > 0f) agent.radius = Mathf.Max(agentRadius, minSafeRadius);

        if (disableAgentAvoidance)
        {
            agent.obstacleAvoidanceType = avoidanceType; // NoObstacleAvoidance
            agent.avoidancePriority = avoidancePriority;
        }

        agent.autoBraking = true;
    }

    void SnapToNav()
    {
        if (NavMesh.SamplePosition(transform.position, out var hit, 10f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    void MoveTo(Vector3 dst)
    {
        if (Time.time - lastSetTime < minSetDestInterval) return;

        if (!NavMesh.SamplePosition(dst, out var hit, 10f, NavMesh.AllAreas))
            return;

        var path = new NavMeshPath();
        if (agent.CalculatePath(hit.position, path) &&
            path.status == NavMeshPathStatus.PathComplete)
        {
            agent.isStopped = false;
            agent.SetPath(path);
            cachedDest = hit.position;
            lastSetTime = Time.time;
        }
    }

    void StopMove()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    void FaceTo(Vector3 p)
    {
        var dir = p - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;

        var rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rot,
            agent.angularSpeed * Time.deltaTime
        );
    }

    void FaceToPath()
    {
        if (!agent.hasPath) return;

        var v = agent.desiredVelocity;
        v.y = 0f;
        if (v.sqrMagnitude < 1e-4f) return;

        var rot = Quaternion.LookRotation(v.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rot,
            agent.angularSpeed * Time.deltaTime
        );
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Attack

    void TryHit()
    {
        if (Time.time - lastHitTime < attackGap) return;

        TriggerAttack();
        DoHit();
        lastHitTime = Time.time;
    }

    void TriggerAttack()
    {
        if (animator && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);
    }

    void DoHit()
    {
        if (target == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        float r = (role == Role.Knife)
            ? knifeStopDistance + 0.2f
            : gunStopDistance + 0.2f;

        gm.AttackInRange(self, target, r);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Target Pick

    Unit PickTarget()
    {
        if (Grid == null) return null;

        var opp = (self.Board == GridManager.Board.Board1)
            ? GridManager.Board.Board2
            : GridManager.Board.Board1;

        string tagNeed = (self.tag == "Player") ? "Enemy" : "Player";
        int tiles = (role == Role.Gun) ? gunDetectTiles : knifeDetectTiles;
        float allow = tiles * Grid.TileSize;

        Unit best = null;
        float bestD = float.MaxValue;

        for (int r = 0; r < Grid.Rows; r++)
            for (int c = 0; c < Grid.Cols; c++)
            {
                var u = Grid.GetOccupantUnit(opp, r, c);
                if (!u || !u.gameObject.activeInHierarchy || u.tag != tagNeed) continue;

                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d <= allow && d < bestD) { bestD = d; best = u; }
            }

        return best;
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Anim

    void SetRun(bool run)
    {
        if (animator && !string.IsNullOrEmpty(runBool))
            animator.SetBool(runBool, run);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Ready & Gizmos

    bool Ready()
    {
        if (Grid == null) return false;
        if (self.IsMergeLocked()) return false;
        if (!agent.isOnNavMesh) return false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        DrawAttackZone();
        DrawDetectZone();
        DrawTargetLine();
        DrawPathPoint();
        DrawStopZone();
    }

    void DrawAttackZone()
    {
        float r = (role == Role.Knife) ? knifeStopDistance : gunStopDistance;
        Gizmos.color = gizAttack;
        Gizmos.DrawWireSphere(transform.position, r);
    }

    void DrawDetectZone()
    {
        if (Grid == null) return;

        int tiles = (role == Role.Gun) ? gunDetectTiles : knifeDetectTiles;
        float r = tiles * Grid.TileSize;

        Gizmos.color = gizDetect;
        Gizmos.DrawWireSphere(transform.position, r);
    }

    void DrawTargetLine()
    {
        if (target == null) return;

        Gizmos.color = gizTarget;
        Gizmos.DrawLine(transform.position, target.transform.position);
        Gizmos.DrawWireSphere(target.transform.position, 0.15f);
    }

    void DrawPathPoint()
    {
        if (cachedDest == default) return;

        Gizmos.color = gizPath;
        Gizmos.DrawWireSphere(cachedDest, 0.12f);
        Gizmos.DrawLine(transform.position, cachedDest);
    }

    void DrawStopZone()
    {
        float r = agent ? agent.stoppingDistance : StopDistance;
        Gizmos.color = gizStop;
        Gizmos.DrawWireSphere(transform.position, r);
    }

    #endregion
}