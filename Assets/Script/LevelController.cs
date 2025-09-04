using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    #region Properties

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
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Public API

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

    #endregion

    #region Load By Index

    private IEnumerator LoadAndInitIndex(int buildIndex)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        yield return op;
        yield return null;
        yield return null;

        GameManager gm = GameManager.Instance;
        GridManager grid = GridManager.Instance;
        UnitManager um = UnitManager.Instance;
        UIManager ui = UIManager.Instance;
        BotManager bot = BotManager.Instance;

        int safety = 60;
        while (grid != null && !grid.IsReady && safety-- > 0)
        {
            yield return null;
        }

        if (gm != null && grid != null)
        {
            gm.OnSceneReady(grid, um, ui, bot);
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ForcePrepState();
        }
    }

    #endregion

    #region Load By Name

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
        int safety = 120;

        while ((GridManager.Instance == null ||
                !GridManager.Instance.IsReady) &&
               safety > 0)
        {
            safety -= 1;
            yield return null;
        }
    }

    #endregion
}
