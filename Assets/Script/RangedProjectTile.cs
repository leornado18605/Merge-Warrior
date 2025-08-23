using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public class RangedProjectile : MonoBehaviour, IPoolable
{
    [Header("Links/Config")]
    [SerializeField] Rigidbody rb;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] bool autoFace = true;
    [SerializeField] Vector3 rotOffsetEuler;

    [Header("Fail-safe")]
    [SerializeField] float loseTargetDespawnDelay = 0.25f;
    [SerializeField] float maxLifetime = 5f;             

    Transform target;
    UnitCore targetCore;
    Team fromTeam;
    int dmg;
    float life;
    float speed;
    float lostTargetTimer;
    Quaternion RotOff => Quaternion.Euler(rotOffsetEuler);

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
            SetRotation(Quaternion.LookRotation((target.position - transform.position).normalized) * RotOff);
    }

    void FixedUpdate()
    {
        life -= Time.fixedDeltaTime;
        if (life <= 0f) { Despawn(); return; }

        bool targetAlive = (targetCore != null && targetCore.Alive());
        if (autoFace && targetAlive)
        {
            var want = Quaternion.LookRotation((target.position - transform.position).normalized) * RotOff;
            var to = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.fixedDeltaTime);
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

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        var core = other.GetComponent<UnitCore>();
        if (!core) return;
        if (!core.Alive()) return;           
        if (core.team == fromTeam) return;    

        core.Hit(new DamageData(dmg, fromTeam, transform.position));
        Despawn();
    }

    void OnTargetDead(UnitCore _)
    {
        Despawn();
    }

    public void OnSpawned() { if (rb) rb.velocity = Vector3.zero; }
    public void OnDespawned() { if (rb) rb.velocity = Vector3.zero; UnhookTarget(); target = null; }

    // helpers
    void SetRotation(Quaternion q) { if (rb) rb.MoveRotation(q); else transform.rotation = q; }

    void Despawn()
    {
        PoolManager.Release(gameObject);
    }

    void UnhookTarget()
    {
        if (targetCore != null) targetCore.onDead -= OnTargetDead;
        targetCore = null;
    }
}