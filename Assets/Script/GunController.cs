using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using ObjectPooling;

[DisallowMultipleComponent]
public class GunController : MonoBehaviour
{
    #region Links & Config
    [Header("Links")]
    [SerializeField] Unit self;
    [SerializeField] UnitCore core;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator anim;

    [Header("Think/Timing")]
    [SerializeField] float think = 0.1f;
    [SerializeField] float attackGap = 0.6f;
    [SerializeField] float fireDelay = 0.15f;

    [Header("Aiming/Rotate")]
    [SerializeField] float stopDist = 6f;
    [SerializeField] float turnSpeed = 540f;

    [Header("Projectile")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float projectileSpeed = 16f;
    [SerializeField] float projectileLife = 2.5f;

    const string LG = "[ENEMY GUN]";
    #endregion

    #region State
    GridManager grid;
    Unit target;
    UnitCore targetCore;
    float lastAtk;
    Coroutine loopCo;
    #endregion

    #region Public API
    public void SetFirePoint(Transform t) { firePoint = t; }
    #endregion

    #region Lifecycle
    void OnEnable()
    {
        grid = self ? self.Grid : null;
        agent.stoppingDistance = stopDist;
        agent.updateRotation = false;
        agent.updatePosition = true;
        loopCo = StartCoroutine(Loop());


    }

    void OnDisable()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        UnsubscribeTarget();
    }

    void Update()
    {
        if (!Valid(target)) return;
        SmoothFace(target.transform.position);
    }
    #endregion

    #region Loop
    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(think);
        while (true) { Tick(); yield return wait; }
    }

    void Tick()
    {
        if (!Ready()) return;

        if (!Valid(target))
        {
            SetTarget(Reacquire());
            if (!Valid(target))
            {
                Debug.Log($"[GunController] {name} cannot find valid target | team={core.team} | tag={self.tag}");
                StopMove();
                return;
            }
        }

        Debug.Log($"[GunController] {name} has target {target.name} | team={core.team}");
        StopMove();
        TryShoot();
    }
    #endregion

    #region Targeting
    bool Valid(Unit u)
    {
        if (!u || !u.core || !u.core.Alive()) return false;
        string need = self.tag == "Player" ? "Enemy" : "Player";
        return u.CompareTag(need);
    }

    Unit Reacquire()
    {
        var opp = self.Board == GridManager.Board.Board1
            ? GridManager.Board.Board2 : GridManager.Board.Board1;

        Unit best = null; float bestD = float.MaxValue;
        for (int r = 0; r < grid.Rows; r++)
            for (int c = 0; c < grid.Cols; c++)
            {
                var u = grid.GetOccupantUnit(opp, r, c);
                if (!Valid(u)) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestD) { bestD = d; best = u; }
            }
        return best;
    }

    void SetTarget(Unit u)
    {
        if (u == target) return;
        UnsubscribeTarget();
        target = u;
        targetCore = target ? target.core : null;
        if (targetCore != null) targetCore.onDead += OnTargetDead;
    }

    void UnsubscribeTarget()
    {
        if (targetCore != null) targetCore.onDead -= OnTargetDead;
        targetCore = null;
    }

    void OnTargetDead(UnitCore _) { SetTarget(null); }
    #endregion

    #region Movement/Rotation
    void StopMove()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    void SmoothFace(Vector3 p)
    {
        var dir = p - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        var to = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, to, turnSpeed * Time.deltaTime);
    }
    #endregion

    #region Shooting
    void TryShoot()
    {
        if (Time.time - lastAtk < attackGap) return;
        if (!Valid(target)) return;
        anim?.SetTrigger("Attack");
        lastAtk = Time.time;
        StartCoroutine(FireAfterDelay());
        Debug.Log($"[GunController] {name} TryShoot() | team={core.team} | target={(target ? target.name : "null")}");
    }

    IEnumerator FireAfterDelay()
    {
        yield return new WaitForSeconds(fireDelay);
        if (!Valid(target)) SetTarget(Reacquire());
        if (!Valid(target) || !projectilePrefab || !firePoint) yield break;

        var go = PoolManager.Spawn(projectilePrefab, firePoint.position, firePoint.rotation, null);
        var proj = go ? go.GetComponent<RangedProjectile>() : null;
        if (proj)
        {
            proj.Launch(core.team, core.dmg, target.transform, projectileLife, projectileSpeed);
        }
    }
    #endregion

    #region Validation
    bool Ready()
    {
        if (!self) return false; 
        if (!core) return false; 
        if (!agent) return false; 

        if (!grid)
        {
            grid = self.Grid;
            if (!grid) 
                return false;
        }

        if (self.IsMergeLocked()) return false;

        if (!agent.isOnNavMesh)
        {
            SnapToNav();
            if (!agent.isOnNavMesh) return false;
        }
        return true;
    }
    #endregion
    void SnapToNav()
    {
        if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

}
