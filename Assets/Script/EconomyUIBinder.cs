using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EconomyUIBinder : MonoBehaviour
{
    [Header("Coin label + Shop UI")]
    [SerializeField] TMP_Text coinText;
    [SerializeField] Button buyKnifeButton;
    [SerializeField] TMP_Text knifePriceText;
    [SerializeField] Button buyGunButton;
    [SerializeField] TMP_Text gunPriceText;

    void Start()
    {
        var econ = GameEconomyManager.Instance;
        if (econ)
        {
            econ.BindUI(coinText, buyKnifeButton, knifePriceText, buyGunButton, gunPriceText);
        }

    }

    public void Rebind()
    {
        GameEconomyManager econ = GameEconomyManager.Instance;
        if (econ == null) return;
        econ.BindDisplayOnly(coinText, knifePriceText, gunPriceText);
    }
}
