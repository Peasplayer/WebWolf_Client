using System.Drawing;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Spectre.Console;
using Websocket.Client;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;
using WebWolf_Client.Settings;
using Color = Spectre.Console.Color;

namespace WebWolf_Client.Ui;

public static class UiHandler
{
    // Zeigt das Hauptmenü an
    public static void DisplayMainMenu()
    {
        Console.Clear();
        RenderCard(RoleType.Werwolf, "WebWolf");
        
        // Benutzer wird aufgefordert zu wählen welches Menü geöffnet werden soll
        var openSettings = UiHandler.Prompt(new SelectionPrompt<string>()
            .Title("Möchtest du das Spiel starten oder die Einstellungen öffnen?")
            .AddChoices("Spiel starten", "Regeln","Einstellungen"));
        switch (openSettings)
        {
            // Wen "Spiel starten" ausgewählt wird das Spiel gestartet
            case "Spiel starten":
                // Benutzer wird aufgefordert einen Namen einzugeben
                NetworkingManager.InitialName = UiHandler.Prompt(
                    new TextPrompt<string>("Wie möchtest du im Spiel heißen?")
                        .Validate(n =>
                        {
                            // Name muss kürzer als 15 Zeichen sein
                            if (n.Length > 15)
                            {
                                ClearConsoleLine(2);
                                return ValidationResult.Error("[red]Der Name darf maximal 15 Zeichen lang sein![/]");
                            }
        
                            // Name darf kein Leerzeichen enthalten
                            if (n.Contains(" "))
                            {
                                ClearConsoleLine(2);
                                return ValidationResult.Error("[red]Der Name darf keine Leerzeichen enthalten![/]");
                            }
                    
                            return ValidationResult.Success();
                        }));
                ClearConsoleLine(2);
                AnsiConsole.MarkupLine("\nHallo, [green]{0}[/]!", NetworkingManager.InitialName);
                
                // Client verbindet sich mit dem Server
                if (NetworkingManager.ConnectToServer())
                {
                    // Programm wird am Leben erhalten
                    Program.KeepAlive = true;
                    // Verbindung wird als erfolgreich markiert
                    NetworkingManager.Instance.InitialConnectionSuccessful = true;
                    // Die passenden Zustände werden gesetzt
                    GameManager.ChangeState(GameManager.GameState.InLobby);
                    GameManager.ChangeInGameState(GameManager.InGameStateType.NoGame);
                }
                // Wenn Server nicht erreichbar ist, wird ein Fehler ausgegeben
                else
                {
                    UiHandler.DisplayDisconnectionScreen(new DisconnectionInfo(DisconnectionType.Error, 
                        WebSocketCloseStatus.EndpointUnavailable, "Connection failed", 
                        null, null));
                }
                break;
            // Wenn Einstellung ausgewählt wurde, werden die Einstellungen geöffnet
            case "Einstellungen":
                DisplaySettings();
                break;
            // Wenn Regeln ausgewählt wurde, werden die Regeln geöffnet 
            case "Regeln":
                DisplayRules();
                break;
        }
    }

    // Variable ob die Start-Frage schon gestellt wurde
    private static bool _AskedQuestion;

    public static void ResetLobby()
    {
        _AskedQuestion = false;
    }
    
    public static void DisplayLobby()
    {
        // Tabelle mit allen Spielern die auf dem Server sind. Der erste Spieler, der den Server joint, ist der Host
        AnsiConsole.Clear();
        var table = new Table();
        table.AddColumn("[blue]Name[/]");
                    
        foreach (var player in PlayerData.Players)
        {
            table.AddRow($"{(player.IsLocal ? "[bold green]" : "[white]")}{player.Name}" +
                         $"{(player.IsLocal ? " (Du)" : "")}[/]{(player.IsHost ? " (Host)" : "")}");
        }
        
        AnsiConsole.Write(table);
        // Wenn der Spieler der Host ist und 4 Spieler insgesamt /vorhanden sind, kann er das Spiel starten
        if (PlayerData.LocalPlayer.IsHost && PlayerData.Players.Count >= 4)
        {
            AnsiConsole.Write("Drücke ENTER um das Spiel zu starten!");
            // Falls die Frage noch nicht gestellt wurde, wird sie gestellt
            if (!_AskedQuestion)
            {
                _AskedQuestion = true;
                var prompt = new ConfirmationPrompt("");
                prompt.HideChoices().HideDefaultValue();
                prompt.DefaultValue = true;
                Program.DebugLog("Asking for start..." + _AskedQuestion);
                var result = UiHandler.Prompt(prompt);
                Program.DebugLog("Start? " + result);
                
                // Je nach Ergebnis wird das Spiel gestartet
                if (!result)
                {
                    _AskedQuestion = false;
                    DisplayLobby();
                    return;
                }
                
                NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                    new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.StartGame, "")));
            }
            else
                AnsiConsole.Write(" ");
        }
    }

    private static CancellationTokenSource? _promptCancel;
    private static Task? _task;
    // Prompt-Methode, die den vorherigen Prompt abbricht und einen neu startet
    public static T Prompt<T>(IPrompt<T> prompt)
    {
        try
        {
            // Entfernt alle Tasteneingaben, die vor dem Prompt gemacht wurden
            while (Console.KeyAvailable) Console.ReadKey();
            
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

    // Bricht den aktuellen Prompt ab
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

    private static Tuple<Action, Predicate<PlayerData>, string, Action<PlayerData>>? CurrentPlayerPrompt;
    // Methode um die Spieler-Auswahl erneuern zu können, bei Änderungen
    public static void StartPlayerPrompt(Action renderPage, Predicate<PlayerData> playerSelector, string text, Action<PlayerData> finishingAction, Func<PlayerData, string>? converter = null)
    {
        // Alte Abfrage wird abgebrochen
        CancelPrompt();
        // Seite wird gerendert
        renderPage.Invoke();
        // Angaben werden gespeichert, für Wiederholungen
        CurrentPlayerPrompt = new Tuple<Action, Predicate<PlayerData>, string, Action<PlayerData>>(renderPage, playerSelector, text, finishingAction);
        // Propmt wird ausgeführt
        var prompt = new SelectionPrompt<PlayerData>()
            .Title(text)
            .PageSize(10)
            .AddChoices(PlayerData.Players.FindAll(playerSelector));
        prompt.Converter = converter ?? (player => player.Name);
        var result = Prompt(prompt);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (result == null)
            return;
        
        // Nur wenn er erfolgreich war, wird der Callback ausgeführt
        CurrentPlayerPrompt = null;
        Program.DebugLog("Inoviking finishing action...");
        finishingAction.Invoke(result);
    }

    // Wiederholt die letzte Spieler-Auswahl
    public static void ReRunPlayerPrompt()
    {
        if (CurrentPlayerPrompt != null)
            StartPlayerPrompt(CurrentPlayerPrompt.Item1, CurrentPlayerPrompt.Item2, CurrentPlayerPrompt.Item3, CurrentPlayerPrompt.Item4);
    }

    // Löscht nur eine Reihe der Console
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

    // Zeigt das Bild einer Rolle an
    public static void RenderCard(RoleType role, string title, int size = 20)
    {
        var image =
            new CanvasImage(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"WebWolf_Client.Resources.{role}.jpg") ?? throw new InvalidOperationException());

        image.MaxWidth(size);//((int) (Console.WindowHeight * 0.6f));
        AnsiConsole.Write(new Align(image, HorizontalAlignment.Center, VerticalAlignment.Top));
        AnsiConsole.Write(new FigletText(title).Centered().Color(Color.RosyBrown));
    }

    // Wenn der Benutzer die Verbindung zu dem Server verliert, wird ein Fehlerbildschirm mit weiteren Informationen angezeigt
    public static void DisplayDisconnectionScreen(DisconnectionInfo info)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText(":/").Centered().Color(Color.Red));
        AnsiConsole.Write(new Markup("[red]Verbindung verloren!" + (NetworkingManager.DisconnectionReason == "" ? 
            "" : $"\nGrund: {NetworkingManager.DisconnectionReason}") + "[/]").Centered());
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
            if (info.CloseStatus != null)
                AnsiConsole.MarkupLine($"[red]Status: {info.CloseStatus}[/]");
            if (info.CloseStatusDescription != null)
                AnsiConsole.MarkupLine($"[red]Description: {info.CloseStatusDescription}[/]");
            if (info.Exception != null)
                AnsiConsole.MarkupLine($"[red]Exception: {info.Exception.Message}[/]");
        }
        // Fragt den Benutzer, ob er zum Hauptmenü zurückkehren möchte
        var backToMainMenu = new ConfirmationPrompt("Möchtest du zurück zum Hauptmenü?")
        {
            DefaultValue = true,
            ShowDefaultValue = true
        };

        var backToMainMenuResult = UiHandler.Prompt(backToMainMenu);
        if (backToMainMenuResult)
        {
            AnsiConsole.Clear();
            UiHandler.DisplayMainMenu();   
        }
        else
        {
            Program.KeepAlive = false;
        }
    }
    
    // Am Anfang des Spieles wird jedem Spieler seine Rolle angezeigt
    private static void DisplayCardReveal()
    {
        AnsiConsole.Clear();
        RenderCard(PlayerData.LocalPlayer.Role,PlayerData.LocalPlayer.Role.ToString());
        Thread.Sleep(5 * 1000);
    }
    
    // Mach einen Kreis in dem die Namen der Spieler angezeigt wird 
    private static Point DrawCircle(List<string> texts)
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
    
    // Text wird Zeichen für Zeichen geschrieben, anstatt direkt aufzutauchen
    private static void RenderText(string text, int delayBetweenChar = 47)
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
    
    // Stellt Text um einen Mittelpunkt herum dar ...
    private static void RenderTextAroundPoint(Point point, string text, bool instant, int delayBetweenChar = 47, int delayBetweenLines = 200)
    {
        var lines = text.Split('\n');
        if (instant)
        {
            // ... ohne Verzögerung
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                AnsiConsole.Cursor.SetPosition(point.X - line.Length / 2, point.Y - lines.Length / 2 + i);
                AnsiConsole.Write(line);
                if (i < lines.Length - 1)
                    AnsiConsole.Write("\n");
            }
            return;
        }
        
        // ... Zeichen für Zeichen 
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

    public static readonly Dictionary<string, List<string>> UiMessagesWaitList = new Dictionary<string, List<string>>();
    
    // Sendet eine UI-Nachricht an andere Spieler und wartet bis diese dargestellt wurde
    public static void RpcUiMessage(UiMessageType type, string data = "", List<string>? receivers = null)
    {
        // Erstellt eine eindeutige ID für die Nachricht ...
        var messageId = Guid.NewGuid().ToString();
        // ... und eine Warteliste
        UiMessagesWaitList.Add(messageId, new List<string>());
        // Alle Empfänger werden zur Warteliste hinzugefügt
        foreach (var playerId in receivers ?? PlayerData.Players.ConvertAll(player => player.Id))
        {
            UiMessagesWaitList[messageId].Add(playerId);
        }
        
        // Nachricht wird an die Empfänger gesendet
        if (receivers == null)
        {
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.UiMessage,
                    JsonConvert.SerializeObject(new Pakets.UiMessage(type, data, messageId)))));
        }
        else
        {
            foreach (var receiver in receivers)
            {
                NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                    new SendToPaket(NetworkingManager.Instance.CurrentId, PaketDataType.UiMessage,
                        JsonConvert.SerializeObject(new Pakets.UiMessage(type, data, messageId)), receiver)));
            }
        }
        
        // Wartet bis alle Empfänger die Nachricht dargestellt haben
        while (UiMessagesWaitList[messageId].Count > 0) { }
        // Entfernt die Nachricht aus der Warteliste
        UiMessagesWaitList.Remove(messageId);
    }
    
    // Zeigt eine UI-Nachricht nur dem lokalen Spieler an
    public static void LocalUiMessage(UiMessageType type, string data = "", string messageId = "")
    {
        switch (type)
        {
            case UiMessageType.RenderText:
                IsInInGameMenu = false;
                RenderText(data);
                break;
            case UiMessageType.DrawPlayerNameCircle:
                IsInInGameMenu = false;
                // Falls zusätzliche Daten da sind, werden diese genutzt
                if (data.StartsWith("{") && data.EndsWith("}"))
                {
                    var specialData = JsonConvert.DeserializeObject<SpecialPlayerCircle>(data);
                    DrawPlayerNameCircle(specialData.CenterText, false, specialData.PlayerNames);
                }
                else
                    DrawPlayerNameCircle(data);
                break;
            case UiMessageType.DisplayInGameMenu:
                DisplayInGameMenu(false);
                break;
            case UiMessageType.DisplayCardReveal:
                IsInInGameMenu = false;
                DisplayCardReveal();
                break;
            case UiMessageType.Clear:
                IsInInGameMenu = false;
                AnsiConsole.Clear();
                break;
            case UiMessageType.RenderCard:
                IsInInGameMenu = false;
                RenderCard((RoleType) Enum.Parse(typeof(RoleType), data), "");
                break;
        }
        
        if (messageId != "")
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.UiMessageFinished, messageId)));
    }
    
    // Zeichnet die Namen der Spieler in den berechneten Kreis 
    private static void DrawPlayerNameCircle(string centerText = "", bool instant = false, List<string>? playerNames = null)
    {
        var point = DrawCircle(playerNames ?? PlayersToPlayerNames(PlayerData.Players));
        if (centerText != "")
            RenderTextAroundPoint(point, centerText, instant);
    }

    // Variable ob das InGame-Menü angezeigt wird
    public static bool IsInInGameMenu { get; set; }
    
    // Zeigt alle Spieler in einem Kreis und den passenden Text in der Mitte an
    public static void DisplayInGameMenu(bool instant)
    {
        IsInInGameMenu = true;
        DrawPlayerNameCircle(GameManager.InGameState == GameManager.InGameStateType.Night ? "Alle Dorfbewohner schlafen (zzZ)" : "", instant);
    }
    
    // Alle Spieler stimmen tagsüber ab, wen sie lynchen wollen
    public static void DisplayVillageVote()
    {
        UiHandler.CancelPrompt();
        AnsiConsole.Clear();
        
        // Übersicht aller Spieler
        AnsiConsole.Write(new Align(new Panel(String.Join(", ", PlayersToPlayerNames(PlayerData.Players))).Header("Spieler"), HorizontalAlignment.Center));
        AnsiConsole.WriteLine("\n");

        // Liste an Optionen mit jeweiligen Stimmen
        string VotesForOption(string id, string name)
        {
            var votes = GameManager.Votes.Where(pair => pair.Value == id)
                .ToList()
                .ConvertAll(pair => PlayerData.GetPlayer(pair.Key).Name);
            return name + (votes.ToArray().Length > 0 ? $" (Votes: {string.Join(", ", votes)})" : "");
        }
        var voterList = PlayerData.Players.FindAll(player => player.IsAlive).ConvertAll(player => VotesForOption(player.Id, player.Name));
        voterList.Add(VotesForOption("skipped", "   Überspringen"));

        if (!GameManager.HasVoted)
        {
            // Gibt den Spielern eine Auswahl für wen sie abstimmen wollen
            var playerName = UiHandler.Prompt(
                new SelectionPrompt<string>()
                    .Title("Wähle einen Spieler den du für einen Werwolf hälst:")
                    .PageSize(10)
                    .AddChoices(voterList));
            Program.DebugLog("Choice: " + playerName);
            // Überprüft, ob "Überspringen" ausgewählt wurde
            var votedPlayerId = "";
            if (playerName.Split(" (Votes: ")[0] == "   Überspringen")
            {
                votedPlayerId = "skipped";
            }
            // Überprüft, ob und welcher Spieler gewählt wurde
            else
            {
                var player = PlayerData.Players.FirstOrDefault(p => p.Name == playerName.Split(" (Votes: ")[0]);
                if (player == null)
                    return;
                votedPlayerId = player.Id;
            }
            
            // Sendet dem Host welcher Spieler gewählt wurde
            GameManager.HasVoted = true;
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.VillageVoteVoted, JsonConvert.SerializeObject(new Pakets.SimplePlayerId(votedPlayerId))))); 
        }
        else
        {
            foreach (var player in voterList)
            {
                AnsiConsole.WriteLine(player); 
            }
        }
    }

    // Zeigt den End-Screen, in dem angezeigt wird, welches Team gewonnen hat 
    public static void DisplayEndScreen(bool villageWon)
    {
        AnsiConsole.Clear();
        RenderCard(villageWon ? RoleType.Dorfbewohner : RoleType.Werwolf, 
            (villageWon ? PlayerData.LocalPlayer.Role != RoleType.Werwolf : PlayerData.LocalPlayer.Role == RoleType.Werwolf)
                ? "GEWONNEN" : "VERLOREN");
        RenderText(villageWon ? "Die Dorfbewohner haben überlebt! Alle Werwölfe sind Tot."
            : "Die Werwölfe haben alle Dorfbewohner gefressen!");
        NetworkingManager.Instance.Client.Stop(WebSocketCloseStatus.NormalClosure, "Game ended");
        Task.Delay(1000).Wait();
        
        // Fragt, ob die Spieler nochmal spielen möchten
        if (AnsiConsole.Confirm("Möchtest du nochmal spielen?"))
        {
            if (NetworkingManager.ConnectToServer())
            {
                Program.KeepAlive = true;
                NetworkingManager.Instance.InitialConnectionSuccessful = true;
                GameManager.ChangeState(GameManager.GameState.InLobby);
                GameManager.ChangeInGameState(GameManager.InGameStateType.NoGame);
            }
        }
        // Wenn nein, wird das Programm beendet
        else
        {
            Program.KeepAlive = false;
        }
    }
    
    // Wandelt eine Liste von Spielern in eine Liste von Spielernamen um, mit den gewünschten Attributen
    public static List<string> PlayersToPlayerNames(List<PlayerData> players, bool showSelf = true, bool showDead = true, string color = "", RoleType roleForColor = RoleType.NoRole)
    {
        return players.ConvertAll(player =>
        {
            var result = "";
            if (color != "" && (roleForColor == RoleType.NoRole || player.Role == roleForColor))
                result += "[" + color + "]";
            result += player.Name;
            if (color != "" && (roleForColor == RoleType.NoRole || player.Role == roleForColor))
                result += "[/]";
            if (showSelf && player.IsLocal) // Hinter dem lokalen Spieler wird in grün "Du" angezeigt
                result += " [green](Du)[/]";
            if (showDead && !player.IsAlive) // Hinter Toten Spielern wird in rot "Tot" angezeigt
                result += " [red](Tot)[/]";

            return result;
        });
    }

    // Zeigt die Einstellungen an
    public static void DisplaySettings()
    {
        AnsiConsole.Clear();
        AnsiConsole.Markup("[blue]Einstellungen[/]");

        // Fügt alle möglichen Einstellung ,und die Option zurück zum Hauptmenü zu gehen, in eine Auswahl ein aus die der Benutzer wählen kann
        var prompt = new SelectionPrompt<string>()
            .Title("Wähle eine Option:");
        prompt.AddChoice("[[Zurück zum Hauptmenü]]");
        foreach (var generalSetting in SettingsManager.AllSettings)
        {
            switch (generalSetting)
            {
                case NumberSetting setting:
                    prompt.AddChoice($"{setting.Name}: {setting.Value}");
                    break;
                case FloatSetting setting:
                    prompt.AddChoice($"{setting.Name}: {setting.Value.ToString("0.0")}"); // Float settings werden mit einer nachkommer Zahl angezeigt
                    break;
                case BooleanSetting setting:
                    prompt.AddChoice($"{setting.Name}: {(setting.Value ? "Ja" : "Nein")}");
                    break;
                case StringSetting setting:
                    prompt.AddChoice($"{setting.Name}: {setting.Value}");
                    break;
            }
        }
        var option = UiHandler.Prompt(prompt);
        if (option == "[[Zurück zum Hauptmenü]]")
        {
            DisplayMainMenu();
            return;
        }
        
        var settingName = option.Split(": ")[0];
        var selectedSetting = SettingsManager.AllSettings.Find(s => s.Name == settingName);
        if (selectedSetting == null)
        {
            DisplaySettings();
            return;
        }
        
        // Die ausgewählte Einstellung kann durch eine Eingabe verändert werden
        switch (selectedSetting)
        {
            case NumberSetting setting:
                var numberPrompt = new TextPrompt<int>($"Gib einen neuen Wert für \"{setting.Name}\" ein:")
                    .DefaultValue(setting.Value)
                    .ValidationErrorMessage("Ungültige Eingabe!") // Wenn die Eingabe ungültig ist, wir das dem Benutzer gesagt
                    .Validate(n =>
                    {
                        if (n < setting.Min || n > setting.Max)
                            return ValidationResult.Error($"Der Wert muss zwischen {setting.Min} und {setting.Max} liegen!"); // Überprüft ob der eingegebene wert zwischen dem min und max wert liegt
                        return ValidationResult.Success();
                    });
                var newNumber = UiHandler.Prompt(numberPrompt);
                setting.SetValue(newNumber);
                break;
            case FloatSetting setting:
                var floatPrompt = new TextPrompt<float>($"Gib einen neuen Wert für \"{setting.Name}\" ein:")
                    .DefaultValue(setting.Value)
                    .ValidationErrorMessage("Ungültige Eingabe!") // Wenn die Eingabe ungültig ist, wir das dem Benutzer gesagt
                    .Validate(n =>
                    {
                        if (n < setting.Min || n > setting.Max)
                            return ValidationResult.Error($"Der Wert muss zwischen {setting.Min} und {setting.Max} liegen!"); // Überprüft ob der eingegebene wert zwischen dem min und max wert liegt
                        return ValidationResult.Success();
                    });
                var newFloat = UiHandler.Prompt(floatPrompt);
                setting.SetValue(newFloat);
                break;
            case BooleanSetting setting:
                var boolPrompt = new ConfirmationPrompt($"Möchtest du \"{setting.Name}\" {(setting.Value ? "deaktivieren" : "aktivieren")}?")
                {
                    InvalidChoiceMessage = "Ungültige Eingabe!",
                    DefaultValue = setting.Value
                };
                var newBool = UiHandler.Prompt(boolPrompt);
                setting.SetValue(newBool);
                break;
            case StringSetting setting:
                var stringPrompt = new TextPrompt<string>($"Gib einen neuen Wert für \"{setting.Name}\" ein:") // Überprüft ob der eingegebene wert zwischen dem min und max wert liegt
                    .DefaultValue(setting.Value)
                    .ValidationErrorMessage("Ungültige Eingabe!"); // Wenn die Eingabe ungültig ist, wir das dem Benutzer gesagt
                var newString = UiHandler.Prompt(stringPrompt);
                setting.SetValue(newString);
                break;
        }

        DisplaySettings();
    }

    // Zeigt die Relgen in einem Panel pro Punkt an
    public static void DisplayRules()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Regeln") // Schreibt "Regeln" in einem besonderem Text Font
            .Color(Color.Blue)
            .Centered()
        );
        
        // Disclaimer, dass man miteinander reden sollte
        AnsiConsole.Write(new Panel("[red] Das nutzen von Voice Chat oder das Spiel lokal zu spielen ist sehr empfohlen! [/]").Header("[red] Disclaimer [/]"));
        // Panel für die Regeln des Spieles
        AnsiConsole.Write(new Panel("Das Ziel des Spiels ist es, alle Werwölfe zu entlarven und zu eliminieren," +
                                    "bevor die Werwölfe alle Dorfbewohner gefressen haben.").Header("[green] Ziel des Spieles [/]"));
        AnsiConsole.Write(new Panel("Das Spiel besteht aus Tag- und Nachtphasen. In den Nachtphasen werden die einzelnen Rollen aufgerufen." +
                                    "\nAm Tag stimmen alle Spieler ab, wen sie für einen Werwolf halten und lynchen möchten.").Header("[green] Spielablauf [/]"));
        // Panel für die Erklärung der Rollen
        AnsiConsole.Write(new Panel("Es gibt verschiedene Rollen. Das Ziel der [red] Werwölfe [/] ist es alle [green] Dorfbewohner [/] zu fressen. Sie gewinnen, wenn es gleich viele oder mehr [red] Werwölfe [/] als [green] Dorfbewohner [/] gibt." +
                                    "\nDie Aufgabe der [green] Dorfbewohner [/] ist es, die [red] Werwölfe [/] zu entlarven und sie tagsüber bei einer Abstimmung zu lynchen." +
                                    "\n\nDie [green] Seherin [/] ist Teil des Dorfes. Sie kann jede Nacht die Identität eines Spielers erfahren." + 
                                    "\n\nDie [green] Hexe [/] ist Teil des Dorfes. Sie kann einmalig das Opfer der Werwölfe heilen und einmalig einen Spieler ihrer Wahl töten." +
                                    "\n\nDer [green] Jäger [/] ist Teil des Dorfes. Wenn er stirbt kann er eine Person mit in den tot reisen." +
                                    "\n\nDer [green] Amor [/] ist Teil des Dorfes. Er kann einmalig zwei Spieler verlieben. Stirbt einer der beiden, stirbt der andere auch.").Header("[green] Rollen [/]"));
        
        AnsiConsole.MarkupLine("\nDrücke [orange3] ENTER [/], um zurück zum Hauptmenü zu gelangen.");
        Console.ReadLine();
        DisplayMainMenu();
    }

    public static void RpcVoteAnnounceVictim(PlayerData? player)
    {
        if (player == null)
        {
            // Es wurde kein Opfer gefunden
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                $"Die Dorfbewohner haben entschieden!\nNiemand wurde zum Tode verurteilt!");
        }
        else
        {
            // Es wurde ein Opfer gefunden
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                $"Die Dorfbewohner haben entschieden!\n{player.Name} wurde zum Tode verurteilt!");
            Task.Delay(1000).Wait();
            
            if (PlayerData.LocalPlayer.IsHost)
            {
                // Host setzt das Opfer auf Tod
                player.RpcMarkAsDead();
                PlayerData.RpcProcessDeaths();
            }
        }
    }
}