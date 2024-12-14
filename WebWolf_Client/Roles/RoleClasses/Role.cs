using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public abstract class Role
{
    public abstract RoleType RoleType { get; }
    
    public abstract bool IsActiveRole { get; }

    protected bool IsActionCancelled { get; private set; }

    public void PrepareCancelAction()
    {
        IsActionCancelled = true;
        if (PlayerData.LocalPlayer.Role == RoleType)
            CancelAction();
    }

    protected virtual void CancelAction()
    {
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
    }
    
    public virtual void AfterCancel() { }
    
    protected bool CancelCheck(Action test)
    {
        if (!IsActionCancelled)
        {
            test.DynamicInvoke();
            return false;
        }
        
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleFinished, JsonConvert.SerializeObject(new Packets.SimpleRole(RoleType)))));
        return false;
    }
    
    public void PrepareAction()
    {
        IsActionCancelled = false;
        StartAction();
    }

    protected abstract void StartAction();

    protected void RpcFinishedAction()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleFinished,
                JsonConvert.SerializeObject(new Packets.SimpleRole(RoleType)))));
    }
}