using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour, IService
{
    public string currentSceneName = null;
    public Action<Scene, LoadSceneMode> onLoadAction;

    public void Init()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += (scene, sceneMode) => onLoadAction?.Invoke(scene, sceneMode);
    }

    public void Load(string sceneName, float fadeSpeed = 0.5f)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName, fadeSpeed));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, float fadeSpeed)
    {
        yield return new WaitForFixedUpdate();
        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (currentSceneName == null) return;
        SceneManager.LoadScene(sceneName);
        currentSceneName = sceneName;
    }
}