using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject handPointer;   // Prefab pointer (có script HandPointerAnim)
    public TMP_Text tutorialText;

    [Header("Targets")]
    public Button placeKnifeButton;
    public Button placeGunButton;
    public Button startFightButton;

    private int step = 0;
    private Coroutine pointerRoutine;

    void Start()
    {
        ShowStep(0);
    }

    public void ShowStep(int index)
    {
        step = index;

        // Dừng routine cũ nếu có
        if (pointerRoutine != null)
        {
            StopCoroutine(pointerRoutine);
            pointerRoutine = null;
        }

        handPointer.SetActive(true);

        switch (step)
        {
            case 0: // Knife
                MoveHandTo(placeKnifeButton.GetComponent<RectTransform>(), new Vector2(0, 50));
                placeKnifeButton.onClick.AddListener(OnPlacedKnife);

                placeGunButton.interactable = false;
                startFightButton.interactable = false;
                break;

            case 1: // Gun
                MoveHandTo(placeGunButton.GetComponent<RectTransform>(), new Vector2(0, 50));
                placeGunButton.onClick.AddListener(OnPlacedGun);

                startFightButton.interactable = false;
                break;

            case 2: // Merge
                handPointer.SetActive(false);
                startFightButton.interactable = true;
                pointerRoutine = StartCoroutine(PointerMoveBetweenTwoUnits());
                break;

            case 3: // Start
                handPointer.SetActive(false);
                MoveHandTo(startFightButton.GetComponent<RectTransform>(), new Vector2(0, 50));
                startFightButton.onClick.AddListener(OnStartFight);
                break;

            case 4: // Done

                handPointer.SetActive(false);

                placeKnifeButton.interactable = true;
                placeGunButton.interactable = true;
                startFightButton.interactable = true;
                break;
        }
    }

    IEnumerator PointerMoveBetweenTwoUnits()
    {
        RectTransform handRT = handPointer.GetComponent<RectTransform>();

        Unit first = null, second = null;

        while (step == 2 && (first == null || second == null))
        {
            var all = FindObjectsOfType<Unit>();
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].CompareTag("Player")) continue;

                for (int j = i + 1; j < all.Length; j++)
                {
                    if (!all[j].CompareTag("Player")) continue;

                    if (all[i].unitType == all[j].unitType && all[i].level == all[j].level)
                    {
                        first = all[i];
                        second = all[j];
                        break;
                    }
                }
                if (first != null && second != null) break;
            }

            yield return null;
        }

        if (first == null || second == null) yield break;

        handPointer.SetActive(true);

        while (step == 2 && first != null && second != null)
        {
            Vector2 posA = WorldToCanvas(first.transform.position + Vector3.up * 1.5f, handRT.parent as RectTransform);
            Vector2 posB = WorldToCanvas(second.transform.position + Vector3.up * 1.5f, handRT.parent as RectTransform);

            yield return MoveToPosition(handRT, posA, 0.5f);
            yield return MoveToPosition(handRT, posB, 0.5f);
        }
    }

    Vector2 WorldToCanvas(Vector3 worldPos, RectTransform canvasRT)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            Camera.main.WorldToScreenPoint(worldPos),
            null,
            out localPos
        );
        return localPos;
    }

    IEnumerator MoveToPosition(RectTransform handRT, Vector2 target, float duration)
    {
        Vector2 start = handRT.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            handRT.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        handRT.anchoredPosition = target;
    }

    void MoveHandTo(RectTransform target, Vector2 offset)
    {
        RectTransform handRT = handPointer.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handRT.parent as RectTransform,
            target.position,
            null,
            out localPos
        );
        localPos += offset*5;
        handRT.anchoredPosition = localPos;

        var anim = handPointer.GetComponent<HandPointerAnim>();
        if (anim != null) anim.SetStartPos(localPos);
    }

    void OnPlacedKnife()
    {
        placeKnifeButton.onClick.RemoveListener(OnPlacedKnife);
        ShowStep(1);
    }

    void OnPlacedGun()
    {
        placeGunButton.onClick.RemoveListener(OnPlacedGun);
        ShowStep(2);
    }

    public void OnMerged()
    {
        if (step == 2)
        {
            if (pointerRoutine != null)
            {
                StopCoroutine(pointerRoutine);
                pointerRoutine = null;
            }

            StartCoroutine(GoToStep3());
        }
    }

    private IEnumerator GoToStep3()
    {
        handPointer.SetActive(false);              
        yield return new WaitForSeconds(0.3f);     
        ShowStep(3);                              
    }

    void OnStartFight()
    {
        startFightButton.onClick.RemoveListener(OnStartFight);
        ShowStep(4);
    }
}
