using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Button deleteDataButton;

    [Header("Reset config")]
    [SerializeField] private string initialSceneName = "Level1";
    [SerializeField] private int initialSceneBuildIndex = 0;    
    // ───── lifecycle ─────
    private void Awake()
    {
        if (panel) panel.SetActive(false);
        if (openButton) openButton.onClick.AddListener(ShowPanel);
        if (closeButton) closeButton.onClick.AddListener(HidePanel);
        if (deleteDataButton) deleteDataButton.onClick.AddListener(DeleteAllDataAndReload);
        if (soundToggle)
        {
            soundToggle.onValueChanged.AddListener(OnSoundToggle);
            LoadSoundPref();
        }
    }

    // ───── UI show/hide ─────
    private void ShowPanel()
    {
        if (!panel) return;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling(); 
        var cg = panel.GetComponentInParent<CanvasGroup>();
        if (cg) { cg.alpha = 1; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    private void HidePanel()
    {
        if (panel) panel.SetActive(false);
    }

    // ───── Sound toggle ─────
    private const string SoundKey = "SND_ENABLED"; // 1:on, 0:off

    private void LoadSoundPref()
    {
        bool on = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        AudioListener.volume = on ? 1f : 0f;
        if (soundToggle) soundToggle.isOn = on;
    }

    private void OnSoundToggle(bool on)
    {
        AudioListener.volume = on ? 1f : 0f;
        PlayerPrefs.SetInt(SoundKey, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ───── Delete data ─────
    private void DeleteAllDataAndReload()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameEconomyManager.Instance?.ResetForNewGame();
        CombatManager.Instance?.ForcePrepState();
        GameManager.Instance?.SetGunsEnabled(false);

        var tut = FindObjectOfType<TutorialManager>();
        if (tut) tut.gameObject.SetActive(false);

        if (LevelController.Instance != null)
        {
            LevelController.Instance.LoadFirst();
        }
        else
        {
            SceneManager.LoadScene(string.IsNullOrEmpty(initialSceneName) ? initialSceneBuildIndex : SceneUtility.GetBuildIndexByScenePath(initialSceneName));
        }
    }

}
