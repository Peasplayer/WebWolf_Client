using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

// Rollen-Basis-Klasse
public abstract class Role
{
    public abstract RoleType RoleType { get; }
    
    // Ob die Rolle ausgeführt wird, wenn der Spieler noch lebt
    public abstract bool IsAliveRole { get; }

    // Ob die Aktion abgebrochen wurde
    protected bool IsActionCancelled { get; private set; }
    
    // Die maximale erlaubte Anzahl an Spielern mit dieser Rolle
    public int MaxAmount => SettingsManager.GetMaxAmount(RoleType);
    
    // Initialisierung der Rolle am Anfang des Spiels
    public virtual void InitRole() { }

    // Vorbereitung des Abrechens der Aktion
    public void PrepareCancelAction()
    {
        IsActionCancelled = true;
        // Falls der lokale Spieler die Rolle hat, wird die Aktion abgebrochen
        if (PlayerData.LocalPlayer.Role == RoleType)
        {
            CancelAction();
            AfterCancel();
        }
    }

    // Wenn die Aktion abgebrochen wird, wird das InGame-Menü angezeigt
    protected virtual void CancelAction()
    {
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
    }

    // Nach dem Abbruch der Aktion wird dem Host mitgeteilt, dass die Aktion beendet wurde
    public virtual void AfterCancel()
    {
        if (PlayerData.LocalPlayer.Role == RoleType)
            RpcFinishedAction();
    }
    
    // Überprüft, ob die Aktion abgebrochen wurde und ob der Code ausgeführt werden soll
    protected bool CancelCheck(Action code)
    {
        if (!IsActionCancelled)
        {
            code.DynamicInvoke();
            return false;
        }
        return false;
    }
    
    // Vorbereitung der Ausführung der Aktion
    public void PrepareAction()
    {
        IsActionCancelled = false;
        UiHandler.IsInInGameMenu = false;
        StartAction();
    }

    // Zurücksetzen der Aktion
    public virtual void ResetAction() {}

    // Starten der Aktion
    protected abstract void StartAction();

    // Dem Host wird mitgeteilt, dass die Aktion beendet wurde bzw. fortgeschritten ist
    protected void RpcFinishedAction()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.RoleFinished,
                JsonConvert.SerializeObject(new Pakets.SimpleRole(RoleType)))));
    }
}