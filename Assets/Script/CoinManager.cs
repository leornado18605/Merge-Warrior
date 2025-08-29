using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameEconomyManager : MonoBehaviour
{
    public static GameEconomyManager Instance { get; private set; }

    // ───────── Coin ─────────
    [Header("Coin")]
    public int defaultCoins = 120;
    public int Coins = 120;
    [SerializeField] private TMP_Text coinText;

    // ───────── Shop ─────────
    [Header("Shop")]
    [SerializeField] private Button buyKnifeButton;
    [SerializeField] private Button buyGunButton;

    [SerializeField] private TMP_Text knifePriceText;
    [SerializeField] private TMP_Text gunPriceText;

    public int basePriceKnife = 60;
    public int basePriceGun = 100;

    public float priceScale = 1.1f;

    [SerializeField] private Color priceAffordable = Color.white;
    [SerializeField] private Color priceNotEnough = new Color(1f, 0.65f, 0.65f);

    private int currentPriceKnife;
    private int currentPriceGun;

    public int KnifePrice => currentPriceKnife;
    public int GunPrice => currentPriceGun;

    // ───────── Editor Behavior ─────────
    [Header("Editor Behavior")]
    public bool resetCoinsOnPlayInEditor = true;
    public bool persistCoinsInEditor = false;

    private const string PlayerCoinsKey = "PlayerCoins";

    // ───────── Lifecycle ─────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        if (resetCoinsOnPlayInEditor)
        {
            PlayerPrefs.DeleteKey(PlayerCoinsKey);
            Coins = defaultCoins;
        }
        else
        {
            Coins = PlayerPrefs.GetInt(PlayerCoinsKey, defaultCoins);
        }
#else
        Coins = PlayerPrefs.GetInt(PlayerCoinsKey, defaultCoins);
#endif
    }

    private void Start()
    {
        currentPriceKnife = basePriceKnife;
        currentPriceGun = basePriceGun;

        if (buyKnifeButton) buyKnifeButton.onClick.AddListener(BuyKnife);
        if (buyGunButton) buyGunButton.onClick.AddListener(BuyGun);

        UpdateAllShopUI();
    }

    // ───────── Public API ─────────
    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        SaveCoins();
        UpdateAllShopUI();
    }

    public bool SpendCoin(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        SaveCoins();
        UpdateAllShopUI();
        return true;
    }

    public void BindUI(
        TMP_Text coinLabel,
        Button knifeBtn, TMP_Text knifePrice,
        Button gunBtn, TMP_Text gunPrice)
    {
        coinText = coinLabel;
        buyKnifeButton = knifeBtn;
        buyGunButton = gunBtn;
        knifePriceText = knifePrice;
        gunPriceText = gunPrice;

     
        UpdateAllShopUI();
    }

    // ───────── Buying Logic ─────────
    private void BuyKnife()
    {
        if (!SpendCoin(currentPriceKnife)) return;

        currentPriceKnife = Mathf.CeilToInt(currentPriceKnife * priceScale);
        UpdateAllShopUI();
    }

    private void BuyGun()
    {
        if (!SpendCoin(currentPriceGun)) return;

        currentPriceGun = Mathf.CeilToInt(currentPriceGun * priceScale);
        UpdateAllShopUI();
    }

    // ───────── UI Updates ─────────
    private void UpdateAllShopUI()
    {
        UpdateCoinUI();
        UpdatePriceUI();
        UpdateButtonsInteractable();
    }

    public void UpdateCoinUI()
    {
        if (coinText) coinText.text = $"{Coins}";
    }

    private void UpdatePriceUI()
    {
        if (knifePriceText)
        {
            knifePriceText.text = FormatPrice(currentPriceKnife);
            knifePriceText.color = Coins >= currentPriceKnife ? priceAffordable : priceNotEnough;
        }
        if (gunPriceText)
        {
            gunPriceText.text = FormatPrice(currentPriceGun);
            gunPriceText.color = Coins >= currentPriceGun ? priceAffordable : priceNotEnough;
        }
    }

    private void UpdateButtonsInteractable()
    {
        if (buyKnifeButton) buyKnifeButton.interactable = Coins >= currentPriceKnife;
        if (buyGunButton) buyGunButton.interactable = Coins >= currentPriceGun;
    }

    private string FormatPrice(int value)
    {
        if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
        if (value >= 1_000) return (value / 1_000f).ToString("0.#") + "K";
        return value.ToString();
    }

    // ───────── Persistence ─────────
    private void SaveCoins()
    {
#if UNITY_EDITOR
        if (!persistCoinsInEditor) return;
#endif
        PlayerPrefs.SetInt(PlayerCoinsKey, Coins);
        PlayerPrefs.Save();
    }

    // ───────── Reset for new game ─────────
    public void ResetForNewGame()
    {
        // reset coins
        Coins = defaultCoins;

        // reset giá Knife/Gun
        currentPriceKnife = basePriceKnife;
        currentPriceGun = basePriceGun;

        SaveCoins();
        UpdateAllShopUI();

        Debug.Log("[Economy] ResetForNewGame -> Coins & shop reset về mặc định");
    }

    public bool TryBuyKnife(UnitManager um)
    {
        if (!SpendCoin(currentPriceKnife)) return false;
        currentPriceKnife = Mathf.CeilToInt(currentPriceKnife * priceScale);
        if (um != null) um.PlaceKnife();
        UpdateAllShopUI();
        return true;
    }

    public bool TryBuyGun(UnitManager um)
    {
        if (!SpendCoin(currentPriceGun)) return false;
        currentPriceGun = Mathf.CeilToInt(currentPriceGun * priceScale);
        if (um != null) um.PlaceGun();
        UpdateAllShopUI();
        return true;
    }

    public void BindDisplayOnly(TMP_Text coinLabel, TMP_Text knifePrice, TMP_Text gunPrice)
    {
        coinText = coinLabel;
        knifePriceText = knifePrice;
        gunPriceText = gunPrice;
        UpdateAllShopUI();
    }

}
