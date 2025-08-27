using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadSceneHelper
{
    public static void NextLevel()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
        else
        {
            Debug.Log("🎉 Finished all levels!");
            SceneManager.LoadScene(0);
        }
    }

    public static void ReloadCurrent()
    {
        string currentSceneName = SceneManager.GetSceneAt(0).name;
        SceneManager.LoadScene(currentSceneName);
    }
}
