using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(NavMeshAgent))]
public class UnitTargeting : MonoBehaviour
{
    public enum Role { Knife, Gun }

    [Header("Role")]
    public Role role = Role.Knife;

    [Header("Detect (tiles)")]
    public int knifeDetectTiles = 6;
    public int gunDetectTiles = 99;

    [Header("Ranges (meters)")]
    public float knifeStopDistance = 0.1f;
    public float gunStopDistance = 6f;

    [Header("Movement")]
    public float knifeSpeed = 8f;
    public float gunSpeed = 6f;
    public float knifeTurn = 720f;
    public float gunTurn = 540f;
    public float repathEpsilon = 0.4f;
    public float minSetDestInterval = 0.08f;
    public float agentRadius = 0.25f;

    [Header("Animation")]
    public Animator animator;
    public string runBool = "Running";

    [Header("Think Loop")]
    public float thinkInterval = 0.1f;

    // ────────────────────────────────
    Unit self;
    NavMeshAgent agent;
    Unit target;

    float baseSpeed;
    float lastSetDestTime;
    Vector3 cachedDest;
    Vector3 smoothedDir;

    GridManager Grid => self?.Grid;
    float StopDistance => (role == Role.Knife) ? knifeStopDistance : gunStopDistance;

    // ────────────────────────────────
    void Awake()
    {
        self = GetComponent<Unit>();
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        ConfigureAgent();
        SnapToNavMesh();
    }

    void OnEnable()
    {
        SnapToNavMesh();
        StartCoroutine(Loop());
    }

    void LateUpdate()
    {
        if (agent && !agent.updatePosition)
            transform.position = agent.nextPosition;
    }

    void ConfigureAgent()
    {
        if (role == Role.Knife)
        {
            baseSpeed = knifeSpeed;
            agent.acceleration = knifeSpeed * 10f;
            agent.angularSpeed = knifeTurn;
            agent.avoidancePriority = 40;
        }
        else
        {
            baseSpeed = gunSpeed;
            agent.acceleration = gunSpeed * 10f;
            agent.angularSpeed = gunTurn;
            agent.avoidancePriority = 50;
        }

        agent.speed = baseSpeed;
        agent.stoppingDistance = StopDistance;
        agent.updateRotation = false;   
        agent.updatePosition = false;   
        if (agentRadius > 0f) agent.radius = agentRadius;

        smoothedDir = transform.forward;
    }

    void SnapToNavMesh()
    {
        if (NavMesh.SamplePosition(transform.position, out var hit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(thinkInterval);

        while (true)
        {
            if (!Ready())
            {
                StopAndAnim(false);
                yield return wait;
                continue;
            }

            target = FindNearestOnOppositeBoard();

            if (target != null)
            {
                Vector3 aim = target.transform.position;
                UpdateDestinationSmooth(aim);

                if (agent.hasPath)
                    SmoothRotate();

                SetRunAnim(agent.velocity.magnitude > 0.1f);
            }
            else
            {
                StopAndAnim(false);
            }

            yield return wait;
        }
    }

    bool Ready()
    {
        if (Grid == null) return false;
        if (self.IsMergeLocked()) return false;
        if (!agent.isOnNavMesh) return false;
        return true;
    }

    void UpdateDestinationSmooth(Vector3 raw)
    {
        if (Time.time - lastSetDestTime < minSetDestInterval) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(raw, out hit, 10f, NavMesh.AllAreas))
        {
            cachedDest = hit.position;
        }
        else
        {
            return; 
        }

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(cachedDest, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.isStopped = false;
            agent.SetPath(path);
            lastSetDestTime = Time.time;
        }

    }

    void SmoothRotate()
    {
        if (!agent.hasPath || agent.desiredVelocity.sqrMagnitude < 1e-4f)
            return;

        Vector3 vel = agent.desiredVelocity;
        vel.y = 0f;
        if (vel.sqrMagnitude < 1e-4f) return;

        Vector3 targetDir = vel.normalized;
        Vector3 forward = smoothedDir;

        float maxTurnPerFrame = 30f;
        float angle = Vector3.SignedAngle(forward, targetDir, Vector3.up);

        if (Mathf.Abs(angle) > maxTurnPerFrame)
            targetDir = Quaternion.AngleAxis(Mathf.Sign(angle) * maxTurnPerFrame, Vector3.up) * forward;

        smoothedDir = Vector3.Slerp(forward, targetDir, Time.deltaTime * 8f);

        Quaternion targetRot = Quaternion.LookRotation(smoothedDir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            agent.angularSpeed * Time.deltaTime
        );
    }

    void StopAndAnim(bool running)
    {
        agent.isStopped = true;
        SetRunAnim(running);
    }

    void SetRunAnim(bool isRunning)
    {
        if (animator && !string.IsNullOrEmpty(runBool))
            animator.SetBool(runBool, isRunning);
    }

    // ───── Targeting: 
    Unit FindNearestOnOppositeBoard()
    {
        if (Grid == null) return null;

        GridManager.Board oppositeBoard =
            (self.Board == GridManager.Board.Board1) ? GridManager.Board.Board2 : GridManager.Board.Board1;

        int detectRadius = (role == Role.Gun) ? gunDetectTiles : knifeDetectTiles;
        string targetTag = (self.tag == "Player") ? "Enemy" : "Player";

        Unit nearest = null;
        float bestDist = float.MaxValue;

        for (int r = 0; r < Grid.Rows; r++)
        {
            for (int c = 0; c < Grid.Cols; c++)
            {
                Unit u = Grid.GetOccupantUnit(oppositeBoard, r, c);
                if (u && u.gameObject.activeInHierarchy && u.tag == targetTag)
                {
                    float dist = Vector3.Distance(transform.position, u.transform.position);
                    if (dist <= detectRadius * Grid.TileSize && dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = u;
                    }
                }
            }
        }
        return nearest;
    }
}
