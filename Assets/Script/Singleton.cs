using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ObjectPooling;

public class GameEconomyManager : MonoBehaviour
{
    public static GameEconomyManager Instance { get; private set; }

    [Header("Coin")]
    public int Coins = 120;
    public TMP_Text coinText;

    [Header("Shop")]
    public Button buyLv1Button;
    public Button buyLv2Button;
    public Button buyLv3Button;
    public int basePrice = 20;
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
        currentPriceLv1 = basePrice;
        currentPriceLv2 = Mathf.RoundToInt(basePrice * 1.2f);
        currentPriceLv3 = Mathf.RoundToInt(basePrice * 1.5f);

        UpdateCoinUI();

        // Gắn button
        buyLv1Button.onClick.AddListener(() => BuyUnit(1));
        buyLv2Button.onClick.AddListener(() => BuyUnit(2));
        buyLv3Button.onClick.AddListener(() => BuyUnit(3));
    }

    // ================= COIN =================
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

    // ================= SHOP =================
    private void BuyUnit(int level)
    {
        int price = GetCurrentPrice(level);
        if (Coins >= price)
        {
            SpendCoin(price);
            SpawnUnit(level);
            IncreasePrice(level);
        }
        else
        {
            ShowAdOption(level);
        }
    }

    private int GetCurrentPrice(int level)
    {
        return level switch
        {
            1 => currentPriceLv1,
            2 => currentPriceLv2,
            3 => currentPriceLv3,
            _ => basePrice
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

    private void ShowAdOption(int level)
    {
        Debug.Log($"Không đủ coin! Xem quảng cáo để nhận Unit Lv {level}");
    }
}
