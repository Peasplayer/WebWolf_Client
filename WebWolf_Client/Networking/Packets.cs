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
        public string Id { get; }
        public string Name { get; }
        public bool IsHost { get; }

        public PlayerDataPattern(string id, string name, bool isHost)
        {
            Id = id;
            Name = name;
            IsHost = isHost;
        }
    }
}