using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        // clear cũ
        if (startFightButton) startFightButton.onClick.RemoveAllListeners();
        if (placeKnifeButton) placeKnifeButton.onClick.RemoveAllListeners();
        if (placeGunButton) placeGunButton.onClick.RemoveAllListeners();

        // gắn mới
        if (startFightButton) startFightButton.onClick.AddListener(OnStartFightClicked);
        if (placeKnifeButton) placeKnifeButton.onClick.AddListener(OnPlaceKnifeClicked);
        if (placeGunButton) placeGunButton.onClick.AddListener(OnPlaceGunClicked);
    }

    private void OnStartFightClicked()
    {
        if (combatManager) combatManager.StartCombat();

        if (buttonsPanel) buttonsPanel.SetActive(false);
        else if (startFightButton) startFightButton.gameObject.SetActive(false);
    }

    private void OnPlaceKnifeClicked()
    {
        if (unitManager) unitManager.PlaceKnife();

    }

    private void OnPlaceGunClicked()
    {
        if (unitManager) unitManager.PlaceGun();

    }
}
