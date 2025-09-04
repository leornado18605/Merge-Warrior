using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AllyPhaseThroughNav : MonoBehaviour
{
    [Header("References (assign in prefab)")]
    [SerializeField] private Unit unit;
    [SerializeField] private NavMeshAgent agent;

    [Header("Settings")]
    [SerializeField] private float allyRadius = 0.8f;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField]
    private ObstacleAvoidanceType normalAvoidance =
        ObstacleAvoidanceType.HighQualityObstacleAvoidance;

    private float originalRadius;
    private ObstacleAvoidanceType originalAvoid;

    // ──────────────────────────────────────────────
    // Lifecycle
    private void Awake()
    {
        if (agent == null) return;

        originalRadius = agent.radius;
        originalAvoid = normalAvoidance;

        agent.obstacleAvoidanceType = normalAvoidance;
    }

    private void Update()
    {
        if (agent == null || unit == null) return;

        bool nearAlly = CheckNearAlly();

        if (nearAlly)
        {
            EnterPhaseMode();
        }
        else
        {
            RestoreNormalMode();
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    private bool CheckNearAlly()
    {
        Vector3 pos = transform.position;

        Collider[] hits = Physics.OverlapSphere(
            pos,
            allyRadius,
            unitLayer,
            QueryTriggerInteraction.Ignore
        );

        string myTag = unit.tag;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider other = hits[i];

            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && rb.gameObject == gameObject)
                continue;

            GameObject go = rb != null ? rb.gameObject : other.gameObject;
            if (go == gameObject) continue;

            if (go.CompareTag(myTag)) return true;
        }

        return false;
    }

    private void EnterPhaseMode()
    {
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.radius = Mathf.Min(originalRadius, 0.05f);
    }

    private void RestoreNormalMode()
    {
        agent.obstacleAvoidanceType = originalAvoid;
        agent.radius = originalRadius;
    }

    // ──────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, allyRadius);
    }
#endif
}
