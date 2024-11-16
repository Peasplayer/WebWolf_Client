using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

public class UIHandler
{
    public static bool StartGameMenu()
    {
        //Test Spierler
        PlayerManager.Players.Add(new PlayerData("TestSpieler1", null));
        PlayerManager.Players.Add(new PlayerData("TestSpieler2", "2"));
        
        Console.Clear();
        ConsoleUtils.RenderLogo();
        
        AnsiConsole.WriteLine("");
        PlayerData.LocalPlayer = new PlayerData(AnsiConsole.Prompt(
            new TextPrompt<string>("Wie möchtest du im Spiel heißen?")
                .Validate(n =>
                {
                    if (n.Length > 15)
                    {
                        ConsoleUtils.ClearConsoleLine(2);
                        return ValidationResult.Error("[red]Der Name darf maximal 15 Zeichen lang sein![/]");
                    }
        
                    if (n.Contains(" "))
                    {
                        ConsoleUtils.ClearConsoleLine(2);
                        return ValidationResult.Error("[red]Der Name darf keine Leerzeichen enthalten![/]");
                    }
                    
                    return ValidationResult.Success();
                })), null);
        ConsoleUtils.ClearConsoleLine(2);
        AnsiConsole.MarkupLine("\nHallo, [green]{0}[/]!", PlayerData.LocalPlayer.Name);
        
        var net = new NetworkingManager();
        net.StartConnection("ws://localhost:8443/json");
        AnsiConsole.Write("Connecting");
        while (net.Client.IsStarted && !net.Client.IsRunning)
        {
            AnsiConsole.Write(".");
            if (net.ConnectionTask.Status == TaskStatus.RanToCompletion)
                break;
            Thread.Sleep(500);
        }
        
        if (net.Client.IsRunning)
        {
            AnsiConsole.MarkupLine("[green]Connected![/]");
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Connection failed[/]");
            return false;
        }
    }

    public static void DisplayLobby()
    {
        AnsiConsole.Live(new Markup("[green]Lobby wird geladen...[/]"))
            .Start(ctx =>
            {
                while (true)
                {
                    var table = new Table();
                    table.AddColumn("[blue]Name[/]");
                    table.AddColumn("[blue]ID[/]");
                    
                    foreach (var player in PlayerManager.Players)
                    {
                        table.AddRow(player.Name, player.Id ?? "[grey]Keine ID[/]");
                    }
                    
                    table.AddRow($"[bold green]{PlayerData.LocalPlayer.Name}[/]", "[green](You)[/]");
                    ctx.UpdateTarget(table);
                }
            });
    }
}