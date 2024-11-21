using WebWolf_Client.Networking;

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
    public bool IsLocal => LocalPlayer.Id == Id;

    public PlayerData(string name, string? id, bool isHost = false)
    {
        Name = name;
        Id = id;
        IsHost = isHost;
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
}