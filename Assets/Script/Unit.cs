using ObjectPooling;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening; 
public class Unit : MonoBehaviour
{
    public string unitType;
    public int level;
    public int row;
    public int col;

    private Vector3 originalPosition;
    private GridManager gridManager;
    private GridManager.Board board;

    private bool mergeLock = false;
    [SerializeField] private float defaultMergeLockSeconds = 0.25f;

    [SerializeField] public UnitCore core;

    [SerializeField] public UnitTargeting targeting;
    [SerializeField] public NavMeshAgent agent;
    [SerializeField] public Animator anim;
    [SerializeField] public DraggableUnit drag;

    [SerializeField] public GunController gun;

    [Header("Merge Effect")]
    [SerializeField] private GameObject mergeEffectPrefab;
    [SerializeField] private float mergeEffectDuration = 1.5f;

    [Header("Level Up Effect")]
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private float levelUpEffectDuration = 1f;

    private GameObject currentMergeEffect;

    public void Initialize(string unitType, int level, GridManager gridManager, GridManager.Board board, int row, int col)
    {
        this.unitType = unitType;
        this.level = level;
        this.gridManager = gridManager;
        this.board = board;
        this.row = row;
        this.col = col;
        originalPosition = gridManager.GridToWorldPosition(board, row, col, true);
        transform.position = originalPosition;
        gameObject.name = $"{unitType}_L{level}_R{row}C{col}";
    }

    public Vector3 GetOriginalPosition() => originalPosition;
    public GridManager Grid => gridManager;

    public GridManager.Board Board => board;

    public void UpdateOriginalPosition(Vector3 newPos, int newRow, int newCol)
    {
        originalPosition = newPos;
        row = newRow;
        col = newCol;
        transform.position = newPos;
    }

    public bool IsMergeLocked() => mergeLock;

    public void MergeIncrement()
    {
        level++;
    }

    public void MergeLockTemporary(float seconds = -1f)
    {
        if (seconds <= 0f) seconds = defaultMergeLockSeconds;
        StartCoroutine(MergeLockCoroutine(seconds));
    }

    private IEnumerator MergeLockCoroutine(float seconds)
    {
        mergeLock = true;
        yield return new WaitForSeconds(seconds);
        mergeLock = false;
    }

    public void MergeWithEffect(GameObject nextPrefab, int nextLevel)
    {
        StartCoroutine(MergeEffectCoroutine(nextPrefab, nextLevel));
    }

    private IEnumerator MergeEffectCoroutine(GameObject nextPrefab, int nextLevel)
    {
        // 🔹 Step 1: thu nhỏ unit cũ
        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);

        // 🔹 Spawn hiệu ứng merge
        GameObject fx = null;
        if (mergeEffectPrefab)
            fx = Instantiate(mergeEffectPrefab, transform.position + Vector3.up, Quaternion.identity, transform);

        MergeLockTemporary(mergeEffectDuration);

        yield return new WaitForSeconds(mergeEffectDuration);

        if (fx) Destroy(fx);

        // despawn unit cũ
        PoolManager.Release(gameObject);

        // 🔹 Step 2: spawn unit mới (scale từ 0 -> 1)
        var pos = Grid.GridToWorldPosition(Board, row, col, true);
        var newObj = GameManager.Instance.CreateMergedUnit(nextPrefab, unitType, nextLevel, Board, row, col, pos);

        if (newObj != null)
        {
            newObj.transform.localScale = Vector3.zero;
            newObj.transform.DOScale(Vector3.one * 300f, 0.4f).SetEase(Ease.OutBack); // hiệu ứng bật nảy
        }

        // 🔹 Step 3: Spawn hiệu ứng level-up
        if (levelUpEffectPrefab && newObj != null)
        {
            var levelFx = Instantiate(levelUpEffectPrefab, newObj.transform.position + Vector3.up, Quaternion.identity, newObj.transform);
            Destroy(levelFx, levelUpEffectDuration);
        }
    }
}