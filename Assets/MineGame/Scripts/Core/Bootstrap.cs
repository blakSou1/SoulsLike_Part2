using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        GameBootstrapper.Init();

        //G.sceneLoader.Load("MainMenu");
    }
}

public static class GameBootstrapper
{
    private static GameObject serviceHolder;

    public static void Init()
    {
        Application.targetFrameRate = 60;

        serviceHolder = new GameObject("===Services===");
        Object.DontDestroyOnLoad(serviceHolder);

        G.inputs = new();
        G.inputs.Enable();

        G.sceneLoader = CreateSimpleService<SceneLoader>();

        CreateSimpleService<InputBufferMovementExample>();
        CreateSimpleService<InputBufferBattleExample>();
        CreateSimpleService<InputBufferLockOnExample>();

        G.sceneLoader.onLoadAction = (scene, sceneMode) =>
        {
            //G.volume = Object.FindFirstObjectByType<Volume>();
        };
    }

    private static T CreateSimpleService<T>() where T : Component, IService
    {
        GameObject g = new(typeof(T).ToString());

        g.transform.parent = serviceHolder.transform;
        T t = g.AddComponent<T>();
        t.Init();
        return g.GetComponent<T>();
    }
}

public interface IService
{
    public void Init();
}
