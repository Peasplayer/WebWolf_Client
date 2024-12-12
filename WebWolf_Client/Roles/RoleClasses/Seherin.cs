using Newtonsoft.Json;
using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client.Roles.RoleClasses;

public class Seherin : Role
{
    public override RoleType RoleType => RoleType.Seherin;
    public override bool IsActiveRole => true;
    public override RoleType AfterRole => RoleType.NoRole;

    protected override void StartAction()
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
                .AddChoices(PlayerData.Players.FindAll(player => !player.IsLocal).ConvertAll(player => player.Name)));
        if (IsActionCancelled) return;
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleFinished, JsonConvert.SerializeObject(new Packets.SimpleRole(RoleType.Seherin)))));
        
        var player = PlayerData.Players.FirstOrDefault(p => p.Name == playerName);
        
        UiHandler.RenderText($"{player.Name} ist ein...");
        Task.Delay(1000).Wait();
        UiHandler.RenderText($" {player.Role}!");
        Task.Delay(2000).Wait();
        UiHandler.DisplayInGameMenu();
        
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleFinished, JsonConvert.SerializeObject(new Packets.SimpleRole(RoleType.Seherin)))));
    }
}