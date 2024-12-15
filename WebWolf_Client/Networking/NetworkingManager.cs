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
    public static NetworkingManager Instance;

    public static string InitialName;
    public static string DisconnectionReason;

    public static bool ConnectToServer()
    {
        var net = new NetworkingManager();
        net.StartConnection($"ws://{SettingsManager.ServerIp.Value}:8443");
        
        return net.Client.IsRunning;
    }
    
    public string CurrentId { get; private set; }
    public WebsocketClient Client { get; private set; }
    public bool InitialConnectionSuccessful;
    public bool ArePlayersSynced { get; private set; }
    private NetworkingManager()
    {
        Instance = this;
        DisconnectionReason = "";
    }

    public void StartConnection(string url)
    {
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.FromSeconds(5),
            }
        });
        Client = new WebsocketClient(new Uri(url), factory);
        Client.IsReconnectionEnabled = false;
        Client.ErrorReconnectTimeout = null;
        Client.ReconnectTimeout = TimeSpan.FromSeconds(5);
        Client.MessageReceived.Subscribe(msg =>
        {
            if (msg.MessageType == WebSocketMessageType.Text && msg.Text != null)
                OnMessage(msg.Text);
        });
        Client.DisconnectionHappened.Subscribe(info =>
        {
            if (!InitialConnectionSuccessful)
                return;
            
            Program.DebugLog("Connection closed: " + JsonConvert.SerializeObject(info));
            
            UiHandler.DisplayDisconnectionScreen(info);

            Program.KeepAlive = false;
        });
        
        Client.Start().Wait();
    }

    private void OnMessage(string message)
    {
        Program.DebugLog("Server says: " + message);
            
        var packet = JsonConvert.DeserializeObject<Packet>(message);
        if (packet == null)
            return;
        
        if (packet.Type == PacketType.Handshake)
        {
            var handshake = JsonConvert.DeserializeObject<HandshakePacket>(message);
            if (handshake == null)
                return;
            
            CurrentId = handshake.Name;
            Program.DebugLog("ID: " + CurrentId);
            Client.Send(JsonConvert.SerializeObject(new HandshakePacket(CurrentId, InitialName)));
        }
        else
        {
            var normalPacket = JsonConvert.DeserializeObject<NormalPacket>(message);
            if (normalPacket == null)
                return;

            try
            {
                switch (normalPacket.DataType)
                {
                    case PacketDataType.Join:
                    {
                        Program.DebugLog("Received Join-packet");

                        var joinPacketData =
                            JsonConvert.DeserializeObject<Packets.PlayerDataPattern>(normalPacket.Data);
                        if (joinPacketData == null)
                            return;

                        if (joinPacketData.Id == PlayerData.LocalPlayer.Id)
                        {
                            Program.DebugLog("Local player is already joined");
                            return;
                        }

                        Task.Run(() => GameManager.OnPlayerJoin(joinPacketData));
                        break;
                    }
                    case PacketDataType.SyncLobby:
                    {
                        PlayerData.Players.Clear();
                        var syncPacketData = JsonConvert.DeserializeObject<Packets.SyncLobbyPacket>(normalPacket.Data);
                        if (syncPacketData == null)
                            return;

                        foreach (var playerDataPattern in syncPacketData.Players)
                        {
                            PlayerData.Players.Add(new PlayerData(playerDataPattern.Name, playerDataPattern.Id, playerDataPattern.IsHost));
                        }
                        
                        Program.DebugLog("Players after sync: " + JsonConvert.SerializeObject(PlayerData.Players));
                        ArePlayersSynced = true;
                        if (GameManager.State == GameManager.GameState.InLobby)
                            Task.Run(UiHandler.DisplayLobby);

                        break;
                    }
                    case PacketDataType.Leave:
                    {
                        Program.DebugLog("Received Leave-packet");

                        var packetData = JsonConvert.DeserializeObject<Packets.PlayerDataPattern>(normalPacket.Data);
                        if (packetData == null)
                            return;

                        if (packetData.Id == PlayerData.LocalPlayer.Id)
                        {
                            Program.DebugLog("Local player can't leave!!!");
                            return;
                        }

                        GameManager.OnPlayerLeave(packetData);
                        break;
                    }
                    case PacketDataType.SetHost:
                    {
                        Program.DebugLog("Received SetHost-packet");

                        var packetData = JsonConvert.DeserializeObject<Packets.PlayerDataPattern>(normalPacket.Data);
                        if (packetData == null)
                            return;
                        
                        PlayerData.GetPlayer(packetData.Id).SetHost();
                        if (GameManager.State == GameManager.GameState.InLobby)
                            Task.Run(UiHandler.DisplayLobby);
                        break;
                    }
                    case PacketDataType.Disconnect:
                    {
                        Program.DebugLog("Received Disconnect-packet");

                        DisconnectionReason = normalPacket.Data;
                        Client.Stop(WebSocketCloseStatus.NormalClosure, normalPacket.Data);
                        break;
                    }
                    // Das Spiel wird vom Host gestartet
                    case PacketDataType.StartGame:
                    {
                        Program.DebugLog("Received StartGame-packet");
                        Task.Run(() => GameManager.ChangeState(GameManager.GameState.InGame));
                        break;
                    }
                    case PacketDataType.EndGame:
                    {
                        Program.DebugLog("Received EndGame-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SimpleBoolean>(normalPacket.Data);
                        Task.Run(() =>
                        {
                            GameManager.ChangeState(GameManager.GameState.NoGame);
                            UiHandler.DisplayEndScreen(data.Value);
                        });
                        break;
                    }
                    case PacketDataType.SetRole:
                    {
                        Program.DebugLog("Received SetRole-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SetRolePattern>(normalPacket.Data);
                        PlayerData.GetPlayer(data.Id).SetRole(data.Role);
                        Program.DebugLog($"Player {PlayerData.GetPlayer(data.Id).Name} is a {data.Role}");
                        break;
                    }
                    case PacketDataType.StartNightOrDay:
                    {
                        Program.DebugLog("Received StartNightOrDay-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SimpleBoolean>(normalPacket.Data);
                        Program.DebugLog($"It is now {(data.Value ? "Night" : "Day")}");
                        Task.Run(() => GameManager.ChangeInGameState(data.Value ? GameManager.InGameStateType.Night : GameManager.InGameStateType.Day));
                        break;
                    }
                    case PacketDataType.CallRole:
                    {
                        Program.DebugLog("Received CallRole-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SimpleRole>(normalPacket.Data);
                        Program.DebugLog($"Role {data.Role} is being called");
                        var role = RoleManager.GetRole(data.Role);
                        role.ResetAction();
                        if (PlayerData.LocalPlayer.Role == data.Role && (PlayerData.LocalPlayer.IsAlive || data.Role == RoleType.Jäger))
                        {
                            Task.Run(role.PrepareAction);
                        }
                        break;
                    }
                    case PacketDataType.RoleFinished:
                    {
                        Program.DebugLog("Received RoleFinished-packet");
                        if (!PlayerData.LocalPlayer.IsHost)
                            return;
                        
                        var data = JsonConvert.DeserializeObject<Packets.SimpleRole>(normalPacket.Data);
                        var currentWaitState = RoleManager.WaitingForRole[normalPacket.Sender];
                        if (currentWaitState == 0)
                        {
                            Program.DebugLog($"{normalPacket.Sender} completed role {data.Role} action (no UI)");
                            RoleManager.WaitingForRole[normalPacket.Sender] = 1;
                        }
                        else
                        {
                            Program.DebugLog($"{normalPacket.Sender} finished role {data.Role}");
                            RoleManager.WaitingForRole.Remove(normalPacket.Sender);
                        }
                        break;
                    }
                    case PacketDataType.RoleCanceled:
                    {
                        Program.DebugLog("Received RoleCanceled-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SimpleRole>(normalPacket.Data);
                        Program.DebugLog($"{data.Role}'s action was canceled");
                        if (PlayerData.LocalPlayer.Role == data.Role)
                        {
                            UiHandler.CancelPrompt();
                        }
                        RoleManager.GetRole(data.Role).PrepareCancelAction();
                        break;
                    }
                    // Stimme eines Werwolfes wurde gesendet
                    case PacketDataType.WerwolfVote:
                    {
                        var data = JsonConvert.DeserializeObject<Packets.SimplePlayerId>(normalPacket.Data);
                        var role = (Werwolf) RoleManager.GetRole(RoleType.Werwolf);
                        role.Votes[normalPacket.Sender] = data.Id;
                        if (PlayerData.LocalPlayer.Role == RoleType.Werwolf && role.Votes.Count < PlayerData.Players.Count(player => player is { Role: RoleType.Werwolf, IsAlive: true }))
                            Task.Run(role.SelectVictim);

                        if (PlayerData.LocalPlayer.IsHost)
                        {
                            role.CalculateVictim();
                        }
                        break;
                    }
                    // Den Werwölfen wird ihr Opfer angekündigt
                    case PacketDataType.WerwolfAnnounceVictim:
                    {
                        var data = JsonConvert.DeserializeObject<Packets.SimplePlayerId>(normalPacket.Data);
                        Werwolf.LastVicitmId = data.Id;
                        if (PlayerData.LocalPlayer.Role == RoleType.Werwolf && PlayerData.LocalPlayer.IsAlive)
                            ((Werwolf) RoleManager.GetRole(RoleType.Werwolf)).AnnounceVictim(data.Id);
                        break;
                    }
                    case PacketDataType.PlayerMarkedAsDead:
                    {
                        var data = JsonConvert.DeserializeObject<Packets.SimplePlayerId>(normalPacket.Data);
                        PlayerData.GetPlayer(data.Id).MarkAsDead(true);
                        break;
                    }
                    case PacketDataType.PlayerUnmarkedAsDead:
                    {
                        var data = JsonConvert.DeserializeObject<Packets.SimplePlayerId>(normalPacket.Data);
                        PlayerData.GetPlayer(data.Id).MarkAsDead(false);
                        break;
                    }
                    case PacketDataType.PlayerProcessDeaths:
                    {
                        PlayerData.ProcessDeaths();
                        break;
                    }
                    case PacketDataType.VillageVoteStart:
                    {
                        Program.DebugLog("Received VillageVoteStart-packet");
                        GameManager.Votes.Clear();
                        GameManager.HasVoted = !PlayerData.LocalPlayer.IsAlive;
                        GameManager.VoteIsStarted = true;
                        Task.Run(UiHandler.DisplayVillageVote);
                        break;
                    }
                    case PacketDataType.VillageVoteVoted:
                    {
                        Program.DebugLog("Received VillageVoteStart-packet");
                        var data = JsonConvert.DeserializeObject<Packets.SimplePlayerId>(normalPacket.Data);
                        GameManager.Votes[normalPacket.Sender] = data.Id;
                        Task.Run(UiHandler.DisplayVillageVote);
                        break;
                    }
                    case PacketDataType.VillageVoteCanceled:
                    {
                        Program.DebugLog("Received VillageVoteCanceled-packet");
                        UiHandler.CancelPrompt();
                        GameManager.VoteIsStarted = false;
                        break;
                    }
                    case PacketDataType.VillageVoteAnnounceVictim:
                    {
                        Program.DebugLog("Received VillageVoteAnnounceVictim-packet");
                        break;
                    }
                    case PacketDataType.UiMessage:
                    {
                        Program.DebugLog("Received UiMessage-packet");
                        var data = JsonConvert.DeserializeObject<Packets.UiMessage>(normalPacket.Data);
                        UiHandler.LocalUiMessage(data.Type, data.Message, data.Id);
                        break;
                    }
                    case PacketDataType.UiMessageFinished:
                    {
                        Program.DebugLog("Received UiMessageFinished-packet");
                        if (PlayerData.LocalPlayer.IsHost)
                            UiHandler.UiMessagesWaitList[normalPacket.Data].Remove(normalPacket.Sender);
                        break;
                    }
                    default:
                    {
                        Program.DebugLog("Data: " + normalPacket.Data);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Program.DebugLog($"Unhandled Exception: {e}");
            }
        }
    }
}