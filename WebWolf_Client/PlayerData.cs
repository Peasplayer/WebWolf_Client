namespace WebWolf_Client;

public class PlayerData
{
    public static PlayerData LocalPlayer;
    public static List<PlayerData> Players = new List<PlayerData>();
    
    public static PlayerData? GetPlayer(string id)
    {
        return Players.Find(player => player.Id == id);
    }
    
    public string Name { get; }
    public string? Id { get; private set; }
    public bool IsLocal => LocalPlayer.Id == Id;

    public PlayerData(string name, string? id)
    {
        Name = name;
        Id = id;
    }

    public void SetId(string id)
    {
        if (Id == null)
            Id = id;
    }
}