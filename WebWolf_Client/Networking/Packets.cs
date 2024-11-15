namespace WebWolf_Client.Networking;

public class Packets
{
    public class SyncLobbyPacket
    {
        public PlayerDataPattern[] Players;

        public SyncLobbyPacket(PlayerDataPattern[] players)
        {
            Players = players;
        }
    }
    
    public class PlayerDataPattern
    {
        public string ID { get; }
        public string Name { get; }

        public PlayerDataPattern(string id, string name)
        {
            ID = id;
            Name = name;
        }
    }
}