namespace WebWolf_Client.Roles;

public abstract class Role
{
    public abstract RoleType RoleType { get; }
    
    public abstract bool IsActiveRole { get; }

    public abstract RoleType AfterRole { get; }
    
    public bool IsActionCancelled { get; set; }

    public void PrepareAction()
    {
        IsActionCancelled = false;
        StartAction();
    }
    
    public abstract void StartAction();
}