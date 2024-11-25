using System.Net.WebSockets;
using Newtonsoft.Json;
using Websocket.Client;

namespace WebWolf_Client.Networking;

public class NetworkingManager
{
    public static NetworkingManager Instance;

    public static string InitialName;
    public static bool InitialConnectionSuccessful;
    
    public string CurrentId { get; private set; }
    public WebsocketClient Client { get; private set; }
    public Task ConnectionTask { get; private set; }
    public NetworkingManager()
    {
        Instance = this;
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
        ConnectionTask = Client.Start();
        Client.MessageReceived.Subscribe(msg =>
        {
            if (msg.MessageType == WebSocketMessageType.Text && msg.Text != null)
                OnMessage(msg.Text);
        });
        Client.DisconnectionHappened.Subscribe(info =>
        {
            if (!InitialConnectionSuccessful)
                return;
            
            Program.DebugLog("Connection closed: " + info.Type);
            
            UiHandler.DisplayDisconnectionScreen(info);

            Program.KeepAlive = false;
        });
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

                        GameManager.OnPlayerJoin(joinPacketData);
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