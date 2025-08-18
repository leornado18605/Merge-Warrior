using UnityEngine;

public enum Team { Player, Enemy }

public class UnitTeam : MonoBehaviour
{
    public Team team = Team.Player; // Knife/Gun là đội người chơi
}

public class UnitAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Unit unit;
    [SerializeField] private GridManager grid;

    [Header("Role")]
    [SerializeField] private bool isRanged = false; // Knife=false, Gun=true
    [SerializeField] private Team myTeam = Team.Player;

    [Header("Ranges (tiles)")]
    [SerializeField] private int searchRadius = 6;  // melee: 6
    [SerializeField] private int attackRange = 3;  // ranged: 3 (vd)

    [Header("Loop")]
    [SerializeField] private float reacquireEvery = 0.2f;
    private float _nextScan;

    [Header("LOS (ranged)")]
    [SerializeField] private LayerMask losBlockMask;
    [SerializeField] private Transform firePoint;

    void Reset() { unit = GetComponent<Unit>(); }

    public void Inject(GridManager g, bool ranged)
    {
        grid = g; isRanged = ranged;
    }

    void Update()
    {
        if (grid == null || unit == null) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + reacquireEvery;

        Think();
    }

    void Think()
    {
        // filter: khác team
        System.Predicate<Unit> isEnemy = (u) =>
        {
            var t = u.GetComponent<UnitTeam>();
            return t != null && t.team != myTeam;
        };

        var target = grid.FindNearestUnit(unit.Board, unit.row, unit.col,
                          isEnemy,
                          maxRadiusTiles: isRanged ? Mathf.Max(searchRadius, attackRange) : searchRadius,
                          priorityAdjacency: !isRanged);

        if (target == null) return;

        if (!isRanged)
        {
            // melee: nếu kề ô -> đánh; chưa kề -> nhích 1 ô
            if (IsAdjacent(unit.row, unit.col, target.row, target.col))
            {
                // TODO: gọi damage/animation
                // Debug.Log($"[Melee] {name} hit {target.name}");
            }
            else
            {
                StepToward(target);
            }
        }
        else
        {
            // ranged: trong tầm + có LOS thì bắn, không thì nhích
            int dr = Mathf.Abs(target.row - unit.row);
            int dc = Mathf.Abs(target.col - unit.col);
            int chebyshev = Mathf.Max(dr, dc);

            if (chebyshev <= attackRange && HasLOS(target))
            {
                // TODO: bắn đạn/FX
                // Debug.Log($"[Ranged] {name} shot {target.name}");
            }
            else
            {
                StepToward(target);
            }
        }
    }

    bool IsAdjacent(int r1, int c1, int r2, int c2)
    {
        int dr = Mathf.Abs(r1 - r2), dc = Mathf.Abs(c1 - c2);
        return (dr + dc) == 1;
    }

    bool HasLOS(Unit t)
    {
        Vector3 start = firePoint ? firePoint.position : transform.position;
        Vector3 end = t.transform.position + Vector3.up * 0.3f;
        Vector3 dir = end - start; float dist = dir.magnitude;
        if (dist <= 0.001f) return true;
        dir /= dist;
        return !Physics.Raycast(start, dir, dist, losBlockMask, QueryTriggerInteraction.Ignore);
    }

    void StepToward(Unit target)
    {
        // tìm 1 ô trống kề target để áp sát
        Vector2Int goal = AnyEmptyAdjOf(target);
        if (goal.x < 0) return;

        // greedy 1 ô về goal
        Vector2Int next = GreedyStep(unit.row, unit.col, goal.x, goal.y);
        if (!grid.IsValidGridPosition(next.x, next.y)) return;
        if (!grid.IsEmptyCell(unit.Board, next.x, next.y)) return;

        grid.SetCellOccupied(unit.Board, unit.row, unit.col, null);
        unit.row = next.x; unit.col = next.y;
        grid.SetCellOccupied(unit.Board, unit.row, unit.col, gameObject);

        Vector3 w = grid.GridToWorldPosition(unit.Board, unit.row, unit.col);
        transform.position = new Vector3(w.x, transform.position.y, w.z);
    }

    Vector2Int AnyEmptyAdjOf(Unit t)
    {
        int[] ro = { -1, 1, 0, 0 }; int[] co = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int r = t.row + ro[i], c = t.col + co[i];
            if (!grid.IsValidGridPosition(r, c)) continue;
            if (grid.IsEmptyCell(t.Board, r, c)) return new Vector2Int(r, c);
        }
        return new Vector2Int(-1, -1);
    }

    Vector2Int GreedyStep(int sr, int sc, int gr, int gc)
    {
        int rr = sr, cc = sc;
        if (gr > sr) rr++;
        else if (gr < sr) rr--;
        else if (gc > sc) cc++; else if (gc < sc) cc--;
        return new Vector2Int(rr, cc);
    }
}
