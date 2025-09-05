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

    /// <summary>
    /// Setup dữ liệu thẻ (icon + tên)
    /// </summary>
    public void Setup(Sprite sprite, string cardName)
    {
        if (icon) icon.sprite = sprite;
        if (title) title.text = cardName;
    }

    /// <summary>
    /// Lật thẻ (flip 180 độ)
    /// </summary>
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

    /// <summary>
    /// Hiệu ứng nhấn rung (punch scale)
    /// </summary>
    public void Punch()
    {
        rect.DOPunchScale(Vector3.one * scalePunch, punchDuration, 5, 0.5f);
    }

    /// <summary>
    /// Di chuyển thẻ tới vị trí mới
    /// </summary>
    public void MoveTo(Vector3 target, float duration = 0.5f)
    {
        rect.DOMove(target, duration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Làm mờ và ẩn thẻ
    /// </summary>
    public void FadeOut(float duration = 0.5f)
    {
        cg.DOFade(0f, duration).OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// Hiện thẻ (reset alpha = 1)
    /// </summary>
    public void FadeIn(float duration = 0.5f)
    {
        gameObject.SetActive(true);
        cg.alpha = 0f;
        cg.DOFade(1f, duration);
    }
}
