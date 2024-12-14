using WebWolf_Client.Roles;
using WebWolf_Client.Ui;

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
    
    public class SetRolePattern
    {
        public string Id { get; }
        public RoleType Role { get; }

        public SetRolePattern(string id, RoleType role)
        {
            Id = id;
            Role = role;
        }
    }
    
    public class SimpleBoolean
    {
        public bool Value { get; }

        public SimpleBoolean(bool value)
        {
            Value = value;
        }
    }
    
    public class SimpleRole
    {
        public RoleType Role { get; }

        public SimpleRole(RoleType role)
        {
            Role = role;
        }
    }
    
    public class SimplePlayerId
    {
        public string Id { get; }

        public SimplePlayerId(string id)
        {
            Id = id;
        }
    }
    
    public class UiMessage
    {
        public UiMessageType Type { get; }
        public string Message { get; }
        public string Id { get; }

        public UiMessage(UiMessageType type, string message, string id)
        {
            Type = type;
            Message = message;
            Id = id;
        }
    }
}