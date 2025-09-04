using UnityEngine;
using UnityEngine.UI;
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

    [Header("Gameplay refs")]
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private bool useEconomyInTutorial = true;

    [Header("Config")]
    [SerializeField] private float step3Delay = 0.1f;
    [SerializeField] private GameObject rootToHide;

    private int step = 0;
    private Coroutine subRoutine;
    private Coroutine pointerRoutine;

    // ─────────────────────────────────────────────────────────────────────────────
    #region Unity

    private void Awake()
    {
        if (!rootToHide) rootToHide = gameObject; 
    }

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

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Steps

    public void ShowStep(int index)
    {
        step = index;
        if (handPointerGO) handPointerGO.SetActive(true);

        switch (step)
        {
            case 0: ShowStepKnife(); break;
            case 1: ShowStepGun(); break;
            case 2: ShowStepMerge(); break;
            case 3: ShowStepStart(); break;
            case 4: ShowStepDone(); break;
        }
    }

    private void ShowStepKnife()
    {
        SetInteractable(placeKnifeButton, true);
        SetInteractable(placeGunButton, false);
        SetInteractable(startFightButton, false);

        SetTutorialRaycast(false);
        MoveHandTo(placeKnifeRT, new Vector2(0f, 50f));

        BindOneShot(placeKnifeButton, () =>
        {
            if (useEconomyInTutorial && GameEconomyManager.Instance != null)
                _ = GameEconomyManager.Instance.TryBuyKnife(unitManager);
            else
                unitManager?.PlaceKnife();

            ShowStep(1);
        });
    }

    private void ShowStepGun()
    {
        SetInteractable(placeGunButton, true);
        SetInteractable(startFightButton, false);

        SetTutorialRaycast(false);
        MoveHandTo(placeGunRT, new Vector2(0f, 50f));

        BindOneShot(placeGunButton, () =>
        {
            if (useEconomyInTutorial && GameEconomyManager.Instance != null)
                _ = GameEconomyManager.Instance.TryBuyGun(unitManager);
            else
                unitManager?.PlaceGun();

            ShowStep(2);
        });
    }

    private void ShowStepMerge()
    {
        if (handPointerGO) handPointerGO.SetActive(true);
        SetInteractable(startFightButton, false);
        SetTutorialRaycast(true);

        if (pointerRoutine != null) { StopCoroutine(pointerRoutine); pointerRoutine = null; }
        pointerRoutine = StartCoroutine(PointerMoveBetweenTwoUnits());
    }

    private void ShowStepStart()
    {
        if (handPointerGO) handPointerGO.SetActive(true);
        MoveHandTo(startFightRT, Vector2.zero);
        SetTutorialRaycast(false);


        BindOneShot(startFightButton, () =>
        {
            CombatManager.Instance?.StartCombat();
            UIManager.Instance?.SetPlacementUI(false);

            if (handPointerGO) handPointerGO.SetActive(false);
            if (rootToHide) rootToHide.SetActive(false);
        });
    }

    private void ShowStepDone()
    {
        if (handPointerGO) handPointerGO.SetActive(false);
        DetachAll();
        SetTutorialRaycast(false);
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Pointer move (+ camera safe)

    bool EnsureCanvasAndPointer()
    {
        if (!uiCanvas)
            uiCanvas = GetComponentInParent<Canvas>();
        if (!canvasRT && uiCanvas)
            canvasRT = uiCanvas.GetComponent<RectTransform>();

        if (!handPointerRT || !canvasRT) return false;

        if (handPointerRT.parent != canvasRT)
            handPointerRT.SetParent(canvasRT, false);

        return true;
    }

    Camera UICamera()
    {
        if (!uiCanvas) return null;
        if (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return uiCanvas.worldCamera ? uiCanvas.worldCamera : Camera.main;
    }

    public void MoveHandTo(RectTransform target, Vector2 offset)
    {
        if (!target) return;
        if (handPointerGO) handPointerGO.SetActive(true);
        StartCoroutine(CoMoveHandNextFrame(target, offset));
    }

    IEnumerator CoMoveHandNextFrame(RectTransform target, Vector2 offset)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (!EnsureCanvasAndPointer()) yield break;

        var cam = UICamera();
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, cam, out local))
        {
            handPointerRT.anchoredPosition = local + offset * 5;
            if (handPointerAnim) handPointerAnim.SetStartPos(handPointerRT.anchoredPosition);
        }
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Merge detection pointer

    private void HandleUnitMerged(Unit u, int row, int col)
    {
        if (step != 2) return;

        if (pointerRoutine != null) { StopCoroutine(pointerRoutine); pointerRoutine = null; }
        StartCoroutine(GoToStep3());
    }

    private IEnumerator GoToStep3()
    {
        if (handPointerGO) handPointerGO.SetActive(false);
        yield return new WaitForSeconds(step3Delay);
        ShowStep(3);
    }

    private IEnumerator PointerMoveBetweenTwoUnits()
    {
        Unit a = null, b = null;
        yield return StartCoroutine(WaitForMergeablePair((u1, u2) => { a = u1; b = u2; }));
        if (a == null || b == null) yield break;

        if (handPointerGO) handPointerGO.SetActive(true);
        yield return StartCoroutine(LoopMoveBetweenTwoUnits(a, b));
    }

    private IEnumerator WaitForMergeablePair(System.Action<Unit, Unit> onFound)
    {
        Unit first = null, second = null;

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

    private bool IsPlayerUnit(Unit u) => u && u.CompareTag("Player");
    private bool IsSameTypeAndLevel(Unit a, Unit b) => a && b && a.unitType == b.unitType && a.level == b.level;

    private IEnumerator LoopMoveBetweenTwoUnits(Unit a, Unit b)
    {
        while (step == 2 && a && b)
        {
            Vector2 pa = WorldToCanvas(a.transform.position + Vector3.up * 1.5f);
            Vector2 pb = WorldToCanvas(b.transform.position + Vector3.up * 1.5f);
            yield return MoveToPosition(handPointerRT, pa, 0.5f);
            yield return MoveToPosition(handPointerRT, pb, 0.5f);
        }
    }

    private Vector2 WorldToCanvas(Vector3 worldPos)
    {
        if (!EnsureCanvasAndPointer()) return Vector2.zero;
        var cam = UICamera();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            Camera.main ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero,
            cam,
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

    #endregion
    // ─────────────────────────────────────────────────────────────────────────────
    #region Utils: UI & one-shot

    private void SetInteractable(Button button, bool on)
    {
        if (button) button.interactable = on;
    }

    private void SetTutorialRaycast(bool on)
    {
        if (tutorialCanvasGroup)
        {
            tutorialCanvasGroup.blocksRaycasts = on;
            tutorialCanvasGroup.interactable = on;
        }
    }

    private void DetachAll()
    {
        if (placeKnifeButton) placeKnifeButton.onClick.RemoveAllListeners();
        if (placeGunButton) placeGunButton.onClick.RemoveAllListeners();
        if (startFightButton) startFightButton.onClick.RemoveAllListeners();
    }


    private void BindOneShot(Button btn, UnityEngine.Events.UnityAction onClickOnce)
    {
        if (!btn) return;

        btn.onClick.RemoveAllListeners();
        btn.interactable = true;

        btn.onClick.AddListener(() =>
        {
            if (!btn.interactable) return;      
            btn.interactable = false;           
            btn.onClick.RemoveAllListeners();   
            btn.gameObject.SetActive(false);    

            onClickOnce?.Invoke();
        });
    }

    #endregion
}
