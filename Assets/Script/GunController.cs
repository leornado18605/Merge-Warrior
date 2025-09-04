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

        if (CombatManager.Instance == null ||
            CombatManager.Instance.CurrentState == CombatManager.State.Prep)
        {
            enabled = false; 
            return;
        }

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
                StopMove();
                return;
            }
        }

        StopMove();
        TryShoot();
    }
    #endregion

    #region Targeting

    private bool Valid(Unit unit)
    {
        if (unit == null) return false;
        if (unit.core == null) return false;
        if (!unit.core.Alive()) return false;

        string neededTag;
        if (self.tag == "Player")
        {
            neededTag = "Enemy";
        }
        else
        {
            neededTag = "Player";
        }

        return unit.CompareTag(neededTag);
    }

    private Unit Reacquire()
    {
        GridManager.Board opponentBoard;
        if (self.Board == GridManager.Board.Board1)
        {
            opponentBoard = GridManager.Board.Board2;
        }
        else
        {
            opponentBoard = GridManager.Board.Board1;
        }

        Unit bestUnit = null;
        float bestDistance = float.MaxValue;

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                Unit candidate = grid.GetOccupantUnit(opponentBoard, r, c);
                if (!Valid(candidate)) continue;

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestUnit = candidate;
                }
            }
        }

        return bestUnit;
    }

    private void SetTarget(Unit unit)
    {
        if (unit == target) return;

        UnsubscribeTarget();
        target = unit;

        if (target != null)
        {
            targetCore = target.core;
            if (targetCore != null)
            {
                targetCore.onDead += OnTargetDead;
            }
        }
        else
        {
            targetCore = null;
        }
    }

    private void UnsubscribeTarget()
    {
        if (targetCore != null)
        {
            targetCore.onDead -= OnTargetDead;
        }
        targetCore = null;
    }

    private void OnTargetDead(UnitCore deadCore)
    {
        SetTarget(null);
    }

    #endregion

    #region Movement / Rotation

    private void StopMove()
    {
        if (agent == null) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void SmoothFace(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Shooting

    private void TryShoot()
    {
        if (Time.time - lastAtk < attackGap) return;
        if (!Valid(target)) return;

        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        lastAtk = Time.time;
        StartCoroutine(FireAfterDelay());

        string targetName = (target != null) ? target.name : "null";
    }

    private IEnumerator FireAfterDelay()
    {
        yield return new WaitForSeconds(fireDelay);

        if (!Valid(target))
        {
            SetTarget(Reacquire());
        }

        if (!Valid(target)) yield break;
        if (projectilePrefab == null) yield break;
        if (firePoint == null) yield break;

        GameObject projectileObject = PoolManager.Spawn(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation,
            null
        );

        if (projectileObject == null) yield break;

        RangedProjectile projectile =
            projectileObject.GetComponent<RangedProjectile>();

        if (projectile != null)
        {
            projectile.Launch(
                core.team,
                core.dmg,
                target.transform,
                projectileLife,
                projectileSpeed
            );
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

    #region Navigation

    private void SnapToNav()
    {
        NavMeshHit hit;

        bool found = NavMesh.SamplePosition(
            transform.position,
            out hit,
            5f,
            NavMesh.AllAreas
        );

        if (found)
        {
            agent.Warp(hit.position);
        }
    }

    #endregion

}
