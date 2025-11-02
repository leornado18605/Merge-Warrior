using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameEconomyManager : MonoBehaviour
{
    public static GameEconomyManager Instance { get; private set; }

    #region Coin
    [Header("Coin Settings")]
    [SerializeField] private int defaultCoins = 120;
    public int Coins = 120;

    [SerializeField] private TMP_Text coinText;
    private const string PlayerCoinsKey = "PlayerCoins";
    #endregion

    #region Shop
    [Header("Shop Settings")]
    [SerializeField] private Button buyKnifeButton;
    [SerializeField] private Button buyGunButton;

    [SerializeField] private TMP_Text knifePriceText;
    [SerializeField] private TMP_Text gunPriceText;

    [SerializeField] private int basePriceKnife = 60;
    [SerializeField] private int basePriceGun = 100;

    [SerializeField] private float priceScale = 1.1f;

    [SerializeField] private Color priceAffordable = Color.white;
    [SerializeField] private Color priceNotEnough = new Color(1f, 0.65f, 0.65f);

    private int currentPriceKnife;
    private int currentPriceGun;

    public int KnifePrice => currentPriceKnife;
    public int GunPrice => currentPriceGun;
    #endregion

    #region Editor Options
    [Header("Editor Behavior")]
    [SerializeField] private bool resetCoinsOnPlayInEditor = true;
    [SerializeField] private bool persistCoinsInEditor = false;
    #endregion

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

        if (buyKnifeButton != null) buyKnifeButton.onClick.AddListener(BuyKnife);
        if (buyGunButton != null) buyGunButton.onClick.AddListener(BuyGun);

        UpdateAllShopUI();
    }
    #endregion

    #region Public API
    // Add coins to the player balance.
    public void AddCoin(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        SaveCoins();
        UpdateAllShopUI();
    }

    // Try to spend coins. Returns true if successful.
    public bool SpendCoin(int amount)
    {
        if (Coins < amount) return false;

        Coins -= amount;
        SaveCoins();
        UpdateAllShopUI();
        return true;
    }

    // Reset coins and shop prices for a new game session.
    public void ResetForNewGame()
    {
        Coins = defaultCoins;
        currentPriceKnife = basePriceKnife;
        currentPriceGun = basePriceGun;

        SaveCoins();
        UpdateAllShopUI();

        Debug.Log("[Economy] ResetForNewGame -> Coins & shop reset to default");
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

    // Bind UI elements for interactive shop.
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

    // Bind UI elements only for displaying values (non-interactive).

    public void BindDisplayOnly(
        TMP_Text coinLabel,
        TMP_Text knifePrice,
        TMP_Text gunPrice)
    {
        coinText = coinLabel;
        knifePriceText = knifePrice;
        gunPriceText = gunPrice;
        UpdateAllShopUI();
    }
    #endregion

    #region Buying Logic
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
    #endregion

    #region UI Updates
    private void UpdateAllShopUI()
    {
        UpdateCoinUI();
        UpdatePriceUI();
        UpdateButtonsInteractable();
    }

    public void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = Coins.ToString();
    }

    private void UpdatePriceUI()
    {
        if (knifePriceText != null)
        {
            knifePriceText.text = FormatPrice(currentPriceKnife);
            knifePriceText.color =
                Coins >= currentPriceKnife ? priceAffordable : priceNotEnough;
        }

        if (gunPriceText != null)
        {
            gunPriceText.text = FormatPrice(currentPriceGun);
            gunPriceText.color =
                Coins >= currentPriceGun ? priceAffordable : priceNotEnough;
        }
    }

    private void UpdateButtonsInteractable()
    {
        if (buyKnifeButton != null)
            buyKnifeButton.interactable = Coins >= currentPriceKnife;

        if (buyGunButton != null)
            buyGunButton.interactable = Coins >= currentPriceGun;
    }

    private string FormatPrice(int value)
    {
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.#") + "M";

        if (value >= 1_000)
            return (value / 1_000f).ToString("0.#") + "K";

        return value.ToString();
    }
    #endregion

    #region Persistence
    private void SaveCoins()
    {
#if UNITY_EDITOR
        if (!persistCoinsInEditor) return;
#endif
        PlayerPrefs.SetInt(PlayerCoinsKey, Coins);
        PlayerPrefs.Save();
    }
    #endregion
}
