using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Coin")]
    public int Coins = 120;
    public TMP_Text coinText;

    [Header("Shop Buttons")]
    public Button buyLv1Button;
    public Button buyLv2Button;
    public Button buyLv3Button;

    [Header("Unit Prices")]
    public int basePriceLv1 = 20;
    public int basePriceLv2 = 40;
    public int basePriceLv3 = 60;

    private int currentPriceLv1;
    private int currentPriceLv2;
    private int currentPriceLv3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        currentPriceLv1 = basePriceLv1;
        currentPriceLv2 = basePriceLv2;
        currentPriceLv3 = basePriceLv3;

        UpdateCoinUI();

        buyLv1Button.onClick.AddListener(() => BuyUnit(1));
        buyLv2Button.onClick.AddListener(() => BuyUnit(2));
        buyLv3Button.onClick.AddListener(() => BuyUnit(3));
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        UpdateCoinUI();
    }

    public bool SpendCoin(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        UpdateCoinUI();
        return true;
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = $"Coin: {Coins}";
    }

    private void BuyUnit(int level)
    {
        int price = GetCurrentPrice(level);

        if (Coins < price)
        {
            return; 
        }

        SpendCoin(price);
        SpawnUnit(level);
        IncreasePrice(level);

        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        buyLv1Button.interactable = Coins >= currentPriceLv1;
        buyLv2Button.interactable = Coins >= currentPriceLv2;
        buyLv3Button.interactable = Coins >= currentPriceLv3;
    }


    private int GetCurrentPrice(int level)
    {
        return level switch
        {
            1 => currentPriceLv1,
            2 => currentPriceLv2,
            3 => currentPriceLv3,
            _ => 0
        };
    }

    private void IncreasePrice(int level)
    {
        switch (level)
        {
            case 1: currentPriceLv1 = Mathf.CeilToInt(currentPriceLv1 * 1.2f); break;
            case 2: currentPriceLv2 = Mathf.CeilToInt(currentPriceLv2 * 1.2f); break;
            case 3: currentPriceLv3 = Mathf.CeilToInt(currentPriceLv3 * 1.2f); break;
        }
    }

    private void SpawnUnit(int level)
    {
        Debug.Log($"Spawn Unit Lv {level}");
       
    }
}
