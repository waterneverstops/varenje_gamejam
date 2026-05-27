using Plugins.RProjects.RUtils.Scripts.Core;

public class InputService : SingleBehaviour<InputService>
{
    public GameInputs GameControls { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    protected override void Init()
    {
        if (GameControls != null)
        {
            return;
        }

        GameControls = new GameInputs();
        GameControls.Enable();
    }

    private void OnDestroy()
    {
        if (GameControls == null)
        {
            return;
        }

        GameControls.Disable();
        GameControls.Dispose();
        GameControls = null;
    }

    public void SubscribePlayer(GameInputs.IPlayerActions subscriber)
    {
        Init();
        GameControls.Player.AddCallbacks(subscriber);
    }

    public void UnsubscribePlayer(GameInputs.IPlayerActions subscriber)
    {
        if (GameControls == null)
        {
            return;
        }

        GameControls.Player.RemoveCallbacks(subscriber);
    }

    public void SubscribeMovement(GameInputs.IPlayerActions subscriber) => SubscribePlayer(subscriber);

    public void UnsubscribeMovement(GameInputs.IPlayerActions subscriber) => UnsubscribePlayer(subscriber);

    public void SubscribeGrab(GameInputs.IPlayerActions subscriber) => SubscribePlayer(subscriber);

    public void UnsubscribeGrab(GameInputs.IPlayerActions subscriber) => UnsubscribePlayer(subscriber);
}
