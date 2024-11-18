using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

public static class UiHandler
{
    public static bool StartGameMenu()
    {
        //Test Spieler
        PlayerData.Players.Add(new PlayerData("TestSpieler1", null));
        PlayerData.Players.Add(new PlayerData("TestSpieler2", "2"));
        
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
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .Start("Connecting", ctx => {
                while (net.Client.IsStarted && !net.Client.IsRunning)
                {
                    if (net.ConnectionTask.Status == TaskStatus.RanToCompletion)
                        break;
                    Thread.Sleep(500);
                }
            });
        
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

    public static void DisplayLobby(bool initial)
    {
        AnsiConsole.Clear();
        var table = new Table();
        table.AddColumn("[blue]Name[/]");
        //table.AddColumn("[blue]ID[/]");
                    
        foreach (var player in PlayerData.Players)
        {
            if (player.IsLocal)
                table.AddRow($"[bold green]{player.Name}[/]" + " [green](You)[/]");
            else
                table.AddRow(player.Name);
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.Write("Spiel Starten? [y/n]");
        if (initial)
            AnsiConsole.Prompt(new ConfirmationPrompt("").HideChoices().HideDefaultValue());

        //Fals in der live preview die ID angezeigt werden soll
        //player.Id ?? "[grey]Keine ID[/]"
    }
}