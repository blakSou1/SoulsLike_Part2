using Zenject;

public class SceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        RegisterControllers();
        RegisterDatabases();
    }

    private void RegisterControllers()
    {
    }

    private void RegisterDatabases()
    {
    }
}