using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AllyPhaseThroughNav : MonoBehaviour
{
    [Header("Refs (assign in prefab)")]
    [SerializeField] private Unit unit;           // gán sẵn
    [SerializeField] private NavMeshAgent agent;  // gán sẵn

    [Header("Settings")]
    [SerializeField] private float allyRadius = 0.8f;     // bán kính kiểm tra đồng đội gần
    [SerializeField] private LayerMask unitLayer;          // Layer của các unit (để Overlap)
    [SerializeField] private ObstacleAvoidanceType normalAvoidance = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

    float originalRadius;
    ObstacleAvoidanceType originalAvoid;

    void Awake()
    {
        if (!agent) return;
        originalRadius = agent.radius;
        originalAvoid = normalAvoidance;
        agent.obstacleAvoidanceType = normalAvoidance;
    }

    void Update()
    {
        if (!agent || !unit) return;

        // kiểm tra có đồng đội rất gần không
        bool nearAlly = false;
        var pos = transform.position;
        var hits = Physics.OverlapSphere(pos, allyRadius, unitLayer, QueryTriggerInteraction.Ignore);
        var myTag = unit.tag;

        for (int i = 0; i < hits.Length; i++)
        {
            var other = hits[i];
            if (other.attachedRigidbody && other.attachedRigidbody.gameObject == gameObject) continue;
            var go = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            if (go == gameObject) continue;
            if (go.CompareTag(myTag)) { nearAlly = true; break; }
        }

        if (nearAlly)
        {
            // tắt avoidance để không “lách” đồng đội, giảm radius cho dễ xuyên
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.radius = Mathf.Min(originalRadius, 0.05f);
        }
        else
        {
            // khôi phục
            agent.obstacleAvoidanceType = originalAvoid;
            agent.radius = originalRadius;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawWireSphere(transform.position, allyRadius);
    }
#endif
}
