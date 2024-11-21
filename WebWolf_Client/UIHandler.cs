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
        NetworkingManager.InitialName = AnsiConsole.Prompt(
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
                }));
        ConsoleUtils.ClearConsoleLine(2);
        AnsiConsole.MarkupLine("\nHallo, [green]{0}[/]!", NetworkingManager.InitialName);
        
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

    private static bool _AskedQuestion;

    public static void ResetLobby()
    {
        _AskedQuestion = false;
    }
    
    public static void DisplayLobby()
    {
        AnsiConsole.Clear();
        var table = new Table();
        table.AddColumn("[blue]Name[/]");
        //table.AddColumn("[blue]ID[/]");
                    
        foreach (var player in PlayerData.Players)
        {
            table.AddRow($"{(player.IsLocal ? "[bold green]" : "[white]")}{player.Name}" +
                         $"{(player.IsLocal ? " (Du)" : "")}[/]{(player.IsHost ? " (Host)" : "")}");
        }
        
        AnsiConsole.Write(table);
        if (PlayerData.LocalPlayer.IsHost)
        {
            AnsiConsole.Write("Spiel Starten? [y/n]");
            if (!_AskedQuestion)
            {
                _AskedQuestion = true;
                var prompt = new ConfirmationPrompt("");
                prompt.HideChoices().HideDefaultValue();
                prompt.DefaultValue = true;
                Program.DebugLog("Asking for start..." + _AskedQuestion);
                Program.DebugLog("Start? " + AnsiConsole.Prompt(prompt));
            }
            else
                AnsiConsole.Write(" ");
        }

        //Fals in der live preview die ID angezeigt werden soll
        //player.Id ?? "[grey]Keine ID[/]"
    }
}