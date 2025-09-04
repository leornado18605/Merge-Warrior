using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Wiring")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deleteDataButton;
    [SerializeField] private GameObject panel;

    [Header("Reset Config")]
    [SerializeField] private string initialSceneName = "Level1";
    [SerializeField] private int initialSceneBuildIndex = 0;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (openButton != null) openButton.onClick.AddListener(ShowPanel);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
        if (deleteDataButton != null) deleteDataButton.onClick.AddListener(OnDeleteClicked);
    }

    // ───── UI show/hide ─────
    private void ShowPanel()
    {
        if (panel == null) return;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    private void HidePanel()
    {
        if (panel == null) return;
        panel.SetActive(false);
    }

    // ───── Delete data / Reset game ─────
    private void OnDeleteClicked()
    {
        if (LevelController.Instance != null)
        {
            LevelController.Instance.LoadFirst();
        }
        else
        {
            if (!string.IsNullOrEmpty(initialSceneName))
                SceneManager.LoadScene(initialSceneName);
            else
                SceneManager.LoadScene(initialSceneBuildIndex);
        }
    }
}
