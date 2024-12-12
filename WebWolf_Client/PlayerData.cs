using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;

namespace WebWolf_Client;

public class PlayerData
{
    public static PlayerData LocalPlayer => GetPlayer(NetworkingManager.Instance.CurrentId);
    public static List<PlayerData> Players = new List<PlayerData>();
    
    public static PlayerData? GetPlayer(string id)
    {
        return Players.Find(player => player.Id == id);
    }
    
    public string Name { get; }
    public string? Id { get; private set; }
    public bool IsHost { get; private set; }
    public RoleType Role { get; private set; }
    public bool IsLocal => LocalPlayer.Id == Id;
    public bool IsAlive = true;

    public PlayerData(string name, string? id, bool isHost = false, RoleType role = RoleType.NoRole)
    {
        Name = name;
        Id = id;
        IsHost = isHost;
        Role = role;
    }

    public void SetId(string id)
    {
        if (Id == null)
            Id = id;
    }

    public void SetHost()
    {
        foreach (var playerData in Players)
        {
            playerData.IsHost = false;
        }

        IsHost = true;
    }

    public void RpcSetRole(RoleType role)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.SetRole, 
                "{'Id': '" + Id + "', 'Role': '" + role + "'}")));
        SetRole(role);
    }

    public void SetRole(RoleType role)
    {
        Role = role;
    }
}