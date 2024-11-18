using System.Net.WebSockets;
using Newtonsoft.Json;
using Websocket.Client;

namespace WebWolf_Client.Networking;

public class NetworkingManager
{
    public static NetworkingManager Instance;

    //public WebSocket Socket { get; private set; }
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
            // To-Do
        });
        /*Socket = new WebSocket(url);
        Socket.OnMessage += (sender, e) =>
        {
            if (e.IsText)
                OnMessage(e.Data);
        };
        Socket.ConnectAsync();*/
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
            
            PlayerData.LocalPlayer.SetId(handshake.Name);
            Program.DebugLog("ID: " + PlayerData.LocalPlayer.Id);
            Client.Send(JsonConvert.SerializeObject(new HandshakePacket(PlayerData.LocalPlayer.Id, PlayerData.LocalPlayer.Name)));
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

                        if (joinPacketData.ID == PlayerData.LocalPlayer.Id)
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
                            PlayerData.Players.Add(new PlayerData(playerDataPattern.Name, playerDataPattern.ID));
                        }

                        break;
                    }
                    case PacketDataType.Leave:
                    {
                        Program.DebugLog("Received Leave-packet");

                        var packetData = JsonConvert.DeserializeObject<Packets.PlayerDataPattern>(normalPacket.Data);
                        if (packetData == null)
                            return;

                        if (packetData.ID == PlayerData.LocalPlayer.Id)
                        {
                            Program.DebugLog("Local player can't leave!!!");
                            return;
                        }

                        GameManager.OnPlayerLeave(packetData);
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
                Console.WriteLine(e);
            }
        }
    }
}