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

    private void Awake()
    {
        // Clear old listeners
        if (startFightButton) startFightButton.onClick.RemoveAllListeners();
        if (placeKnifeButton) placeKnifeButton.onClick.RemoveAllListeners();
        if (placeGunButton) placeGunButton.onClick.RemoveAllListeners();

        if (startFightButton) startFightButton.onClick.AddListener(OnStartFightClicked);
        if (placeKnifeButton) placeKnifeButton.onClick.AddListener(OnPlaceKnifeClicked);
        if (placeGunButton) placeGunButton.onClick.AddListener(OnPlaceGunClicked);

        // Hide result panel lúc đầu
        if (resultPanel) resultPanel.SetActive(false);
        if (victoryPrefab) victoryPrefab.SetActive(false);
        if (defeatPrefab) defeatPrefab.SetActive(false);
        if (spinPrefab) spinPrefab.SetActive(false);

        if (claimButton) claimButton.onClick.AddListener(OnClaimClicked);
        if (noThanksButton) noThanksButton.onClick.AddListener(OnNoThanksClicked);
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

        if (victoryPrefab) victoryPrefab.SetActive(false);
        if (defeatPrefab) defeatPrefab.SetActive(false);

        if (win)
        {
            if (victoryPrefab) victoryPrefab.SetActive(true);
        }
        else
        {
            if (defeatPrefab) defeatPrefab.SetActive(true);
        }

        if (rewardText)
            rewardText.text = win
                ? $"YOU EARNED: " +
                $"{reward} coins"
                : $"YOU EARNED: " +
                $"{reward} coins";
    }

    private void OnClaimClicked()
    {
        GameEconomyManager.Instance?.AddCoin(pendingReward);

        Debug.Log("Claim reward, go next scene!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void OnNoThanksClicked()
    {
        Debug.Log("Replay same level!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
