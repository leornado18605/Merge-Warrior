using ObjectPooling;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[DisallowMultipleComponent]
public class Unit : MonoBehaviour
{
    [Header("Identity")]
    public string unitType;
    public int level;
    public int row;
    public int col;

    [Header("Flags")]
    [SerializeField] private bool isOriginalUnit = false; 
    
    [Header("Core Refs")]
    [SerializeField] public UnitCore core;
    [SerializeField] public UnitTargeting targeting;
    [SerializeField] public NavMeshAgent agent;
    [SerializeField] public Animator anim;
    [SerializeField] public DraggableUnit drag;
    [SerializeField] public GunController gun;
    [SerializeField] public Rigidbody rb;

    [Header("Cached UI/Events")]
    public EventSystem[] eventSystems;
    public GraphicRaycaster[] raycasters;

    [Header("Merge Effect")]
    [SerializeField] private GameObject mergeEffectPrefab;
    [SerializeField] private float mergeEffectDuration = 1.5f;

    [Header("Level Up Effect")]
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private float levelUpEffectDuration = 1f;

    private Vector3 originalPosition;
    private GridManager gridManager;
    private GridManager.Board board;

    private bool mergeLock = false;
    [SerializeField] private float defaultMergeLockSeconds = 0.25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        targeting = GetComponent<UnitTargeting>();
        drag = GetComponent<DraggableUnit>();
        gun = GetComponent<GunController>();

        // cache UI/Event refs once
        eventSystems = GetComponentsInChildren<EventSystem>(true);
        raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
    }

    public void Initialize(
        string unitType,
        int level,
        GridManager gridManager,
        GridManager.Board board,
        int row,
        int col)
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

    public GridManager Grid { get { return gridManager; } }
    public GridManager.Board Board { get { return board; } }

    public void UpdateOriginalPosition(Vector3 newPos, int newRow, int newCol)
    {
        originalPosition = newPos;
        row = newRow;
        col = newCol;
        transform.position = newPos;
    }

    public bool IsMergeLocked() { return mergeLock; }

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
        yield return StartCoroutine(PlayMergeShrinkEffect());

        DespawnOldUnit();

        GameObject newObj = SpawnMergedUnit(nextPrefab, nextLevel);

        PlayLevelUpEffect(newObj);
    }
    private IEnumerator PlayMergeShrinkEffect()
    {
        // Shrink animation
        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);

        // Spawn merge FX
        GameObject fx = null;
        if (mergeEffectPrefab != null)
        {
            fx = Instantiate(
                mergeEffectPrefab,
                transform.position + Vector3.up,
                Quaternion.identity,
                transform);
        }

        MergeLockTemporary(mergeEffectDuration);
        yield return new WaitForSeconds(mergeEffectDuration);

        if (fx != null) Destroy(fx);
    }
    private void DespawnOldUnit()
    {

        PoolManager.Release(gameObject);
    }
    private GameObject SpawnMergedUnit(GameObject nextPrefab, int nextLevel)
    {
        if (Grid == null || GameManager.Instance == null)
            return null;

        Vector3 pos = Grid.GridToWorldPosition(Board, row, col, true);
        GameObject newObj = GameManager.Instance.CreateMergedUnit(
            nextPrefab, unitType, nextLevel, Board, row, col, pos);

        if (newObj != null)
        {
            newObj.transform.localScale = Vector3.zero;
            newObj.transform.DOScale(Vector3.one * 300f, 0.4f).SetEase(Ease.OutBack);
        }

        return newObj;
    }
    private void PlayLevelUpEffect(GameObject newObj)
    {
        if (newObj == null || levelUpEffectPrefab == null)
            return;

        GameObject fx = Instantiate(
            levelUpEffectPrefab,
            newObj.transform.position + Vector3.up,
            Quaternion.identity,
            newObj.transform);

        Destroy(fx, levelUpEffectDuration);
    }

}
