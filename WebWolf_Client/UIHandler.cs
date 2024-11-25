using System.Reflection;
using Newtonsoft.Json;
using Spectre.Console;
using Websocket.Client;
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
        RenderLogo();
        
        AnsiConsole.WriteLine("");
        NetworkingManager.InitialName = UiHandler.Prompt(
            new TextPrompt<string>("Wie möchtest du im Spiel heißen?")
                .Validate(n =>
                {
                    if (n.Length > 15)
                    {
                        ClearConsoleLine(2);
                        return ValidationResult.Error("[red]Der Name darf maximal 15 Zeichen lang sein![/]");
                    }
        
                    if (n.Contains(" "))
                    {
                        ClearConsoleLine(2);
                        return ValidationResult.Error("[red]Der Name darf keine Leerzeichen enthalten![/]");
                    }
                    
                    return ValidationResult.Success();
                }));
        ClearConsoleLine(2);
        AnsiConsole.MarkupLine("\nHallo, [green]{0}[/]!", NetworkingManager.InitialName);
        
        var net = new NetworkingManager();
        net.StartConnection("ws://localhost:8443/json");
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .StartAsync("Connecting", _ => net.ConnectionTask.WaitAsync(CancellationToken.None)).Wait();

        return net.Client.IsRunning;
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
        // Wenn der Spieler der Host ist und 5 Spieler vorhanden sind, kann er das Spiel starten
        if (PlayerData.LocalPlayer.IsHost && PlayerData.Players.Count >= 5)
        {
            AnsiConsole.Write("Drücke ENTER um das Spiel zu starten!");
            if (!_AskedQuestion)
            {
                _AskedQuestion = true;
                var prompt = new ConfirmationPrompt("");
                prompt.HideChoices().HideDefaultValue();
                prompt.DefaultValue = true;
                Program.DebugLog("Asking for start..." + _AskedQuestion);
                var result = UiHandler.Prompt(prompt);
                Program.DebugLog("Start? " + result);
                
                if (!result)
                {
                    _AskedQuestion = false;
                    DisplayLobby();
                    return;
                }
                
                NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                    new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.StartGame, "")));
            }
            else
                AnsiConsole.Write(" ");
        }

        //Fals in der live preview die ID angezeigt werden soll
        //player.Id ?? "[grey]Keine ID[/]"
    }

    private static CancellationTokenSource? _promptCancel;
    private static Task? _task;
    public static T Prompt<T>(IPrompt<T> prompt)
    {
        try
        {
            if (_promptCancel != null && _task != null)
            {
                _promptCancel.Cancel();
                _promptCancel.Token.WaitHandle.WaitOne();
                try {
                    _task.Wait();
                }
                catch(Exception _) {}
            }

            _promptCancel = new CancellationTokenSource();
            Program.DebugLog("Starting new prompt...");
            var task = prompt.ShowAsync(AnsiConsole.Console, _promptCancel.Token);
            _task = task;
            task.Wait();
            return task.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Program.DebugLog("Prompt failed: " + e.Message);
            return default;
        }
    }

    public static void ClearConsoleLine(int line = 1)
    {
        int currentLineCursor = Console.CursorTop;
        for (int i = 0; i < line; i++)
        {
            Console.SetCursorPosition(0, currentLineCursor - i - 1);
            Console.Write(new string(' ', Console.WindowWidth)); 
        }
        Console.SetCursorPosition(0, currentLineCursor - line);
    }

    public static void RenderLogo()
    {
        var image =
            new CanvasImage(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("WebWolf_Client.Resources.Werwolf.jpg") ?? throw new InvalidOperationException());

        image.MaxWidth(20);//((int) (Console.WindowHeight * 0.6f));
        AnsiConsole.Write(new Align(image, HorizontalAlignment.Center, VerticalAlignment.Top));
        AnsiConsole.Write(new FigletText("WebWolf").Centered().Color(Color.RosyBrown));
    }

    public static void DisplayDisconnectionScreen(DisconnectionInfo info)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText(":/").Centered().Color(Color.Red));
        AnsiConsole.Write(new Markup("[red]Verbindung verloren![/]").Centered());
        var prompt = new ConfirmationPrompt("Details?")
        {
            DefaultValue = false,
            ShowDefaultValue = false
        };
        var result = UiHandler.Prompt(prompt);
        if (result)
        {
            UiHandler.ClearConsoleLine();
            AnsiConsole.MarkupLine($"[red]Type: {info.Type}[/]");
            AnsiConsole.MarkupLine($"[red]Status: {info.CloseStatus}[/]");
            AnsiConsole.MarkupLine($"[red]Description: {info.CloseStatusDescription}[/]");
            AnsiConsole.MarkupLine($"[red]Exception: {info.Exception?.Message}[/]");
                
        }
    }
}