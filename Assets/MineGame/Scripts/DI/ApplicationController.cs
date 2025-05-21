using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// контролер VContainer
/// </summary>
public class ApplicationController : LifetimeScope
{
    [Header("Views")] 

    [field: SerializeField] private PlayerInput input;

    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);
        
        RegisterControllers(builder);
        RegisterDatabases(builder);
        RegisterViews(builder);
    }

    private static void RegisterControllers(IContainerBuilder builder)
    {
            //builder.Register<YouController>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces(); контролеры не монобех сам создает их
    }

    private void RegisterDatabases(IContainerBuilder builder)
    {
            //builder.RegisterInstance(_ingredientDatabase); параметры которые раскидываются по классам
    }
        
    private void RegisterViews(IContainerBuilder builder)
    {
        builder.RegisterInstance(input = new());
        input.Player.Enable();

    }
}
