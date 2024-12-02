using Newtonsoft.Json;
using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client.Roles;

public class Seherin : Role
{
    public override RoleType RoleType => RoleType.Seherin;
    public override bool IsActiveRole => true;
    public override RoleType AfterRole => RoleType.NoRole;
    public override void StartAction()
    {
        if (IsActionCancelled) return;
        var center = UiHandler.DrawPlayerNameCircle();//UiHandler.DrawPlayerNameCircle();
        if (IsActionCancelled) return;
        UiHandler.RenderTextAroundPoint(center, "Die Seherin (Du) erwacht...");
        if (IsActionCancelled) return;
        Task.Delay(1000).Wait();
        if (IsActionCancelled) return;
        AnsiConsole.Clear();
        if (IsActionCancelled) return;
        //AnsiConsole.Write(new Align(new Panel(String.Join(", ", Program.DebugNames)).Header("Spieler"), HorizontalAlignment.Center));
        UiHandler.RenderCard(RoleType.Seherin, "", 10);
        if (IsActionCancelled) return;
        AnsiConsole.WriteLine("\n");
        if (IsActionCancelled) return;
        var playerName = UiHandler.Prompt(
            new SelectionPrompt<string>()
                .Title("Wähle einen Spieler dessen Karte du sehen möchtest:")
                .PageSize(10)
                .AddChoices(PlayerData.Players.ConvertAll(player => player.Name)));
        if (IsActionCancelled) return;
        
        var player = PlayerData.Players.FirstOrDefault(p => p.Name == playerName);
        if (IsActionCancelled) return;
        
        UiHandler.RenderText($"{player.Name} ist ein...");
        if (IsActionCancelled) return;
        Task.Delay(1000).Wait();
        if (IsActionCancelled) return;
        UiHandler.RenderText($" {player.Role}!");
        if (IsActionCancelled) return;
        Task.Delay(2000).Wait();
        if (IsActionCancelled) return;
        UiHandler.DisplayInGameMenu();
        if (IsActionCancelled) return;
        
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleFinished, JsonConvert.SerializeObject(new Packets.SimpleRole(RoleType.Seherin)))));
    }
}