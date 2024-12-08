using System.Drawing;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Spectre.Console;
using Websocket.Client;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;
using Color = Spectre.Console.Color;

namespace WebWolf_Client;

public static class UiHandler
{
    public static bool StartGameMenu()
    {
        Console.Clear();
        RenderCard(RoleType.Werwolf, "WebWolf");
        
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
        net.StartConnection(Program.URL);
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
        if (PlayerData.LocalPlayer.IsHost && PlayerData.Players.Count >= 3)
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
            CancelPrompt();

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

    public static void CancelPrompt()
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

    public static void RenderCard(RoleType role, string title, int size = 20)
    {
        var image =
            new CanvasImage(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"WebWolf_Client.Resources.{role}.jpg") ?? throw new InvalidOperationException());

        image.MaxWidth(size);//((int) (Console.WindowHeight * 0.6f));
        AnsiConsole.Write(new Align(image, HorizontalAlignment.Center, VerticalAlignment.Top));
        AnsiConsole.Write(new FigletText(title).Centered().Color(Color.RosyBrown));
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

    public static void DisplayCardReveal()
    {
        AnsiConsole.Clear();
        RenderCard(PlayerData.LocalPlayer.Role,PlayerData.LocalPlayer.Role.ToString());
        Thread.Sleep(5 * 1000);
    }
    
    public static Point DrawCircle(List<string> texts)
    {
        AnsiConsole.Clear();
        var height = Console.WindowHeight;
        var width = Console.WindowWidth;
        var maxHeight = height * 0.9f;
        var maxWidth = width * 0.9f;
        var padX = (width - maxWidth) / 2f;
        var padY = (height - maxHeight) / 2f;
        var centerX = (padX + maxWidth / 2);
        var centerY = (padY + maxHeight / 2);
        var radius = maxHeight / 2;
        
        var mirrorAngel = 360 / texts.Count;
        for (var i = 0; i < texts.Count; i++)
        {
            var text = texts[i];
            var angel = 90 - (mirrorAngel * i);
            var deltaY = Math.Sin(angel * (Math.PI / 180)) * radius;
            var deltaX = (Math.Cos(angel * (Math.PI / 180)) * radius) * 3;
            if (deltaX + centerX > maxWidth)
                deltaX = maxWidth/2;
            if (deltaX + centerX < padX)
                deltaX = - maxWidth/2;
        
            AnsiConsole.Console.Cursor.SetPosition((int) Math.Round(centerX + deltaX, MidpointRounding.AwayFromZero) -
                                                   Regex.Replace(text,  @"\[[^\]]+\]", "").Length / 2, (int) Math.Ceiling(centerY - deltaY));
            AnsiConsole.Markup(text);
        }
        return new Point((int) Math.Round(centerX, MidpointRounding.AwayFromZero), (int) Math.Round(centerY, MidpointRounding.AwayFromZero));
    }

    public static void RenderText(string text, int delayBetweenChar = 47)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var c in line)
            {
                AnsiConsole.Write(c);
                Task.Delay(delayBetweenChar).Wait();
            }
            
            if (i < lines.Length - 1)
                AnsiConsole.Write("\n");
        }
    }

    public static void RenderTextAroundPoint(Point point, string text, int delayBetweenChar = 47, int delayBetweenLines = 200)
    {
        var lines = text.Split('\n');
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            AnsiConsole.Cursor.SetPosition(point.X - line.Length / 2, point.Y - lines.Length / 2 + i);
            RenderText(line, delayBetweenChar);
            Task.Delay(delayBetweenLines).Wait();
            if (i < lines.Length - 1)
                AnsiConsole.Write("\n");
        }
    }

    public static Point DrawPlayerNameCircle()
    {
        return DrawCircle(PlayerData.Players.ConvertAll(player => player.Name + (player.IsLocal ? " [green](Du)[/]" : "")));
    }

    public static void DisplayInGameMenu()
    {
        var center = DrawPlayerNameCircle();
        RenderTextAroundPoint(center, GameManager.InGameState == GameManager.InGameStateType.Night ? "Alle Dorfbewohner schlafen (zzZ)" : "Tag");
    }
}