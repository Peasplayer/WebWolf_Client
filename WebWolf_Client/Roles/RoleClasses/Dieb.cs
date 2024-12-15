using Spectre.Console;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Dieb : Role
{
    public override RoleType RoleType => RoleType.Dieb;
    public override bool IsAliveRole => true;
    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Der Dieb (Du) erwacht..."))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        // Der Dieb darf sich einen Spieler aussuchen, dessen Rolle er klauen möchte
        if (CancelCheck(() => UiHandler.StartPlayerPrompt(() => CancelCheck(() =>
            {
                AnsiConsole.Clear();
                UiHandler.RenderCard(RoleType.Dieb, "", 10);
                AnsiConsole.WriteLine("\n");
            }), player => player is { IsLocal: false, IsAlive: true }, "Wähle einen Spieler dessen Role du klauen möchtest:",
            player =>
            {
                // User-Input endet, also wird dies signalisiert
                if (CancelCheck(RpcFinishedAction)) return;
                
                // Rolle des ausgewählten Spielers wird geklaut
                UiHandler.LocalUiMessage(UiMessageType.RenderText, $"Du hast die Rolle von {player.Name} geklaut. Du bist nun ...");
                Task.Delay(1000).Wait();
                UiHandler.LocalUiMessage(UiMessageType.RenderText, $" {player.Role}!");
                Task.Delay(2000).Wait();
                PlayerData.LocalPlayer.RpcSetRole(player.Role);
                
                // Aktion endet für den Dieb
                UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
                
                // Das Opfer wird zum Dorfbewohner ...
                player.RpcSetRole(RoleType.Dorfbewohner);
                // ... und wird über sein Schicksal informiert
                var receivers = new List<string> {player.Id};
                UiHandler.RpcUiMessage(UiMessageType.Clear, "", receivers);
                UiHandler.RpcUiMessage(UiMessageType.RenderCard, RoleType.Dieb.ToString(), receivers);
                UiHandler.RpcUiMessage(UiMessageType.RenderText, "Oh nein!\nDer Dieb hat deine Identität geklaut!" +
                                                                 "\nDu bist nun ein normaler Dorfbewohner.", receivers);
                Task.Delay(1000).Wait();
                UiHandler.RpcUiMessage(UiMessageType.DisplayInGameMenu, "", receivers);
                
                // Aktion endet
                RpcFinishedAction();
            }))) return;
    }
}