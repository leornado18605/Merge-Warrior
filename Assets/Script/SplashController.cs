using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashController : MonoBehaviour
{
    [SerializeField] private float splashDuration = 2.5f; 
    [SerializeField] private string nextSceneName = "Level1"; 

    private void Start()
    {
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private System.Collections.IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(splashDuration);

        SceneManager.LoadSceneAsync(nextSceneName);
    }
}
