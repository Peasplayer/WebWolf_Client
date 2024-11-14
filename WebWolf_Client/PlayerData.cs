namespace WebWolf_Client;

public class PlayerData
{
    public static PlayerData LocalPlayer;
    
    public string Name { get; }
    public string? Id { get; private set; }

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