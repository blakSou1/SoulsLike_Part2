using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoaderEditor
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if(SceneManager.GetActiveScene().buildIndex != 0)
            GameBootstrapper.Init();
    }
}
