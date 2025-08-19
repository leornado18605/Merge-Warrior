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
    [Min(1)] public int knifeDetectTiles = 6;
    [Min(1)] public int gunDetectTiles = 99;

    [Header("Ranges (meters)")]
    public float knifeStopDistance = 0.7f;
    public float gunKeepDistance = 6f;

    [Header("Movement")]
    public float knifeSpeed = 8f;
    public float knifeAccel = 80f;
    public float knifeTurn = 720f;
    public float gunSpeed = 6f;
    public float gunAccel = 70f;
    public float gunTurn = 540f;
    public bool useAutoBraking = false;
    public float repathEpsilon = 0.4f;
    public float minSetDestInterval = 0.08f;
    public ObstacleAvoidanceType avoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    public float agentRadius = 0.25f;

    [Header("Smoothing / Anti‑jitter")]
    public float knifeHysteresis = 0.25f;
    public float gunHysteresis = 1f;
    public bool manualRotate = true;
    public float rotateLerp = 0.25f;
    public float stopVelThreshold = 0.25f;

    [Header("Turn smoothing")]
    public bool useSteeringTarget = true;
    public float headingLerp = 0.2f;
    public float minTurnRate = 240f;
    public float maxTurnRate = 720f;
    public float turnInPlaceAngle = 60f;
    public float turnInPlaceSpeed = 2.5f;

    [Header("Think Loop")]
    public float thinkInterval = 0.1f;

    [Header("High‑speed smoothing")]
    public bool enableSlowdownNearTarget = true;
    public float slowDownRadius = 3f;
    public float minChaseSpeed = 4f;
    public bool enableCurveSlowdown = true;
    [Range(10f, 180f)] public float curveSlowdownAngle = 60f;
    [Range(0.2f, 1f)] public float curveSlowdownFactor = 0.5f;
    public float stateCooldown = 0.25f;
    public bool disableAvoidanceWhenClose = true;
    public float disableAvoidanceDist = 1.2f;
    public float avoidHys = 0.3f;

    [Header("Animation")]
    public Animator animator;
    public string runBool = "Running";

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public bool gizmosOnlyWhenSelected = true;
    public float gizmoYOffset = 0.05f;
    public Color colorStop = new(0.2f, 1f, 0.2f, 0.9f);
    public Color colorDetect = new(1f, 0.85f, 0.2f, 0.9f);
    public Color colorPath = Color.white;

    // ─────────────────────────────────────────────────────────────
    Unit self;
    NavMeshAgent agent;
    Unit target;

    float baseSpeed, baseAccel, currentSpeed;
    ObstacleAvoidanceType baseAvoid;
    float nextStateChangeTime, lastSetDestTime;
    Vector3 cachedDest, smoothedDir;

    GridManager Grid => self?.Grid;
    float StopDistance => role == Role.Knife ? knifeStopDistance : gunKeepDistance;
    float Hysteresis => role == Role.Knife ? knifeHysteresis : gunHysteresis;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        self = GetComponent<Unit>();
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
        ConfigureAgent();
    }

    void OnEnable() => StartCoroutine(Loop());
    void LateUpdate() { if (agent && !agent.updatePosition) transform.position = agent.nextPosition; }

    void ConfigureAgent()
    {
        float spd = role == Role.Knife ? knifeSpeed : gunSpeed;
        float acc = role == Role.Knife ? knifeAccel : gunAccel;
        float turn = role == Role.Knife ? knifeTurn : gunTurn;

        acc = Mathf.Max(acc, spd * 10f);

        agent.speed = spd;
        agent.acceleration = acc;
        agent.angularSpeed = turn;
        agent.autoBraking = useAutoBraking;
        agent.stoppingDistance = StopDistance;
        agent.updateRotation = false;   
        agent.updatePosition = false;   
        agent.obstacleAvoidanceType = avoidanceType;
        agent.autoRepath = true;
        if (agentRadius > 0f) agent.radius = agentRadius;
        agent.avoidancePriority = role == Role.Knife ? 40 : 50;

        baseSpeed = spd; baseAccel = acc; baseAvoid = avoidanceType;
        currentSpeed = spd;
        smoothedDir = transform.forward;
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(thinkInterval);
        while (true)
        {
            if (!Ready()) { StopAndAnim(false); yield return wait; continue; }

            target = FindNearestOnOppositeBoard();

            if (target)
            {
                Vector3 aim = target.transform.position;
                float d = DistanceTo(aim);

                SmoothSpeed(d, aim);
                SmoothAvoidance(d);
                ApplyStopOrGo(d);
                UpdateDestination(aim);
                SmoothRotate(aim);

                if (role == Role.Knife) SetRunAnim(!agent.isStopped && d > StopDistance + 0.05f);
            }
            else
            {
                StopAndAnim(false);
            }

            yield return wait;
        }
    }

    bool Ready() => Grid != null && !self.IsMergeLocked();

    // ===== Movement =============================================================

    float DistanceTo(Vector3 dest)
    {
        if (agent.hasPath && !agent.pathPending) return agent.remainingDistance;
        return Vector3.Distance(transform.position, dest);
    }

    void SmoothSpeed(float d, Vector3 aim)
    {
        float targetSpeed = baseSpeed;
        float targetAccel = baseAccel;

        if (enableSlowdownNearTarget)
        {
            float start = Mathf.Max(StopDistance, 0.01f);
            float t = Mathf.InverseLerp(start, start + slowDownRadius, d);
            targetSpeed = Mathf.Lerp(minChaseSpeed, baseSpeed, t);
            targetAccel = Mathf.Max(baseAccel, targetSpeed * 10f);
        }

        float angleToAim = AngleTo(aim);
        if (angleToAim >= turnInPlaceAngle) targetSpeed = Mathf.Min(targetSpeed, turnInPlaceSpeed);

        if (enableCurveSlowdown && agent.hasPath && agent.path.corners.Length >= 3)
            if (MaxTurnAngle(agent.path) >= curveSlowdownAngle)
                targetSpeed *= curveSlowdownFactor;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 50f * Time.deltaTime);
        agent.speed = currentSpeed;
        agent.acceleration = Mathf.MoveTowards(agent.acceleration, targetAccel, 200f * Time.deltaTime);
    }

    void SmoothAvoidance(float d)
    {
        float enter = disableAvoidanceDist - avoidHys * 0.5f;
        float exit = disableAvoidanceDist + avoidHys * 0.5f;

        if (disableAvoidanceWhenClose && d <= enter &&
            agent.obstacleAvoidanceType != ObstacleAvoidanceType.NoObstacleAvoidance)
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        else if (d >= exit && agent.obstacleAvoidanceType != baseAvoid)
            agent.obstacleAvoidanceType = baseAvoid;
    }

    void ApplyStopOrGo(float d)
    {
        if (Time.time < nextStateChangeTime) return;

        float enter = Mathf.Max(0.01f, StopDistance - Hysteresis * 0.5f);
        float exit = StopDistance + Hysteresis * 0.5f;

        if (d <= enter && agent.velocity.magnitude <= stopVelThreshold && !agent.isStopped)
        { agent.isStopped = true; nextStateChangeTime = Time.time + stateCooldown; }
        else if (d >= exit && agent.isStopped)
        { agent.isStopped = false; nextStateChangeTime = Time.time + stateCooldown; }
    }

    void UpdateDestination(Vector3 raw)
    {
        if (Time.time - lastSetDestTime < minSetDestInterval) return;
        if (!agent.hasPath || (agent.destination - raw).sqrMagnitude > repathEpsilon * repathEpsilon)
        {
            cachedDest = NavMesh.SamplePosition(raw, out var hit, 2f, agent.areaMask) ? hit.position : raw;
            agent.SetDestination(cachedDest);
            lastSetDestTime = Time.time;
        }
    }

    void SmoothRotate(Vector3 aimWorld)
    {
        Vector3 desiredDir = DesiredDirection(aimWorld);
        smoothedDir = Vector3.Slerp(smoothedDir, desiredDir, headingLerp);

        float v01 = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(1f, baseSpeed));
        float turnRate = Mathf.Lerp(minTurnRate, maxTurnRate, v01);

        Quaternion targetRot = Quaternion.LookRotation(smoothedDir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnRate * Time.deltaTime);
    }

    Vector3 DesiredDirection(Vector3 aimWorld)
    {
        Vector3 dir;
        if (useSteeringTarget && agent.hasPath)
            dir = agent.steeringTarget - transform.position;
        else
            dir = aimWorld - transform.position;

        dir.y = 0f;
        return dir.sqrMagnitude > 1e-6f ? dir.normalized : transform.forward;
    }

    float AngleTo(Vector3 aimWorld)
    {
        Vector3 to = aimWorld - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 1e-4f) return 0f;
        return Vector3.Angle(transform.forward, to);
    }

    void StopAndAnim(bool running)
    {
        agent.isStopped = true;
        if (role == Role.Knife) SetRunAnim(running);
    }

    void SetRunAnim(bool isRunning)
    {
        if (animator && !string.IsNullOrEmpty(runBool))
            animator.SetBool(runBool, isRunning);
    }

    // ===== Targeting (Opposite Board ONLY) =====================================

    Unit FindNearestOnOppositeBoard()
    {
        var opposite = self.Board == GridManager.Board.Board1 ? GridManager.Board.Board2 : GridManager.Board.Board1;

        Vector2Int probe = Grid.WorldToGridNearest(opposite, transform.position);

        int radius = role == Role.Gun ? gunDetectTiles : knifeDetectTiles;

        System.Predicate<Unit> filter = u => u != null && u.gameObject.activeInHierarchy;

        return Grid.FindNearestUnit(opposite, probe.x, probe.y, filter, radius, priorityAdjacency: true);
    }

    float MaxTurnAngle(NavMeshPath path)
    {
        var c = path.corners; float max = 0f;
        for (int i = 1; i < c.Length - 1; i++)
        {
            Vector3 a = c[i] - c[i - 1]; a.y = 0;
            Vector3 b = c[i + 1] - c[i]; b.y = 0;
            if (a.sqrMagnitude < 1e-4f || b.sqrMagnitude < 1e-4f) continue;
            float ang = Vector3.Angle(a, b);
            if (ang > max) max = ang;
        }
        return max;
    }

    // ===== Gizmos ===============================================================
    void OnDrawGizmos() { if (drawGizmos && !gizmosOnlyWhenSelected) DrawGiz(); }
    void OnDrawGizmosSelected() { if (drawGizmos && gizmosOnlyWhenSelected) DrawGiz(); }

    void DrawGiz()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!self) self = GetComponent<Unit>();
        if (!Grid || !self) return;

        Vector3 me = transform.position + Vector3.up * gizmoYOffset;
        Gizmos.color = colorStop;
        DrawCircle(me, StopDistance);

        if (agent.hasPath && agent.path != null)
        {
            Gizmos.color = colorPath;
            var cs = agent.path.corners;
            for (int i = 0; i < cs.Length - 1; i++)
                Gizmos.DrawLine(cs[i] + Vector3.up * gizmoYOffset, cs[i + 1] + Vector3.up * gizmoYOffset);
        }

        var enemyBoard = self.Board == GridManager.Board.Board1 ? GridManager.Board.Board2 : GridManager.Board.Board1;
        var probe = Grid.WorldToGridNearest(enemyBoard, transform.position);
        Vector3 center = Grid.GridToWorldPosition(enemyBoard, probe.x, probe.y) + Vector3.up * gizmoYOffset;
        float radius = Grid.TileSize * (role == Role.Gun ? gunDetectTiles : knifeDetectTiles);
        Gizmos.color = colorDetect; DrawCircle(center, radius);
    }

    void DrawCircle(Vector3 c, float r, int seg = 32)
    {
        if (r <= 0f) return;
        float step = Mathf.PI * 2f / seg;
        Vector3 p = c + new Vector3(r, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = step * i;
            Vector3 q = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Gizmos.DrawLine(p, q); p = q;
        }
    }
}
