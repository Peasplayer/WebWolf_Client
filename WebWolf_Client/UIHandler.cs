using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

public class UIHandler
{
    public static bool StartGameMenu()
    {
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
        // Fehlt noch
    }
}