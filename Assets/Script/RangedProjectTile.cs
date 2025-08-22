using UnityEngine;
using ObjectPooling;

[DisallowMultipleComponent]
public class RangedProjectile : MonoBehaviour, IPoolable
{
    [Header("Links/Config")]
    [SerializeField] Rigidbody rb;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] bool autoFace = true;          // bật/tắt tự xoay
    [SerializeField] Vector3 rotOffsetEuler;        // offset góc (yaw/pitch/roll)

    Transform target; Team fromTeam; int dmg;
    float life; float speed; Quaternion RotOff => Quaternion.Euler(rotOffsetEuler);

    public void Launch(Team from, int damage, Transform t, float lifetime, float projSpeed)
    {
        fromTeam = from; dmg = Mathf.Max(1, damage);
        target = t; life = lifetime; speed = Mathf.Max(0.1f, projSpeed);
        if (rb) rb.velocity = Vector3.zero;
        if (target) SetRotation(Quaternion.LookRotation((target.position - transform.position).normalized) * RotOff);
    }

    void FixedUpdate()
    {
        if (life <= 0f) { PoolManager.Release(gameObject); return; }
        life -= Time.fixedDeltaTime;

        if (autoFace && target)
        {
            var want = Quaternion.LookRotation((target.position - transform.position).normalized) * RotOff;
            var to = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.fixedDeltaTime);
            SetRotation(to);
        }

        if (rb) rb.velocity = transform.forward * speed;
        else transform.position += transform.forward * speed * Time.fixedDeltaTime;
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

    void SetRotation(Quaternion q) { if (rb) rb.MoveRotation(q); else transform.rotation = q; }
}
