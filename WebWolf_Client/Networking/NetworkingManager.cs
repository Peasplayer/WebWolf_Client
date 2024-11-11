using Newtonsoft.Json;
using WebSocketSharp;

namespace WebWolf_Client.Networking;

public class NetworkingManager
{
    public static NetworkingManager Instance;
    
    public string ID { get; private set; }

    private WebSocket Socket;

    public NetworkingManager()
    {
        ID = "";
        Instance = this;
    }

    public void StartConnection(string url)
    {
        Socket = new WebSocket(url);
        Socket.OnMessage += (sender, e) => 
        {
            if (e.IsText)
                OnMessage(e.Data);
        };
        Socket.Connect();
    }

    private void OnMessage(string message)
    {
        Console.WriteLine("Server says: " + message);
            
        var packet = JsonConvert.DeserializeObject<Packet>(message);
        if (packet == null)
            return;
        
        if (packet.Type == PacketType.Handshake)
        {
            var handshake = JsonConvert.DeserializeObject<HandshakePacket>(message);
            if (handshake == null)
                return;
            
            ID = handshake.Name;
            Console.WriteLine("ID: " + ID);
            Socket.Send(JsonConvert.SerializeObject(new HandshakePacket(ID, "Laputa")));
        }
        else
        {
            var normalPacket = JsonConvert.DeserializeObject<NormalPacket>(message);
            if (normalPacket == null)
                return;

            switch (normalPacket.DataType)
            {
                case PacketDataType.Join:
                {
                    Console.WriteLine("Received Join-packet");
                                    
                    var joinPacketData = JsonConvert.DeserializeObject<Packets.PlayerDataPattern>(normalPacket.Data);
                    if (joinPacketData == null)
                        return;
                                
                    Console.WriteLine($"Player {joinPacketData.Name} joined with ID {joinPacketData.ID}");
                    PlayerManager.Players[joinPacketData.ID] = joinPacketData.Name;
                    break;
                }
                case PacketDataType.SyncLobby:
                {
                    PlayerManager.Players.Clear();
                    var syncPacketData = JsonConvert.DeserializeObject<Packets.SyncLobbyPacket>(normalPacket.Data);
                    if (syncPacketData == null)
                        return;
                    
                    foreach (var playerDataPattern in syncPacketData.Players)
                    {
                        PlayerManager.Players[playerDataPattern.ID] = playerDataPattern.Name;
                    }
                    break;
                }
                default:
                {
                    Console.WriteLine("Data: " + normalPacket.Data);
                    break;
                }
            }
        }
    }
}