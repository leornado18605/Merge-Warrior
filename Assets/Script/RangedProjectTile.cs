using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public class RangedProjectile : MonoBehaviour, IPoolable
{
    #region Config
    [Header("Links/Config")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private bool autoFace = true;
    [SerializeField] private Vector3 rotOffsetEuler;

    [Header("Fail-safe")]
    [SerializeField] private float loseTargetDespawnDelay = 0.25f;
    [SerializeField] private float maxLifetime = 5f;
    #endregion

    #region Runtime state
    private Transform target;
    private UnitCore targetCore;
    private Team fromTeam;
    private int dmg;
    private float life;
    private float speed;
    private float lostTargetTimer;
    private Quaternion RotOff => Quaternion.Euler(rotOffsetEuler);
    #endregion

    #region Launch
    public void Launch(Team from, int damage, Transform t, float lifetime, float projSpeed)
    {
        fromTeam = from;
        dmg = Mathf.Max(1, damage);
        target = t;
        life = (lifetime > 0f) ? lifetime : maxLifetime;
        speed = Mathf.Max(0.1f, projSpeed);
        lostTargetTimer = 0f;

        if (rb) rb.velocity = Vector3.zero;

        UnhookTarget();
        targetCore = t ? t.GetComponent<UnitCore>() : null;
        if (targetCore != null) targetCore.onDead += OnTargetDead;

        if (target)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            SetRotation(Quaternion.LookRotation(dir) * RotOff);
        }
    }
    #endregion

    #region FixedUpdate
    private void FixedUpdate()
    {
        life -= Time.fixedDeltaTime;
        if (life <= 0f) { Despawn(); return; }

        bool targetAlive = (targetCore != null && targetCore.Alive());
        if (autoFace && targetAlive)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            Quaternion want = Quaternion.LookRotation(dir) * RotOff;
            Quaternion to = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.fixedDeltaTime);
            SetRotation(to);
        }
        else
        {
            lostTargetTimer += Time.fixedDeltaTime;
            if (lostTargetTimer >= loseTargetDespawnDelay)
            {
                Despawn();
                return;
            }
        }

        if (rb) rb.velocity = transform.forward * speed;
        else transform.position += transform.forward * speed * Time.fixedDeltaTime;
    }
    #endregion

    #region Collision
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        UnitCore core = other.GetComponent<UnitCore>();
        if (!core) return;
        if (!core.Alive()) return;
        if (core.team == fromTeam) return;

        core.Hit(new DamageData(dmg, fromTeam, transform.position));
        Despawn();
    }
    #endregion

    #region Target Hooks
    private void OnTargetDead(UnitCore _) { Despawn(); }

    private void UnhookTarget()
    {
        if (targetCore != null) targetCore.onDead -= OnTargetDead;
        targetCore = null;
    }
    #endregion

    #region Pool Callbacks
    public void OnSpawned() { if (rb) rb.velocity = Vector3.zero; }
    public void OnDespawned()
    {
        if (rb) rb.velocity = Vector3.zero;
        UnhookTarget();
        target = null;
    }
    #endregion

    #region Helpers
    private void SetRotation(Quaternion q)
    {
        if (rb) rb.MoveRotation(q);
        else transform.rotation = q;
    }

    private void Despawn()
    {
        PoolManager.Release(gameObject);
    }
    #endregion
}
