namespace WebWolf_Client.Roles.RoleClasses;

public abstract class Role
{
    public abstract RoleType RoleType { get; }
    
    public abstract bool IsActiveRole { get; }

    public abstract RoleType AfterRole { get; }
    
    public bool IsActionCancelled { get; private set; }

    public void PrepareCancelAction()
    {
        IsActionCancelled = true;
        if (PlayerData.LocalPlayer.Role == RoleType)
            CancelAction();
    }

    protected virtual void CancelAction()
    {
        UiHandler.DisplayInGameMenu();
    }
    
    public virtual void AfterCancel() { }
    
    public void PrepareAction()
    {
        IsActionCancelled = false;
        StartAction();
    }

    protected abstract void StartAction();
}