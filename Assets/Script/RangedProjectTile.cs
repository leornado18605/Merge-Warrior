using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public class RangedProjectile : MonoBehaviour, IPoolable
{
    [Header("Links/Config")]
    [SerializeField] Rigidbody rb;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float turnSpeed = 720f;

    Transform target;
    Team fromTeam; int dmg;
    float life; float speed;

    public void Launch(Team from, int damage, Transform t, float lifetime, float projSpeed)
    {
        fromTeam = from; dmg = Mathf.Max(1, damage);
        target = t; life = lifetime; speed = Mathf.Max(0.1f, projSpeed);
        if (rb) rb.velocity = Vector3.zero;
        if (target) transform.forward = (target.position - transform.position).normalized;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { PoolManager.Release(gameObject); return; }

        Vector3 aim = target ? (target.position - transform.position) : transform.forward;
        if (aim.sqrMagnitude > 1e-6f)
        {
            var to = Quaternion.LookRotation(aim.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, to, turnSpeed * Time.deltaTime);
        }
        if (rb) rb.velocity = transform.forward * speed;
        else transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;
        var core = other.GetComponent<UnitCore>();
        if (!core || core.team == fromTeam) return;
        core.Hit(new DamageData(dmg, fromTeam, transform.position));
        PoolManager.Release(gameObject);
    }

    public void OnSpawned() { if (rb) rb.velocity = Vector3.zero; }
    public void OnDespawned() { if (rb) rb.velocity = Vector3.zero; target = null; }
}