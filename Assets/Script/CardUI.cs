using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[DisallowMultipleComponent]
public class CardUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private RectTransform rect;

    [Header("Anim Settings")]
    [SerializeField] private float flipDuration = 0.4f;
    [SerializeField] private float scalePunch = 0.2f;
    [SerializeField] private float punchDuration = 0.3f;

    private bool isFront = true;

    private CanvasGroup cg;

    private void Awake()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }


    public void Setup(Sprite sprite, string cardName)
    {
        if (icon) icon.sprite = sprite;
        if (title) title.text = cardName;
    }

    public void Flip()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(rect.DOScaleX(0f, flipDuration / 2f))
           .AppendCallback(() =>
           {
               isFront = !isFront;
               if (title) title.text = isFront ? "Mặt trước" : "Mặt sau";
           })
           .Append(rect.DOScaleX(1f, flipDuration / 2f));
    }


    public void Punch()
    {
        rect.DOPunchScale(Vector3.one * scalePunch, punchDuration, 5, 0.5f);
    }

    public void MoveTo(Vector3 target, float duration = 0.5f)
    {
        rect.DOMove(target, duration).SetEase(Ease.OutBack);
    }

    public void FadeOut(float duration = 0.5f)
    {
        cg.DOFade(0f, duration).OnComplete(() => gameObject.SetActive(false));
    }

    public void FadeIn(float duration = 0.5f)
    {
        gameObject.SetActive(true);
        cg.alpha = 0f;
        cg.DOFade(1f, duration);
    }
}
