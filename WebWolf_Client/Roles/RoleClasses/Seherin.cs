using Spectre.Console;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Roles.RoleClasses;

public class Seherin : Role
{
    public override RoleType RoleType => RoleType.Seherin;
    public override bool IsActiveRole => true;

    public override void ResetAction() { }

    protected override void StartAction()
    {
        if (CancelCheck(() => UiHandler.LocalUiMessage(UiMessageType.DrawPlayerNameCircle, "Die Seherin (Du) erwacht..."))) return;
        if (CancelCheck(() => Task.Delay(1000).Wait())) return;
        
        if (CancelCheck(AnsiConsole.Clear)) return;
        //AnsiConsole.Write(new Align(new Panel(String.Join(", ", Program.DebugNames)).Header("Spieler"), HorizontalAlignment.Center));
        if (CancelCheck(() => UiHandler.RenderCard(RoleType.Seherin, "", 10))) return;
        if (CancelCheck(() => AnsiConsole.WriteLine("\n"))) return;
        
        var playerName = "";
        if (CancelCheck(() => playerName = UiHandler.Prompt(
                new SelectionPrompt<string>()
                    .Title("Wähle einen Spieler dessen Karte du sehen möchtest:")
                    .PageSize(10)
                    .AddChoices(PlayerData.Players.FindAll(player => player is { IsLocal: false, IsAlive: true })
                        .ConvertAll(player => player.Name))))) return;
        if (CancelCheck(RpcFinishedAction)) return;
        
        var player = PlayerData.Players.FirstOrDefault(p => p.Name == playerName);
        UiHandler.LocalUiMessage(UiMessageType.RenderText, $"{player.Name} ist ein...");
        Task.Delay(1000).Wait();
        UiHandler.LocalUiMessage(UiMessageType.RenderText, $" {player.Role}!");
        Task.Delay(2000).Wait();
        UiHandler.LocalUiMessage(UiMessageType.DisplayInGameMenu);
        
        RpcFinishedAction();
    }
}