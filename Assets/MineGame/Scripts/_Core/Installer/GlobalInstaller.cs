using Zenject;

public class GlobalInstaller : MonoInstaller
{
    private PlayerInput inputs;

    public override void InstallBindings()
    {
        Init();
        RegisterControllers();
        RegisterDatabases();
    }

    private void Init()
    {
        inputs = new();
        inputs.Enable();
    }

    private void RegisterControllers()
    {
    }

    private void RegisterDatabases()
    {
        Container.Bind<PlayerInput>().FromInstance(inputs).AsSingle();
    }
}