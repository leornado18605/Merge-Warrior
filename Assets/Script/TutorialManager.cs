using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform canvasRT;
    [SerializeField] private CanvasGroup tutorialCanvasGroup;

    [Header("Pointer")]
    [SerializeField] private RectTransform handPointerRT;
    [SerializeField] private GameObject handPointerGO;
    [SerializeField] private HandPointerAnim handPointerAnim;

    [Header("Targets")]
    [SerializeField] private Button placeKnifeButton;
    [SerializeField] private Button placeGunButton;
    [SerializeField] private Button startFightButton;
    [SerializeField] private RectTransform placeKnifeRT;
    [SerializeField] private RectTransform placeGunRT;
    [SerializeField] private RectTransform startFightRT;

    [Header("Config")]
    [SerializeField] private float step3Delay = 0.1f;

    private int step = 0;
    private Coroutine subRoutine;
    private Coroutine pointerRoutine;

    private void OnEnable()
    {
        subRoutine = StartCoroutine(WaitAndSubscribe());
    }

    private void OnDisable()
    {
        if (subRoutine != null) StopCoroutine(subRoutine);
        subRoutine = null;
        if (GameManager.Instance != null)
            GameManager.Instance.OnUnitMerged -= HandleUnitMerged;
        DetachAll();
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (GameManager.Instance == null) yield return null;
        GameManager.Instance.OnUnitMerged -= HandleUnitMerged;
        GameManager.Instance.OnUnitMerged += HandleUnitMerged;
    }

    private void Start()
    {
        if (canvasRT == null && uiCanvas != null)
            canvasRT = uiCanvas.GetComponent<RectTransform>();
        ShowStep(0);
    }

    public void ShowStep(int index)
    {
        step = index;
        if (handPointerGO != null) handPointerGO.SetActive(true);
        if (step == 0) ShowStepKnife();
        else if (step == 1) ShowStepGun();
        else if (step == 2) ShowStepMerge();
        else if (step == 3) ShowStepStart();
        else if (step == 4) ShowStepDone();
    }

    private void ShowStepKnife()
    {
        MoveHandTo(placeKnifeRT, new Vector2(0f, 50f));
        SafeRemove(placeKnifeButton, OnPlacedKnife);
        SafeAdd(placeKnifeButton, OnPlacedKnife);
        SetInteractable(placeGunButton, false);
        SetInteractable(startFightButton, false);
        SetTutorialRaycast(true);
    }

    private void ShowStepGun()
    {
        MoveHandTo(placeGunRT, new Vector2(0f, 50f));
        SafeRemove(placeGunButton, OnPlacedGun);
        SafeAdd(placeGunButton, OnPlacedGun);
        SetInteractable(startFightButton, false);
        SetTutorialRaycast(true);
    }

    private void ShowStepMerge()
    {
        if (handPointerGO != null) handPointerGO.SetActive(true);
        SetInteractable(startFightButton, false);
        SetTutorialRaycast(true);

        if (pointerRoutine != null) { StopCoroutine(pointerRoutine); pointerRoutine = null; }
        pointerRoutine = StartCoroutine(PointerMoveBetweenTwoUnits()); 
    }

    private void ShowStepStart()
    {
        if (handPointerGO != null) handPointerGO.SetActive(true);
        SetInteractable(startFightButton, true);
        MoveHandTo(startFightRT, Vector2.zero);
        SafeRemove(startFightButton, OnStartFight);
        SafeAdd(startFightButton, OnStartFight);
        SetTutorialRaycast(false);
    }

    private void ShowStepDone()
    {
        if (handPointerGO != null) handPointerGO.SetActive(false);
        SetInteractable(placeKnifeButton, true);
        SetInteractable(placeGunButton, true);
        SetInteractable(startFightButton, true);
        DetachAll();
        SetTutorialRaycast(false);
    }

    private void MoveHandTo(RectTransform target, Vector2 offset)
    {
        if (handPointerRT == null || canvasRT == null || target == null) return;
        Camera cam = uiCanvas != null ? uiCanvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, cam, out local);
        handPointerRT.anchoredPosition = local + offset*5;
        if (handPointerAnim != null) handPointerAnim.SetStartPos(handPointerRT.anchoredPosition);
    }

    private void SetInteractable(Button button, bool on)
    {
        if (button == null) return;
        button.interactable = on;
    }
    private void SetTutorialRaycast(bool on)
    {
        if (tutorialCanvasGroup == null) return;
        tutorialCanvasGroup.blocksRaycasts = on;
        tutorialCanvasGroup.interactable = on;
    }

    private void SafeAdd(Button button, UnityEngine.Events.UnityAction cb)
    {
        if (button == null || cb == null) return;
        button.onClick.AddListener(cb);
    }

    private void SafeRemove(Button button, UnityEngine.Events.UnityAction cb)
    {
        if (button == null || cb == null) return;
        button.onClick.RemoveListener(cb);
    }

    private void DetachAll()
    {
        SafeRemove(placeKnifeButton, OnPlacedKnife);
        SafeRemove(placeGunButton, OnPlacedGun);
        SafeRemove(startFightButton, OnStartFight);
    }

    private void OnPlacedKnife()
    {
        SafeRemove(placeKnifeButton, OnPlacedKnife);
        ShowStep(1);
    }

    private void OnPlacedGun()
    {
        SafeRemove(placeGunButton, OnPlacedGun);
        ShowStep(2);
    }

    public void OnMerged()
    {
        if (step != 2) return;

        if (pointerRoutine != null)
        {
            StopCoroutine(pointerRoutine);
            pointerRoutine = null;
        }

        StartCoroutine(GoToStep3());
    }

    private IEnumerator GoToStep3()
    {
        if (handPointerGO != null) handPointerGO.SetActive(false);
        yield return new WaitForSeconds(step3Delay);
        ShowStep(3);
    }

    private void OnStartFight()
    {
        SafeRemove(startFightButton, OnStartFight);
        ShowStep(4);
    }

    private void HandleUnitMerged(Unit u, int row, int col)
    {
        if (step != 2) return;
        OnMerged();
    }

    private IEnumerator PointerMoveBetweenTwoUnits()
    {
        Unit a = null;
        Unit b = null;

        yield return StartCoroutine(WaitForMergeablePair((u1, u2) => { a = u1; b = u2; }));
        if (a == null || b == null) yield break;

        if (handPointerGO != null) handPointerGO.SetActive(true);
        yield return StartCoroutine(LoopMoveBetweenTwoUnits(a, b));
    }

    private IEnumerator WaitForMergeablePair(System.Action<Unit, Unit> onFound)
    {
        Unit first = null;
        Unit second = null;

        while (step == 2 && (first == null || second == null))
        {
            Unit[] all = FindObjectsOfType<Unit>();
            TryPickPairFromArray(all, out first, out second);
            if (first != null && second != null)
            {
                onFound(first, second);   
                yield break;
            }
            yield return null;
        }
    }

    private void TryPickPairFromArray(Unit[] all, out Unit first, out Unit second)
    {
        first = null; second = null;

        for (int i = 0; i < all.Length; i++)
        {
            if (!IsPlayerUnit(all[i])) continue;
            for (int j = i + 1; j < all.Length; j++)
            {
                if (!IsPlayerUnit(all[j])) continue;
                if (IsSameTypeAndLevel(all[i], all[j])) { first = all[i]; second = all[j]; return; }
            }
        }
    }

    private bool IsPlayerUnit(Unit u)
    {
        if (u == null) return false;
        return u.CompareTag("Player");
    }

    private bool IsSameTypeAndLevel(Unit a, Unit b)
    {
        if (a == null || b == null) return false;
        return a.unitType == b.unitType && a.level == b.level;
    }

    private IEnumerator LoopMoveBetweenTwoUnits(Unit a, Unit b)
    {
        while (step == 2 && a != null && b != null)
        {
            Vector2 pa = GetTopCanvasPos(a);
            Vector2 pb = GetTopCanvasPos(b);
            yield return MoveToPosition(handPointerRT, pa, 0.5f);
            yield return MoveToPosition(handPointerRT, pb, 0.5f);
        }
    }

    private Vector2 GetTopCanvasPos(Unit u)
    {
        Vector3 top = u.transform.position + Vector3.up * 1.5f;
        return WorldToCanvas(top, handPointerRT.parent as RectTransform);
    }

    private Vector2 WorldToCanvas(Vector3 worldPos, RectTransform canvasRt)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt,
            Camera.main.WorldToScreenPoint(worldPos),
            null,
            out localPos
        );
        return localPos;
    }

    private IEnumerator MoveToPosition(RectTransform rt, Vector2 target, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

}
