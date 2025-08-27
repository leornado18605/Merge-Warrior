using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AllyPhaseThroughNav : MonoBehaviour
{
    [Header("Refs (assign in prefab)")]
    [SerializeField] private Unit unit;          
    [SerializeField] private NavMeshAgent agent;  

    [Header("Settings")]
    [SerializeField] private float allyRadius = 0.8f;     
    [SerializeField] private LayerMask unitLayer;          
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
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.radius = Mathf.Min(originalRadius, 0.05f);
        }
        else
        {
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
