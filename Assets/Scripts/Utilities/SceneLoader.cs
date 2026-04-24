using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader 
{
    public static IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            Debug.Log($"Loading {sceneName}...");
            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}
