using System.Net.WebSockets;
using Newtonsoft.Json;
using Websocket.Client;
using WebWolf_Client.Roles;
using WebWolf_Client.Roles.RoleClasses;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client.Networking;

public class NetworkingManager
{
    // Globale Instanze von NetworkingManager
    public static NetworkingManager Instance;

    // Name der sich vom Client gewünscht wurde
    public static string InitialName;
    // Begründung für geschlossene Verbindung, wenn der Client sie selbst geschlossen hat
    public static string DisconnectionReason;

    // Verbindung zum Server herstellen mit angegebener IP
    public static bool ConnectToServer()
    {
        var net = new NetworkingManager();
        net.StartConnection($"ws://{SettingsManager.ServerIp.Value}:8443");
        
        return net.Client.IsRunning;
    }
    
    // Aktuelle ID des Clients
    public string CurrentId { get; private set; }
    // Websocket-Client-Obejkt
    public WebsocketClient Client { get; private set; }
    // Ob die Verbindung erfolgreich hergestellt wurde
    public bool InitialConnectionSuccessful;
    private NetworkingManager()
    {
        // Setzt die globale Instanz
        Instance = this;
        DisconnectionReason = "";
    }

    // Verbindung zum Server herstellen
    private void StartConnection(string url)
    {
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.FromSeconds(5),
            }
        });
        Client = new WebsocketClient(new Uri(url), factory);
        // Neu-Verbindungsversuche deaktivieren
        Client.IsReconnectionEnabled = false;
        Client.ErrorReconnectTimeout = null;
        Client.ReconnectTimeout = TimeSpan.FromSeconds(5);
        // Nachrichten empfangen und an den Listener weiterleiten
        Client.MessageReceived.Subscribe(msg =>
        {
            // Nachricht wird nur verwendet, wenn es Text ist
            if (msg.MessageType == WebSocketMessageType.Text && msg.Text != null)
                OnMessage(msg.Text);
        });
        // Falls die Verbindung nach dem ersten erfolgreichen Verbinden geschlossen wird ...
        Client.DisconnectionHappened.Subscribe(info =>
        {
            if (!InitialConnectionSuccessful)
                return;
            
            // ... wird der Disconnect-Dialog angezeigt
            Program.DebugLog("Connection closed: " + JsonConvert.SerializeObject(info));
            UiHandler.DisplayDisconnectionScreen(info);
        });
        
        // Wartet bis die Verbindung hergestellt wurde oder fehlschlägt
        Client.Start().Wait();
    }

    // Listener für Nachrichten
    private void OnMessage(string message)
    {
        Program.DebugLog("Server says: " + message);
        
        // Nachricht wird in ein Paket umgewandelt
        var paket = JsonConvert.DeserializeObject<Paket>(message);
        if (paket == null)
            return;
        
        // Falls das Paket ein Handshake-Paket ist...
        if (paket.Type == PaketType.Handshake)
        {
            var handshake = JsonConvert.DeserializeObject<HandshakePaket>(message);
            if (handshake == null)
                return;
            
            // ... wird die ID des Clients gesetzt ...
            CurrentId = handshake.Name;
            Program.DebugLog("ID: " + CurrentId);
            // ... und der gewünschte Name geantwortet
            Client.Send(JsonConvert.SerializeObject(new HandshakePaket(CurrentId, InitialName)));
        }
        else
        {
            // Falls das Paket kein Handshake-Paket ist, wird es an den Paket-Handler weitergeleitet
            var normalPaket = JsonConvert.DeserializeObject<NormalPaket>(message);
            if (normalPaket == null)
                return;

            try
            {
                switch (normalPaket.DataType)
                {
                    // Spieler tritt dem Spiel bei
                    case PaketDataType.Join:
                    {
                        Program.DebugLog("Received Join-paket");

                        var joinPaketData =
                            JsonConvert.DeserializeObject<Pakets.PlayerDataPattern>(normalPaket.Data);
                        if (joinPaketData == null)
                            return;

                        if (joinPaketData.Id == PlayerData.LocalPlayer.Id)
                        {
                            Program.DebugLog("Local player is already joined");
                            return;
                        }

                        // Lifecycle-Methode wird aufgerufen
                        Task.Run(() => GameManager.OnPlayerJoin(joinPaketData));
                        break;
                    }
                    // Spielerdaten werden synchronisiert
                    case PaketDataType.SyncLobby:
                    {
                        var syncPaketData = JsonConvert.DeserializeObject<Pakets.SyncLobbyPaket>(normalPaket.Data);
                        if (syncPaketData == null)
                            return;

                        // Spieler-Objekte werden neu erstellt
                        PlayerData.Players.Clear();
                        foreach (var playerDataPattern in syncPaketData.Players)
                        {
                            PlayerData.Players.Add(new PlayerData(playerDataPattern.Name, playerDataPattern.Id, playerDataPattern.IsHost));
                        }
                        
                        Program.DebugLog("Players after sync: " + JsonConvert.SerializeObject(PlayerData.Players));
                        // Lobby wird neu gerendert
                        if (GameManager.State == GameManager.GameState.InLobby)
                            Task.Run(UiHandler.DisplayLobby);
                        break;
                    }
                    // Spieler verlässt das Spiel
                    case PaketDataType.Leave:
                    {
                        Program.DebugLog("Received Leave-paket");

                        var paketData = JsonConvert.DeserializeObject<Pakets.PlayerDataPattern>(normalPaket.Data);
                        if (paketData == null)
                            return;

                        if (paketData.Id == PlayerData.LocalPlayer.Id)
                        {
                            Program.DebugLog("Local player can't leave!!!");
                            return;
                        }

                        // Lifecycle-Methode wird aufgerufen
                        GameManager.OnPlayerLeave(paketData);
                        break;
                    }
                    // Host wird festgelegt
                    case PaketDataType.SetHost:
                    {
                        Program.DebugLog("Received SetHost-paket");

                        var paketData = JsonConvert.DeserializeObject<Pakets.PlayerDataPattern>(normalPaket.Data);
                        if (paketData == null)
                            return;
                        
                        PlayerData.GetPlayer(paketData.Id).SetHost();
                        // Lobby wird neu gerendert
                        if (GameManager.State == GameManager.GameState.InLobby)
                            Task.Run(UiHandler.DisplayLobby);
                        break;
                    }
                    // Verbindung soll getrennt werden
                    case PaketDataType.Disconnect:
                    {
                        Program.DebugLog("Received Disconnect-paket");

                        // Der Grund des Servers wird gespeichert
                        DisconnectionReason = normalPaket.Data;
                        Client.Stop(WebSocketCloseStatus.NormalClosure, normalPaket.Data);
                        break;
                    }
                    // Das Spiel wird vom Host gestartet
                    case PaketDataType.StartGame:
                    {
                        Program.DebugLog("Received StartGame-paket");
                        Task.Run(() => GameManager.ChangeState(GameManager.GameState.InGame));
                        break;
                    }
                    // Das Spiel wird beendet
                    case PaketDataType.EndGame:
                    {
                        Program.DebugLog("Received EndGame-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SimpleBoolean>(normalPaket.Data);
                        Task.Run(() =>
                        {
                            // Es wird in den Zustand "Kein Spiel" gewechselt
                            GameManager.ChangeState(GameManager.GameState.NoGame);
                            GameManager.ChangeInGameState(GameManager.InGameStateType.NoGame);
                            // Endbildschirm wird angezeigt
                            UiHandler.DisplayEndScreen(data.Value);
                        });
                        break;
                    }
                    // Rolle eines Spielers wird festgelegt
                    case PaketDataType.SetRole:
                    {
                        Program.DebugLog("Received SetRole-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SetRolePattern>(normalPaket.Data);
                        PlayerData.GetPlayer(data.Id).SetRole(data.Role);
                        Program.DebugLog($"Player {PlayerData.GetPlayer(data.Id).Name} is a {data.Role}");
                        break;
                    }
                    // Nacht bzw. Tag wird gestartet
                    case PaketDataType.StartNightOrDay:
                    {
                        Program.DebugLog("Received StartNightOrDay-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SimpleBoolean>(normalPaket.Data);
                        Program.DebugLog($"It is now {(data.Value ? "Night" : "Day")}");
                        Task.Run(() => GameManager.ChangeInGameState(data.Value ? GameManager.InGameStateType.Night : GameManager.InGameStateType.Day));
                        break;
                    }
                    // Rolle wird aufgerufen
                    case PaketDataType.CallRole:
                    {
                        Program.DebugLog("Received CallRole-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SimpleRole>(normalPaket.Data);
                        Program.DebugLog($"Role {data.Role} is being called");
                        var role = RoleManager.GetRole(data.Role);
                        // Rollen-Aktion wird zurückgesetzt
                        role.ResetAction();
                        // Falls der lokale Spieler die Rolle hat und lebendig ist oder es eine Rolle ist, die auch bei toten Spielern ausgeführt wird ...
                        if (PlayerData.LocalPlayer.Role == data.Role && (PlayerData.LocalPlayer.IsAlive || !role.IsAliveRole))
                        {
                            // ... wird die Aktion vorbereitet
                            Task.Run(role.PrepareAction);
                        }
                        break;
                    }
                    // Rollen-Aktion wurde abgeschlossen
                    case PaketDataType.RoleFinished:
                    {
                        Program.DebugLog("Received RoleFinished-paket");
                        if (!PlayerData.LocalPlayer.IsHost)
                            return;
                        
                        var data = JsonConvert.DeserializeObject<Pakets.SimpleRole>(normalPaket.Data);
                        var currentWaitState = RoleManager.WaitingForRole[normalPaket.Sender];
                        if (currentWaitState == 0)
                        {
                            // Nur der User-Input ist abgeschlossen
                            Program.DebugLog($"{normalPaket.Sender} completed role {data.Role} action (no UI)");
                            RoleManager.WaitingForRole[normalPaket.Sender] = 1;
                        }
                        else
                        {
                            // Aktion ist komplett beendet
                            Program.DebugLog($"{normalPaket.Sender} finished role {data.Role}");
                            RoleManager.WaitingForRole.Remove(normalPaket.Sender);
                        }
                        break;
                    }
                    // Rollen-Aktion wurde abgegbrochen
                    case PaketDataType.RoleCanceled:
                    {
                        Program.DebugLog("Received RoleCanceled-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SimpleRole>(normalPaket.Data);
                        Program.DebugLog($"{data.Role}'s action was canceled");
                        // Falls der lokale Spieler die Rolle hat wird ein möglicher Dialog abgebrochen
                        if (PlayerData.LocalPlayer.Role == data.Role)
                        {
                            UiHandler.CancelPrompt();
                        }
                        // Rollen-Aktion wird abgebrochen
                        RoleManager.GetRole(data.Role).PrepareCancelAction();
                        break;
                    }
                    // Stimme eines Werwolfes wurde gesendet
                    case PaketDataType.WerwolfVote:
                    {
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        var role = (Werwolf) RoleManager.GetRole(RoleType.Werwolf);
                        // Stimme wird gespeichert
                        role.Votes[normalPaket.Sender] = data.Id;
                        // Dialog wird neu angezeigt, falls der lokale Spieler Werwolf ist und es noch ausstehende Stimmen gibt
                        if (PlayerData.LocalPlayer.Role == RoleType.Werwolf && role.Votes.Count < PlayerData.Players.Count(player => player is { Role: RoleType.Werwolf, IsAlive: true }))
                            Task.Run(role.SelectVictim);

                        // Der Host überprüft, ob die Abstimmung beendet ist
                        if (PlayerData.LocalPlayer.IsHost)
                        {
                            role.CalculateVictim();
                        }
                        break;
                    }
                    // Den lebendigen Werwölfen wird ihr Opfer angekündigt
                    case PaketDataType.WerwolfAnnounceVictim:
                    {
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        Werwolf.LastVicitmId = data.Id;
                        if (PlayerData.LocalPlayer.Role == RoleType.Werwolf && PlayerData.LocalPlayer.IsAlive)
                            ((Werwolf) RoleManager.GetRole(RoleType.Werwolf)).AnnounceVictim(data.Id);
                        break;
                    }
                    // Spieler wird als tot markiert
                    case PaketDataType.PlayerMarkedAsDead:
                    {
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        var player = PlayerData.GetPlayer(data.Id);
                        player.MarkAsDead(true);
                        Program.DebugLog($"{player.Name} is marked as dead");
                        break;
                    }
                    // Als-Tot-Markierung eines Spielers wird aufgehoben
                    case PaketDataType.PlayerUnmarkedAsDead:
                    {
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        PlayerData.GetPlayer(data.Id).MarkAsDead(false);
                        break;
                    }
                    // Als-Tot-Markierte Spieler werden auf tot gesetzt
                    case PaketDataType.PlayerProcessDeaths:
                    {
                        PlayerData.ProcessDeaths();
                        break;
                    }
                    // Spieler wird als verliebt markiert
                    case PaketDataType.PlayerInLove:
                    {
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        PlayerData.GetPlayer(data.Id).SetInLove();
                        break;
                    }
                    // Abstimmung des Dorfes wird gestartet
                    case PaketDataType.VillageVoteStart:
                    {
                        Program.DebugLog("Received VillageVoteStart-paket");
                        GameManager.Votes.Clear();
                        // Falls der Spieler lebt, darf er abstimmen
                        GameManager.HasVoted = !PlayerData.LocalPlayer.IsAlive;
                        GameManager.VoteIsStarted = true;
                        // Abstimmung wird angezeigt
                        Task.Run(UiHandler.DisplayVillageVote);
                        break;
                    }
                    // Stimme eines Spielers bei der Dorf-Abstimmung wird gesendet
                    case PaketDataType.VillageVoteVoted:
                    {
                        Program.DebugLog("Received VillageVoteStart-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.SimplePlayerId>(normalPaket.Data);
                        // Stimme wird gespeichert
                        GameManager.Votes[normalPaket.Sender] = data.Id;
                        // Abstimmung wird neu angezeigt
                        Task.Run(UiHandler.DisplayVillageVote);
                        break;
                    }
                    // Dorf-Abstimmung wird abgebrochen bzw. beendet
                    case PaketDataType.VillageVoteCanceled:
                    {
                        Program.DebugLog("Received VillageVoteCanceled-paket");
                        UiHandler.CancelPrompt();
                        GameManager.VoteIsStarted = false;
                        break;
                    }
                    // UI-Nachricht wird angezeigt
                    case PaketDataType.UiMessage:
                    {
                        Program.DebugLog("Received UiMessage-paket");
                        var data = JsonConvert.DeserializeObject<Pakets.UiMessage>(normalPaket.Data);
                        UiHandler.LocalUiMessage(data.Type, data.Message, data.Id);
                        break;
                    }
                    // UI-Nachricht wurde fertig angezeigt
                    case PaketDataType.UiMessageFinished:
                    {
                        Program.DebugLog("Received UiMessageFinished-paket");
                        if (UiHandler.UiMessagesWaitList.TryGetValue(normalPaket.Data, out var value))
                            value.Remove(normalPaket.Sender);
                        break;
                    }
                    default:
                    {
                        Program.DebugLog("Data: " + normalPaket.Data);
                        break;
                    }
                }
            }
            // Fehler werden im Log gespeichert
            catch (Exception e)
            {
                Program.DebugLog($"Unhandled Exception: {e}");
            }
        }
    }
}