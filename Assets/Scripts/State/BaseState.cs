
public abstract class BaseState
{
    protected PlayerController pController{ get; private set; }

    protected Inputs inputManager{ get; private set; }

    public StateName stateName{ get; private set; }
    
    public BaseState (PlayerController pController, Inputs inputsManager, StateName stateName)
    {
        this.pController  = pController;
        this.inputManager = inputsManager;
        this.stateName    = stateName;
    }

    public abstract void OnEnterState();
    public abstract void OnUpdateState();
    public abstract void OnFixedUpdateState();
    public abstract void OnExitState();
}