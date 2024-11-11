namespace WebWolf_Client.Networking;

public class Packets
{
    public class SyncLobbyPacket
    {
        public PlayerDataPattern[] Players;
    }
    
    public class PlayerDataPattern
    {
        public string ID { get; }
        public string Name { get; }
    }
}