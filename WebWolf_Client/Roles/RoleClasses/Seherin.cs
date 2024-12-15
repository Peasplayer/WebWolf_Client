using Spectre.Console;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Seherin : Role
{
    public override RoleType RoleType => RoleType.Seherin;
    public override bool IsAliveRole => true;

    public override void ResetAction() { }

    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Die Seherin (Du) erwacht..."))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        // Die Seherin darf sich einen Spieler aussuchen, dessen Rolle sie sehen möchte
        if (CancelCheck(() => UiHandler.StartPlayerPrompt(() => CancelCheck(() =>
            {
                AnsiConsole.Clear();
                UiHandler.RenderCard(RoleType.Seherin, "", 10);
                AnsiConsole.WriteLine("\n");
            }), player => player is { IsLocal: false, IsAlive: true }, "Wähle einen Spieler dessen Role du sehen möchtest:", RevealRole))) return;
    }

    private void RevealRole(PlayerData player)
    {
        // User-Input endet, also wird dies signalisiert
        if (CancelCheck(RpcFinishedAction)) return;
        
        // Rolle des ausgewählten Spielers wird angezeigt
        UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{player.Name} ist ein...");
        Task.Delay(1000).Wait();
        UiHandler.LocalUiMessage(UiMessageType.RenderText, $" {player.Role}!");
        Task.Delay(2000).Wait();
        
        // Aktion endet
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
        RpcFinishedAction();
    }
}