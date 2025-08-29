using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    public int CurrentIndex
    {
        get { return SceneManager.GetActiveScene().buildIndex; }
    }

    public int TotalScenes
    {
        get { return SceneManager.sceneCountInBuildSettings; }
    }

    public bool HasNext()
    {
        return CurrentIndex + 1 < TotalScenes;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadNext()
    {
        if (HasNext())
        {
            StartCoroutine(LoadAndInitIndex(CurrentIndex + 1));
        }
    }

    public void Reload()
    {
        StartCoroutine(LoadAndInitIndex(CurrentIndex));
    }

    public void LoadFirst()
    {
        StartCoroutine(LoadAndInitIndex(0));
    }

    public void LoadByName(string sceneName)
    {
        StartCoroutine(LoadAndInitName(sceneName));
    }

    private IEnumerator LoadAndInitIndex(int buildIndex)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        yield return op;
        yield return null;
        yield return null;

        yield return WaitManagersReady();

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ForcePrepState();
        }
    }

    private IEnumerator LoadAndInitName(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return op;
        yield return null;
        yield return null;

        yield return WaitManagersReady();

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ForcePrepState();
        }
    }

    private IEnumerator WaitManagersReady()
    {
        int safety = 30;
        while (GridManager.Instance == null && safety > 0)
        {
            safety -= 1;
            yield return null;
        }
    }
}
