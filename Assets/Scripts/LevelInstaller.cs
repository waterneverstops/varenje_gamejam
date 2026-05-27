using Zenject;

public class LevelInstaller : MonoInstaller
{
    public FirstPersonController PlayerController;

    public override void InstallBindings()
    {
        Container.Bind<FirstPersonController>().FromInstance(PlayerController);
    }
}