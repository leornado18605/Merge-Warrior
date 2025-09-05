using UnityEngine;

public class CardDemo : MonoBehaviour
{
    [SerializeField] private CardUI card;

    void Start()
    {
        card.FadeIn(0.5f);
        card.Punch();

        Invoke(nameof(FlipCard), 0.5f);
    }

    void FlipCard()
    {
        card.Flip();
    }
}
