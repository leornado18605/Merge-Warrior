using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private UnitManager unitManager;

    [Header("Buttons")]
    [SerializeField] private Button startFightButton;
    [SerializeField] private Button placeKnifeButton;
    [SerializeField] private Button placeGunButton;

    [Header("Optional: Hide as a group when fight")]
    [SerializeField] private GameObject buttonsPanel;

    // ───────────── Battle Result UI ─────────────
    [Header("Battle Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Image resultTitle;
    [SerializeField] private GameObject victoryPrefab;
    [SerializeField] private GameObject spinPrefab;
    [SerializeField] private GameObject defeatPrefab;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button noThanksButton;

    private int pendingReward;
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        // Clear old listeners
        if (startFightButton) startFightButton.onClick.RemoveAllListeners();
        if (placeKnifeButton) placeKnifeButton.onClick.RemoveAllListeners();
        if (placeGunButton) placeGunButton.onClick.RemoveAllListeners();

        if (startFightButton) startFightButton.onClick.AddListener(OnStartFightClicked);
        if (placeKnifeButton) placeKnifeButton.onClick.AddListener(OnPlaceKnifeClicked);
        if (placeGunButton) placeGunButton.onClick.AddListener(OnPlaceGunClicked);

        if (claimButton) claimButton.onClick.AddListener(OnClaimClicked);
        if (noThanksButton) noThanksButton.onClick.AddListener(OnNoThanksClicked);
    }

    public void HideResultPanel()
    {
        if (resultPanel) resultPanel.SetActive(false);
        if (victoryPrefab) victoryPrefab.SetActive(false);
        if (defeatPrefab) defeatPrefab.SetActive(false);
        if (spinPrefab) spinPrefab.SetActive(false);
    }

    public void ShowPlacementButtons()
    {
        if (buttonsPanel) buttonsPanel.SetActive(true);
    }

    // ───────────── Gameplay Buttons ─────────────
    private void OnStartFightClicked()
    {
        if (combatManager) combatManager.StartCombat();

        if (buttonsPanel) buttonsPanel.SetActive(false);
        else if (startFightButton) startFightButton.gameObject.SetActive(false);
    }

    private void OnPlaceKnifeClicked()
    {
        if (unitManager) unitManager.PlaceKnife();
        RefreshButtons();
    }

    private void OnPlaceGunClicked()
    {
        if (unitManager) unitManager.PlaceGun();
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (!unitManager) return;

        bool full = unitManager.IsBoardFull();
        if (placeKnifeButton) placeKnifeButton.interactable = !full;
        if (placeGunButton) placeGunButton.interactable = !full;
    }

    // ───────────── Battle Result ─────────────
    public void ShowResult(bool win, int reward)
    {
        pendingReward = reward;

        if (resultPanel) resultPanel.SetActive(true);

        if (victoryPrefab) victoryPrefab.SetActive(win);
        if (defeatPrefab) defeatPrefab.SetActive(!win);

        if (rewardText)
            rewardText.text = $"YOU EARNED:\n{reward} coins"; // chỉ hiện coin kiếm được
    }

    private void OnClaimClicked()
    {
        if (GameEconomyManager.Instance != null)
        {
            GameEconomyManager.Instance.AddCoin(pendingReward);
            GameEconomyManager.Instance.UpdateCoinUI();
        }

        Debug.Log("Claim reward, go next level!");
        LevelController.Instance.NextLevel();  // 👈 gọi sang LevelController
    }

    private void OnNoThanksClicked()
    {
        Debug.Log("Replay current level!");
        LevelController.Instance.SetupLevel(LevelController.Instance.currentLevel);
    }
}
